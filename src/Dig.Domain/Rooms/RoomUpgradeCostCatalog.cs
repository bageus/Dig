using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Domain.Inventory;

namespace Dig.Domain.Rooms
{

public static class RoomUpgradeMaterialIds
{
    public static readonly ItemId Stone = new ItemId("material.stone");
    public static readonly ItemId MushroomLeg = new ItemId("material.mushroom_leg");
    public static readonly ItemId Iron = new ItemId("material.iron");
    public static readonly ItemId Crystal = new ItemId("material.crystal");
}

public static class RoomUpgradeCostCatalog
{
    private static readonly IReadOnlyDictionary<RoomTemplateKind, IReadOnlyList<RoomMaterialRequirement>> Costs =
        new ReadOnlyDictionary<RoomTemplateKind, IReadOnlyList<RoomMaterialRequirement>>(
            new Dictionary<RoomTemplateKind, IReadOnlyList<RoomMaterialRequirement>>
            {
                [RoomTemplateKind.Small] = Requirements(
                    new RoomMaterialRequirement(RoomUpgradeMaterialIds.Stone, 4),
                    new RoomMaterialRequirement(RoomUpgradeMaterialIds.MushroomLeg, 4)),
                [RoomTemplateKind.Medium] = Requirements(
                    new RoomMaterialRequirement(RoomUpgradeMaterialIds.Stone, 8),
                    new RoomMaterialRequirement(RoomUpgradeMaterialIds.MushroomLeg, 8)),
                [RoomTemplateKind.Large] = Requirements(
                    new RoomMaterialRequirement(RoomUpgradeMaterialIds.Stone, 12),
                    new RoomMaterialRequirement(RoomUpgradeMaterialIds.MushroomLeg, 8),
                    new RoomMaterialRequirement(RoomUpgradeMaterialIds.Iron, 4)),
                [RoomTemplateKind.Tall] = Requirements(
                    new RoomMaterialRequirement(RoomUpgradeMaterialIds.Stone, 10),
                    new RoomMaterialRequirement(RoomUpgradeMaterialIds.MushroomLeg, 6),
                    new RoomMaterialRequirement(RoomUpgradeMaterialIds.Iron, 4),
                    new RoomMaterialRequirement(RoomUpgradeMaterialIds.Crystal, 4)),
            });

    public static IReadOnlyList<RoomMaterialRequirement> Get(RoomTemplateKind kind)
    {
        if (!Costs.TryGetValue(kind, out IReadOnlyList<RoomMaterialRequirement>? value))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        return value;
    }

    private static IReadOnlyList<RoomMaterialRequirement> Requirements(
        params RoomMaterialRequirement[] values)
    {
        return new ReadOnlyCollection<RoomMaterialRequirement>(values);
    }
}

}
