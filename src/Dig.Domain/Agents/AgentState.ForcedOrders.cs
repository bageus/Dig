using System;
using Dig.Domain.Core;

namespace Dig.Domain.Agents
{

public sealed partial class AgentState
{
    public Result PrepareForForcedOrder(string reason, long tick)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "Forced order replacement reason is required.",
                nameof(reason));
        }

        Result interrupted = InterruptActiveAction(reason.Trim(), tick);
        if (interrupted.IsFailure)
        {
            return interrupted;
        }

        return ClearPlayerOrder(tick);
    }
}

}
