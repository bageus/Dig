using System;
using Dig.Application.Saving;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class LivingMaterialEcologySaveTests
{
    [Fact]
    public void AdapterRoundTripPreservesDeterministicLifecycleAndInventoryLink()
    {
        InventoryState inventory = CreateInventory();
        EntityId hamster = Id(1);
        CellId cell = new CellId(4, 2, 0);
        Assert.True(inventory.AddUnit(
            hamster,
            LivingMaterialEcologyProfiles.HamsterItemId,
            ItemLocation.InWorld(cell),
            0).IsSuccess);
        LivingMaterialEcologyState ecology = new LivingMaterialEcologyState(7788);
        LivingMaterialPlaneKey plane = new LivingMaterialPlaneKey(new CellId(1, 2, 0));
        Assert.True(ecology.Register(
            hamster,
            hamster,
            LivingMaterialSpecies.Hamster,
            cell,
            plane,
            0).IsSuccess);
        for (int step = 0; step < 11; step++)
        {
            Assert.True(ecology.AdvanceOneEcologyStep(step).IsSuccess);
        }

        LivingMaterialEcologySaveData data = LivingMaterialEcologySaveAdapter.Encode(ecology);
        Result<LivingMaterialEcologyState> restored =
            LivingMaterialEcologySaveAdapter.Decode(data, inventory, 999);

        Assert.True(restored.IsSuccess, restored.Error?.ToString());
        LivingMaterialSnapshot before = ecology.Get(hamster)!;
        LivingMaterialSnapshot after = restored.Value.Get(hamster)!;
        Assert.Equal(before.ItemEntityId, after.ItemEntityId);
        Assert.Equal(before.Cell, after.Cell);
        Assert.Equal(before.MovementCredit, after.MovementCredit);
        Assert.Equal(before.DeterministicSequence, after.DeterministicSequence);
        Assert.Equal(ecology.EcologyStep, restored.Value.EcologyStep);
        Assert.Equal(ecology.WorldSeed, restored.Value.WorldSeed);
        Assert.Equal(1, data.TimingCadenceVersion);
    }

    [Fact]
    public void LegacyCooldownIsMigratedOnceToUnifiedGameTime()
    {
        InventoryState inventory = CreateInventory();
        EntityId grub = Id(3);
        CellId cell = new CellId(4, 2, 0);
        Assert.True(inventory.AddUnit(
            grub,
            LivingMaterialEcologyProfiles.GrubItemId,
            ItemLocation.InWorld(cell),
            0).IsSuccess);
        LivingMaterialEcologySaveData legacy = new LivingMaterialEcologySaveData
        {
            EcologyStep = 48,
            Creatures =
            {
                new LivingMaterialIndividualSaveData
                {
                    CreatureId = grub.ToString(),
                    ItemEntityId = grub.ToString(),
                    Species = (int)LivingMaterialSpecies.Grub,
                    Containment = (int)LivingMaterialContainment.Free,
                    HasCell = true,
                    CellX = cell.X,
                    CellY = cell.Y,
                    CellZ = cell.Z,
                    AnchorX = cell.X,
                    AnchorY = cell.Y,
                    AnchorZ = cell.Z,
                    PlaneRootX = cell.X,
                    PlaneRootY = cell.Y,
                    PlaneRootZ = cell.Z,
                    Direction = 1,
                    Activity = (int)LivingMaterialActivity.Moving,
                    NextSearchAtStep = int.MaxValue,
                    NextSleepAtStep = int.MaxValue,
                    NextReproductionStep = 96,
                },
            },
        };

        Result<LivingMaterialEcologyState> restored =
            LivingMaterialEcologySaveAdapter.Decode(legacy, inventory, 1);

        Assert.True(restored.IsSuccess, restored.Error?.ToString());
        Assert.Equal(
            48 + (LivingMaterialEcologyProfiles.EcologyStepsPerDay / 2),
            restored.Value.Get(grub)!.NextReproductionStep);
    }

    [Fact]
    public void AdapterRejectsCreatureWithoutQuantityOneInventoryIdentity()
    {
        InventoryState inventory = CreateInventory();
        LivingMaterialEcologySaveData data = new LivingMaterialEcologySaveData
        {
            WorldSeed = 1,
            Creatures =
            {
                new LivingMaterialIndividualSaveData
                {
                    CreatureId = Id(2).ToString(),
                    ItemEntityId = Id(2).ToString(),
                    Species = (int)LivingMaterialSpecies.Grub,
                    Containment = (int)LivingMaterialContainment.Stored,
                    AnchorX = 1,
                    AnchorY = 1,
                    PlaneRootX = 1,
                    PlaneRootY = 1,
                    Direction = 1,
                    Activity = (int)LivingMaterialActivity.Stored,
                    NextSearchAtStep = int.MaxValue,
                    NextSleepAtStep = int.MaxValue,
                    NextReproductionStep = 96,
                },
            },
        };

        Result<LivingMaterialEcologyState> restored =
            LivingMaterialEcologySaveAdapter.Decode(data, inventory, 1);

        Assert.True(restored.IsFailure);
        Assert.Equal(LivingMaterialErrors.InvalidSnapshot, restored.Error);
    }

    [Fact]
    public void VersionElevenMigrationAddsEmptyLivingMaterialSection()
    {
        SaveGameDocument document = new SaveGameDocument
        {
            FormatVersion = 11,
            Metadata = new SaveMetadataData { WorldSeed = 4455 },
            LivingMaterials = null!,
        };

        new SaveVersionElevenLivingMaterialsMigration().Apply(document);

        Assert.Equal(12, document.FormatVersion);
        Assert.NotNull(document.LivingMaterials);
        Assert.Equal((ulong)4455, document.LivingMaterials.WorldSeed);
        Assert.Empty(document.LivingMaterials.Creatures);
    }

    private static InventoryState CreateInventory() =>
        new InventoryState(new ItemCatalog(LivingMaterialContent.CreateItems()));

    private static EntityId Id(int suffix) => EntityId.Parse(
        "4000000000000000000000000000" + suffix.ToString("D4"));
}

}
