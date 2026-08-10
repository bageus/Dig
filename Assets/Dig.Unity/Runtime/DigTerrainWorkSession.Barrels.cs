using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.WorldObjects;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Runtime;
using Dig.Domain.World;
using Dig.Domain.WorldObjects;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private const int BarrelDirectPriority = 900;
    private static readonly BarrelDefinitionId DemoBarrelDefinitionId =
        new BarrelDefinitionId("world.barrel.wooden");
    private static readonly ItemId BarrelStoneItemId = new ItemId("material.stone");
    private static readonly ItemId BarrelOreItemId = new ItemId("ore.iron");

    private InMemoryBarrelRepository? _barrelRepository;
    private StartDirectBarrelAttackCommandHandler? _startBarrelAttack;
    private ArriveAtBarrelCommandHandler? _arriveAtBarrel;
    private CompleteBarrelHitCommandHandler? _completeBarrelHit;
    private CompleteBarrelDestructionCommandHandler? _completeBarrelDestruction;
    private CancelBarrelAttackCommandHandler? _cancelBarrelAttack;
    private SettleBarrelAfterSupportLossCommandHandler? _settleBarrel;
    private long _nextBarrelJobSequence;
    private long _nextBarrelOutputSequence;

    internal void InitializeBarrelDemo(long tick)
    {
        if (_barrelRepository != null)
        {
            return;
        }

        BarrelDefinition definition = new BarrelDefinition(
            DemoBarrelDefinitionId,
            new[] { BarrelStoneItemId, BarrelOreItemId });
        BarrelState state = new BarrelState(new BarrelCatalog(new[] { definition }));
        CellId[] surface = FindBarrelDemoCells(surface: true, count: 2, excluded: null);
        CellId[] cave = FindBarrelDemoCells(
            surface: false,
            count: 2,
            excluded: new HashSet<CellId>(surface));
        CellId[] cells = surface.Concat(cave).ToArray();
        for (int index = 0; index < cells.Length; index++)
        {
            EntityId barrelId = DemoId('b', index + 1);
            Require(state.Add(
                barrelId,
                definition.Id,
                cells[index],
                SelectBarrelContents(barrelId),
                tick));
        }

        _barrelRepository = new InMemoryBarrelRepository(state);
        _startBarrelAttack = new StartDirectBarrelAttackCommandHandler(
            _barrelRepository,
            _jobRepository,
            _journal);
        _arriveAtBarrel = new ArriveAtBarrelCommandHandler(_jobRepository, _journal);
        _completeBarrelHit = new CompleteBarrelHitCommandHandler(
            _jobRepository,
            _journal);
        _completeBarrelDestruction = new CompleteBarrelDestructionCommandHandler(
            _barrelRepository,
            _jobRepository,
            _inventoryRepository,
            _journal);
        _cancelBarrelAttack = new CancelBarrelAttackCommandHandler(
            _jobRepository,
            _journal);
        _settleBarrel = new SettleBarrelAfterSupportLossCommandHandler(
            _barrelRepository,
            _journal);
    }

    internal IReadOnlyList<BarrelSnapshot> LoadBarrels() =>
        _barrelRepository?.Get().GetAll() ?? Array.Empty<BarrelSnapshot>();

    internal IReadOnlyCollection<CellId> BarrelBuildingBlockedCells =>
        _barrelRepository?.Get().GetBuildingBlockedCells() ?? Array.Empty<CellId>();

    internal IReadOnlyCollection<CellId> BuildingPlacementBlockedCells =>
        MushroomBuildingBlockedCells
            .Concat(BarrelBuildingBlockedCells)
            .Distinct()
            .OrderBy(value => value)
            .ToArray();

    internal bool CanDirectAttackBarrel(
        EntityId barrelId,
        CellId workerCell,
        out CellId workPosition)
    {
        workPosition = default;
        BarrelSnapshot? barrel = _barrelRepository?.Get().Get(barrelId);
        return barrel != null
            && barrel.IsAttackable
            && TryResolveBarrelWorkPosition(barrel.Cell, workerCell, out workPosition);
    }

    internal Result StartDirectBarrelAttack(
        EntityId barrelId,
        EntityId workerId,
        CellId workerCell,
        long tick)
    {
        EnsureBarrelsInitialized();
        if (!CanDirectAttackBarrel(barrelId, workerCell, out CellId workPosition))
        {
            return Result.Failure(new DomainError(
                "barrel.direct_target_unavailable",
                "The barrel is unavailable or has no reachable attack position."));
        }

        Result prepared = PrepareResidentsForDirectCommand(
            new[] { workerId.ToString() },
            tick);
        if (prepared.IsFailure)
        {
            return prepared;
        }

        EntityId jobId = DemoId('c', checked(++_nextBarrelJobSequence));
        Result<BarrelAttackStartedResult> started = _startBarrelAttack!.Handle(
            new StartDirectBarrelAttackCommand(
                jobId,
                barrelId,
                workerId,
                workPosition,
                BarrelDirectPriority,
                tick));
        return started.IsSuccess ? Result.Success() : Result.Failure(started.Error!);
    }

    internal Result AdvanceBarrels(long tick, IReadOnlyList<AgentViewModel> agents)
    {
        if (_barrelRepository == null)
        {
            return Result.Success();
        }

        Dictionary<string, AgentViewModel> agentsById = agents.ToDictionary(
            value => value.Id,
            StringComparer.Ordinal);
        foreach (JobSnapshot job in _jobRepository.Get().GetAll())
        {
            if (job.Definition is not BarrelAttackJobDefinition definition
                || !IsActive(job)
                || !job.AssignedAgentId.HasValue
                || !agentsById.TryGetValue(
                    job.AssignedAgentId.Value.ToString(),
                    out AgentViewModel? worker))
            {
                continue;
            }

            if (worker.CellX != definition.WorkPosition.X
                || worker.CellY != definition.WorkPosition.Y
                || worker.CellZ != definition.WorkPosition.Z)
            {
                continue;
            }

            Result result = AdvanceBarrelAtWorkPosition(job, tick);
            if (result.IsFailure)
            {
                return result;
            }
        }

        return Result.Success();
    }

    internal Result SettleUnsupportedBarrels(long tick)
    {
        if (_barrelRepository == null)
        {
            return Result.Success();
        }

        foreach (BarrelSnapshot barrel in LoadBarrels())
        {
            if (barrel.Lifecycle != BarrelLifecycle.Supported
                || HasSolidSupport(barrel.Cell)
                || !TryResolveBarrelLanding(barrel.Cell, out CellId landing))
            {
                continue;
            }

            Result settled = _settleBarrel!.Handle(
                new SettleBarrelAfterSupportLossCommand(barrel.BarrelId, landing, tick));
            if (settled.IsFailure)
            {
                return settled;
            }
        }

        return Result.Success();
    }

    private Result AdvanceBarrelAtWorkPosition(JobSnapshot job, long tick)
    {
        if (job.Stage == JobStageKind.TravelToTarget)
        {
            return _arriveAtBarrel!.Handle(new ArriveAtBarrelCommand(job.Id, tick));
        }

        if (job.Stage == JobStageKind.PerformWork)
        {
            if (tick % 2 != 0)
            {
                return Result.Success();
            }

            Result hit = _completeBarrelHit!.Handle(
                new CompleteBarrelHitCommand(job.Id, tick));
            if (hit.IsFailure)
            {
                return hit;
            }
        }

        JobSnapshot? updated = _jobRepository.Get().Get(job.Id);
        if (updated?.Stage != JobStageKind.Finalize)
        {
            return Result.Success();
        }

        EntityId outputId = DemoId('d', checked(++_nextBarrelOutputSequence));
        Result<BarrelDestructionResult> completion = _completeBarrelDestruction!.Handle(
            new CompleteBarrelDestructionCommand(job.Id, outputId, tick));
        _routePlans.Remove(job.Id);
        if (completion.IsSuccess)
        {
            return Result.Success();
        }

        if (completion.Error == BarrelApplicationErrors.GenerationConflict
            || completion.Error == BarrelErrors.NotAttackable
            || completion.Error == BarrelErrors.VersionConflict)
        {
            Result cancelled = _cancelBarrelAttack!.Handle(new CancelBarrelAttackCommand(
                job.Id,
                "barrel_destroyed_by_concurrent_attack",
                tick));
            return cancelled.IsSuccess ? Result.Success() : cancelled;
        }

        return Result.Failure(completion.Error!);
    }

    private ItemId SelectBarrelContents(EntityId barrelId)
    {
        RandomStreamCatalog streams = new RandomStreamCatalog(
            unchecked((ulong)(uint)_worldSession.MiningOutputWorldSeed));
        DeterministicRandomStream stream = streams.GetOrCreate(
            $"barrel.contents.{barrelId}");
        return stream.NextInt(2) == 0 ? BarrelStoneItemId : BarrelOreItemId;
    }

    private void EnsureBarrelsInitialized()
    {
        if (_barrelRepository == null
            || _startBarrelAttack == null
            || _completeBarrelDestruction == null)
        {
            throw new InvalidOperationException("Barrel demo is not initialized.");
        }
    }
}

}
