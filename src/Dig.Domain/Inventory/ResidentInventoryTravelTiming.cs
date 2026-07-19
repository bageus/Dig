using System;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public readonly struct ResidentTravelTimingSnapshot
{
    public ResidentTravelTimingSnapshot(
        EntityId residentId,
        double speedMultiplier,
        int baseTicks,
        int effectiveTicks)
    {
        if (residentId.IsEmpty)
        {
            throw new ArgumentException("Resident id is required.", nameof(residentId));
        }

        if (speedMultiplier <= 0d
            || speedMultiplier > 1d
            || baseTicks <= 0
            || effectiveTicks < baseTicks)
        {
            throw new ArgumentOutOfRangeException(nameof(speedMultiplier));
        }

        ResidentId = residentId;
        SpeedMultiplier = speedMultiplier;
        BaseTicks = baseTicks;
        EffectiveTicks = effectiveTicks;
    }

    public EntityId ResidentId { get; }
    public double SpeedMultiplier { get; }
    public int BaseTicks { get; }
    public int EffectiveTicks { get; }
    public int AddedTicks => EffectiveTicks - BaseTicks;
    public double CostMultiplier => (double)EffectiveTicks / BaseTicks;
}

public sealed partial class InventoryState
{
    public ResidentTravelTimingSnapshot ResolveResidentTravelTiming(
        EntityId residentId,
        int baseTicks)
    {
        ValidateResidentId(residentId);
        if (baseTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseTicks));
        }

        double speed = GetResidentMoveSpeedMultiplier(residentId);
        int effective = checked((int)Math.Ceiling(baseTicks / speed));
        return new ResidentTravelTimingSnapshot(
            residentId,
            speed,
            baseTicks,
            effective);
    }

    public int ResolveResidentPathEtaTicks(
        EntityId residentId,
        int pathSteps,
        int baseTicksPerStep)
    {
        ValidateResidentId(residentId);
        if (pathSteps < 0 || baseTicksPerStep <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pathSteps));
        }

        if (pathSteps == 0)
        {
            return 0;
        }

        int baseTicks = checked(pathSteps * baseTicksPerStep);
        return ResolveResidentTravelTiming(residentId, baseTicks).EffectiveTicks;
    }
}

}