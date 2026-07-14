using Dig.Domain.Core;

namespace Dig.Domain.Jobs;

public sealed class JobSnapshot
{
    public JobSnapshot(
        JobDefinition definition,
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

    public JobDefinition Definition { get; }

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
    private int _stageIndex = -1;

    internal JobState(JobDefinition definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Status = JobStatus.Created;
        Stage = JobStageKind.None;
    }

    public JobDefinition Definition { get; }

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
        ResetExecution();
        Reason = null;
        IncrementVersion();
    }

    internal void Claim(EntityId agentId)
    {
        Status = JobStatus.Claimed;
        AssignedAgentId = agentId;
        Stage = JobStageKind.None;
        _stageIndex = -1;
        Reason = null;
        IncrementVersion();
    }

    internal void Start()
    {
        Status = JobStatus.InProgress;
        _stageIndex = 0;
        Stage = Definition.Stages[_stageIndex];
        Reason = null;
        IncrementVersion();
    }

    internal bool AdvanceStage()
    {
        if (_stageIndex < 0 || Status != JobStatus.InProgress)
        {
            throw new InvalidOperationException("Only an in-progress job can advance a stage.");
        }

        int nextIndex = _stageIndex + 1;
        if (nextIndex >= Definition.Stages.Count)
        {
            Complete();
            return true;
        }

        _stageIndex = nextIndex;
        Stage = Definition.Stages[_stageIndex];
        IncrementVersion();
        return false;
    }

    internal void Block(JobBlockReason reason, long nextRetryTick)
    {
        Status = JobStatus.Blocked;
        ResetExecution();
        RetryCount = checked(RetryCount + 1);
        NextRetryTick = nextRetryTick;
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
        IncrementVersion();
    }

    internal void Complete()
    {
        Status = JobStatus.Completed;
        ResetExecution();
        Reason = null;
        IncrementVersion();
    }

    internal void Cancel(JobBlockReason reason)
    {
        Status = JobStatus.Cancelled;
        ResetExecution();
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
        IncrementVersion();
    }

    internal void Fail(JobBlockReason reason)
    {
        Status = JobStatus.Failed;
        ResetExecution();
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

    private void ResetExecution()
    {
        Stage = JobStageKind.None;
        AssignedAgentId = null;
        _stageIndex = -1;
    }

    private void IncrementVersion()
    {
        Version = checked(Version + 1);
    }
}
