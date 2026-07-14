using Dig.Domain.Core;

namespace Dig.Domain.Agents;

public sealed partial class AgentState
{
    public Result BlockCurrentAction(
        AgentIntentKind blockedIntent,
        string reason,
        long tick)
    {
        ValidateTick(tick);
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Action block reason is required.", nameof(reason));
        }

        AgentIntentKind intent = _activeAction?.IntentKind ?? blockedIntent;
        _activeAction = null;
        LastActionBlockReason = reason.Trim();
        LastActionSwitchTick = tick;
        Version = checked(Version + 1);
        Raise(new AgentActionBlocked(tick, Id, intent, LastActionBlockReason));
        return Result.Success();
    }
}
