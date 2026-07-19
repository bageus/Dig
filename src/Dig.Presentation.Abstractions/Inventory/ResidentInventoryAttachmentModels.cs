using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Presentation.Inventory
{

public sealed class ResidentInventoryAttachmentViewModel
{
    public ResidentInventoryAttachmentViewModel(
        string residentId,
        string stackId,
        string itemId,
        InventoryExpansionGroup group,
        int tier,
        string visualAttachmentId)
    {
        if (string.IsNullOrWhiteSpace(residentId)
            || string.IsNullOrWhiteSpace(stackId)
            || string.IsNullOrWhiteSpace(itemId)
            || string.IsNullOrWhiteSpace(visualAttachmentId))
        {
            throw new ArgumentException("Inventory attachment identifiers are required.");
        }

        if (tier <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tier));
        }

        ResidentId = residentId.Trim();
        StackId = stackId.Trim();
        ItemId = itemId.Trim();
        Group = group;
        Tier = tier;
        VisualAttachmentId = visualAttachmentId.Trim();
    }

    public string ResidentId { get; }
    public string StackId { get; }
    public string ItemId { get; }
    public InventoryExpansionGroup Group { get; }
    public int Tier { get; }
    public string VisualAttachmentId { get; }
}

public sealed class ResidentInventoryAttachmentPresenter
{
    public IReadOnlyList<ResidentInventoryAttachmentViewModel> Present(
        InventoryState inventory)
    {
        if (inventory is null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        EntityId[] residents = inventory.CreateSnapshot().Stacks
            .Where(stack => stack.Location.Kind == ItemLocationKind.AgentInventory)
            .Where(stack => stack.Location.HasOwner)
            .Select(stack => stack.Location.OwnerId)
            .Distinct()
            .OrderBy(id => id.ToString(), StringComparer.Ordinal)
            .ToArray();
        List<ResidentInventoryAttachmentViewModel> models =
            new List<ResidentInventoryAttachmentViewModel>();
        for (int index = 0; index < residents.Length; index++)
        {
            AddResidentAttachments(inventory, residents[index], models);
        }

        return new ReadOnlyCollection<ResidentInventoryAttachmentViewModel>(models
            .OrderBy(model => model.ResidentId, StringComparer.Ordinal)
            .ThenBy(model => model.Group)
            .ToArray());
    }

    private static void AddResidentAttachments(
        InventoryState inventory,
        EntityId residentId,
        ICollection<ResidentInventoryAttachmentViewModel> models)
    {
        ResidentInventoryLayoutSnapshot layout =
            inventory.GetResidentInventoryLayout(residentId);
        bool cargoOccupied = layout.Slots.Any(slot =>
            slot.Slot.Compartment == ResidentInventoryCompartment.Cargo
            && !slot.IsEmpty);
        if (cargoOccupied && layout.ActiveCargoExpansion.HasValue)
        {
            Add(models, residentId, layout.ActiveCargoExpansion.Value);
        }

        if (layout.ActiveWeaponExpansion.HasValue)
        {
            Add(models, residentId, layout.ActiveWeaponExpansion.Value);
        }
    }

    private static void Add(
        ICollection<ResidentInventoryAttachmentViewModel> models,
        EntityId residentId,
        ActiveInventoryExpansionSnapshot active)
    {
        models.Add(new ResidentInventoryAttachmentViewModel(
            residentId.ToString(),
            active.StackId.ToString(),
            active.ItemId.ToString(),
            active.Definition.Group,
            active.Definition.Tier,
            active.Definition.VisualAttachmentId));
    }
}

}
