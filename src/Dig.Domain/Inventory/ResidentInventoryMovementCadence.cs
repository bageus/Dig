using System;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public static class ResidentInventoryMovementCadence
{
    private const int Scale = 10_000;

    public static bool IsDue(long tick, double speedMultiplier)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (speedMultiplier <= 0d || speedMultiplier > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(speedMultiplier));
        }

        if (tick == 0)
        {
            return false;
        }

        int units = checked((int)Math.Round(
            speedMultiplier * Scale,
            MidpointRounding.AwayFromZero));
        long completed = checked(tick * units) / Scale;
        long previous = checked((tick - 1) * units) / Scale;
        return completed > previous;
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