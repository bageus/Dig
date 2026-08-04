using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Runtime;
using Dig.Domain.World;

namespace Dig.Domain.Generation
{

public sealed class WorldGenerator
{
    public const int CurrentGeneratorVersion = 1;

    private readonly WorldGenerationValidator _validator;

    public WorldGenerator()
        : this(new WorldGenerationValidator())
    {
    }

    public WorldGenerator(WorldGenerationValidator validator)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public Result<GeneratedWorld> Generate(WorldGenerationRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        WorldGenerationProfile profile = request.Profile;
        if (profile.GeneratorVersion != CurrentGeneratorVersion)
        {
            return Result<GeneratedWorld>.Failure(
                WorldGenerationErrors.UnsupportedGeneratorVersion);
        }

        Result materialValidation = ValidateMaterials(profile, request.Materials);
        if (materialValidation.IsFailure)
        {
            return Result<GeneratedWorld>.Failure(materialValidation.Error!);
        }

        RandomStreamCatalog streams = new RandomStreamCatalog(request.WorldSeed);
        string streamPrefix = $"generation.v{profile.GeneratorVersion}.{profile.Id}";
        IReadOnlyList<GeneratedZonePlan> zonePlans = WorldGenerationLayout.CreateZones(
            profile,
            streams.GetOrCreate(streamPrefix + ".start"),
            streams.GetOrCreate(streamPrefix + ".zones"),
            streams.GetOrCreate(streamPrefix + ".biomes"));
        GenerationCellBuffer buffer = WorldGenerationLayout.CreateSolidBuffer(profile, zonePlans);

        IReadOnlyList<GeneratedZoneConnection> connections = WorldGenerationLayout.CarveConnectedZones(
            buffer,
            profile,
            zonePlans,
            streams.GetOrCreate(streamPrefix + ".connections"));

        IReadOnlyList<CellId> startingResources = WorldGenerationLayout.PlaceResources(
            buffer,
            profile,
            zonePlans,
            streams.GetOrCreate(streamPrefix + ".resources"));
        PropagateFrontUnmineableColumns(buffer, request.Materials);
        IReadOnlyList<CellId> pointsOfInterest = WorldGenerationLayout.SelectPointsOfInterest(
            buffer,
            profile,
            zonePlans,
            streams.GetOrCreate(streamPrefix + ".points-of-interest"));

        Result<WorldState> worldResult = WorldState.CreateGenerated(
            profile.Size,
            profile.ChunkSize,
            request.Materials,
            buffer.Cells);
        if (worldResult.IsFailure)
        {
            return Result<GeneratedWorld>.Failure(worldResult.Error!);
        }

        WorldState world = worldResult.Value;
        PopulateTerrainDeposits(world, request);
        ulong fingerprint = WorldGenerationFingerprint.Compute(
            world,
            request.WorldSeed,
            profile.GeneratorVersion,
            profile.Id);
        GeneratedWorldMetadata metadata = new GeneratedWorldMetadata(
            request.WorldSeed,
            profile.GeneratorVersion,
            profile.Id,
            zonePlans[0].Center,
            CreateMetadataZones(zonePlans),
            connections,
            startingResources,
            pointsOfInterest,
            fingerprint);
        GeneratedWorld generated = new GeneratedWorld(world, metadata);
        WorldGenerationValidationReport report = _validator.Validate(generated, profile);
        if (!report.IsValid)
        {
            return Result<GeneratedWorld>.Failure(
                WorldGenerationErrors.InvalidGeneratedWorld);
        }

        return Result<GeneratedWorld>.Success(generated);
    }

    private static void PropagateFrontUnmineableColumns(
        GenerationCellBuffer buffer,
        MaterialCatalog materials)
    {
        for (int y = 0; y < buffer.Size.Height; y++)
        {
            for (int x = 0; x < buffer.Size.Width; x++)
            {
                CellId frontCell = new CellId(x, y, CellId.MinimumDepth);
                CellState front = buffer.Get(frontCell);
                MaterialDefinition? material = materials.Get(front.MaterialId);
                if (material == null || !material.IsSolid || material.IsMineable)
                {
                    continue;
                }

                for (int z = CellId.MinimumDepth + 1; z < buffer.Size.Depth; z++)
                {
                    CellId deepCell = new CellId(x, y, z);
                    buffer.Set(
                        deepCell,
                        buffer.Get(deepCell).WithTerrain(front.MaterialId));
                }
            }
        }
    }

    private static void PopulateTerrainDeposits(
        WorldState world,
        WorldGenerationRequest request)
    {
        if (request.TerrainDepositCatalog is null
            || request.TerrainDepositSettings is null)
        {
            return;
        }

        TerrainDepositHostCell[] hosts = world.CreateSnapshot().Chunks
            .SelectMany(chunk => chunk.Cells)
            .Where(cell => cell.IsSolid)
            .Select(cell => new TerrainDepositHostCell(
                cell.Id,
                request.Materials.Get(cell.State.MaterialId)!))
            .Where(host => host.Material.IsMineable)
            .OrderBy(host => host.Cell)
            .ToArray();
        TerrainDepositGenerationResult generated =
            new TerrainDepositGenerator().Generate(
                world.Size,
                hosts,
                request.TerrainDepositCatalog,
                request.TerrainDepositSettings);
        world.ReplaceTerrainDeposits(
            generated.Deposits,
            generated.AlgorithmVersion);
    }

    private static Result ValidateMaterials(
        WorldGenerationProfile profile,
        MaterialCatalog materials)
    {
        MaterialDefinition? empty = materials.Get(profile.EmptyMaterialId);
        if (empty is null)
        {
            return Result.Failure(WorldGenerationErrors.UnknownMaterial);
        }

        if (empty.IsSolid)
        {
            return Result.Failure(WorldGenerationErrors.InvalidMaterialRole);
        }

        foreach (WorldGenerationBiomeDefinition biome in profile.Biomes)
        {
            MaterialDefinition? solid = materials.Get(biome.SolidMaterialId);
            MaterialDefinition? resource = materials.Get(biome.ResourceMaterialId);
            if (solid is null || resource is null)
            {
                return Result.Failure(WorldGenerationErrors.UnknownMaterial);
            }

            if (!solid.IsSolid || !resource.IsSolid)
            {
                return Result.Failure(WorldGenerationErrors.InvalidMaterialRole);
            }
        }

        return Result.Success();
    }

    private static IReadOnlyList<GeneratedZone> CreateMetadataZones(
        IReadOnlyList<GeneratedZonePlan> plans)
    {
        List<GeneratedZone> zones = new List<GeneratedZone>(plans.Count);
        foreach (GeneratedZonePlan plan in plans)
        {
            zones.Add(new GeneratedZone(
                plan.Index,
                plan.Center,
                plan.Biome.Id,
                plan.LayerIndex));
        }

        return zones;
    }
}

}
