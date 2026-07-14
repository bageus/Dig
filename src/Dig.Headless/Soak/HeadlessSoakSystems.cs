using Dig.Application.Diagnostics;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Application.Runtime;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Runtime;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;

namespace Dig.Headless.Soak;

internal sealed class SimulationEntityHaulingJobIdSource : IHaulingJobIdSource
{
    private readonly SimulationState _state;

    public SimulationEntityHaulingJobIdSource(SimulationState state)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    public EntityId Next()
    {
        Result<EntityId> id = _state.Entities.RegisterNew();
        if (id.IsFailure)
        {
            throw new InvalidOperationException(id.Error!.ToString());
        }

        return id.Value;
    }
}

internal sealed class SoakResourceSpawnerSystem : ISimulationSystem
{
    private readonly IInventoryRepository _inventory;
    private readonly IEventSink _events;
    private readonly ItemId _oreItemId;
    private readonly long _stopAfterTick;

    public SoakResourceSpawnerSystem(
        IInventoryRepository inventory,
        IEventSink events,
        ItemId oreItemId,
        long stopAfterTick,
        int intervalTicks = 20)
    {
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _oreItemId = oreItemId;
        _stopAfterTick = stopAfterTick;
        IntervalTicks = intervalTicks;
    }

    public string Name => "soak.resource_spawn";

    public int Order => 100;

    public int IntervalTicks { get; }

    public int SpawnedQuantity { get; private set; }

    public void Execute(SimulationContext context)
    {
        if (context.Tick > _stopAfterTick)
        {
            return;
        }

        Result<EntityId> created = context.State.Entities.RegisterNew();
        if (created.IsFailure)
        {
            throw new InvalidOperationException(created.Error!.ToString());
        }

        int quantity = 5;
        CellId location = new CellId(
            x: checked((int)(context.Tick % 16)),
            y: checked(1 + (int)((context.Tick / 16) % 8)));
        InventoryState inventory = _inventory.Get();
        Result added = inventory.AddStack(
            created.Value,
            _oreItemId,
            quantity,
            ItemLocation.InWorld(location),
            context.Tick);
        if (added.IsFailure)
        {
            throw new InvalidOperationException(added.Error!.ToString());
        }

        SpawnedQuantity = checked(SpawnedQuantity + quantity);
        _inventory.Save(inventory);
        _events.Append(inventory.DequeueUncommittedEvents());
    }
}

internal sealed class SoakHaulingSystem : ISimulationSystem
{
    private readonly IJobRepository _jobs;
    private readonly InMemoryJobCandidateProvider _candidates;
    private readonly PlanHaulingHandler _planner;
    private readonly AssignAvailableJobsHandler _assign;
    private readonly AdvanceJobHandler _advance;
    private readonly CompleteHaulingJobHandler _complete;
    private readonly EntityId[] _workers;
    private readonly List<EntityId> _trackedJobIds = new List<EntityId>();

    public SoakHaulingSystem(
        IInventoryRepository inventory,
        IStorageRepository storage,
        IJobRepository jobs,
        IEventSink events,
        SimulationState state,
        IEnumerable<EntityId> workers)
    {
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _workers = workers?.OrderBy(value => value.ToString(), StringComparer.Ordinal).ToArray()
            ?? throw new ArgumentNullException(nameof(workers));
        if (_workers.Length == 0 || _workers.Any(value => value.IsEmpty))
        {
            throw new ArgumentException("At least one valid hauling worker is required.");
        }

        _candidates = new InMemoryJobCandidateProvider();
        _planner = new PlanHaulingHandler(
            inventory,
            storage,
            jobs,
            new SimulationEntityHaulingJobIdSource(state),
            events);
        _assign = new AssignAvailableJobsHandler(jobs, _candidates, events);
        _advance = new AdvanceJobHandler(jobs, events);
        _complete = new CompleteHaulingJobHandler(inventory, storage, jobs, events);
    }

    public string Name => "soak.hauling";

