using System.Linq;
using Dig.Application.World;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class TerrainOutputCatalogValidationTests
{
    [Fact]
    public void Default_terrain_catalog_validates_against_raw_material_items()
    {
        ItemCatalog items = new ItemCatalog(new[]
        {
            Item("material.stone", 20),
            Item("material.coal", 20),
            Item("ore.iron", 20),
            Item("ore.gold", 20),
            Item("ore.crystal", 20),
        });

        TerrainOutputCatalogValidationReport report =
            new TerrainOutputCatalogValidator().Validate(
                DefaultTerrainMaterials.CreateCatalog(),
                items);

        Assert.True(report.IsValid, string.Join("; ", report.Issues.Select(x => x.Code)));
    }

    [Fact]
    public void Validation_reports_unknown_legacy_and_range_violations_stably()
    {
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            Terrain("terrain.a", "profile.a", "material.metal", 1),
            Terrain("terrain.b", "profile.b", "ore.missing", 1),
            Terrain("terrain.c", "profile.c", "material.stone", 3),
        });
        ItemCatalog items = new ItemCatalog(new[] { Item("material.stone", 2) });

        TerrainOutputCatalogValidationReport report =
            new TerrainOutputCatalogValidator().Validate(materials, items);

        Assert.Equal(new[]
        {
            TerrainOutputCatalogValidationCodes.ForbiddenLegacyMetal,
            TerrainOutputCatalogValidationCodes.UnknownItem,
            TerrainOutputCatalogValidationCodes.StackSizeExceeded,
        }, report.Issues.Select(value => value.Code));
    }

    private static MaterialDefinition Terrain(
        string materialId,
        string profileId,
        string itemId,
        int maximumQuantity)
    {
        return new MaterialDefinition(
            new MaterialId(materialId),
            materialId,
            true,
            10,
            true,
            new TerrainOutputProfile(profileId, 1, new[]
            {
                new TerrainOutputEntry(
                    new ItemId(itemId),
                    1_000,
                    1,
                    maximumQuantity),
            }));
    }

    private static ItemDefinition Item(string id, int maximumStackSize)
    {
        return new ItemDefinition(new ItemId(id), id, maximumStackSize, false);
    }
}

}
