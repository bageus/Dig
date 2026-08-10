using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Presentation.Agents;
using Dig.Presentation.Inventory;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    private IReadOnlyList<HeldBuildingBoxRosterEntry> LoadHeldBuildingBoxes(
        IReadOnlyList<ResidentRosterRowViewModel> residents)
    {
        return residents
            .SelectMany(resident => _terrainSession!
                .LoadResidentInventoryLayout(resident.Id)
                .Slots
                .Where(slot => !slot.IsEmpty
                    && slot.VisualKind == ResidentInventorySlotVisualKind.BuildingBox)
                .Select(slot => new HeldBuildingBoxRosterEntry(
                    slot.StackId!,
                    slot.ItemId!,
                    slot.DisplayName,
                    slot.Quantity,
                    slot.ReservedQuantity,
                    resident.Id,
                    resident.Name)))
            .OrderBy(value => value.DisplayName, StringComparer.Ordinal)
            .ThenBy(value => value.StackId, StringComparer.Ordinal)
            .ToArray();
    }

    private sealed class HeldBuildingBoxRosterEntry
    {
        internal HeldBuildingBoxRosterEntry(
            string stackId,
            string itemId,
            string displayName,
            int quantity,
            int reservedQuantity,
            string residentId,
            string residentName)
        {
            StackId = stackId;
            ItemId = itemId;
            DisplayName = displayName;
            Quantity = quantity;
            ReservedQuantity = reservedQuantity;
            ResidentId = residentId;
            ResidentName = residentName;
        }

        internal string StackId { get; }
        internal string ItemId { get; }
        internal string DisplayName { get; }
        internal int Quantity { get; }
        internal int ReservedQuantity { get; }
        internal string ResidentId { get; }
        internal string ResidentName { get; }
    }
}

}
