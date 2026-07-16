using System;

namespace Dig.Domain.Inventory
{

public static class EquipmentWorkCadence
{
    public static bool IsDue(long tick, int intervalTicks)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (intervalTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(intervalTicks));
        }

        return tick % intervalTicks == 0;
    }
}

}