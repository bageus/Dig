using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Application.World
{

public static class TerrainOutputCatalogValidationCodes
{
    public const string UnknownItem = "terrain_output.catalog.unknown_item";
    public const string ForbiddenLegacyMetal = "terrain_output.catalog.legacy_metal";
    public const string StackSizeExceeded = "terrain_output.catalog.stack_size_exceeded";
    public const string UnmineableOutput = "terrain_output.catalog.unmineable_output";
}

public sealed class TerrainOutputCatalogValidationIssue
{
    public TerrainOutputCatalogValidationIssue(
        MaterialId materialId,
        string profileId,
        ItemId itemId,
        string code,
        string message)
    {
        MaterialId = materialId;
        ProfileId = profileId ?? string.Empty;
        ItemId = itemId;
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }

    public MaterialId MaterialId { get; }
    public string ProfileId { get; }
    public ItemId ItemId { get; }
    public string Code { get; }
    public string Message { get; }
}

public sealed class TerrainOutputCatalogValidationReport
{
    internal TerrainOutputCatalogValidationReport(
        IEnumerable<TerrainOutputCatalogValidationIssue> issues)
    {
        TerrainOutputCatalogValidationIssue[] ordered = (issues
            ?? throw new ArgumentNullException(nameof(issues)))
            .OrderBy(value => value.MaterialId)
            .ThenBy(value => value.ProfileId, StringComparer.Ordinal)
            .ThenBy(value => value.ItemId)
            .ThenBy(value => value.Code, StringComparer.Ordinal)
            .ToArray();
        Issues = new ReadOnlyCollection<TerrainOutputCatalogValidationIssue>(ordered);
    }

    public IReadOnlyList<TerrainOutputCatalogValidationIssue> Issues { get; }
    public bool IsValid => Issues.Count == 0;
}

public sealed class TerrainOutputCatalogValidator
{
    private static readonly ItemId LegacyMetal = new ItemId("material.metal");

    public TerrainOutputCatalogValidationReport Validate(
        MaterialCatalog materials,
        ItemCatalog items)
    {
        if (materials == null)
        {
            throw new ArgumentNullException(nameof(materials));
        }

        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        List<TerrainOutputCatalogValidationIssue> issues =
            new List<TerrainOutputCatalogValidationIssue>();
        foreach (MaterialDefinition material in materials.Definitions)
        {
            TerrainOutputProfile? profile = material.OutputProfile;
            if (!material.IsMineable && profile != null)
            {
                issues.Add(Issue(
                    material,
                    profile,
                    default,
                    TerrainOutputCatalogValidationCodes.UnmineableOutput,
                    "Unmineable terrain cannot have an output profile."));
                continue;
            }

            if (profile == null)
            {
                continue;
            }

            foreach (TerrainOutputEntry entry in profile.Entries)
            {
                if (entry.ItemId == LegacyMetal)
                {
                    issues.Add(Issue(
                        material,
                        profile,
                        entry.ItemId,
                        TerrainOutputCatalogValidationCodes.ForbiddenLegacyMetal,
                        "Terrain output cannot create material.metal; use material.iron."));
                    continue;
                }

                if (!items.Contains(entry.ItemId))
                {
                    issues.Add(Issue(
                        material,
                        profile,
                        entry.ItemId,
                        TerrainOutputCatalogValidationCodes.UnknownItem,
                        "Terrain output references an unknown ItemId."));
                    continue;
                }

                if (entry.MaximumQuantity > items.Get(entry.ItemId).MaximumStackSize)
                {
                    issues.Add(Issue(
                        material,
                        profile,
                        entry.ItemId,
                        TerrainOutputCatalogValidationCodes.StackSizeExceeded,
                        "Terrain output range exceeds the item maximum stack size."));
                }
            }
        }

        return new TerrainOutputCatalogValidationReport(issues);
    }

    private static TerrainOutputCatalogValidationIssue Issue(
        MaterialDefinition material,
        TerrainOutputProfile profile,
        ItemId itemId,
        string code,
        string message)
    {
        return new TerrainOutputCatalogValidationIssue(
            material.Id,
            profile.Id,
            itemId,
            code,
            message);
    }
}

}
