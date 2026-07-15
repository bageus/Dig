using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Messaging;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Jobs
{

public sealed class SyncDigDesignationJobsHandler
    : ICommandHandler<SyncDigDesignationJobsCommand, DigDesignationJobSyncReport>
{
    private readonly IWorldRepository _worldRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IDigJobIdSource _jobIds;
    private readonly IEventSink _eventSink;

    public SyncDigDesignationJobsHandler(
        IWorldRepository worldRepository,
        IJobRepository jobRepository,
        IDigJobIdSource jobIds,
        IEventSink eventSink)
    {
        _worldRepository = worldRepository
            ?? throw new ArgumentNullException(nameof(worldRepository));
        _jobRepository = jobRepository
            ?? throw new ArgumentNullException(nameof(jobRepository));
        _jobIds = jobIds ?? throw new ArgumentNullException(nameof(jobIds));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public DigDesignationJobSyncReport Handle(SyncDigDesignationJobsCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        WorldSnapshot world = _worldRepository.Get().CreateSnapshot();
        JobSystem jobs = _jobRepository.Get();
        HashSet<CellId> designated = CollectDesignatedSolidCells(world);
        Dictionary<CellId, JobSnapshot> activeByCell =
            new Dictionary<CellId, JobSnapshot>();
        List<EntityId> cancelled = new List<EntityId>();

        foreach (JobSnapshot job in jobs.GetAll()
            .Where(value => value.Definition is DigJobDefinition && !value.IsTerminal)
            .OrderBy(value => value.Id.ToString(), StringComparer.Ordinal))
        {
            DigJobDefinition definition = (DigJobDefinition)job.Definition;
            CellId cellId = definition.Target.CellId;
            if (!designated.Contains(cellId))
            {
                Cancel(jobs, job.Id, "designation_removed", command.Tick, cancelled);
                continue;
            }

            if (activeByCell.ContainsKey(cellId))
            {
                Cancel(jobs, job.Id, "duplicate_designation_job", command.Tick, cancelled);
                continue;
            }

            activeByCell.Add(cellId, job);
        }

        List<CreatedDigDesignationJob> created = new List<CreatedDigDesignationJob>();
        foreach (CellId cellId in designated.OrderBy(value => value))
        {
            if (activeByCell.ContainsKey(cellId))
            {
                continue;
            }

            EntityId jobId = _jobIds.Next();
            if (jobId.IsEmpty)
            {
                throw new InvalidOperationException("Dig job id source returned an empty id.");
            }

            DigJobDefinition definition = new DigJobDefinition(
                jobId,
                new DigJobTarget(cellId),
                command.Priority,
                command.Tick,
                JobRetryPolicy.Default);
            Require(jobs.Add(definition));
            Require(jobs.MakeAvailable(jobId, command.Tick));
            created.Add(new CreatedDigDesignationJob(jobId, cellId));
        }

        _jobRepository.Save(jobs);
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return new DigDesignationJobSyncReport(created, cancelled);
    }

    private static HashSet<CellId> CollectDesignatedSolidCells(WorldSnapshot world)
    {
        HashSet<CellId> cells = new HashSet<CellId>();
        foreach (ChunkSnapshot chunk in world.Chunks)
        {
            foreach (CellSnapshot cell in chunk.Cells)
            {
                if (cell.IsSolid && cell.State.Designation == CellDesignation.Dig)
                {
                    cells.Add(cell.Id);
                }
            }
        }

        return cells;
    }

    private static void Cancel(
        JobSystem jobs,
        EntityId jobId,
        string reasonCode,
        long tick,
        ICollection<EntityId> cancelled)
    {
        Result result = jobs.Cancel(
            jobId,
            new JobBlockReason(reasonCode, "Digging designation is no longer active."),
            tick);
        Require(result);
        cancelled.Add(jobId);
    }

    private static void Require(Result result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error!.ToString());
        }
    }
}
}
