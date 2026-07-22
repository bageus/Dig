using Dig.Domain.Core;

namespace Dig.Domain.Agents
{

public sealed partial class AgentState
{
    public const int HungerNotificationThreshold = 1_500;
    public const int CriticalMoodNotificationThreshold = 500;

    private void ApplyNeedDelta(NeedDelta delta, long tick)
    {
        AgentNeedsSnapshot previous = _needs.CreateSnapshot();
        _needs.Apply(delta);
        RaiseNeedThresholdCrossings(previous, _needs.CreateSnapshot(), tick);
    }

    private void AdvancePassiveNeeds(AgentNeedPolicy policy, long tick)
    {
        AgentNeedsSnapshot previous = _needs.CreateSnapshot();
        _needs.AdvancePassive(policy);
        RaiseNeedThresholdCrossings(previous, _needs.CreateSnapshot(), tick);
    }

    private void RaiseNeedThresholdCrossings(
        AgentNeedsSnapshot previous,
        AgentNeedsSnapshot current,
        long tick)
    {
        RaiseThresholdCrossing(
            AgentNeedThresholdKind.Hunger,
            HungerNotificationThreshold,
            previous.Nutrition,
            current.Nutrition,
            tick);
        RaiseThresholdCrossing(
            AgentNeedThresholdKind.CriticalMood,
            CriticalMoodNotificationThreshold,
            previous.Mood,
            current.Mood,
            tick);
    }

    private void RaiseThresholdCrossing(
        AgentNeedThresholdKind kind,
        int threshold,
        NeedValue previous,
        NeedValue current,
        long tick)
    {
        if (previous.Points >= threshold && current.Points < threshold)
        {
            Raise(new AgentNeedThresholdCrossed(
                tick,
                Id,
                kind,
                threshold,
                previous.Points,
                current.Points));
        }
    }
}

}
