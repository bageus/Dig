using Dig.Domain.Inventory;

namespace Dig.Domain.World
{

public static class DefaultTerrainMaterials
{
    public static readonly MaterialId Sand = new MaterialId("terrain.sand");
    public static readonly MaterialId StoneRock = new MaterialId("terrain.stone_rock");
    public static readonly MaterialId MetalBearingRock =
        new MaterialId("terrain.metal_bearing_rock");
    public static readonly MaterialId CrystallineRock =
        new MaterialId("terrain.crystalline_rock");
    public static readonly MaterialId LavaRock = new MaterialId("terrain.lava_rock");
    public static readonly MaterialId Unmineable = new MaterialId("terrain.unmineable");

    public static MaterialCatalog CreateCatalog()
    {
        ItemId stone = new ItemId("material.stone");
        ItemId ironOre = new ItemId("ore.iron");
        ItemId goldOre = new ItemId("ore.gold");
        ItemId crystalOre = new ItemId("ore.crystal");
        ItemId coal = new ItemId("material.coal");

        return new MaterialCatalog(new[]
        {
            Material(Sand, "Песчаный грунт", 25, Profile("terrain-output.sand", 1)),
            Material(StoneRock, "Каменная порода", 100,
                Profile("terrain-output.stone-rock", 2,
                    Entry(stone, 100, 1, 2))),
            Material(MetalBearingRock, "Рудная порода", 140,
                Profile("terrain-output.metal-bearing-rock", 2,
                    Entry(stone, 100, 1, 2),
                    Entry(ironOre, 70, 1, 2),
                    Entry(goldOre, 5, 1, 1),
                    Entry(coal, 40, 1, 2))),
            Material(CrystallineRock, "Кристаллическая порода", 170,
                Profile("terrain-output.crystalline-rock", 2,
                    Entry(stone, 40, 1, 2),
                    Entry(ironOre, 60, 1, 2),
                    Entry(crystalOre, 80, 1, 2),
                    Entry(goldOre, 10, 1, 1))),
            Material(LavaRock, "Лавовая порода", 220,
                Profile("terrain-output.lava-rock", 2,
                    Entry(goldOre, 30, 1, 2),
                    Entry(stone, 40, 1, 2),
                    Entry(crystalOre, 50, 1, 2),
                    Entry(ironOre, 60, 1, 2),
                    Entry(coal, 70, 1, 2))),
            new MaterialDefinition(
                Unmineable,
                "Недобываемая порода",
                isSolid: true,
                hardness: int.MaxValue,
                isMineable: false,
                outputProfile: null),
        });
    }

    private static MaterialDefinition Material(
        MaterialId id,
        string displayName,
        int hardness,
        TerrainOutputProfile profile)
    {
        return new MaterialDefinition(
            id,
            displayName,
            isSolid: true,
            hardness: hardness,
            isMineable: true,
            outputProfile: profile);
    }

    private static TerrainOutputProfile Profile(
        string id,
        int version,
        params TerrainOutputEntry[] entries)
    {
        return new TerrainOutputProfile(id, version, entries);
    }

    private static TerrainOutputEntry Entry(
        ItemId itemId,
        int probabilityPermille,
        int minimumQuantity,
        int maximumQuantity)
    {
        return new TerrainOutputEntry(
            itemId,
            probabilityPermille,
            minimumQuantity,
            maximumQuantity);
    }
}

}
