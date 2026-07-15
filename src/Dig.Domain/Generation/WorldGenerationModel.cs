using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Generation
{

public sealed class WorldGenerationBiomeDefinition
{
    public WorldGenerationBiomeDefinition(
        string id,
        MaterialId solidMaterialId,
        MaterialId resourceMaterialId)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Biome id is required.", nameof(id));
        }

        if (solidMaterialId.IsEmpty)
        {
            throw new ArgumentException(
                "Biome solid material is required.",
                nameof(solidMaterialId));
        }

        if (resourceMaterialId.IsEmpty)
        {
            throw new ArgumentException(
                "Biome resource material is required.",
                nameof(resourceMaterialId));
        }

        Id = id.Trim();
        SolidMaterialId = solidMaterialId;
        ResourceMaterialId = resourceMaterialId;
    }

    public string Id { get; }

    public MaterialId SolidMaterialId { get; }

    public MaterialId ResourceMaterialId { get; }
}

public sealed class WorldGenerationProfile
{
    private readonly IReadOnlyList<WorldGenerationBiomeDefinition> _biomes;

    public WorldGenerationProfile(
        string id,
        int generatorVersion,
        WorldSize size,
        int chunkSize,
        MaterialId emptyMaterialId,
        IEnumerable<WorldGenerationBiomeDefinition> biomes,
        int zoneCount = 5,
        int startRoomRadius = 3,
        int zoneRoomRadius = 2,
        int corridorHalfWidth = 0,
        int minimumStartingResources = 6,
        int pointOfInterestCount = 3,
        int layerCount = 3)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Generation profile id is required.", nameof(id));
        }

        if (generatorVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(generatorVersion));
        }

        if (chunkSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkSize));
        }

        if (emptyMaterialId.IsEmpty)
        {
            throw new ArgumentException(
                "Empty material id is required.",
                nameof(emptyMaterialId));
        }

        if (biomes is null)
        {
            throw new ArgumentNullException(nameof(biomes));
        }

        WorldGenerationBiomeDefinition[] orderedBiomes = biomes.ToArray();
        if (orderedBiomes.Length == 0)
        {
            throw new ArgumentException(
                "At least one biome definition is required.",
                nameof(biomes));
        }

        HashSet<string> biomeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldGenerationBiomeDefinition biome in orderedBiomes)
        {
            if (biome is null)
            {
                throw new ArgumentException(
                    "Biome definitions cannot contain null values.",
                    nameof(biomes));
            }

            if (!biomeIds.Add(biome.Id))
            {
                throw new ArgumentException(
                    $"Duplicate biome id '{biome.Id}'.",
                    nameof(biomes));
            }
        }

        if (zoneCount < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(zoneCount));
        }

        if (startRoomRadius <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startRoomRadius));
        }

        if (zoneRoomRadius <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(zoneRoomRadius));
        }

        if (corridorHalfWidth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(corridorHalfWidth));
        }

        if (minimumStartingResources <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumStartingResources));
        }

        if (pointOfInterestCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pointOfInterestCount));
        }

        if (layerCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(layerCount));
        }

        int requiredMargin = Math.Max(startRoomRadius, zoneRoomRadius) + 3;
        if (size.Width < (requiredMargin * 2) + zoneCount
            || size.Height < (requiredMargin * 2) + 1)
        {
            throw new ArgumentException(
                "World size is too small for the configured rooms and zones.",
                nameof(size));
        }

        Id = id.Trim();
        GeneratorVersion = generatorVersion;
        Size = size;
        ChunkSize = chunkSize;
        EmptyMaterialId = emptyMaterialId;
        _biomes = new ReadOnlyCollection<WorldGenerationBiomeDefinition>(orderedBiomes);
        ZoneCount = zoneCount;
        StartRoomRadius = startRoomRadius;
        ZoneRoomRadius = zoneRoomRadius;
        CorridorHalfWidth = corridorHalfWidth;
        MinimumStartingResources = minimumStartingResources;
        PointOfInterestCount = pointOfInterestCount;
        LayerCount = layerCount;
    }

    public string Id { get; }

    public int GeneratorVersion { get; }

    public WorldSize Size { get; }

    public int ChunkSize { get; }

    public MaterialId EmptyMaterialId { get; }

    public IReadOnlyList<WorldGenerationBiomeDefinition> Biomes => _biomes;

    public int ZoneCount { get; }

    public int StartRoomRadius { get; }

    public int ZoneRoomRadius { get; }

    public int CorridorHalfWidth { get; }

    public int MinimumStartingResources { get; }

    public int PointOfInterestCount { get; }

    public int LayerCount { get; }
}

public sealed class WorldGenerationRequest
{
    public WorldGenerationRequest(
        ulong worldSeed,
        WorldGenerationProfile profile,
        MaterialCatalog materials)
    {
        WorldSeed = worldSeed;
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        Materials = materials ?? throw new ArgumentNullException(nameof(materials));
    }

    public ulong WorldSeed { get; }

    public WorldGenerationProfile Profile { get; }

    public MaterialCatalog Materials { get; }
}

}
