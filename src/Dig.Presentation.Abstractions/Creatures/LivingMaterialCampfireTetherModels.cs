using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Ecology;
using Dig.Domain.Inventory;
using Dig.Presentation.Buildings;

namespace Dig.Presentation.Creatures
{

public sealed class LivingMaterialCampfireTetherViewModel
{
    public LivingMaterialCampfireTetherViewModel(
        string creatureId,
        string buildingId,
        int slotIndex,
        long version)
    {
        if (string.IsNullOrWhiteSpace(creatureId)
            || string.IsNullOrWhiteSpace(buildingId)
            || slotIndex < 0
            || slotIndex > 1
            || version < 0)
        {
            throw new ArgumentException("Living material tether values are invalid.");
        }

        CreatureId = creatureId.Trim();
        BuildingId = buildingId.Trim();
        SlotIndex = slotIndex;
        Version = version;
    }

    public string CreatureId { get; }
    public string BuildingId { get; }
    public int SlotIndex { get; }
    public long Version { get; }
}

public sealed class LivingMaterialCampfireTetherProjector
{
    public IReadOnlyList<LivingMaterialCampfireTetherViewModel> Project(
        InventorySnapshot inventory,
        IReadOnlyCollection<BuildingWorldViewModel> buildings)
    {
        if (inventory == null || buildings == null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        string campfireDefinitionId =
            CampfireBuildingBoxContent.CampfireBuildingId.ToString();
        HashSet<string> campfires = buildings
            .Where(value => (value.Status == BuildingStatus.Completed
                    || value.Status == BuildingStatus.Damaged)
                && string.Equals(
                    value.DefinitionId,
                    campfireDefinitionId,
                    StringComparison.Ordinal))
            .Select(value => value.Id)
            .ToHashSet(StringComparer.Ordinal);
        if (campfires.Count == 0)
        {
            return Array.Empty<LivingMaterialCampfireTetherViewModel>();
        }

        List<LivingMaterialCampfireTetherViewModel> result =
            new List<LivingMaterialCampfireTetherViewModel>();
        foreach (IGrouping<string, ItemStackSnapshot> group in inventory.Stacks
            .Where(value => value.Quantity == 1
                && value.ItemId == LivingMaterialEcologyProfiles.HamsterItemId
                && value.Location.Kind == ItemLocationKind.BuildingInventory
                && value.Location.HasOwner
                && campfires.Contains(value.Location.OwnerId.ToString()))
            .GroupBy(value => value.Location.OwnerId.ToString(), StringComparer.Ordinal)
            .OrderBy(value => value.Key, StringComparer.Ordinal))
        {
            ItemStackSnapshot[] hamsters = group
                .OrderBy(value => value.StackId.ToString(), StringComparer.Ordinal)
                .Take(2)
                .ToArray();
            for (int index = 0; index < hamsters.Length; index++)
            {
                ItemStackSnapshot hamster = hamsters[index];
                result.Add(new LivingMaterialCampfireTetherViewModel(
                    hamster.StackId.ToString(),
                    group.Key,
                    index,
                    inventory.Version));
            }
        }

        return result;
    }
}

}
