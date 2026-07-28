using System;
using Dig.Application.Inventory;
using Dig.Application.Messaging;
using Dig.Domain.Agents;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Application.Agents
{
    public static class ResidentFoodMealErrors
    {
        public static readonly DomainError UnsupportedFood = new DomainError(
            "resident.food_meal.unsupported_food",
            "The carried item is not supported food.");
    }

    public sealed class StartResidentFoodMealCommand : ICommand<Result>
    {
        public StartResidentFoodMealCommand(
            EntityId residentId,
            EntityId stackId,
            long tick)
        {
            if (residentId.IsEmpty || stackId.IsEmpty)
            {
                throw new ArgumentException("Resident and stack ids are required.");
            }

            if (tick < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tick));
            }

            ResidentId = residentId;
            StackId = stackId;
            Tick = tick;
        }

        public EntityId ResidentId { get; }
        public EntityId StackId { get; }
        public long Tick { get; }
    }

    public sealed class StartResidentFoodMealHandler
        : ICommandHandler<StartResidentFoodMealCommand, Result>
    {
        public const int GrilledMushroomNutritionUnits = 1_500;
        public const int MealBiteCount = 3;

        private readonly IAgentRepository _agents;
        private readonly IInventoryRepository _inventory;
        private readonly IEventSink _events;

        public StartResidentFoodMealHandler(
            IAgentRepository agents,
            IInventoryRepository inventory,
            IEventSink events)
        {
            _agents = agents ?? throw new ArgumentNullException(nameof(agents));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public Result Handle(StartResidentFoodMealCommand command)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            AgentState? agent = _agents.Get(command.ResidentId);
            if (agent == null)
            {
                return Result.Failure(AgentErrors.AgentNotFound);
            }

            if (!agent.IsAlive)
            {
                return Result.Failure(AgentErrors.AgentDead);
            }

            if (agent.HasActiveFoodMeal)
            {
                return Result.Failure(FoodMealErrors.AlreadyActive);
            }

            InventoryState inventory = _inventory.Get();
            ItemStackSnapshot? stack = inventory.GetStack(command.StackId);
            if (stack == null)
            {
                return Result.Failure(InventoryErrors.StackNotFound);
            }

            if (!DropResidentInventoryStackHandler.IsOwnedByResident(
                    stack.Location,
                    command.ResidentId))
            {
                return Result.Failure(
                    ResidentInventoryActionErrors.StackNotCarriedByActor);
            }

            if (stack.ItemId != CampfireProductionContent.GrilledMushroomItemId)
            {
                return Result.Failure(ResidentFoodMealErrors.UnsupportedFood);
            }

            if (stack.AvailableQuantity < 1)
            {
                return Result.Failure(InventoryErrors.InsufficientAvailableQuantity);
            }

            Result reserved = inventory.ReserveQuantity(
                command.StackId,
                command.ResidentId,
                1,
                command.Tick);
            if (reserved.IsFailure)
            {
                return reserved;
            }

            Result consumed = inventory.ConsumeReserved(
                command.ResidentId,
                command.StackId,
                1,
                command.Tick);
            if (consumed.IsFailure)
            {
                inventory.ReleaseReservations(command.ResidentId, command.Tick);
                return consumed;
            }

            Result started = agent.BeginFoodMeal(
                command.StackId,
                stack.ItemId,
                GrilledMushroomNutritionUnits,
                MealBiteCount,
                command.Tick);
            if (started.IsFailure)
            {
                throw new InvalidOperationException(
                    "Validated food consumption could not start a meal: " + started.Error);
            }

            _inventory.Save(inventory);
            _agents.Save(agent);
            _events.Append(inventory.DequeueUncommittedEvents());
            _events.Append(agent.DequeueUncommittedEvents());
            return Result.Success();
        }
    }
}