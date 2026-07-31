using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class TerrainOutputProfileTests
{
    [Fact]
    public void Default_catalog_contains_all_six_typed_terrain_materials()
    {
        MaterialCatalog catalog = DefaultTerrainMaterials.CreateCatalog();

        Assert.Equal(6, catalog.Definitions.Count);
        Assert.Equal(
            "Рудная порода",
            catalog.Get(DefaultTerrainMaterials.MetalBearingRock)!.DisplayName);
        Assert.False(catalog.Get(DefaultTerrainMaterials.Unmineable)!.IsMineable);
        Assert.Null(catalog.Get(DefaultTerrainMaterials.Unmineable)!.OutputProfile);
    }

    [Fact]
    public void Default_profiles_reference_only_approved_raw_outputs()
    {
        MaterialCatalog catalog = DefaultTerrainMaterials.CreateCatalog();
        Dictionary<MaterialId, ItemId[]> allowed = new Dictionary<MaterialId, ItemId[]>
        {
            [DefaultTerrainMaterials.Sand] = Array.Empty<ItemId>(),
            [DefaultTerrainMaterials.StoneRock] = Items("material.stone"),
            [DefaultTerrainMaterials.MetalBearingRock] = Items(
                "material.stone", "ore.iron", "ore.gold", "material.coal"),
            [DefaultTerrainMaterials.CrystallineRock] = Items(
                "material.stone", "ore.iron", "ore.crystal", "ore.gold"),
            [DefaultTerrainMaterials.LavaRock] = Items(
                "ore.gold", "material.stone", "ore.crystal", "ore.iron", "material.coal"),
        };

        foreach (KeyValuePair<MaterialId, ItemId[]> pair in allowed)
        {
            Assert.Equal(
                pair.Value.OrderBy(value => value),
                catalog.Get(pair.Key)!.OutputProfile!.Entries
                    .Select(value => value.ItemId));
        }

        Assert.DoesNotContain(
            catalog.Definitions
                .Where(value => value.OutputProfile != null)
                .SelectMany(value => value.OutputProfile!.Entries),
            value => value.ItemId == new ItemId("material.metal")
                || value.ItemId == new ItemId("material.iron")
                || value.ItemId == new ItemId("material.gold")
                || value.ItemId == new ItemId("material.crystal"));
    }

    [Fact]
    public void Sand_always_resolves_to_empty_output()
    {
        MaterialDefinition sand = DefaultTerrainMaterials.CreateCatalog()
            .Get(DefaultTerrainMaterials.Sand)!;
        TerrainOutputResolver resolver = new TerrainOutputResolver();

        for (int x = 0; x < 64; x++)
        {
            Assert.True(resolver.Resolve(
                42,
                3,
                new CellId(x, 7, x % WorldSize.RequiredDepth),
                sand.OutputProfile!).IsEmpty);
        }
    }

    [Fact]
    public void Stone_rock_never_resolves_to_ore()
    {
        MaterialDefinition stone = DefaultTerrainMaterials.CreateCatalog()
            .Get(DefaultTerrainMaterials.StoneRock)!;
        TerrainOutputResolver resolver = new TerrainOutputResolver();

        for (int x = 0; x < 64; x++)
        {
            TerrainOutputRoll roll = resolver.Resolve(
                81,
                2,
                new CellId(x, 3, x % WorldSize.RequiredDepth),
                stone.OutputProfile!);

            TerrainOutputResult output = Assert.Single(roll.Outputs);
            Assert.Equal(new ItemId("material.stone"), output.ItemId);
        }
    }

    [Fact]
    public void Independent_entries_can_resolve_multiple_outputs_deterministically()
    {
        TerrainOutputProfile profile = new TerrainOutputProfile(
            "terrain-output.multi",
            version: 7,
            new[]
            {
                new TerrainOutputEntry(new ItemId("material.stone"), 1_000, 2, 2),
                new TerrainOutputEntry(new ItemId("ore.iron"), 1_000, 1, 1),
            });
        TerrainOutputResolver resolver = new TerrainOutputResolver();

        TerrainOutputRoll first = resolver.Resolve(123, 4, new CellId(8, 9, 1), profile);
        TerrainOutputRoll replay = resolver.Resolve(123, 4, new CellId(8, 9, 1), profile);
        TerrainOutputRoll otherLayer = resolver.Resolve(123, 4, new CellId(8, 9, 2), profile);

        Assert.Equal(2, first.Outputs.Count);
        Assert.Equal(3, first.Outputs.Sum(value => value.Quantity));
        Assert.Equal(Describe(first), Describe(replay));
        Assert.NotEqual(first.Roll, otherLayer.Roll);
    }

    [Fact]
    public void Profiles_reject_duplicate_item_ids()
    {
        Assert.Throws<ArgumentException>(() => new TerrainOutputProfile(
            "invalid",
            1,
            new[]
            {
                new TerrainOutputEntry(new ItemId("material.stone"), 700, 1, 1),
                new TerrainOutputEntry(new ItemId("material.stone"), 400, 1, 1),
            }));
    }

    [Fact]
    public void Unmineable_material_cannot_receive_an_output_profile()
    {
        TerrainOutputProfile profile = new TerrainOutputProfile(
            "invalid",
            1,
            Array.Empty<TerrainOutputEntry>());

        Assert.Throws<ArgumentException>(() => new MaterialDefinition(
            DefaultTerrainMaterials.Unmineable,
            "Недобываемая порода",
            isSolid: true,
            hardness: 100,
            isMineable: false,
            outputProfile: profile));
    }

    private static ItemId[] Items(params string[] values)
    {
        return values.Select(value => new ItemId(value)).OrderBy(value => value).ToArray();
    }

    private static string Describe(TerrainOutputRoll roll)
    {
        return string.Join(
            ";",
            roll.Outputs.Select(value => $"{value.ItemId}:{value.Quantity}:"
                + $"{value.ProbabilityRoll}:{value.QuantityRoll}"));
    }
}

}
