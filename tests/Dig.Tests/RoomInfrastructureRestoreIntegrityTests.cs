using Dig.Domain.Core;
using Dig.Domain.Rooms;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class RoomInfrastructureRestoreIntegrityTests
{
    [Fact]
    public void Restore_rejects_completed_unit_count_assigned_to_wrong_material()
    {
        EntityId roomId = EntityId.Parse("00000000000000000000000000000001");
        EntityId workJob = EntityId.Parse("00000000000000000000000000000002");
        RoomInfrastructureProjectSnapshot room =
            new RoomInfrastructureProjectSnapshot(
                roomId,
                "room.template.integrity",
                RoomTemplateKind.Small,
                upgradeOrderCount: 1,
                RoomImprovementStatus.Improving,
                cancellationLocked: true,
                RoomPurposeKind.Bedroom,
                RoomPurposeKind.None,
                new CellId(4, 4, 0),
                new[]
                {
                    new RoomMaterialLedgerSnapshot(
                        RoomUpgradeMaterialIds.Stone,
                        required: 4,
                        delivered: 4,
                        consumed: 1,
                        releasedOnCancel: 0),
                    new RoomMaterialLedgerSnapshot(
                        RoomUpgradeMaterialIds.MushroomLeg,
                        required: 4,
                        delivered: 4,
                        consumed: 0,
                        releasedOnCancel: 0),
                },
                new[]
                {
                    new RoomMaterialUnitId(
                        RoomUpgradeMaterialIds.MushroomLeg,
                        ordinal: 1),
                },
                new[] { workJob },
                version: 5);

        Result<RoomInfrastructureState> restored =
            RoomInfrastructureState.Restore(
                new RoomInfrastructureSnapshot(
                    version: 5,
                    new[] { room }));

        Assert.True(restored.IsFailure);
        Assert.Equal(RoomInfrastructureErrors.InvalidSnapshot, restored.Error);
    }
}

}
