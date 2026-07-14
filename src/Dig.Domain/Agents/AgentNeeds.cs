using System;
namespace Dig.Domain.Agents
{

public readonly struct NeedValue : IEquatable<NeedValue>, IComparable<NeedValue>
{
    public const int Minimum = 0;
    public const int Maximum = 10_000;

    public NeedValue(int points)
    {
        if (points < Minimum || points > Maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(points));
        }

        Points = points;
    }

    public int Points { get; }

    public int Deficit => Maximum - Points;

    public bool IsAtOrBelow(int threshold)
    {
        return Points <= threshold;
    }

    public NeedValue AddClamped(int delta)
    {
        long updated = (long)Points + delta;
        return new NeedValue((int)Math.Clamp(updated, Minimum, Maximum));
    }

    public int CompareTo(NeedValue other)
    {
        return Points.CompareTo(other.Points);
    }

    public bool Equals(NeedValue other)
    {
        return Points == other.Points;
    }

    public override bool Equals(object? obj)
    {
        return obj is NeedValue other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Points;
    }

    public override string ToString()
    {
        return Points.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public static bool operator ==(NeedValue left, NeedValue right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(NeedValue left, NeedValue right)
    {
        return !left.Equals(right);
    }
}

public readonly struct NeedDelta
{
    public NeedDelta(int nutrition, int alertness, int mood, int health)
    {
        Nutrition = nutrition;
        Alertness = alertness;
        Mood = mood;
        Health = health;
    }

    public int Nutrition { get; }

    public int Alertness { get; }

    public int Mood { get; }

    public int Health { get; }
}

public readonly struct AgentNeedsSnapshot
{
    public AgentNeedsSnapshot(
        NeedValue nutrition,
        NeedValue alertness,
        NeedValue mood,
        NeedValue health)
    {
        Nutrition = nutrition;
        Alertness = alertness;
        Mood = mood;
        Health = health;
    }

    public NeedValue Nutrition { get; }

    public NeedValue Alertness { get; }

    public NeedValue Mood { get; }

    public NeedValue Health { get; }
}

internal sealed class AgentNeedsState
{
    public AgentNeedsState(
        NeedValue nutrition,
        NeedValue alertness,
        NeedValue mood,
        NeedValue health)
    {
        Nutrition = nutrition;
        Alertness = alertness;
        Mood = mood;
        Health = health;
    }

    public NeedValue Nutrition { get; private set; }

    public NeedValue Alertness { get; private set; }

    public NeedValue Mood { get; private set; }

    public NeedValue Health { get; private set; }

    public void AdvancePassive(AgentNeedPolicy policy)
    {
        if (policy is null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        Apply(policy.PassiveDelta);
        bool survivalCritical = Nutrition.IsAtOrBelow(policy.CriticalThreshold)
            || Alertness.IsAtOrBelow(policy.CriticalThreshold);
        int healthDelta = survivalCritical
            ? -policy.HealthDamagePerCriticalTick
            : policy.HealthRecoveryPerStableTick;
        int moodDelta = survivalCritical ? -policy.MoodCriticalPenalty : 0;
        Apply(new NeedDelta(0, 0, moodDelta, healthDelta));
    }

    public void Apply(NeedDelta delta)
    {
        Nutrition = Nutrition.AddClamped(delta.Nutrition);
        Alertness = Alertness.AddClamped(delta.Alertness);
        Mood = Mood.AddClamped(delta.Mood);
        Health = Health.AddClamped(delta.Health);
    }

    public AgentNeedsSnapshot CreateSnapshot()
    {
        return new AgentNeedsSnapshot(Nutrition, Alertness, Mood, Health);
    }
}
}
