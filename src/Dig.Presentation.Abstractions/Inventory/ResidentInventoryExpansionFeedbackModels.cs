using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Presentation.Inventory
{

public sealed class ResidentInventoryExpansionFeedbackViewModel
{
    public ResidentInventoryExpansionFeedbackViewModel(
        string residentId,
        string stackId,
        string itemId,
        string displayName,
        InventoryExpansionGroup group,
        int tier,
        int addedSlots,
        IEnumerable<string> acceptedCategories,
        double moveSpeedMultiplierWhenOccupied,
        string visualAttachmentId,
        bool isActive,
        int occupiedSlotCount)
    {
        if (string.IsNullOrWhiteSpace(residentId)
            || string.IsNullOrWhiteSpace(stackId)
            || string.IsNullOrWhiteSpace(itemId)
            || string.IsNullOrWhiteSpace(displayName)
            || string.IsNullOrWhiteSpace(visualAttachmentId))
        {
            throw new ArgumentException("Expansion feedback identifiers are required.");
        }

        if (tier <= 0
            || addedSlots <= 0
            || occupiedSlotCount < 0
            || moveSpeedMultiplierWhenOccupied <= 0d
            || moveSpeedMultiplierWhenOccupied > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(tier));
        }

        ResidentId = residentId.Trim();
        StackId = stackId.Trim();
        ItemId = itemId.Trim();
        DisplayName = displayName.Trim();
        Group = group;
        Tier = tier;
        AddedSlots = addedSlots;
        AcceptedCategories = new ReadOnlyCollection<string>(
            (acceptedCategories ?? throw new ArgumentNullException(nameof(acceptedCategories)))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray());
        MoveSpeedMultiplierWhenOccupied = moveSpeedMultiplierWhenOccupied;
        VisualAttachmentId = visualAttachmentId.Trim();
        IsActive = isActive;
        OccupiedSlotCount = occupiedSlotCount;
    }

    public string ResidentId { get; }
    public string StackId { get; }
    public string ItemId { get; }
    public string DisplayName { get; }
    public InventoryExpansionGroup Group { get; }
    public int Tier { get; }
    public int AddedSlots { get; }
    public IReadOnlyList<string> AcceptedCategories { get; }
    public double MoveSpeedMultiplierWhenOccupied { get; }
    public string VisualAttachmentId { get; }
    public bool IsActive { get; }
    public int OccupiedSlotCount { get; }
    public int SpeedPenaltyPercent => checked((int)Math.Round(
        (1d - MoveSpeedMultiplierWhenOccupied) * 100d,
        MidpointRounding.AwayFromZero));
    public bool RequiresSpillConfirmation => IsActive && OccupiedSlotCount > 0;
}

public sealed class ResidentInventoryExpansionFeedbackPresenter
{
    public ResidentInventoryExpansionFeedbackViewModel? Present(
        InventoryState inventory,
        EntityId residentId,
        EntityId stackId)
    {
        if (inventory is null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        ItemStackSnapshot? stack = inventory.GetStack(stackId);
        if (stack is null
            || stack.Location.Kind != ItemLocationKind.AgentInventory
            || !stack.Location.HasOwner
            || stack.Location.OwnerId != residentId)
        {
            return null;
        }

        ItemDefinition item = inventory.Catalog.Get(stack.ItemId);
        InventoryExpansionDefinition? expansion = item.InventoryExpansion;
        if (expansion is null)
        {
            return null;
        }

        ResidentInventoryLayoutSnapshot layout =
            inventory.GetResidentInventoryLayout(residentId);
        ActiveInventoryExpansionSnapshot? active = expansion.Group switch
        {
            InventoryExpansionGroup.Cargo => layout.ActiveCargoExpansion,
            InventoryExpansionGroup.Weapon => layout.ActiveWeaponExpansion,
            _ => null,
        };
        ResidentInventoryCompartment compartment = expansion.Group
            == InventoryExpansionGroup.Cargo
                ? ResidentInventoryCompartment.Cargo
                : ResidentInventoryCompartment.Weapon;
        int occupied = layout.Slots.Count(slot =>
            slot.Slot.Compartment == compartment && !slot.IsEmpty);
        return new ResidentInventoryExpansionFeedbackViewModel(
            residentId.ToString(),
            stack.StackId.ToString(),
            stack.ItemId.ToString(),
            item.DisplayName,
            expansion.Group,
            expansion.Tier,
            expansion.AddedSlots,
            expansion.AcceptedCategories.Select(value => value.ToString()),
            expansion.MoveSpeedMultiplierWhenOccupied,
            expansion.VisualAttachmentId,
            active.HasValue && active.Value.StackId == stack.StackId,
            occupied);
    }
}

}
