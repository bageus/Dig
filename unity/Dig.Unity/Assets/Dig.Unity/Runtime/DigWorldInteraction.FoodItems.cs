using System;
using Dig.Presentation.Input;
using Dig.Presentation.Inventory;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private const string GrilledMushroomItemId = "food.grilled_mushroom";

        private static bool IsDirectFoodItem(WorldItemViewModel item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            return string.Equals(
                item.ItemId,
                GrilledMushroomItemId,
                StringComparison.Ordinal);
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