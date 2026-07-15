using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Generation;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class WorldGenerationTests
{
    private static readonly MaterialId Rock = new MaterialId("terrain.rock");
    private static readonly MaterialId CrystalRock = new MaterialId("terrain.crystal");
    private static readonly MaterialId IronOre = new MaterialId("deposit.iron");
    private static readonly MaterialId CrystalOre = new MaterialId("deposit.crystal");
    private static readonly MaterialId Air = new MaterialId("terrain.air");

    [Fact]
    public void Same_seed_and_version_produce_the_same_logical_world()
    {
        WorldGenerationProfile profile = CreateProfile();
        MaterialCatalog materials = CreateMaterials();
        WorldGenerator generator = new WorldGenerator();

        Result<GeneratedWorld> first = generator.Generate(
            new WorldGenerationRequest(123456UL, profile, materials));
        Result<GeneratedWorld> second = generator.Generate(
            new WorldGenerationRequest(123456UL, profile, materials));

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value.Metadata.Fingerprint, second.Value.Metadata.Fingerprint);
        Assert.Equal(
            first.Value.Metadata.Zones.Select(zone => zone.Center),
            second.Value.Metadata.Zones.Select(zone => zone.Center));
        Assert.Equal(
            first.Value.Metadata.Connections.Select(edge => (edge.FromZoneIndex, edge.ToZoneIndex)),
            second.Value.Metadata.Connections.Select(edge => (edge.FromZoneIndex, edge.ToZoneIndex)));
        Assert.Equal(
            first.Value.Metadata.StartingResourceCells,
            second.Value.Metadata.StartingResourceCells);
        Assert.Equal(first.Value.Metadata.PointsOfInterest, second.Value.Metadata.PointsOfInterest);
    }

    [Fact]
    public void Different_seeds_change_the_generated_world()
    {
        WorldGenerator generator = new WorldGenerator();
        WorldGenerationProfile profile = CreateProfile();
        MaterialCatalog materials = CreateMaterials();

        GeneratedWorld first = generator.Generate(
            new WorldGenerationRequest(10UL, profile, materials)).Value;
        GeneratedWorld second = generator.Generate(
            new WorldGenerationRequest(11UL, profile, materials)).Value;

        Assert.NotEqual(first.Metadata.Fingerprint, second.Metadata.Fingerprint);
    }

    [Fact]
    public void Generated_world_has_connected_critical_zones_and_starting_resources()
    {
        WorldGenerationProfile profile = CreateProfile();
        GeneratedWorld generated = new WorldGenerator().Generate(
            new WorldGenerationRequest(999UL, profile, CreateMaterials())).Value;

        WorldGenerationValidationReport report = new WorldGenerationValidator().Validate(
            generated,
            profile);

        Assert.True(report.IsValid, string.Join(Environment.NewLine, report.Failures));
        Assert.Equal(profile.MinimumStartingResources, generated.Metadata.StartingResourceCells.Count);
        Assert.Equal(profile.PointOfInterestCount, generated.Metadata.PointsOfInterest.Count);
        Assert.Equal(0, generated.World.Version);
        Assert.Empty(generated.World.PeekDirtyChunks());
        Assert.Empty(generated.World.PeekUncommittedEvents());
    }

    [Fact]
    public void Point_of_interest_settings_do_not_perturb_earlier_generation_stages()
    {
        MaterialCatalog materials = CreateMaterials();
        WorldGenerationProfile onePoint = CreateProfile(pointOfInterestCount: 1);
        WorldGenerationProfile fivePoints = CreateProfile(pointOfInterestCount: 5);
        WorldGenerator generator = new WorldGenerator();

        GeneratedWorld first = generator.Generate(
            new WorldGenerationRequest(777UL, onePoint, materials)).Value;
        GeneratedWorld second = generator.Generate(
            new WorldGenerationRequest(777UL, fivePoints, materials)).Value;

        Assert.Equal(first.Metadata.Fingerprint, second.Metadata.Fingerprint);
        Assert.Equal(
            first.Metadata.Zones.Select(zone => zone.Center),
            second.Metadata.Zones.Select(zone => zone.Center));
        Assert.Equal(first.Metadata.StartingResourceCells, second.Metadata.StartingResourceCells);
        Assert.Single(first.Metadata.PointsOfInterest);
        Assert.Equal(5, second.Metadata.PointsOfInterest.Count);
    }

    [Fact]
    public void Unsupported_generator_version_is_rejected()
    {
        WorldGenerationProfile profile = CreateProfile(
            generatorVersion: WorldGenerator.CurrentGeneratorVersion + 1);

        Result<GeneratedWorld> result = new WorldGenerator().Generate(
            new WorldGenerationRequest(1UL, profile, CreateMaterials()));

        Assert.True(result.IsFailure);
        Assert.Equal(WorldGenerationErrors.UnsupportedGeneratorVersion, result.Error);
    }

    [Fact]
    public void Seed_sweep_produces_valid_playable_worlds()
    {
        WorldGenerationProfile profile = CreateProfile();
        MaterialCatalog materials = CreateMaterials();
        WorldGenerator generator = new WorldGenerator();
        WorldGenerationValidator validator = new WorldGenerationValidator();

        for (ulong seed = 0; seed < 128; seed++)
        {
            Result<GeneratedWorld> result = generator.Generate(
                new WorldGenerationRequest(seed, profile, materials));

            Assert.True(result.IsSuccess, $"Generation failed for seed {seed}.");
            WorldGenerationValidationReport report = validator.Validate(result.Value, profile);
            Assert.True(
                report.IsValid,
                $"Seed {seed}: {string.Join("; ", report.Failures)}");
        }
    }

    [Fact]
    public void World_changes_can_be_stored_and_reapplied_over_the_generated_base()
    {
        WorldGenerationProfile profile = CreateProfile();
        MaterialCatalog materials = CreateMaterials();
        WorldGenerator generator = new WorldGenerator();
        GeneratedWorld generatedBase = generator.Generate(
            new WorldGenerationRequest(4242UL, profile, materials)).Value;
        GeneratedWorld changed = generator.Generate(
            new WorldGenerationRequest(4242UL, profile, materials)).Value;
        CellId target = changed.Metadata.StartingResourceCells[0];
        Assert.True(changed.World.SetDigDesignation(target, designated: true, tick: 12).IsSuccess);

        Result<IReadOnlyList<WorldCellOverride>> captured = WorldGenerationOverlay.Capture(
            generatedBase.World,
            changed.World);
        GeneratedWorld restored = generator.Generate(
            new WorldGenerationRequest(4242UL, profile, materials)).Value;
        Result<WorldMutationResult> applied = WorldGenerationOverlay.Apply(
            restored.World,
            captured.Value,
            tick: 12);

        Assert.True(captured.IsSuccess);
        Assert.Single(captured.Value);
        Assert.True(applied.IsSuccess);
        Assert.Equal(
            WorldGenerationFingerprint.Compute(changed.World, 4242UL, profile.GeneratorVersion, profile.Id),
            WorldGenerationFingerprint.Compute(restored.World, 4242UL, profile.GeneratorVersion, profile.Id));
    }

    private static MaterialCatalog CreateMaterials()
    {
        return new MaterialCatalog(new[]
        {
            new MaterialDefinition(Air, isSolid: false, hardness: 0),
            new MaterialDefinition(CrystalOre, isSolid: true, hardness: 170),
            new MaterialDefinition(CrystalRock, isSolid: true, hardness: 130),
            new MaterialDefinition(IronOre, isSolid: true, hardness: 140),
            new MaterialDefinition(Rock, isSolid: true, hardness: 100),
        });
    }

    private static WorldGenerationProfile CreateProfile(
        int generatorVersion = WorldGenerator.CurrentGeneratorVersion,
        int pointOfInterestCount = 3)
    {
        return new WorldGenerationProfile(
            "test.cavern",
            generatorVersion,
            new WorldSize(48, 32),
            8,
            Air,
            new[]
            {
                new WorldGenerationBiomeDefinition("stone", Rock, IronOre),
                new WorldGenerationBiomeDefinition("crystal", CrystalRock, CrystalOre),
            },
            zoneCount: 6,
            startRoomRadius: 3,
            zoneRoomRadius: 2,
            corridorHalfWidth: 0,
            minimumStartingResources: 6,
            pointOfInterestCount: pointOfInterestCount,
            layerCount: 3);
    }
}

}
