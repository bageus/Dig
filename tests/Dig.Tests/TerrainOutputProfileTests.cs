using System;
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
    public void Sand_always_resolves_to_empty_output()
    {
        MaterialDefinition sand = DefaultTerrainMaterials.CreateCatalog()
            .Get(DefaultTerrainMaterials.Sand)!;
        TerrainOutputResolver resolver = new TerrainOutputResolver();

        for (int x = 0; x < 64; x++)
        {
            TerrainOutputRoll roll = resolver.Resolve(
                worldSeed: 42,
                generatorVersion: 3,
                new CellId(x, 7, x % WorldSize.RequiredDepth),
                sand.OutputProfile!);

            Assert.True(roll.IsEmpty);
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
                worldSeed: 81,
                generatorVersion: 2,
                new CellId(x, 3, x % WorldSize.RequiredDepth),
                stone.OutputProfile!);

            Assert.False(roll.IsEmpty);
            Assert.Equal(new ItemId("material.stone"), roll.ItemId);
        }
    }

    [Fact]
    public void Resolver_is_deterministic_and_Z_aware()
    {
        TerrainOutputProfile profile = DefaultTerrainMaterials.CreateCatalog()
            .Get(DefaultTerrainMaterials.MetalBearingRock)!
            .OutputProfile!;
        TerrainOutputResolver resolver = new TerrainOutputResolver();

        TerrainOutputRoll first = resolver.Resolve(123, 4, new CellId(8, 9, 1), profile);
        TerrainOutputRoll replay = resolver.Resolve(123, 4, new CellId(8, 9, 1), profile);
        TerrainOutputRoll otherLayer = resolver.Resolve(123, 4, new CellId(8, 9, 2), profile);

        Assert.Equal(first.Roll, replay.Roll);
        Assert.Equal(first.ItemId, replay.ItemId);
        Assert.Equal(first.Quantity, replay.Quantity);
        Assert.NotEqual(first.Roll, otherLayer.Roll);
    }

    [Fact]
    public void Profiles_reject_invalid_or_forbidden_output_configuration()
    {
        Assert.Throws<ArgumentException>(() => new TerrainOutputProfile(
            "invalid",
            1,
            new[]
            {
                new TerrainOutputEntry(new ItemId("material.stone"), 700, 1, 1),
                new TerrainOutputEntry(new ItemId("material.iron"), 400, 1, 1),
            }));

        MaterialCatalog catalog = DefaultTerrainMaterials.CreateCatalog();
        Assert.DoesNotContain(
            catalog.Definitions
                .Where(value => value.OutputProfile != null)
                .SelectMany(value => value.OutputProfile!.Entries),
            value => value.ItemId == new ItemId("material.metal"));
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
}

}