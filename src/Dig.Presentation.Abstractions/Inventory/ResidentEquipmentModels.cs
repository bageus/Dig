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

        List<ResidentEquipmentViewModel> models = new List<ResidentEquipmentViewModel>();
        for (int index = 0; index < snapshots.Length; index++)
        {
            InventorySnapshot snapshot = snapshots[index];
            Dictionary<Dig.Domain.Core.EntityId, ItemStackSnapshot> stacks = snapshot.Stacks
                .ToDictionary(stack => stack.StackId);
            foreach (HeldItemReferenceSnapshot held in snapshot.HeldItems)
            {
                if (!stacks.TryGetValue(held.StackId, out ItemStackSnapshot? stack))
                {
                    throw new InvalidOperationException(
                        "A held item reference points to a missing stack.");
                }

                models.Add(new ResidentEquipmentViewModel(
                    held.ResidentId.ToString(),
                    held.StackId.ToString(),
                    stack.ItemId.ToString()));
            }

            models.AddRange(snapshot.Stacks
                .Where(stack => stack.Location.Kind == ItemLocationKind.Equipped
                    && stack.Location.HasOwner)
                .Select(stack => new ResidentEquipmentViewModel(
                    stack.Location.OwnerId.ToString(),
                    stack.StackId.ToString(),
                    stack.ItemId.ToString())));
        }

        ResidentEquipmentViewModel[] ordered = models
            .OrderBy(model => model.ResidentId, StringComparer.Ordinal)
            .ThenBy(model => model.StackId, StringComparer.Ordinal)
            .ToArray();
        if (ordered.GroupBy(model => model.ResidentId, StringComparer.Ordinal)
            .Any(group => group.Count() > 1))
        {
            throw new InvalidOperationException(
                "A resident cannot have more than one held or legacy equipped item.");
        }

        return new ReadOnlyCollection<ResidentEquipmentViewModel>(ordered);
    }
}

}
