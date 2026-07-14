using Dig.Domain.Core;

namespace Dig.Domain.Jobs;

public sealed class JobSnapshot
{
    public JobSnapshot(
        DigJobDefinition definition,
        JobStatus status,
        JobStageKind stage,
        EntityId? assignedAgentId,
        int retryCount,
        long nextRetryTick,
        long version,
        JobBlockReason? reason)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Status = status;
        Stage = stage;
        AssignedAgentId = assignedAgentId;
        RetryCount = retryCount;
        NextRetryTick = nextRetryTick;
        Version = version;
        Reason = reason;
    }

    public DigJobDefinition Definition { get; }

    public EntityId Id => Definition.Id;

    public JobStatus Status { get; }

    public JobStageKind Stage { get; }

    public EntityId? AssignedAgentId { get; }

    public int RetryCount { get; }

    public long NextRetryTick { get; }

    public long Version { get; }

    public JobBlockReason? Reason { get; }

    public bool IsTerminal => Status == JobStatus.Completed
        || Status == JobStatus.Cancelled
        || Status == JobStatus.Failed;
}

public sealed class JobState
{
    internal JobState(DigJobDefinition definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Status = JobStatus.Created;
        Stage = JobStageKind.None;
    }

    public DigJobDefinition Definition { get; }

    public EntityId Id => Definition.Id;

    public JobStatus Status { get; private set; }

    public JobStageKind Stage { get; private set; }

    public EntityId? AssignedAgentId { get; private set; }

    public int RetryCount { get; private set; }

    public long NextRetryTick { get; private set; }

    public long Version { get; private set; }

    public JobBlockReason? Reason { get; private set; }

    internal void MakeAvailable()
    {
        Status = JobStatus.Available;
        Stage = JobStageKind.None;
        AssignedAgentId = null;
        Reason = null;
        IncrementVersion();
    }

    internal void Claim(EntityId agentId)
    {
        Status = JobStatus.Claimed;
        AssignedAgentId = agentId;
        Stage = JobStageKind.None;
        Reason = null;
        IncrementVersion();
    }

    internal void Start()
    {
        Status = JobStatus.InProgress;
        Stage = JobStageKind.TravelToTarget;
        Reason = null;
        IncrementVersion();
    }

    internal bool AdvanceStage()
    {
        switch (Stage)
        {
            case JobStageKind.TravelToTarget:
                Stage = JobStageKind.PerformWork;
                IncrementVersion();
                return false;
            case JobStageKind.PerformWork:
                Stage = JobStageKind.Finalize;
                IncrementVersion();
                return false;
            case JobStageKind.Finalize:
                Complete();
                return true;
            default:
                throw new InvalidOperationException("Only an in-progress job can advance a stage.");
        }
    }

    internal void Block(JobBlockReason reason, long nextRetryTick)
    {
        Status = JobStatus.Blocked;
        Stage = JobStageKind.None;
        AssignedAgentId = null;
        RetryCount = checked(RetryCount + 1);
        NextRetryTick = nextRetryTick;
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
        IncrementVersion();
    }

    internal void Complete()
    {
        Status = JobStatus.Completed;
        Stage = JobStageKind.None;
        AssignedAgentId = null;
        Reason = null;
        IncrementVersion();
    }

    internal void Cancel(JobBlockReason reason)
    {
        Status = JobStatus.Cancelled;
        Stage = JobStageKind.None;
        AssignedAgentId = null;
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
        IncrementVersion();
    }

    internal void Fail(JobBlockReason reason)
    {
        Status = JobStatus.Failed;
        Stage = JobStageKind.None;
        AssignedAgentId = null;
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
        IncrementVersion();
    }

    public JobSnapshot CreateSnapshot()
    {
        return new JobSnapshot(
            Definition,
            Status,
            Stage,
            AssignedAgentId,
            RetryCount,
            NextRetryTick,
            Version,
            Reason);
    }

    private void IncrementVersion()
    {
        Version = checked(Version + 1);
    }
}
