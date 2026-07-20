using System;
using Dig.Domain.Core;

namespace Dig.Domain.Agents
{

public sealed partial class AgentState
{
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