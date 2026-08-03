using System;
using Dig.Application.Rooms;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Rooms;
using Dig.Domain.World;
using Dig.Presentation.Rooms;
using Xunit;

namespace Dig.Tests
{

public sealed class RoomInfrastructurePresentationTests
{
    private static readonly EntityId RoomId = EntityId.Parse(
        "11111111111111111111111111111111");

    [Fact]
    public void Presenter_projects_marker_progress_purposes_and_cancel_lock()
    {
        RoomInfrastructureProjectSnapshot room = new RoomInfrastructureProjectSnapshot(
            RoomId,
            "template-room-1",
            RoomTemplateKind.Small,
            upgradeOrderCount: 1,
            RoomImprovementStatus.Improving,
            cancellationLocked: true,
            RoomPurposeKind.Workshop,
            RoomPurposeKind.None,
            new CellId(4, 5, 0),
            new[]
            {
                new RoomMaterialLedgerSnapshot(
                    new ItemId("material.stone"),
                    required: 4,
                    delivered: 4,
                    consumed: 1,
                    releasedOnCancel: 0),
                new RoomMaterialLedgerSnapshot(
                    new ItemId("material.mushroom_leg"),
                    required: 4,
                    delivered: 4,
                    consumed: 0,
                    releasedOnCancel: 0),
            },
            new[]
            {
                new RoomMaterialUnitId(new ItemId("material.stone"), 1),
            },
            new[] { EntityId.Parse("22222222222222222222222222222222") },
            version: 8);
        RoomInfrastructureSnapshot snapshot = new RoomInfrastructureSnapshot(
            version: 9,
            new[] { room });
        CompletedRoomInfrastructureProvenance provenance =
            new CompletedRoomInfrastructureProvenance(
                RoomId,
                "template-room-1",
                RoomTemplateKind.Small,
                new[]
                {
                    new CellId(2, 3, 0),
                    new CellId(3, 3, 0),
                    new CellId(4, 3, 0),
                    new CellId(2, 4, 0),
                    new CellId(4, 4, 0),
                });

        RoomInfrastructureViewModel model = Assert.Single(
            new RoomInfrastructurePresenter().Present(
                snapshot,
                new[] { provenance }));

        Assert.Equal(RoomId.ToString(), model.Id);
        Assert.Equal(3f, model.MarkerX);
        Assert.Equal(3, model.MarkerY);
        Assert.Equal(0, model.MarkerZ);
        Assert.Equal(8, model.RequiredUnits);
        Assert.Equal(8, model.DeliveredUnits);
        Assert.Equal(1, model.ConsumedUnits);
        Assert.Equal(10000, model.DeliveryProgressBasisPoints);
        Assert.Equal(1250, model.WorkProgressBasisPoints);
        Assert.False(model.CancellationAllowed);
        Assert.Equal(RoomPurposeKind.Workshop, model.RequestedPurpose);
        Assert.Equal(RoomPurposeKind.None, model.ActivePurpose);
        Assert.Single(model.CompletedUnits);
    }

    [Fact]
    public void Presenter_rejects_missing_completed_provenance()
    {
        RoomInfrastructureProjectSnapshot room = new RoomInfrastructureProjectSnapshot(
            RoomId,
            "template-room-1",
            RoomTemplateKind.Small,
            upgradeOrderCount: 0,
            RoomImprovementStatus.Unimproved,
            cancellationLocked: false,
            RoomPurposeKind.None,
            RoomPurposeKind.None,
            temporaryStockCell: null,
            new[]
            {
                new RoomMaterialLedgerSnapshot(
                    new ItemId("material.stone"), 4, 0, 0, 0),
            },
            Array.Empty<RoomMaterialUnitId>(),
            Array.Empty<EntityId>(),
            version: 0);

        Assert.Throws<InvalidOperationException>(() =>
            new RoomInfrastructurePresenter().Present(
                new RoomInfrastructureSnapshot(0, new[] { room }),
                Array.Empty<CompletedRoomInfrastructureProvenance>()));
    }
}

}
