using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class TunnelJunctionStoneTrimTests
{
    private static readonly EntityId FirstSegmentId = Id(1);
    private static readonly EntityId SecondSegmentId = Id(2);
    private static readonly CellId Junction = new CellId(20, 8, 1);

    [Fact]
    public void Split_junction_projects_one_trim_target_with_stable_owner()
    {
        TunnelInfrastructureState state = CreateSplitJunction();

        TunnelInfrastructureSnapshot snapshot = state.CaptureSnapshot();
        TunnelJunctionStoneTrimTargetSnapshot target = Assert.Single(
            snapshot.PendingJunctionStoneTrimTargets);

        Assert.Equal(Junction, target.Cell);
        Assert.Equal(FirstSegmentId, target.OwnerSegmentId);
    }

    [Fact]
    public void Completing_trim_is_idempotent_and_round_trips()
    {
        TunnelInfrastructureState state = CreateSplitJunction();
        state.DequeueUncommittedEvents();

        RequireSuccess(state.RegisterCompletedJunctionStoneTrim(Junction, tick: 2));
        long version = state.Version;
        RequireSuccess(state.RegisterCompletedJunctionStoneTrim(Junction, tick: 3));

        TunnelInfrastructureSnapshot snapshot = state.CaptureSnapshot();
        Assert.Empty(snapshot.PendingJunctionStoneTrimTargets);
        Assert.Equal(Junction, Assert.Single(snapshot.CompletedJunctionStoneTrimCells));
        Assert.Equal(version, state.Version);
        Assert.Single(
            state.PeekUncommittedEvents().OfType<TunnelJunctionStoneTrimCompleted>());

        Result<TunnelInfrastructureState> restored =
            TunnelInfrastructureState.Restore(snapshot);
        Assert.True(restored.IsSuccess, restored.Error?.ToString());
        Assert.Equal(
            snapshot.CompletedJunctionStoneTrimCells.ToArray(),
            restored.Value.CaptureSnapshot().CompletedJunctionStoneTrimCells.ToArray());
        Assert.Empty(restored.Value.PeekUncommittedEvents());
    }

    [Fact]
    public void Removing_one_side_rebinds_owner_and_last_side_removes_target()
    {
        TunnelInfrastructureState state = CreateSplitJunction();
        state.DequeueUncommittedEvents();

        RequireSuccess(state.RemoveSegment(FirstSegmentId, tick: 2));

        TunnelJunctionStoneTrimTargetSnapshot remaining = Assert.Single(
            state.CaptureSnapshot().PendingJunctionStoneTrimTargets);
        Assert.Equal(SecondSegmentId, remaining.OwnerSegmentId);
        TunnelJunctionStoneTrimTargetChanged rebind = Assert.Single(
            state.PeekUncommittedEvents()
                .OfType<TunnelJunctionStoneTrimTargetChanged>());
        Assert.Equal(FirstSegmentId, rebind.PreviousOwnerSegmentId);
        Assert.Equal(SecondSegmentId, rebind.NextOwnerSegmentId);

        state.DequeueUncommittedEvents();
        RequireSuccess(state.RemoveSegment(SecondSegmentId, tick: 3));

        Assert.Empty(state.CaptureSnapshot().PendingJunctionStoneTrimTargets);
        TunnelJunctionStoneTrimTargetChanged removed = Assert.Single(
            state.PeekUncommittedEvents()
                .OfType<TunnelJunctionStoneTrimTargetChanged>());
        Assert.Equal(SecondSegmentId, removed.PreviousOwnerSegmentId);
        Assert.Null(removed.NextOwnerSegmentId);
    }

    [Fact]
    public void Removing_last_junction_discards_completed_trim_provenance()
    {
        TunnelInfrastructureState state = CreateSplitJunction();
        RequireSuccess(state.RegisterCompletedJunctionStoneTrim(Junction, tick: 2));
        RequireSuccess(state.RemoveSegment(FirstSegmentId, tick: 3));
        state.DequeueUncommittedEvents();

        RequireSuccess(state.RemoveSegment(SecondSegmentId, tick: 4));

        Assert.Empty(state.CaptureSnapshot().CompletedJunctionStoneTrimCells);
        Assert.Single(
            state.PeekUncommittedEvents()
                .OfType<TunnelJunctionStoneTrimCompletionRemoved>());
    }

    private static TunnelInfrastructureState CreateSplitJunction()
    {
        TunnelInfrastructureState state = new TunnelInfrastructureState();
        RequireSuccess(state.RegisterSegment(
            FirstSegmentId,
            TunnelSegmentOriginKind.VerticalJunction,
            Junction,
            Cells(direction: -1),
            tick: 1));
        RequireSuccess(state.RegisterSegment(
            SecondSegmentId,
            TunnelSegmentOriginKind.VerticalJunction,
            Junction,
            Cells(direction: 1),
            tick: 1));
        return state;
    }

    private static IReadOnlyList<CellId> Cells(int direction)
    {
        return Enumerable.Range(1, 20)
            .Select(distance => new CellId(
                Junction.X + (distance * direction),
                Junction.Y,
                Junction.Z))
            .ToArray();
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }

    private static void RequireSuccess(Result result)
    {
        Assert.True(result.IsSuccess, result.Error?.ToString());
    }
}
}
