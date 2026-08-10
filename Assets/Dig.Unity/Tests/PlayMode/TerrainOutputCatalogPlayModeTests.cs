using System.Linq;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using NUnit.Framework;

namespace Dig.Unity.Tests
{

public sealed class TerrainOutputCatalogPlayModeTests
{
    [Test]
    public void Demo_uses_six_typed_terrain_profiles_with_raw_ore_outputs()
    {
        DigWorldSession session = DigWorldSession.CreateDemo(24, 16, 29);
        MaterialCatalog materials = session.Repository.Get().Materials;

        Assert.That(materials.Get(DefaultTerrainMaterials.Sand), Is.Not.Null);
        Assert.That(materials.Get(DefaultTerrainMaterials.StoneRock), Is.Not.Null);
        Assert.That(materials.Get(DefaultTerrainMaterials.MetalBearingRock), Is.Not.Null);
        Assert.That(materials.Get(DefaultTerrainMaterials.CrystallineRock), Is.Not.Null);
        Assert.That(materials.Get(DefaultTerrainMaterials.LavaRock), Is.Not.Null);
        Assert.That(materials.Get(DefaultTerrainMaterials.Unmineable), Is.Not.Null);
        Assert.That(
            materials.Get(DefaultTerrainMaterials.MetalBearingRock)!.DisplayName,
            Is.EqualTo("Рудная порода"));
        Assert.That(materials.Get(DefaultTerrainMaterials.Unmineable)!.IsMineable, Is.False);

        ItemId[] outputs = materials.Definitions
            .Where(value => value.OutputProfile != null)
            .SelectMany(value => value.OutputProfile!.Entries)
            .Select(value => value.ItemId)
            .Distinct()
            .OrderBy(value => value)
            .ToArray();
        Assert.That(outputs, Has.Member(new ItemId("material.stone")));
        Assert.That(outputs, Has.Member(new ItemId("material.coal")));
        Assert.That(outputs, Has.Member(new ItemId("ore.iron")));
        Assert.That(outputs, Has.Member(new ItemId("ore.gold")));
        Assert.That(outputs, Has.Member(new ItemId("ore.crystal")));
        Assert.That(outputs, Has.No.Member(new ItemId("material.metal")));
    }

    [Test]
    public void Demo_contains_all_terrain_regions_and_restored_deposits()
    {
        DigWorldSession session = DigWorldSession.CreateDemo(24, 16, 29);
        WorldState world = session.Repository.Get();
        MaterialId[] solidMaterials = world.CreateSnapshot().Chunks
            .SelectMany(chunk => chunk.Cells)
            .Where(cell => cell.IsSolid)
            .Select(cell => cell.State.MaterialId)
            .Distinct()
            .OrderBy(value => value)
            .ToArray();

        Assert.That(solidMaterials, Has.Member(DefaultTerrainMaterials.Sand));
        Assert.That(solidMaterials, Has.Member(DefaultTerrainMaterials.StoneRock));
        Assert.That(solidMaterials, Has.Member(DefaultTerrainMaterials.MetalBearingRock));
        Assert.That(solidMaterials, Has.Member(DefaultTerrainMaterials.CrystallineRock));
        Assert.That(solidMaterials, Has.Member(DefaultTerrainMaterials.LavaRock));
        Assert.That(solidMaterials, Has.Member(DefaultTerrainMaterials.Unmineable));
        Assert.That(world.TerrainDeposits.Snapshot(), Is.Not.Empty);
        Assert.That(
            world.TerrainDeposits.Snapshot().All(value =>
                value.Definition.OutputItemId != default),
            Is.True);
    }
}

}
