using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class TunnelInfrastructureAnchorTests
{
    private static readonly EntityId FirstSegmentId =
        EntityId.Parse("57400000000000000000000000000001");
    private static readonly EntityId SecondSegmentId =
        EntityId.Parse("57400000000000000000000000000002");

    [Fact]
    public void Room_exit_origin_creates_initial_target_at_cell_ten()
    {
        TunnelInfrastructureState state = CreateSegment(
            FirstSegmentId,
            new CellId(10, 4, 0),
            count: 25);

        HorizontalTunnelSegmentSnapshot segment = RequireSegment(state, FirstSegmentId);

        Assert.Equal(new CellId(20, 4, 0), RequireTarget(segment).TargetCell);
        Assert.Equal(new CellId(10, 4, 0), RequireTarget(segment).AnchorCell);
        Assert.Equal(10, RequireTarget(segment).DistanceFromAnchor);
    }

    [Theory]
    [InlineData(TunnelStructuralAnchorKind.WoodenSupport)]
    [InlineData(TunnelStructuralAnchorKind.Door)]
    public void Anchor_at_cell_five_moves_next_target_to_cell_fifteen(
        TunnelStructuralAnchorKind kind)
    {
        CellId origin = new CellId(10, 4, 0);
        TunnelInfrastructureState state = CreateSegment(FirstSegmentId, origin, count: 30);
        state.DequeueUncommittedEvents();

        Result result = kind == TunnelStructuralAnchorKind.WoodenSupport
            ? state.RegisterCompletedWoodenSupport(FirstSegmentId, new CellId(15, 4, 0), tick: 2)
            : state.RegisterCompletedDoor(FirstSegmentId, new CellId(15, 4, 0), tick: 2);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        HorizontalTunnelSegmentSnapshot segment = RequireSegment(state, FirstSegmentId);
        TunnelAutomaticSupportTargetSnapshot target = RequireTarget(segment);
        Assert.Equal(new CellId(25, 4, 0), target.TargetCell);
        Assert.Equal(new CellId(15, 4, 0), target.AnchorCell);
        Assert.DoesNotContain(
            state.PeekUncommittedEvents().OfType<TunnelAutomaticSupportTargetChanged>(),
            value => value.NextTargetCell == new CellId(20, 4, 0));

        TunnelAutomaticSupportTargetChanged change = Assert.Single(
            state.PeekUncommittedEvents().OfType<TunnelAutomaticSupportTargetChanged>());
        Assert.Equal(new CellId(20, 4, 0), change.PreviousTargetCell);
        Assert.Equal(new CellId(25, 4, 0), change.NextTargetCell);
    }

    [Fact]
    public void Repeated_anchor_commit_is_idempotent()
    {
        TunnelInfrastructureState state = CreateSegment(
            FirstSegmentId,
            new CellId(0, 0, 0),
            count: 20);
        RequireSuccess(state.RegisterCompletedDoor(
            FirstSegmentId,
            new CellId(5, 0, 0),
            tick: 2));
        state.DequeueUncommittedEvents();
        long version = state.Version;
        long segmentVersion = RequireSegment(state, FirstSegmentId).Version;

        RequireSuccess(state.RegisterCompletedDoor(
            FirstSegmentId,
            new CellId(5, 0, 0),
            tick: 3));

        Assert.Equal(version, state.Version);
        Assert.Equal(segmentVersion, RequireSegment(state, FirstSegmentId).Version);
        Assert.Empty(state.PeekUncommittedEvents());
    }

    [Fact]
    public void Door_and_support_in_same_cell_keep_one_derived_target()
    {
        TunnelInfrastructureState state = CreateSegment(
            FirstSegmentId,
            new CellId(0, 0, 0),
            count: 25);
        CellId anchorCell = new CellId(5, 0, 0);
        RequireSuccess(state.RegisterCompletedWoodenSupport(FirstSegmentId, anchorCell, tick: 2));
        RequireSuccess(state.RegisterCompletedDoor(FirstSegmentId, anchorCell, tick: 3));

        HorizontalTunnelSegmentSnapshot segment = RequireSegment(state, FirstSegmentId);
        TunnelAutomaticSupportTargetSnapshot target = RequireTarget(segment);
        Assert.Equal(new CellId(15, 0, 0), target.TargetCell);
        Assert.Equal(anchorCell, target.AnchorCell);
        Assert.Equal(2, segment.StructuralAnchors.Count(value => value.Cell == anchorCell));
    }

    [Fact]
    public void Forward_door_replaces_the_older_derived_target()
    {
        TunnelInfrastructureState state = CreateSegment(
            FirstSegmentId,
            new CellId(0, 0, 0),
            count: 30);
        state.DequeueUncommittedEvents();

        RequireSuccess(state.RegisterCompletedDoor(
            FirstSegmentId,
            new CellId(12, 0, 0),
            tick: 2));

        HorizontalTunnelSegmentSnapshot segment = RequireSegment(state, FirstSegmentId);
        Assert.Equal(new CellId(22, 0, 0), RequireTarget(segment).TargetCell);
        TunnelAutomaticSupportTargetChanged change = Assert.Single(
            state.PeekUncommittedEvents().OfType<TunnelAutomaticSupportTargetChanged>());
        Assert.Equal(new CellId(10, 0, 0), change.PreviousTargetCell);
        Assert.Equal(new CellId(22, 0, 0), change.NextTargetCell);
    }

    [Fact]
    public void Historical_anchor_does_not_rewind_a_later_anchor()
    {
        TunnelInfrastructureState state = CreateSegment(
            FirstSegmentId,
            new CellId(0, 0, 0),
            count: 35);
        RequireSuccess(state.RegisterCompletedWoodenSupport(
            FirstSegmentId,
            new CellId(10, 0, 0),
            tick: 2));
        RequireSuccess(state.RegisterCompletedDoor(
            FirstSegmentId,
            new CellId(5, 0, 0),
            tick: 3));

        HorizontalTunnelSegmentSnapshot segment = RequireSegment(state, FirstSegmentId);
        Assert.Equal(new CellId(20, 0, 0), RequireTarget(segment).TargetCell);
        Assert.Contains(segment.StructuralAnchors,
            value => value.Cell == new CellId(5, 0, 0)
                && value.Kind == TunnelStructuralAnchorKind.Door);
    }

    [Fact]
    public void Vertical_junction_split_creates_independent_left_and_right_chains()
    {
        TunnelInfrastructureState state = new TunnelInfrastructureState();
        CellId junction = new CellId(20, 8, 1);
        RequireSuccess(state.RegisterSegment(
            FirstSegmentId,
            TunnelSegmentOriginKind.VerticalJunction,
            junction,
            CreateCells(junction, count: 20, direction: -1),
            tick: 1));
        RequireSuccess(state.RegisterSegment(
            SecondSegmentId,
            TunnelSegmentOriginKind.VerticalJunction,
            junction,
            CreateCells(junction, count: 20, direction: 1),
            tick: 1));

        RequireSuccess(state.RegisterCompletedDoor(
            FirstSegmentId,
            new CellId(15, 8, 1),
            tick: 2));

        Assert.Equal(new CellId(5, 8, 1),
            RequireTarget(RequireSegment(state, FirstSegmentId)).TargetCell);
        Assert.Equal(new CellId(30, 8, 1),
            RequireTarget(RequireSegment(state, SecondSegmentId)).TargetCell);
    }

    [Fact]
    public void Segment_end_has_no_phantom_target()
    {
        TunnelInfrastructureState state = CreateSegment(
            FirstSegmentId,
            new CellId(0, 0, 0),
            count: 12);
        RequireSuccess(state.RegisterCompletedWoodenSupport(
            FirstSegmentId,
            new CellId(5, 0, 0),
            tick: 2));

        Assert.Null(RequireSegment(state, FirstSegmentId).NextAutomaticSupportTarget);
    }

    [Fact]
    public void Snapshot_restore_preserves_anchor_kinds_and_next_target()
    {
        TunnelInfrastructureState state = CreateSegment(
            FirstSegmentId,
            new CellId(0, 0, 0),
            count: 30);
        RequireSuccess(state.RegisterCompletedWoodenSupport(
            FirstSegmentId,
            new CellId(5, 0, 0),
            tick: 2));
        RequireSuccess(state.RegisterCompletedDoor(
            FirstSegmentId,
            new CellId(5, 0, 0),
            tick: 3));
        TunnelInfrastructureSnapshot saved = state.CaptureSnapshot();

        Result<TunnelInfrastructureState> restoredResult =
            TunnelInfrastructureState.Restore(saved);

        Assert.True(restoredResult.IsSuccess, restoredResult.Error?.ToString());
        TunnelInfrastructureSnapshot restored = restoredResult.Value.CaptureSnapshot();
        Assert.Equal(saved.Version, restored.Version);
        Assert.Equal(saved.Segments.Count, restored.Segments.Count);
        Assert.Equal(
            saved.Segments[0].StructuralAnchors.ToArray(),
            restored.Segments[0].StructuralAnchors.ToArray());
        Assert.Equal(
            saved.Segments[0].NextAutomaticSupportTarget,
            restored.Segments[0].NextAutomaticSupportTarget);
        Assert.Empty(restoredResult.Value.PeekUncommittedEvents());
    }

    [Fact]
    public void Segment_must_be_contiguous_and_horizontal()
    {
        TunnelInfrastructureState state = new TunnelInfrastructureState();
        Result result = state.RegisterSegment(
            FirstSegmentId,
            TunnelSegmentOriginKind.RoomExit,
            new CellId(0, 0, 0),
            new[] { new CellId(1, 0, 0), new CellId(3, 0, 0) },
            tick: 1);

        Assert.True(result.IsFailure);
        Assert.Equal(TunnelInfrastructureErrors.InvalidHorizontalSegment, result.Error);
        Assert.Equal(0, state.Version);
    }

    private static TunnelInfrastructureState CreateSegment(
        EntityId segmentId,
        CellId origin,
        int count)
    {
        TunnelInfrastructureState state = new TunnelInfrastructureState();
        RequireSuccess(state.RegisterSegment(
            segmentId,
            TunnelSegmentOriginKind.RoomExit,
            origin,
            CreateCells(origin, count, direction: 1),
            tick: 1));
        return state;
    }

    private static IReadOnlyList<CellId> CreateCells(
        CellId origin,
        int count,
        int direction)
    {
        return Enumerable.Range(1, count)
            .Select(distance => new CellId(
                origin.X + (distance * direction),
                origin.Y,
                origin.Z))
            .ToArray();
    }

    private static HorizontalTunnelSegmentSnapshot RequireSegment(
        TunnelInfrastructureState state,
        EntityId segmentId)
    {
        HorizontalTunnelSegmentSnapshot? segment = state.GetSegment(segmentId);
        Assert.NotNull(segment);
        return segment!;
    }

    private static TunnelAutomaticSupportTargetSnapshot RequireTarget(
        HorizontalTunnelSegmentSnapshot segment)
    {
        Assert.True(segment.NextAutomaticSupportTarget.HasValue);
        return segment.NextAutomaticSupportTarget!.Value;
    }

    private static void RequireSuccess(Result result)
    {
        Assert.True(result.IsSuccess, result.Error?.ToString());
    }
}
}
