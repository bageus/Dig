using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.World
{
public sealed class EraseExcavationBatchCommand
    : ICommand<Result<EraseExcavationBatchReport>>
{
    public EraseExcavationBatchCommand(IEnumerable<CellId> cells, long tick)
    {
        if (cells == null)
        {
            throw new ArgumentNullException(nameof(cells));
        }

        Cells = new ReadOnlyCollection<CellId>(
            cells.Distinct().OrderBy(cell => cell).ToArray());
        Tick = tick;
    }

    public IReadOnlyList<CellId> Cells { get; }
    public long Tick { get; }
}

public sealed class EraseExcavationBatchReport
{
    public EraseExcavationBatchReport(
        int designationCount,
        IReadOnlyList<EntityId> cancelledJobIds)
    {
        DesignationCount = designationCount;
        CancelledJobIds = new ReadOnlyCollection<EntityId>(
            (cancelledJobIds ?? throw new ArgumentNullException(nameof(cancelledJobIds)))
            .ToArray());
    }

    public int DesignationCount { get; }
    public IReadOnlyList<EntityId> CancelledJobIds { get; }
}

public sealed class EraseExcavationBatchHandler
    : ICommandHandler<EraseExcavationBatchCommand, Result<EraseExcavationBatchReport>>
{
    private readonly IWorldRepository _worldRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public EraseExcavationBatchHandler(
        IWorldRepository worldRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _worldRepository = worldRepository
            ?? throw new ArgumentNullException(nameof(worldRepository));
        _jobRepository = jobRepository
            ?? throw new ArgumentNullException(nameof(jobRepository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result<EraseExcavationBatchReport> Handle(
        EraseExcavationBatchCommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        WorldState world = _worldRepository.Get();
        Result<IReadOnlyList<TerrainChange>> preflight = BuildChanges(world, command.Cells);
        if (preflight.IsFailure)
        {
            return Result<EraseExcavationBatchReport>.Failure(preflight.Error!);
        }

        JobSystem jobs = _jobRepository.Get();
        HashSet<CellId> requested = new HashSet<CellId>(command.Cells);
        EntityId[] jobIds = jobs.GetAll()
            .Where(job => !job.IsTerminal && job.Definition is DigJobDefinition dig
                && requested.Contains(dig.Target.CellId))
            .Select(job => job.Id)
            .OrderBy(id => id.ToString(), StringComparer.Ordinal)
            .ToArray();

        Result<WorldMutationResult> mutation = world.ApplyTerrainChanges(
            preflight.Value,
            command.Tick);
        if (mutation.IsFailure)
        {
            return Result<EraseExcavationBatchReport>.Failure(mutation.Error!);
        }

        JobBlockReason reason = new JobBlockReason(
            "designation_erased",
            "The excavation designation was erased by the player.");
        for (int index = 0; index < jobIds.Length; index++)
        {
            Result cancelled = jobs.Cancel(jobIds[index], reason, command.Tick);
            if (cancelled.IsFailure)
            {
                throw new InvalidOperationException(
                    "Prevalidated excavation job cancellation failed: "
                    + cancelled.Error);
            }
        }

        _worldRepository.Save(world);
        _jobRepository.Save(jobs);
        _eventSink.Append(world.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return Result<EraseExcavationBatchReport>.Success(
            new EraseExcavationBatchReport(
                mutation.Value.ChangedCellCount,
                jobIds));
    }

    private static Result<IReadOnlyList<TerrainChange>> BuildChanges(
        WorldState world,
        IReadOnlyList<CellId> cells)
    {
        List<TerrainChange> changes = new List<TerrainChange>(cells.Count);
        for (int index = 0; index < cells.Count; index++)
        {
            Result<CellSnapshot> cell = world.GetCell(cells[index]);
            if (cell.IsFailure)
            {
                return Result<IReadOnlyList<TerrainChange>>.Failure(cell.Error!);
            }

            if (cell.Value.IsSolid
                && cell.Value.State.Designation == CellDesignation.Dig)
            {
                changes.Add(new TerrainChange(
                    cells[index],
                    cell.Value.State.WithDesignation(CellDesignation.None)));
            }
        }

        return Result<IReadOnlyList<TerrainChange>>.Success(changes);
    }
}
}
