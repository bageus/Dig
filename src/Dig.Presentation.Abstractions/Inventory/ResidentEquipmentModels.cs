using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Inventory;

namespace Dig.Presentation.Inventory
{

public sealed class ResidentEquipmentViewModel
{
    public ResidentEquipmentViewModel(string residentId, string stackId, string itemId)
    {
        if (string.IsNullOrWhiteSpace(residentId)
            || string.IsNullOrWhiteSpace(stackId)
            || string.IsNullOrWhiteSpace(itemId))
        {
            throw new ArgumentException("Resident equipment identifiers are required.");
        }

        ResidentId = residentId.Trim();
        StackId = stackId.Trim();
        ItemId = itemId.Trim();
    }

    public string ResidentId { get; }
    public string StackId { get; }
    public string ItemId { get; }
}

public sealed class ResidentEquipmentPresenter
{
    public IReadOnlyList<ResidentEquipmentViewModel> Present(
        params InventorySnapshot[] snapshots)
    {
        if (snapshots is null || snapshots.Any(snapshot => snapshot is null))
        {
            throw new ArgumentNullException(nameof(snapshots));
        }

        var equipped = snapshots
            .SelectMany(snapshot => snapshot.Stacks)
            .Where(stack => stack.Location.Kind == ItemLocationKind.Equipped
                && stack.Location.HasOwner)
            .GroupBy(stack => stack.Location.OwnerId)
            .OrderBy(group => group.Key.ToString(), StringComparer.Ordinal)
            .ToArray();
        if (equipped.Any(group => group.Count() > 1))
        {
            throw new InvalidOperationException(
                "A resident cannot have more than one equipped item.");
        }

        ResidentEquipmentViewModel[] models = equipped
            .Select(group => group.Single())
            .Select(stack => new ResidentEquipmentViewModel(
                stack.Location.OwnerId.ToString(),
                stack.StackId.ToString(),
                stack.ItemId.ToString()))
            .ToArray();
        return new ReadOnlyCollection<ResidentEquipmentViewModel>(models);
    }
}

}