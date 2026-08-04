using System;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.World;
using Xunit;

namespace Dig.Tests
{

public sealed class TunnelInfrastructureVisualPresenterTests
{
    [Fact]
    public void Presenter_projects_only_completed_supports_and_junction_trim()
    {
        CellId firstOrigin = new CellId(4, 6, 1);
        CellId secondOrigin = new CellId(8, 6, 1);
        CellId support = new CellId(6, 6, 1);
        CellId door = new CellId(7, 6, 1);
        CellId firstTrim = new CellId(8, 6, 1);
        CellId secondTrim = new CellId(12, 6, 2);
        TunnelInfrastructureSnapshot snapshot = new TunnelInfrastructureSnapshot(
            version: 19,
            new[]
            {
                Segment(
                    1,
                    firstOrigin,
                    new[] { firstOrigin, support, door },
                    new[]
                    {
                        new TunnelStructuralAnchorSnapshot(
                            firstOrigin,
                            TunnelStructuralAnchorKind.Origin,
                            distanceFromOrigin: 0),
                        new TunnelStructuralAnchorSnapshot(
                            support,
                            TunnelStructuralAnchorKind.WoodenSupport,
                            distanceFromOrigin: 2),
                        new TunnelStructuralAnchorSnapshot(
                            door,
                            TunnelStructuralAnchorKind.Door,
                            distanceFromOrigin: 3),
                    }),
                Segment(
                    2,
                    secondOrigin,
                    new[] { secondOrigin, support },
                    new[]
                    {
                        new TunnelStructuralAnchorSnapshot(
                            secondOrigin,
                            TunnelStructuralAnchorKind.Origin,
                            distanceFromOrigin: 0),
                        new TunnelStructuralAnchorSnapshot(
                            support,
                            TunnelStructuralAnchorKind.WoodenSupport,
                            distanceFromOrigin: 2),
                    }),
            },
            new[] { secondTrim, firstTrim, firstTrim });

        TunnelInfrastructureVisualVolumeViewModel volume =
            new TunnelInfrastructureVisualPresenter().Present(snapshot);

        Assert.Equal(19, volume.Version);
        Assert.Equal(3, volume.Instances.Count);
        TunnelInfrastructureVisualViewModel supportVisual = volume.Instances[0];
        Assert.Equal(TunnelInfrastructureVisualKind.WoodenSupport, supportVisual.Kind);
        Assert.Equal(support, supportVisual.Cell);
        Assert.Equal("tunnel:wooden-support:6:6:1", supportVisual.InstanceId);
        Assert.DoesNotContain(volume.Instances, value => value.Cell == door);
        Assert.Equal(
            new[] { firstTrim, secondTrim },
            volume.Instances
                .Where(value =>
                    value.Kind == TunnelInfrastructureVisualKind.JunctionStoneTrim)
                .Select(value => value.Cell)
                .ToArray());
    }

    [Fact]
    public void Repeated_projection_is_stable_and_empty_snapshot_is_empty()
    {
        TunnelInfrastructureSnapshot snapshot = new TunnelInfrastructureSnapshot(
            version: 3,
            new[]
            {
                Segment(
                    3,
                    new CellId(2, 4, 0),
                    new[] { new CellId(2, 4, 0), new CellId(3, 4, 0) },
                    new[]
                    {
                        new TunnelStructuralAnchorSnapshot(
                            new CellId(2, 4, 0),
                            TunnelStructuralAnchorKind.Origin,
                            distanceFromOrigin: 0),
                        new TunnelStructuralAnchorSnapshot(
                            new CellId(3, 4, 0),
                            TunnelStructuralAnchorKind.WoodenSupport,
                            distanceFromOrigin: 1),
                    }),
            });
        TunnelInfrastructureVisualPresenter presenter =
            new TunnelInfrastructureVisualPresenter();

        string[] first = presenter.Present(snapshot).Instances
            .Select(value => value.InstanceId)
            .ToArray();
        string[] second = presenter.Present(snapshot).Instances
            .Select(value => value.InstanceId)
            .ToArray();

        Assert.Equal(first, second);
        Assert.Empty(presenter.Present(new TunnelInfrastructureSnapshot(
            version: 0,
            Array.Empty<HorizontalTunnelSegmentSnapshot>())).Instances);
    }

    private static HorizontalTunnelSegmentSnapshot Segment(
        int id,
        CellId origin,
        CellId[] cells,
        TunnelStructuralAnchorSnapshot[] anchors)
    {
        return new HorizontalTunnelSegmentSnapshot(
            Id(id),
            TunnelSegmentOriginKind.RoomExit,
            origin,
            cells,
            anchors,
            nextAutomaticSupportTarget: null,
            version: 1);
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}
}
