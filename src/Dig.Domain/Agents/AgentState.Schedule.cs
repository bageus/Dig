using System;
using Dig.Domain.Core;

namespace Dig.Domain.Agents
{

public sealed partial class AgentState
{
    public Result SetAutomaticPlanningEnabled(bool enabled, long tick)
    {
        ValidateTick(tick);
        if (!IsAlive)
        {
            return Result.Failure(AgentErrors.AgentDead);
        }

        if (AutomaticPlanningEnabled == enabled)
        {
            return Result.Success();
        }

        AutomaticPlanningEnabled = enabled;
        Version = checked(Version + 1);
        Raise(new AgentAutomaticPlanningChanged(tick, Id, enabled));
        return Result.Success();
    }

    public void RestoreAutomaticPlanningEnabled(bool enabled)
    {
        AutomaticPlanningEnabled = enabled;
    }

    public Result SetWorkRestWindow(
        int workStartTickInclusive,
        int workEndTickExclusive,
        long tick)
    {
        ValidateTick(tick);
        if (!IsAlive)
        {
            return Result.Failure(AgentErrors.AgentDead);
        }

        try
        {
            Schedule.SetWorkRestWindow(
                workStartTickInclusive,
                workEndTickExclusive);
        }
        catch (ArgumentException)
        {
            return Result.Failure(AgentErrors.InvalidSchedule);
        }

        Version = checked(Version + 1);
        Raise(new AgentScheduleChanged(
            tick,
            Id,
            workStartTickInclusive,
            workEndTickExclusive));
        return Result.Success();
    }
}

}
