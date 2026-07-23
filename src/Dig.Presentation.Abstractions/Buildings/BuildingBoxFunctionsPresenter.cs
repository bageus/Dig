using System;
using Dig.Presentation.Inventory;

namespace Dig.Presentation.Buildings
{
    public sealed class BuildingBoxFunctionsPresenter
    {
        public BuildingBoxFunctionsViewModel Present(
            WorldItemViewModel item,
            string? activePlacementStackId)
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (!item.IsBuildingBox)
            {
                throw new ArgumentException(
                    "Only BuildingBox world items have box functions.",
                    nameof(item));
            }

            bool available = item.Quantity == 1
                && item.ReservedQuantity == 0
                && item.AvailableQuantity == 1;
            bool active = available
                && string.Equals(
                    item.StackId,
                    activePlacementStackId,
                    StringComparison.Ordinal);
            BuildingBoxFunctionActionViewModel unpack =
                new BuildingBoxFunctionActionViewModel(
                    BuildingBoxFunctionActionKind.Unpack,
                    "building_box.action.unpack",
                    available,
                    active,
                    available ? null : "building_box.action.unavailable");
            return new BuildingBoxFunctionsViewModel(
                item.StackId,
                item.ItemId,
                unpack);
        }
    }
}
