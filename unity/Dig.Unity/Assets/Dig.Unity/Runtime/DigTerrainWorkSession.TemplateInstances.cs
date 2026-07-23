using System;
using System.Collections.Generic;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Agents;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    internal Result PlaceTemplateInstance(
        ExcavationTemplateInstance instance,
        long tick,
        IReadOnlyList<AgentViewModel> agents,
        int priority = DefaultExcavationPriority)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        if (agents == null)
        {
            throw new ArgumentNullException(nameof(agents));
        }

        Result<WorldMutationResult> committed =
            _worldSession.CommitTemplateDesignations(instance);
        if (committed.IsFailure)
        {
            return Result.Failure(committed.Error!);
        }

        SynchronizeDesignations(tick, agents, priority);
        return Result.Success();
    }
}

}
