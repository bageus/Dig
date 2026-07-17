using Dig.Domain.Core;

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
}
