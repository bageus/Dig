namespace Dig.Domain.Agents;

public sealed partial class AgentState
{
    public void RecordBlockedIntent(AgentIntentKind intentKind, string reason, long tick)
    {
        ValidateTick(tick);
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Blocked intent reason is required.", nameof(reason));
        }

        LastActionBlockReason = reason.Trim();
        Version = checked(Version + 1);
        Raise(new AgentActionBlocked(tick, Id, intentKind, LastActionBlockReason));
    }
}
