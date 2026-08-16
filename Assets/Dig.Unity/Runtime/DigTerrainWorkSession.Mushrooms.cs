using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Ecology;
using Dig.Application.Jobs;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.Runtime;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;

namespace Dig.Unity
{
internal sealed partial class DigTerrainWorkSession
{
    private const int MushroomDirectPriority = 900;
    private static readonly MushroomDefinitionId DemoMushroomDefinitionId =
        new MushroomDefinitionId("ecology.mushroom.common");
    private static readonly ItemId MushroomCapItemId =
        new ItemId("material.mushroom_cap");
    private static readonly ItemId MushroomLegItemId =
        new ItemId("material.mushroom_leg");

    private InMemoryMushroomRepository? _mushroomRepository;
    private AdvanceMushroomGrowthCommandHandler? _advanceMushroomGrowth;
    private StartDirectMushroomChopCommandHandler? _startMushroomChop;
    private ArriveAtMushroomCommandHandler? _arriveAtMushroom;
    private CompleteMushroomSwingCommandHandler? _completeMushroomSwing;
    private CompleteMushroomChopCommandHandler? _completeMushroomChop;
    private CancelMushroomChopCommandHandler? _cancelMushroomChop;
    private long _nextMushroomJobSequence;
    private long _nextMushroomOutputSequence;

    internal void InitializeMushroomDemo(long tick)
    {
        if (_mushroomRepository != null)
        {
            return;
        }

        MushroomDefinition definition = new MushroomDefinition(
            DemoMushroomDefinitionId,
            stageDurationTicks: 1,
            capItemId: MushroomCapItemId,
            legItemId: MushroomLegItemId);
        MushroomState state = new MushroomState(new MushroomCatalog(new[] { definition }));
        CellId surface = FindMushroomDemoCell(surface: true, excluded: null);
        CellId cave = FindMushroomDemoCell(surface: false, excluded: surface);
        Require(state.AddSite(DemoId('8', 1), definition.Id, surface, MushroomStage.Tiny, tick));
        Require(state.AddSite(DemoId('8', 2), definition.Id, cave, MushroomStage.Tiny, tick));
        _mushroomRepository = new InMemoryMushroomRepository(state);

        if (_buildingInventoryRepository == null)
        {
            throw new InvalidOperationException(
                "Mushroom drops require the shared production inventory.");
        }

        IAgentSkillLevelReader skillReader = _skillGrants as IAgentSkillLevelReader
            ?? throw new InvalidOperationException(
                "The mushroom demo requires a readable authoritative skill service.");
        MushroomSwingRandom random = new MushroomSwingRandom(new RandomStreamCatalog(1337));
        _advanceMushroomGrowth = new AdvanceMushroomGrowthCommandHandler(
            _mushroomRepository,
            _journal);
        _startMushroomChop = new StartDirectMushroomChopCommandHandler(
            _mushroomRepository,
            _jobRepository,
            skillReader,
            random,
            _journal);
        _arriveAtMushroom = new ArriveAtMushroomCommandHandler(
            _jobRepository,
            _journal);
        _completeMushroomSwing = new CompleteMushroomSwingCommandHandler(
            _mushroomRepository,
            _jobRepository,
            _journal);
        _completeMushroomChop = new CompleteMushroomChopCommandHandler(
            _mushroomRepository,
            _jobRepository,
            _buildingInventoryRepository,
            _skillGrants,
            _journal);
        _cancelMushroomChop = new CancelMushroomChopCommandHandler(
            _mushroomRepository,
            _jobRepository,
            _journal);
    }

    internal IReadOnlyList<MushroomSiteSnapshot> LoadMushrooms()
    {
        return _mushroomRepository?.Get().GetAll()
            ?? Array.Empty<MushroomSiteSnapshot>();
    }

    internal IReadOnlyCollection<CellId> MushroomBuildingBlockedCells =>
        _mushroomRepository?.Get().GetBuildingBlockedCells()
        ?? Array.Empty<CellId>();

    internal bool CanDirectChopMushroom(
        EntityId siteId,
        CellId workerCell,
        out CellId workPosition)
    {
        workPosition = default;
        MushroomSiteSnapshot? site = _mushroomRepository?.Get().Get(siteId);
        return site != null
            && site.IsVisible
            && TryResolveMushroomWorkPosition(site.Cell, workerCell, out workPosition);
    }

