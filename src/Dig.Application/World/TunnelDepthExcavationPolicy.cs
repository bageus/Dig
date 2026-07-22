using System;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Application.World
{

public enum TunnelDepthExcavationFailureReason
{
    None = 0,
    SourceOutsideVolume = 1,
    SourceNotOpen = 2,
    SourceNotHorizontalTunnel = 3,
    MaximumDepthReached = 4,
    NextDepthAlreadyOpen = 5,
}

public sealed class TunnelDepthExcavationPlan
{
    internal TunnelDepthExcavationPlan(
        CellId source,
        CellId target)
    {
        Source = source;
        Target = target;
    }

    public CellId Source { get; }

    public CellId Target { get; }
}

public sealed class TunnelDepthExcavationPlanResult
{
    private TunnelDepthExcavationPlanResult(
        TunnelDepthExcavationPlan? plan,
        TunnelDepthExcavationFailureReason failureReason,
        string detail)
    {
        Plan = plan;
        FailureReason = failureReason;
        Detail = detail;
    }

    public bool Succeeded => Plan != null;

    public TunnelDepthExcavationPlan? Plan { get; }

    public TunnelDepthExcavationFailureReason FailureReason { get; }

    public string Detail { get; }

    internal static TunnelDepthExcavationPlanResult Success(
        TunnelDepthExcavationPlan plan)
    {
        return new TunnelDepthExcavationPlanResult(
            plan ?? throw new ArgumentNullException(nameof(plan)),
            TunnelDepthExcavationFailureReason.None,
            "The next tunnel depth cell can be designated for excavation.");
    }

    internal static TunnelDepthExcavationPlanResult Failure(
        TunnelDepthExcavationFailureReason reason,
        string detail)
    {
        if (reason == TunnelDepthExcavationFailureReason.None)
        {
            throw new ArgumentOutOfRangeException(nameof(reason));
        }

        return new TunnelDepthExcavationPlanResult(null, reason, detail);
    }
}

public sealed class TunnelDepthExcavationPolicy
{
    public TunnelDepthExcavationPlanResult Plan(
        TunnelNavigationVolume volume,
        CellId source)
    {
        if (volume is null)
        {
            throw new ArgumentNullException(nameof(volume));
        }

        if (!volume.Contains(source))
        {
            return TunnelDepthExcavationPlanResult.Failure(
                TunnelDepthExcavationFailureReason.SourceOutsideVolume,
                "The selected tunnel cell is outside the four-layer volume.");
        }

        if (!volume.IsOpen(source))
        {
            return TunnelDepthExcavationPlanResult.Failure(
                TunnelDepthExcavationFailureReason.SourceNotOpen,
                "Depth excavation must start from an already open tunnel or room cell.");
        }

        if (source.Z + 1 >= volume.Depth)
        {
            return TunnelDepthExcavationPlanResult.Failure(
                TunnelDepthExcavationFailureReason.MaximumDepthReached,
                "The tunnel already reaches the maximum depth of four cells.");
        }

        CellId target = new CellId(
            source.X,
            source.Y,
            source.Z + 1);
        if (volume.IsOpen(target))
        {
            return TunnelDepthExcavationPlanResult.Failure(
                TunnelDepthExcavationFailureReason.NextDepthAlreadyOpen,
                "The next depth cell is already open; select that cell to continue deeper.");
        }

        return TunnelDepthExcavationPlanResult.Success(
            new TunnelDepthExcavationPlan(source, target));
    }
}

}