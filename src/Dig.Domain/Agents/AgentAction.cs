namespace Dig.Domain.Agents;

public enum AgentIntentKind
{
    Flee = 0,
    Eat = 1,
    Sleep = 2,
    PlayerOrder = 3,
    Work = 4,
    Rest = 5,
    Idle = 6,
}

public sealed class PlayerOrder
{
    public PlayerOrder(
        string id,
        string label,
        int priority,
        long issuedTick,
        long expiresTick)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Player order id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Player order label is required.", nameof(label));
        }

        if (priority < 0 || priority > 10_000)
        {
            throw new ArgumentOutOfRangeException(nameof(priority));
        }

        if (issuedTick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(issuedTick));
        }

        if (expiresTick < issuedTick)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresTick));
        }

        Id = id.Trim();
        Label = label.Trim();
        Priority = priority;
        IssuedTick = issuedTick;
        ExpiresTick = expiresTick;
    }

    public string Id { get; }

    public string Label { get; }

    public int Priority { get; }

    public long IssuedTick { get; }

    public long ExpiresTick { get; }

    public bool IsActiveAt(long tick)
    {
        return tick >= IssuedTick && tick <= ExpiresTick;
    }
}

public readonly struct AgentActionSnapshot
{
    public AgentActionSnapshot(
        AgentIntentKind intentKind,
        string? playerOrderId,
        long startedTick,
        int requiredTicks,
        int elapsedTicks)
    {
        IntentKind = intentKind;
        PlayerOrderId = playerOrderId;
        StartedTick = startedTick;
        RequiredTicks = requiredTicks;
        ElapsedTicks = elapsedTicks;
    }

    public AgentIntentKind IntentKind { get; }

    public string? PlayerOrderId { get; }

    public long StartedTick { get; }

    public int RequiredTicks { get; }

    public int ElapsedTicks { get; }
}

internal sealed class ActiveAgentAction
{
    public ActiveAgentAction(
        AgentIntentKind intentKind,
        string? playerOrderId,
        long startedTick,
        int requiredTicks)
    {
        if (!Enum.IsDefined(typeof(AgentIntentKind), intentKind))
        {
            throw new ArgumentOutOfRangeException(nameof(intentKind));
        }

        if (startedTick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startedTick));
        }

        if (requiredTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredTicks));
        }

        IntentKind = intentKind;
        PlayerOrderId = playerOrderId;
        StartedTick = startedTick;
        RequiredTicks = requiredTicks;
    }

    public AgentIntentKind IntentKind { get; }

    public string? PlayerOrderId { get; }

    public long StartedTick { get; }

    public int RequiredTicks { get; }

    public int ElapsedTicks { get; private set; }

    public bool Advance()
    {
        ElapsedTicks = checked(ElapsedTicks + 1);
        return ElapsedTicks >= RequiredTicks;
    }

    public AgentActionSnapshot CreateSnapshot()
    {
        return new AgentActionSnapshot(
            IntentKind,
            PlayerOrderId,
            StartedTick,
            RequiredTicks,
            ElapsedTicks);
    }
}
