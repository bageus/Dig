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
}

}