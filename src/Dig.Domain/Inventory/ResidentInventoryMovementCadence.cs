using System;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public static class ResidentInventoryMovementCadence
{
    private const int Scale = 10_000;
    private const double MaximumSupportedSpeed = 4d;

    public static int ResolveStepCount(long tick, double speedMultiplier)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (speedMultiplier <= 0d || speedMultiplier > MaximumSupportedSpeed)
        {
            throw new ArgumentOutOfRangeException(nameof(speedMultiplier));
        }

        if (tick == 0)
        {
            return 0;
        }

        int units = checked((int)Math.Round(
            speedMultiplier * Scale,
            MidpointRounding.AwayFromZero));
        long completed = checked(tick * units) / Scale;
        long previous = checked((tick - 1L) * units) / Scale;
        return checked((int)(completed - previous));
    }

    public static bool IsDue(long tick, double speedMultiplier)
    {
        return ResolveStepCount(tick, speedMultiplier) > 0;
    }
}

public sealed partial class InventoryState
{
    public bool IsResidentMovementDue(EntityId residentId, long tick)
    {
        ValidateResidentId(residentId);
        return ResidentInventoryMovementCadence.IsDue(
            tick,
            GetResidentMoveSpeedMultiplier(residentId));
    }
}

}