using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Jobs;

namespace Dig.Application.Jobs
{

public interface IJobRepository
{
    JobSystem Get();

    void Save(JobSystem jobs);
}

public interface IJobCandidateProvider
{
    IReadOnlyCollection<JobCandidate> GetCandidates(JobSnapshot job, long tick);
}

public enum JobToolPreparationMode
{
    Suggest = 0,
    Automatic = 1,
}

public enum JobToolPreparationOutcome
{
    None = 0,
    AlreadyEquipped = 1,
    Suggested = 2,
    Switched = 3,
    Bypassed = 4,
}

public sealed class CreateDigJobCommand : ICommand<Result>
{
    public CreateDigJobCommand(DigJobDefinition definition, bool makeAvailable)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        MakeAvailable = makeAvailable;
    }

    public DigJobDefinition Definition { get; }

    public bool MakeAvailable { get; }
}

public sealed class AssignAvailableJobsCommand : ICommand<JobAssignmentReport>
{
    public AssignAvailableJobsCommand(
        long tick,
        JobToolPreparationMode toolPreparationMode = JobToolPreparationMode.Automatic)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (!Enum.IsDefined(typeof(JobToolPreparationMode), toolPreparationMode))
        {
            throw new ArgumentOutOfRangeException(nameof(toolPreparationMode));
        }

        Tick = tick;
        ToolPreparationMode = toolPreparationMode;
    }

    public long Tick { get; }

    public JobToolPreparationMode ToolPreparationMode { get; }
}

public sealed class AdvanceJobCommand : ICommand<Result>
{
    public AdvanceJobCommand(EntityId jobId, long tick)
    {
        JobId = jobId;
        Tick = tick;
    }

    public EntityId JobId { get; }

    public long Tick { get; }
}

public sealed class JobAssignment
{
    public JobAssignment(
        EntityId jobId,
        EntityId agentId,
        long score,
        JobToolPreparationOutcome toolPreparation = JobToolPreparationOutcome.None,
        EntityId? toolStackId = null)
    {
        if (!Enum.IsDefined(typeof(JobToolPreparationOutcome), toolPreparation))
        {
            throw new ArgumentOutOfRangeException(nameof(toolPreparation));
        }

        bool needsTool = toolPreparation != JobToolPreparationOutcome.None;
        if (needsTool != toolStackId.HasValue
            || (toolStackId.HasValue && toolStackId.Value.IsEmpty))
        {
            throw new ArgumentException(
                "Tool preparation outcomes require a valid tool stack id.",
                nameof(toolStackId));
        }

        JobId = jobId;
        AgentId = agentId;
        Score = score;
        ToolPreparation = toolPreparation;
        ToolStackId = toolStackId;
    }

    public EntityId JobId { get; }

    public EntityId AgentId { get; }

    public long Score { get; }

    public JobToolPreparationOutcome ToolPreparation { get; }

    public EntityId? ToolStackId { get; }
}

public sealed class JobAssignmentFailure
{
    public JobAssignmentFailure(EntityId jobId, DomainError error)
    {
        JobId = jobId;
        Error = error ?? throw new ArgumentNullException(nameof(error));
    }

    public EntityId JobId { get; }

    public DomainError Error { get; }
}

public sealed class JobAssignmentReport
{
    public JobAssignmentReport(
        long tick,
        IReadOnlyCollection<JobAssignment> assignments,
        IReadOnlyCollection<JobAssignmentFailure> failures)
    {
        Tick = tick;
        Assignments = new ReadOnlyCollection<JobAssignment>(
            assignments.OrderBy(item => item.JobId.ToString(), StringComparer.Ordinal).ToArray());
        Failures = new ReadOnlyCollection<JobAssignmentFailure>(
            failures.OrderBy(item => item.JobId.ToString(), StringComparer.Ordinal).ToArray());
    }

    public long Tick { get; }

    public IReadOnlyList<JobAssignment> Assignments { get; }

    public IReadOnlyList<JobAssignmentFailure> Failures { get; }
}

public sealed class GetJobQuery : IQuery<JobSnapshot?>
{
    public GetJobQuery(EntityId jobId)
    {
        JobId = jobId;
    }

    public EntityId JobId { get; }
}

public sealed class GetJobsQuery : IQuery<IReadOnlyList<JobSnapshot>>
{
}
}
