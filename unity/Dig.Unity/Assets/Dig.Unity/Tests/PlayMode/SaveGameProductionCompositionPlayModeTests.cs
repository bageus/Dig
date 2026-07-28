using System;
using System.IO;
using System.Linq;
using Dig.Application.Saving;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.Saving;
using NUnit.Framework;

namespace Dig.Unity.Tests
{

public sealed class SaveGameProductionCompositionPlayModeTests
{
    [Test]
    public void Production_composition_round_trips_spatial_excavation_job()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "dig-save-playmode-" + Guid.NewGuid().ToString("N"));
        try
        {
            MaterialId rockId = new MaterialId("terrain.rock");
            MaterialCatalog materials = new MaterialCatalog(new[]
            {
                new MaterialDefinition(
                    rockId,
                    "Rock",
                    isSolid: true,
                    hardness: 20,
                    isMineable: true,
                    outputProfile: null),
            });
            ItemCatalog items = new ItemCatalog(new[]
            {
                new ItemDefinition(
                    new ItemId("material.stone"),
                    "Stone",
                    maximumStackSize: 20,
                    isTool: false),
            });
            WorldState world = WorldState.CreateFilled(
                new WorldSize(4, 4, 4),
                chunkSize: 2,
                materials,
                rockId).Value;
            JobSystem jobs = new JobSystem();
            EntityId jobId = Id(1);
            Result added = jobs.Add(new SpatialDigJobDefinition(
                jobId,
                new SpatialDigJobTarget(
                    new CellId(2, 2, 2),
                    new CellId(2, 2, 1)),
                priority: 700,
                createdTick: 11,
                JobRetryPolicy.Default));
            Assert.That(added.IsSuccess, Is.True, added.Error?.ToString());

            SaveGameService service = SaveGameCompositionRoot.Create(directory);
            service.Save(new SaveGameContext(
                new SaveMetadataData
                {
                    SlotId = "spatial",
                    DisplayName = "Spatial excavation",
                    SavedAtUtc = "2026-07-29T00:00:00Z",
                    SimulationTick = 11,
                    WorldSeed = 1337,
                    GeneratorVersion = 1,
                },
                world,
                new InventoryState(items),
                jobs,
                new BuildingsState()));

            Result<LoadedGameState> loaded = service.Load(
                "spatial",
                materials,
                items);

            Assert.That(loaded.IsSuccess, Is.True, loaded.Error?.ToString());
            JobSnapshot restored = loaded.Value.Jobs.GetAll().Single();
            Assert.That(restored.Id, Is.EqualTo(jobId));
            Assert.That(restored.Definition, Is.TypeOf<SpatialDigJobDefinition>());
            SpatialDigJobDefinition definition =
                (SpatialDigJobDefinition)restored.Definition;
            Assert.That(definition.Target.TargetCell, Is.EqualTo(new CellId(2, 2, 2)));
            Assert.That(definition.Target.WorkCell, Is.EqualTo(new CellId(2, 2, 1)));
            Assert.That(loaded.Value.World.CreateSnapshot().Size,
                Is.EqualTo(world.CreateSnapshot().Size));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    private static EntityId Id(int value) =>
        EntityId.Parse(value.ToString("x32"));
}

}