    internal Result StartDirectMushroomChop(
        EntityId siteId,
        EntityId workerId,
        CellId workerCell,
        long tick)
    {
        EnsureMushroomsInitialized();
        if (!CanDirectChopMushroom(siteId, workerCell, out CellId workPosition))
        {
            return Result.Failure(new DomainError(
                "mushroom.direct_target_unavailable",
                "The mushroom is absent or has no reachable work position."));
        }

        Result prepared = PrepareResidentsForDirectCommand(
            new[] { workerId.ToString() },
            tick);
        if (prepared.IsFailure)
        {
            return prepared;
        }

        EntityId jobId = DemoId('9', checked(++_nextMushroomJobSequence));
        Result<MushroomChopStartedResult> started = _startMushroomChop!.Handle(
            new StartDirectMushroomChopCommand(
                jobId,
                siteId,
                workerId,
                workPosition,
                MushroomDirectPriority,
                tick));
        return started.IsSuccess
            ? Result.Success()
            : Result.Failure(started.Error!);
    }

    internal Result AdvanceMushrooms(long tick, IReadOnlyList<AgentViewModel> agents)
    {
        if (_mushroomRepository == null)
        {
            return Result.Success();
        }

        Result growth = _advanceMushroomGrowth!.Handle(
            new AdvanceMushroomGrowthCommand(tick));
        if (growth.IsFailure)
        {
            return growth;
        }

        Dictionary<string, AgentViewModel> agentsById = agents.ToDictionary(
            value => value.Id,
            StringComparer.Ordinal);
        foreach (JobSnapshot job in _jobRepository.Get().GetAll())
        {
            if (job.Definition is not MushroomChopJobDefinition definition
                || !IsActive(job)
                || !job.AssignedAgentId.HasValue
                || !agentsById.TryGetValue(
                    job.AssignedAgentId.Value.ToString(),
                    out AgentViewModel? worker))
            {
                continue;
            }

            SurfacePose required = WorkSurfacePositioning.Resolve(
                definition.WorkPosition,
                definition.TargetCell);
            bool atWork = WorkSurfacePositioning.IsAt(
                ToSurfacePose(worker),
                required);
            if (!atWork)
            {
                continue;
            }

            if (!HasFullStandingSupport(definition.WorkPosition))
            {
                Result cancelled = _cancelMushroomChop!.Handle(
                    new CancelMushroomChopCommand(
                        job.Id,
                        "mushroom_work_position_unsupported",
                        tick));
                if (cancelled.IsFailure)
                {
                    return cancelled;
                }

                continue;
            }

            Result result = TryAdvanceFarmMushroomJob(job, tick, out Result farmResult)
                ? farmResult
                : AdvanceMushroomJob(job, definition, tick);
            if (result.IsFailure)
            {
                return result;
            }
        }

        return Result.Success();
    }

    private Result AdvanceMushroomJob(
        JobSnapshot job,
        MushroomChopJobDefinition definition,
        long tick)
    {
        if (job.Status == JobStatus.Claimed
            || job.Stage == JobStageKind.TravelToTarget)
        {
            return _arriveAtMushroom!.Handle(
                new ArriveAtMushroomCommand(job.Id, tick));
        }

        if (job.Stage == JobStageKind.PerformWork)
        {
            MushroomSiteSnapshot? site = _mushroomRepository!.Get().Get(definition.SiteId);
            if (site == null)
            {
                return Result.Failure(MushroomErrors.NotFound);
            }

            if (site.CompletedSwings < site.RequiredSwings)
            {
                Result<bool> swing = _completeMushroomSwing!.Handle(
                    new CompleteMushroomSwingCommand(job.Id, tick));
                if (swing.IsFailure)
                {
                    return Result.Failure(swing.Error!);
                }

                if (!swing.Value)
                {
                    return Result.Success();
                }
            }
            else
            {
                Result advanced = _advanceHandler.Handle(
                    new AdvanceJobCommand(job.Id, tick));
                if (advanced.IsFailure)
                {
                    return advanced;
                }
            }

            job = _jobRepository.Get().Get(job.Id)
                ?? throw new InvalidOperationException(
                    "The active mushroom job disappeared before completion.");
        }

        if (job.Stage != JobStageKind.Finalize)
        {
            return Result.Success();
        }

        long sequence = checked(_nextMushroomOutputSequence + 1);
        _nextMushroomOutputSequence = sequence;
        Result<MushroomChopCompletionResult> completed = _completeMushroomChop!.Handle(
            new CompleteMushroomChopCommand(
                job.Id,
                DemoId('7', sequence),
                tick));
        return completed.IsSuccess
            ? Result.Success()
            : Result.Failure(completed.Error!);
    }

    private void EnsureMushroomsInitialized()
    {
        if (_mushroomRepository == null)
        {
            throw new InvalidOperationException("Mushroom demo is not initialized.");
        }
    }
}
}
