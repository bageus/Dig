using System;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Application.Inventory
{

public sealed class UseResidentInventoryItemHandler
    : ICommandHandler<UseResidentInventoryItemCommand, Result>
{
    private readonly IInventoryRepository _repository;
    private readonly IEventSink _eventSink;

    public UseResidentInventoryItemHandler(
        IInventoryRepository repository,
        IEventSink eventSink)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(UseResidentInventoryItemCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        InventoryState inventory = _repository.Get();
        ItemStackSnapshot? stack = inventory.GetStack(command.StackId);
        if (stack is null)
        {
            return Result.Failure(InventoryErrors.StackNotFound);
        }

        if (!DropResidentInventoryStackHandler.IsOwnedByResident(
                stack.Location,
                command.ActorId)
            || stack.Location.Kind == ItemLocationKind.Equipped)
        {
            return Result.Failure(ResidentInventoryActionErrors.StackNotCarriedByActor);
        }

        if (stack.ReservedQuantity != 0)
        {
            return Result.Failure(ResidentInventoryActionErrors.StackReserved);
        }

        ItemDefinition definition = inventory.Catalog.Get(stack.ItemId);
        if (!definition.IsTool)
        {
            return Result.Failure(ResidentInventoryActionErrors.ItemNotUsable);
        }

        Result equipped = inventory.EquipTool(command.StackId, command.ActorId, command.Tick);
        if (equipped.IsFailure)
        {
            return equipped;
        }

        _repository.Save(inventory);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        return Result.Success();
    }
}

}
