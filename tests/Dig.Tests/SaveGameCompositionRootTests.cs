using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Saving;
using Dig.Domain.Core;
using Dig.Domain.Factions;
using Dig.Domain.Jobs;
using Dig.Domain.Strategy;
using Dig.Domain.World;
using Dig.Infrastructure.Saving;
using Xunit;

namespace Dig.Tests
{

public sealed class SaveGameCompositionRootTests
{
    [Fact]
    public void Production_registry_covers_every_concrete_job_definition()
    {
        JobDefinitionSaveRegistry registry =
            SaveGameCompositionRoot.CreateJobDefinitionRegistry();
        Type[] concreteDefinitions = typeof(JobDefinition).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract
                && typeof(JobDefinition).IsAssignableFrom(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        registry.ValidateCoverage(concreteDefinitions);
        Assert.Equal(concreteDefinitions.Length, registry.RegisteredTypeIds.Count);
    }

    [Fact]
    public void Production_migration_pipeline_reaches_current_version_from_v0()
    {
        SaveGameDocument document = new SaveGameDocument
        {
            FormatVersion = 0,
            Metadata = new SaveMetadataData
            {
                SlotId = "legacy",
                DisplayName = string.Empty,
                SavedAtUtc = "2026-07-29T00:00:00Z",
                GeneratorVersion = 0,
            },
        };

        Result<SaveMigrationReport> result =
            SaveGameCompositionRoot.CreateMigrationPipeline().Apply(document);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(SaveFormat.CurrentVersion, document.FormatVersion);
        Assert.Equal(SaveFormat.CurrentVersion, result.Value.AppliedSteps.Count);
    }

    [Fact]
    public void Previously_unregistered_job_definitions_round_trip()
    {
        JobDefinitionSaveRegistry registry =
            SaveGameCompositionRoot.CreateJobDefinitionRegistry();
        JobDefinition[] jobs =
        {
            new BuildingWorkJobDefinition(
                Id(1),
                Id(2),
                BuildingWorkKind.Repair,
                new CellId(3, 4, 1),
                priority: 500,
                createdTick: 7,
                JobRetryPolicy.Default,
                new[] { Id(99) }),
            new HealingJobDefinition(
                Id(3),
                Id(4),
                new CellId(5, 6, 0),
                healthRestored: 250,
                priority: 600,
                createdTick: 8,
                JobRetryPolicy.Default),
            new SpatialDigJobDefinition(
                Id(5),
                new SpatialDigJobTarget(
                    new CellId(7, 7, 2),
                    new CellId(7, 7, 1)),
                priority: 700,
                createdTick: 9,
                JobRetryPolicy.Default),
            new StrategicExecutionJobDefinition(
                Id(6),
                new StrategicExecutionPlanId("plan.attack"),
                new FactionId("faction.alpha"),
                StrategicGoalKind.Attack,
                targetCell: null,
                targetFactionId: new FactionId("faction.beta"),
                priority: 800,
                createdTick: 10,
                JobRetryPolicy.Default),
            new ProductionPackageUseJobDefinition(
                Id(7),
                Id(8),
                new CellId(9, 4, 0),
                new CellId(8, 4, 0),
                packageVersion: 3,
                priority: 850,
                createdTick: 11,
                JobRetryPolicy.Default),
        };

        foreach (JobDefinition job in jobs)
        {
            JobDefinition restored = registry.Decode(registry.Encode(job));
            Assert.Equal(job.GetType(), restored.GetType());
            Assert.Equal(job.Id, restored.Id);
            Assert.Equal(job.Description, restored.Description);
            Assert.Equal(job.Dependencies, restored.Dependencies);
            Assert.Equal(job.CreateReservationKeys(), restored.CreateReservationKeys());
        }
    }

    private static EntityId Id(int value) =>
        EntityId.Parse(value.ToString("x32"));
}

}