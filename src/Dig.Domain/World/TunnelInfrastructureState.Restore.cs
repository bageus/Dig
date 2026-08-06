using System;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.World
{

public sealed partial class TunnelInfrastructureState
{
    public static Result<TunnelInfrastructureState> Restore(
        TunnelInfrastructureSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        TunnelInfrastructureState state = new TunnelInfrastructureState();
        foreach (HorizontalTunnelSegmentSnapshot segmentSnapshot in snapshot.Segments)
        {
            if (state._segments.ContainsKey(segmentSnapshot.SegmentId))
            {
                return Result<TunnelInfrastructureState>.Failure(
                    TunnelInfrastructureErrors.InvalidSnapshot);
            }

            Result<HorizontalTunnelSegmentState> restored =
                HorizontalTunnelSegmentState.Restore(segmentSnapshot);
            if (restored.IsFailure)
            {
                return Result<TunnelInfrastructureState>.Failure(restored.Error!);
            }

            state._segments.Add(segmentSnapshot.SegmentId, restored.Value);
        }

        foreach (CellId cell in snapshot.CompletedStoneFloorTrimCells)
        {
            bool belongsToSegment = state._segments.Values.Any(segment =>
                segment.CaptureSnapshot().OrderedHorizontalCells.Contains(cell));
            if (!belongsToSegment || !state._completedStoneFloorTrimCells.Add(cell))
            {
                return Result<TunnelInfrastructureState>.Failure(
                    TunnelInfrastructureErrors.InvalidSnapshot);
            }
        }

        foreach (CellId cell in snapshot.CompletedJunctionStoneTrimCells)
        {
            if (!state.HasVerticalJunction(cell)
                || !state._completedJunctionStoneTrimCells.Add(cell))
            {
                return Result<TunnelInfrastructureState>.Failure(
                    TunnelInfrastructureErrors.InvalidSnapshot);
            }
        }

        TunnelInfrastructureSnapshot derived = state.CaptureSnapshot();
        if (!derived.PendingJunctionStoneTrimTargets.SequenceEqual(
                snapshot.PendingJunctionStoneTrimTargets))
        {
            return Result<TunnelInfrastructureState>.Failure(
                TunnelInfrastructureErrors.InvalidSnapshot);
        }

        state.Version = snapshot.Version;
        return Result<TunnelInfrastructureState>.Success(state);
    }
}

}
