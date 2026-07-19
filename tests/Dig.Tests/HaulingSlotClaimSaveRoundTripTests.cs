using System;
using System.Linq;
using Dig.Application.Saving;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.Saving;
using Xunit;

namespace Dig.Tests
{

public sealed class HaulingSlotClaimSaveRoundTripTests
{
    private static readonly MaterialId RockId = new MaterialId("terrain.rock");
    private static readonly ItemId OreId = new ItemId("ore.iron");
    private static readonly ItemId BasketId = new ItemId("inventory.basket");
    private static readonly EntityId ResidentId = Id(1);
    private static readonly EntityId SourceStackId = Id(2);
    private static readonly EntityId BasketStackId = Id(3);
    private static readonly EntityId JobId = Id(4);
    private static readonly EntityId StorageId = Id(5);

    [Fact]
    public void Active_hauling_claim_round_trips_and_rebuilds_identical_bytes()
    {
        MaterialCatalog materials = CreateMaterials();
        ItemCatalog items = CreateItems();
        SaveGameContext context = CreateContext(materials, items);
        JobDefinitionSaveRegistry registry = CreateRegistry();
        SaveGameBuilder builder = new SaveGameBuilder(registry);
        DataContractJsonSaveCodec codec = new DataContractJsonSaveCodec();
        byte[] firstBytes = codec.Serialize(builder.Build(context));
        SaveGameDocument decoded = codec.Deserialize(firstBytes);

        Result<LoadedGameState> loaded = new SaveGameLoader(
            new SaveMigrationPipeline(Array.Empty<ISaveMigration>()),
            registry).Load(decoded, materials, items);

        Assert.True(loaded.IsSuccess, loaded.Error?.ToString());
        ResidentInventorySlotClaimSnapshot[] expected = context.Inventory
            .GetResidentSlotClaims(JobId)
            .ToArray();
        Assert.Equal(
            expected,
            loaded.Value.Inventory.GetResidentSlotClaims(JobId).ToArray());
        JobSnapshot restoredJob = loaded.Value.Jobs.Get(JobId)!;
        Assert.Equal(JobStatus.Claimed, restoredJob.Status);
        Assert.Equal(ResidentId, restoredJob.AssignedAgentId);
        Assert.Equal(8, loaded.Value.Inventory.GetStack(SourceStackId)!.ReservedQuantity);

        byte[] secondBytes = codec.Serialize(builder.Build(new SaveGameContext(
            loaded.Value.Metadata,
            loaded.Value.World,
            loaded.Value.Inventory,
            loaded.Value.Jobs)));
        Assert.Equal(firstBytes, secondBytes);
    }

    [Fact]
    public void Claim_for_wrong_resident_is_rejected_as_stale()
    {
        MaterialCatalog materials = CreateMaterials();
        ItemCatalog items = CreateItems();
        JobDefinitionSaveRegistry registry = CreateRegistry();
        SaveGameDocument document = new SaveGameBuilder(registry).Build(
            CreateContext(materials, items));
        document.Inventory.ResidentSlotClaims[0].ResidentId = Id(99).ToString();

        Result<LoadedGameState> loaded = new SaveGameLoader(
            new SaveMigrationPipeline(Array.Empty<ISaveMigration>()),
            registry).Load(document, materials, items);

        Assert.Equal(InventoryErrors.ResidentSlotClaimStale, loaded.Error);
    }

    private static SaveGameContext CreateContext(
        MaterialCatalog materials,
        ItemCatalog items)
    {
        WorldState world = WorldState.CreateFilled(
            new WorldSize(4, 4),
            chunkSize: 2,
            materials,
            RockId,
            explored: true).Value;
        InventoryState inventory = new InventoryState(items);
        Assert.True(inventory.AddStack(
            BasketStackId,
            BasketId,
            1,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                0),
            tick: 0).IsSuccess);
        Assert.True(inventory.AddStack(
            SourceStackId,
            OreId,
            10,
            ItemLocation.InWorld(new CellId(1, 1)),
            tick: 0).IsSuccess);
        Assert.True(inventory.ReserveQuantity(
            SourceStackId,
            JobId,
            8,
            tick: 1).IsSuccess);
        Assert.True(inventory.ReserveResidentSlotCapacity(
            JobId,
            ResidentId,
            OreId,
            8,
            tick: 1).IsSuccess);

        JobSystem jobs = new JobSystem();
        Assert.True(jobs.Add(new HaulJobDefinition(
            JobId,
            SourceStackId,
            OreId,
            8,
            StorageId,
            priority: 500,
            createdTick: 1,
            retryPolicy: JobRetryPolicy.Default)).IsSuccess);
        Assert.True(jobs.MakeAvailable(JobId, tick: 1).IsSuccess);
        Assert.True(jobs.Claim(JobId, ResidentId, tick: 2).IsSuccess);
        return new SaveGameContext(
            new SaveMetadataData
            {
                SlotId = "hauling-claim",
                DisplayName = "Hauling claim",
                SavedAtUtc = "2026-07-19T09:00:00Z",
                SimulationTick = 2,
                WorldSeed = 42,
                GeneratorVersion = 1,
            },
            world,
            inventory,
            jobs);
    }

    private static JobDefinitionSaveRegistry CreateRegistry()
    {
        return new JobDefinitionSaveRegistry(new IJobDefinitionSaveCodec[]
        {
            new HaulJobDefinitionSaveCodec(),
        });
    }

    private static MaterialCatalog CreateMaterials()
    {
        return new MaterialCatalog(new[]
        {
            new MaterialDefinition(RockId, isSolid: true, hardness: 100),
        });
    }

    private static ItemCatalog CreateItems()
    {
        ItemCategoryId raw = new ItemCategoryId("raw");
        return new ItemCatalog(new[]
        {
            new ItemDefinition(OreId, "Ore", 100, false, new[] { raw }),
            new ItemDefinition(
                BasketId,
                "Basket",
                1,
                false,
                new[] { raw },
                new InventoryExpansionDefinition(
                    InventoryExpansionGroup.Cargo,
                    tier: 1,
                    addedSlots: 4,
                    acceptedCategories: new[] { raw },
                    moveSpeedMultiplierWhenOccupied: 0.75d,
                    visualAttachmentId: "visual.basket")),
        });
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}