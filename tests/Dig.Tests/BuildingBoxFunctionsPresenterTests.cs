using System;
using Dig.Presentation.Buildings;
using Dig.Presentation.Inventory;
using Xunit;

namespace Dig.Tests
{
    public sealed class BuildingBoxFunctionsPresenterTests
    {
        [Fact]
        public void Available_box_exposes_one_enabled_unpack_action()
        {
            WorldItemViewModel item = Box(reservedQuantity: 0);

            BuildingBoxFunctionsViewModel model =
                new BuildingBoxFunctionsPresenter().Present(item, activePlacementStackId: null);

            Assert.Equal("stack.campfire", model.StackId);
            Assert.Equal("building_box.campfire", model.ItemId);
            Assert.Equal(BuildingBoxFunctionActionKind.Unpack, model.UnpackAction.Kind);
            Assert.True(model.UnpackAction.IsEnabled);
            Assert.False(model.UnpackAction.IsActive);
            Assert.Null(model.UnpackAction.DisabledReasonCode);
        }

        [Fact]
        public void Placement_for_selected_box_marks_unpack_action_active()
        {
            WorldItemViewModel item = Box(reservedQuantity: 0);

            BuildingBoxFunctionsViewModel model =
                new BuildingBoxFunctionsPresenter().Present(item, item.StackId);

            Assert.True(model.UnpackAction.IsActive);
        }

        [Fact]
        public void Reserved_box_disables_unpack_action()
        {
            BuildingBoxFunctionsViewModel model =
                new BuildingBoxFunctionsPresenter().Present(
                    Box(reservedQuantity: 1),
                    activePlacementStackId: "stack.campfire");

            Assert.False(model.UnpackAction.IsEnabled);
            Assert.False(model.UnpackAction.IsActive);
            Assert.Equal(
                "building_box.action.unavailable",
                model.UnpackAction.DisabledReasonCode);
        }

        [Fact]
        public void Non_box_item_is_rejected()
        {
            WorldItemViewModel item = new WorldItemViewModel(
                "stack.stone",
                "material.stone",
                quantity: 1,
                reservedQuantity: 0,
                cellX: 1,
                cellY: 2,
                interactionKind: WorldItemInteractionKind.None);

            Assert.Throws<ArgumentException>(() =>
                new BuildingBoxFunctionsPresenter().Present(item, null));
        }

        private static WorldItemViewModel Box(int reservedQuantity)
        {
            return new WorldItemViewModel(
                "stack.campfire",
                "building_box.campfire",
                quantity: 1,
                reservedQuantity,
                cellX: 4,
                cellY: 5,
                interactionKind: WorldItemInteractionKind.BuildingBox);
        }
    }
}
