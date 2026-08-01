using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Presentation.Input;
using Xunit;

namespace Dig.Tests
{
    public sealed class CampfireFoodInputRouterTests
    {
        private static readonly EntityId Resident = Id(1);
        private static readonly EntityId FoodStack = Id(2);
        private static readonly CellId FoodCell = new CellId(7, 4, 0);
        private readonly ContextInputRouter _router = new ContextInputRouter();

        [Fact]
        public void Plain_left_click_on_food_creates_direct_pickup()
        {
            ContextInputDecision decision = Route(altPressed: false);

            Assert.True(decision.ConsumesPointer);
            Assert.Equal(ApplicationInputCommandKind.PickupWorldItem, decision.CommandKind);
            Assert.Equal(Resident, decision.ActorId);
            Assert.Equal(FoodStack, decision.TargetEntityId);
            Assert.Equal(FoodCell, decision.TargetCell);
        }

        [Fact]
        public void Alt_left_click_on_food_creates_pickup_then_eat()
        {
            ContextInputDecision decision = Route(altPressed: true);

            Assert.True(decision.ConsumesPointer);
            Assert.Equal(ApplicationInputCommandKind.EatWorldItem, decision.CommandKind);
            Assert.Equal(Resident, decision.ActorId);
            Assert.Equal(FoodStack, decision.TargetEntityId);
            Assert.Equal(FoodCell, decision.TargetCell);
        }

        [Fact]
        public void Food_without_selected_resident_is_consumed_with_reason()
        {
            ContextInputDecision decision = _router.Route(
                new ContextPointerEvent(
                    PointerInputSurface.World,
                    PointerButtonKind.Left,
                    altPressed: true),
                new ContextInputState(),
                FoodTarget(altPressed: true));

            Assert.False(decision.HasApplicationCommand);
            Assert.True(decision.ConsumesPointer);
            Assert.Equal("input.world_item.resident_required", decision.ReasonCode);
        }

        private ContextInputDecision Route(bool altPressed)
        {
            return _router.Route(
                new ContextPointerEvent(
                    PointerInputSurface.World,
                    PointerButtonKind.Left,
                    altPressed: altPressed),
                new ContextInputState(selectedResidentId: Resident),
                FoodTarget(altPressed));
        }

        private static ContextPointerTarget FoodTarget(bool altPressed)
        {
            return new ContextPointerTarget(
                ContextWorldTargetKind.FoodItem,
                FoodStack,
                FoodCell,
                reachable: true,
                itemActionAvailable: true,
                itemInteractionAction: altPressed
                    ? ItemWorldInteractionAction.DirectUse
                    : ItemWorldInteractionAction.Pickup);
        }

        private static EntityId Id(int value)
        {
            return EntityId.Parse(value.ToString("x32"));
        }
    }
}