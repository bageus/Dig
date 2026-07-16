using System;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Application.Inventory
{

public sealed class DropResidentInventoryStackHandler
    : ICommandHandler<DropResidentInventoryStackCommand, Result>
{
    private readonly IInventoryRepository _repository;
    private readonly IEventSink _eventSink;

    public DropResidentInventoryStackHandler(
        IInventoryRepository repository,
        IEventSink eventSink)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(DropResidentInventoryStackCommand command)
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

        bool owned = stack.Location == ItemLocation.InAgent(command.ActorId)
            || stack.Location == ItemLocation.EquippedBy(command.ActorId);
        if (!owned)
        {
            return Result.Failure(ResidentInventoryActionErrors.StackNotCarriedByActor);
        }

        if (stack.ReservedQuantity != 0)
        {
            return Result.Failure(ResidentInventoryActionErrors.StackReserved);
        }

        Result moved = inventory.MoveAvailable(
            command.StackId,
            stack.Quantity,
            ItemLocation.InWorld(command.Destination),
            splitStackId: default,
            command.Tick);
        if (moved.IsFailure)
        {
            return moved;
        }

        _repository.Save(inventory);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        return Result.Success();
    }
}

}
