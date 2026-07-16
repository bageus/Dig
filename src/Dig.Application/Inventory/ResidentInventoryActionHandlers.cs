using System;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Application.Inventory
{

public static class ResidentInventoryActionErrors
{
    public static readonly DomainError StackNotCarriedByActor = new DomainError(
        "inventory.resident.stack_not_carried",
        "The selected stack is not carried by the acting resident.");

    public static readonly DomainError StackReserved = new DomainError(
        "inventory.resident.stack_reserved",
        "The selected stack is reserved by an active job.");

    public static readonly DomainError ItemNotUsable = new DomainError(
        "inventory.resident.item_not_usable",
        "The selected item has no resident-use action.");
}

public sealed class DropResidentInventoryStackCommand : ICommand<Result>
{
    public DropResidentInventoryStackCommand(
        EntityId actorId,
        EntityId stackId,
        CellId destination,
        long tick)
    {
        if (actorId.IsEmpty || stackId.IsEmpty)
        {
            throw new ArgumentException("Actor and stack ids are required.");
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        ActorId = actorId;
        StackId = stackId;
        Destination = destination;
        Tick = tick;
    }

    public EntityId ActorId { get; }
    public EntityId StackId { get; }
    public CellId Destination { get; }
    public long Tick { get; }
}

public sealed class UseResidentInventoryItemCommand : ICommand<Result>
{
    public UseResidentInventoryItemCommand(EntityId actorId, EntityId stackId, long tick)
    {
        if (actorId.IsEmpty || stackId.IsEmpty)
        {
            throw new ArgumentException("Actor and stack ids are required.");
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        ActorId = actorId;
        StackId = stackId;
        Tick = tick;
    }

    public EntityId ActorId { get; }
    public EntityId StackId { get; }
    public long Tick { get; }
}

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
        Result validation = ValidateCarriedAvailableStack(stack, command.ActorId);
        if (validation.IsFailure)
        {
            return validation;
        }

        Result moved = inventory.MoveAvailable(
            command.StackId,
            stack!.Quantity,
            ItemLocation.InWorld(command.Destination),
            splitStackId: default,
            command.Tick);
        if (moved.IsFailure)
        {
            return moved;
        }

        Save(inventory);
        return Result.Success();
    }

    private void Save(InventoryState inventory)
    {
        _repository.Save(inventory);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
    }

    internal static Result ValidateCarriedAvailableStack(
        ItemStackSnapshot? stack,
        EntityId actorId)
    {
        if (stack is null)
        {
            return Result.Failure(InventoryErrors.StackNotFound);
        }

        if (stack.Location != ItemLocation.InAgent(actorId))
        {
            return Result.Failure(ResidentInventoryActionErrors.StackNotCarriedByActor);
        }

        return stack.ReservedQuantity == 0
            ? Result.Success()
            : Result.Failure(ResidentInventoryActionErrors.StackReserved);
    }
}

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
        Result validation = DropResidentInventoryStackHandler.ValidateCarriedAvailableStack(
            stack,
            command.ActorId);
        if (validation.IsFailure)
        {
            return validation;
        }

        ItemDefinition definition = inventory.Catalog.Get(stack!.ItemId);
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
