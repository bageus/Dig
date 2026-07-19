using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentInventoryMigrationTests
{
    private static readonly EntityId ResidentId = Id(1);
    private static readonly ItemId OreId = new ItemId("ore.iron");
    private static readonly ItemId ToolId = new ItemId("tool.pickaxe");
    private static readonly ItemId BasketId = new ItemId("inventory.basket");
    private static readonly ItemId ScabbardId = new ItemId("inventory.scabbard");

    [Fact]
    public void Legacy_stacks_are_slotted_and_overflow_spills_without_quantity_loss()
    {
        InventoryState inventory = CreateInventory();
        Add(inventory, Id(10), BasketId, ItemLocation.InAgent(ResidentId));
        Add(inventory, Id(11), ScabbardId, ItemLocation.InAgent(ResidentId));
        Add(inventory, Id(12), ToolId, ItemLocation.EquippedBy(ResidentId));
        for (int index = 0; index < 9; index++)
        {
            Add(inventory, Id(20 + index), OreId, ItemLocation.InAgent(ResidentId));
        }

        int totalBefore = inventory.CreateSnapshot().Stacks.Sum(stack => stack.Quantity);
        CellId residentCell = new CellId(7, 8);
        var migrated = inventory.MigrateLegacyResidentInventory(
            ResidentId,
            residentCell,
            tick: 3);

        Assert.True(migrated.IsSuccess, migrated.Error?.ToString());
        ResidentInventoryMigrationReport report = migrated.Value;
        Assert.Equal(10, report.SlottedStackCount);
        Assert.Equal(2, report.SpilledStackIds.Count);
        Assert.Equal(Id(12), report.RestoredHeldStackId);
        Assert.Equal(totalBefore,
            inventory.CreateSnapshot().Stacks.Sum(stack => stack.Quantity));
        Assert.All(report.SpilledStackIds, stackId =>
            Assert.Equal(
                ItemLocation.InWorld(residentCell),
                inventory.GetStack(stackId)!.Location));
        Assert.Equal(
            ResidentInventoryCompartment.Main,
            inventory.GetStack(Id(12))!.Location.ResidentCompartment);
        Assert.Equal(1, inventory.GetStack(Id(12))!.HeldQuantity);
        Assert.Equal(Id(12), inventory.GetHeldItem(ResidentId)!.Value.StackId);
        Assert.DoesNotContain(
            inventory.CreateSnapshot().Stacks,
            stack => stack.Location.Kind == ItemLocationKind.AgentInventory
                && stack.Location.HasOwner
                && stack.Location.OwnerId == ResidentId
                && !stack.Location.HasResidentSlot);
    }

    [Fact]
    public void Migration_report_is_deterministic_for_same_legacy_state()
    {
        InventoryState first = CreateLegacyState();
        InventoryState second = CreateLegacyState();

        var left = first.MigrateLegacyResidentInventory(
            ResidentId,
            new CellId(2, 2),
            tick: 1);
        var right = second.MigrateLegacyResidentInventory(
            ResidentId,
            new CellId(2, 2),
            tick: 1);

        Assert.True(left.IsSuccess, left.Error?.ToString());
        Assert.True(right.IsSuccess, right.Error?.ToString());
        Assert.Equal(left.Value.SlottedStackCount, right.Value.SlottedStackCount);
        Assert.Equal(left.Value.SpilledStackIds, right.Value.SpilledStackIds);
        Assert.Equal(
            first.CreateSnapshot().Stacks.Select(Format).ToArray(),
            second.CreateSnapshot().Stacks.Select(Format).ToArray());
    }

    [Fact]
    public void Reserved_legacy_stack_blocks_migration_before_mutation()
    {
        InventoryState inventory = CreateLegacyState();
        EntityId reservedStackId = Id(20);
        Assert.True(inventory.ReserveQuantity(
            reservedStackId,
            Id(99),
            1,
            tick: 1).IsSuccess);
        InventorySnapshot before = inventory.CreateSnapshot();

        var migrated = inventory.MigrateLegacyResidentInventory(
            ResidentId,
            new CellId(1, 1),
            tick: 2);

        Assert.Equal(InventoryErrors.ResidentInventoryLayoutInvalid, migrated.Error);
        Assert.Equal(before.Version, inventory.Version);
        Assert.Equal(
            before.Stacks.Select(Format).ToArray(),
            inventory.CreateSnapshot().Stacks.Select(Format).ToArray());
    }

    private static InventoryState CreateLegacyState()
    {
        InventoryState inventory = CreateInventory();
        Add(inventory, Id(10), BasketId, ItemLocation.InAgent(ResidentId));
        Add(inventory, Id(11), ScabbardId, ItemLocation.InAgent(ResidentId));
        Add(inventory, Id(12), ToolId, ItemLocation.EquippedBy(ResidentId));
        for (int index = 0; index < 9; index++)
        {
            Add(inventory, Id(20 + index), OreId, ItemLocation.InAgent(ResidentId));
        }

        return inventory;
    }

    private static InventoryState CreateInventory()
    {
        ItemCategoryId raw = new ItemCategoryId("raw");
        ItemCategoryId weapon = new ItemCategoryId("weapon");
        return new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(OreId, "Ore", 100, false, new[] { raw }),
            new ItemDefinition(ToolId, "Pickaxe", 1, true, new[] { weapon }),
            Expansion(
                BasketId,
                "Basket",
                InventoryExpansionGroup.Cargo,
                4,
                new[] { raw }),
            Expansion(
                ScabbardId,
                "Scabbard",
                InventoryExpansionGroup.Weapon,
                2,
                new[] { weapon }),
        }));
    }

    private static ItemDefinition Expansion(
        ItemId itemId,
        string name,
        InventoryExpansionGroup group,
        int slots,
        ItemCategoryId[] accepted)
    {
        return new ItemDefinition(
            itemId,
            name,
            1,
            false,
            accepted,
            new InventoryExpansionDefinition(
                group,
                tier: 1,
                addedSlots: slots,
                acceptedCategories: accepted,
                moveSpeedMultiplierWhenOccupied:
                    group == InventoryExpansionGroup.Cargo ? 0.75d : 1d,
                visualAttachmentId: $"visual.{itemId}"));
    }

    private static void Add(
        InventoryState inventory,
        EntityId stackId,
        ItemId itemId,
        ItemLocation location)
    {
        Assert.True(inventory.AddStack(
            stackId,
            itemId,
            quantity: 1,
            location,
            tick: 0).IsSuccess);
    }

    private static string Format(ItemStackSnapshot stack)
    {
        return $"{stack.StackId}:{stack.ItemId}:{stack.Quantity}:"
            + $"{stack.Location}:{stack.HeldQuantity}:{stack.ReservedQuantity}";
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}