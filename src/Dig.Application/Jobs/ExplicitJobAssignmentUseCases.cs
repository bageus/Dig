using System;
using System.Linq;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Jobs;

namespace Dig.Application.Jobs
{

public sealed class AssignSpecificJobCommand : ICommand<Result>
{
    public AssignSpecificJobCommand(EntityId jobId, EntityId agentId, long tick)
    {
        if (jobId.IsEmpty)
        {
            throw new ArgumentException("Job id cannot be empty.", nameof(jobId));
        }

        if (agentId.IsEmpty)
        {
            throw new ArgumentException("Agent id cannot be empty.", nameof(agentId));
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        JobId = jobId;
        AgentId = agentId;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public EntityId AgentId { get; }
    public long Tick { get; }
}

public sealed class ReleaseJobAssignmentCommand : ICommand<Result>
{
    public ReleaseJobAssignmentCommand(EntityId jobId, long tick)
    {
        if (jobId.IsEmpty)
        {
            throw new ArgumentException("Job id cannot be empty.", nameof(jobId));
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        JobId = jobId;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public long Tick { get; }
}

public sealed class AssignSpecificJobHandler
    : ICommandHandler<AssignSpecificJobCommand, Result>
{
    private readonly IJobRepository _repository;
    private readonly IEventSink _eventSink;

    public AssignSpecificJobHandler(IJobRepository repository, IEventSink eventSink)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(AssignSpecificJobCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        JobSystem jobs = _repository.Get();
        JobSnapshot? target = jobs.Get(command.JobId);
        if (target is null)
        {
            return Result.Failure(JobErrors.NotFound);
        }

        if ((target.Status == JobStatus.Claimed || target.Status == JobStatus.InProgress)
            && target.AssignedAgentId == command.AgentId)
        {
            return Result.Success();
        }

        JobSnapshot[] currentAssignments = jobs.GetAll()
            .Where(job => job.Id != command.JobId
                && (job.Status == JobStatus.Claimed || job.Status == JobStatus.InProgress)
                && job.AssignedAgentId == command.AgentId)
            .OrderBy(job => job.Id.ToString(), StringComparer.Ordinal)
            .ToArray();
        foreach (JobSnapshot current in currentAssignments)
        {
            Result released = jobs.ReleaseAssignment(current.Id, command.Tick);
            if (released.IsFailure)
            {
                return released;
            }
        }

        target = jobs.Get(command.JobId)!;
        if (target.Status == JobStatus.Claimed || target.Status == JobStatus.InProgress)
        {
            Result released = jobs.ReleaseAssignment(target.Id, command.Tick);
            if (released.IsFailure)
            {
                return released;
            }
        }

        Result result = jobs.Claim(command.JobId, command.AgentId, command.Tick);
        _repository.Save(jobs);
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return result;
    }
}

public sealed class ReleaseJobAssignmentHandler
    : ICommandHandler<ReleaseJobAssignmentCommand, Result>
{
    private readonly IJobRepository _repository;
    private readonly IEventSink _eventSink;

    public ReleaseJobAssignmentHandler(IJobRepository repository, IEventSink eventSink)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(ReleaseJobAssignmentCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        JobSystem jobs = _repository.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job is null)
        {
            return Result.Failure(JobErrors.NotFound);
        }

        if (job.Status == JobStatus.Available)
        {
            return Result.Success();
        }

        Result result = jobs.ReleaseAssignment(command.JobId, command.Tick);
        _repository.Save(jobs);
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return result;
    }
}

}
