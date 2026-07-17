using Dig.Domain.Core;
using Dig.Domain.Jobs;

namespace Dig.Application.Jobs
{

public interface IJobToolPreparationService
{
    Result Prepare(EntityId agentId, EntityId toolStackId, long tick);
}

public interface IJobToolPreparationModeSource
{
    JobToolPreparationMode Mode { get; }
}

public interface IJobExecutionReadinessPolicy
{
    bool CanAdvance(JobSnapshot job);
}
}
