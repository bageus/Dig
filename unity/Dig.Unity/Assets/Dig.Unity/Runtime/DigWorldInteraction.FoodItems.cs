using System;
using Dig.Presentation.Input;
using Dig.Presentation.Inventory;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private static bool IsDirectFoodItem(WorldItemViewModel item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            return HasItemFamily(item.ItemId, "food.");
        }

        private static bool IsDirectConsumableItemId(string itemId)
        {
            return HasItemFamily(itemId, "food.")
                || HasItemFamily(itemId, "potion.")
                || HasItemFamily(itemId, "drink.")
                || HasItemFamily(itemId, "beverage.");
        }

        private static bool HasItemFamily(string itemId, string prefix)
        {
            return !string.IsNullOrWhiteSpace(itemId)
                && itemId.StartsWith(prefix, StringComparison.Ordinal);
        }

        private static ContextWorldTargetKind ResolveWorldItemTargetKind(
            WorldItemViewModel item)
        {
            if (item.IsBuildingBox)
            {
                return ContextWorldTargetKind.BuildingBox;
            }

            return IsDirectFoodItem(item)
                ? ContextWorldTargetKind.FoodItem
                : ContextWorldTargetKind.GenericItem;
        }
    }
}