    public int Order => 400;

    public int IntervalTicks => 1;

    public int CompletedJobCount { get; private set; }

    public void Execute(SimulationContext context)
    {
        HaulingPlanningReport planning = _planner.Handle(new PlanHaulingCommand(
            maximumJobs: _workers.Length,
            priority: 500,
            tick: context.Tick));
        TrackCreatedJobs(planning.Created);
        SetCandidatesForTrackedJobs();
        _assign.Handle(new AssignAvailableJobsCommand(context.Tick));
        AdvanceTrackedJobs(context.Tick);
    }

    private void TrackCreatedJobs(IReadOnlyList<PlannedHaulingJob> created)
    {
        if (created.Count == 0)
        {
            return;
        }

        foreach (PlannedHaulingJob job in created)
        {
            if (!_trackedJobIds.Contains(job.JobId))
            {
                _trackedJobIds.Add(job.JobId);
            }
        }

        _trackedJobIds.Sort(CompareEntityIds);
    }

    private void SetCandidatesForTrackedJobs()
    {
        foreach (EntityId jobId in _trackedJobIds)
        {
            JobSnapshot? job = _jobs.Get().Get(jobId);
            if (job?.Status != JobStatus.Available)
            {
                continue;
            }

            JobCandidate[] candidates = _workers
                .Select((worker, index) => new JobCandidate(
                    worker,
                    skillLevel: 5_000 - index,
                    distanceCost: index + 1,
                    isAvailable: true))
                .ToArray();
            _candidates.SetCandidates(job.Id, candidates);
        }
    }

    private void AdvanceTrackedJobs(long tick)
    {
        int index = 0;
        while (index < _trackedJobIds.Count)
        {
            EntityId jobId = _trackedJobIds[index];
            JobSnapshot? job = _jobs.Get().Get(jobId);
            if (job is null || job.IsTerminal)
            {
                _trackedJobIds.RemoveAt(index);
                continue;
            }

            Result? result = Advance(job, tick);
            if (result?.IsFailure == true)
            {
                throw new InvalidOperationException(result.Error!.ToString());
            }

            JobSnapshot? updated = _jobs.Get().Get(jobId);
            if (updated is null || updated.IsTerminal)
            {
                _trackedJobIds.RemoveAt(index);
                continue;
            }

            index++;
        }
    }

    private Result? Advance(JobSnapshot job, long tick)
    {
        if (job.Status == JobStatus.Claimed)
        {
            return _advance.Handle(new AdvanceJobCommand(job.Id, tick));
        }

        if (job.Status == JobStatus.InProgress
            && job.Stage == JobStageKind.DepositItem)
        {
            Result completed = _complete.Handle(new CompleteHaulingJobCommand(
                job.Id,
                splitStackId: default,
                tick: tick));
            if (completed.IsSuccess)
            {
                CompletedJobCount = checked(CompletedJobCount + 1);
            }

            return completed;
        }

        return job.Status == JobStatus.InProgress
            ? _advance.Handle(new AdvanceJobCommand(job.Id, tick))
            : null;
    }

    private static int CompareEntityIds(EntityId left, EntityId right)
    {
        return string.Compare(
            left.ToString(),
            right.ToString(),
            StringComparison.Ordinal);
    }
}

internal sealed class SoakInvariantSystem : ISimulationSystem
{
    private readonly SettlementInvariantChecker _checker;

    public SoakInvariantSystem(SettlementInvariantChecker checker)
    {
        _checker = checker ?? throw new ArgumentNullException(nameof(checker));
    }

    public string Name => "soak.invariants";

    public int Order => 900;

    public int IntervalTicks => 1;

    public int CheckCount { get; private set; }

    public SimulationInvariantReport? LastReport { get; private set; }

    public void Execute(SimulationContext context)
    {
        LastReport = _checker.Check(context.Tick);
        CheckCount = checked(CheckCount + 1);
        LastReport.ThrowIfInvalid();
    }
}
