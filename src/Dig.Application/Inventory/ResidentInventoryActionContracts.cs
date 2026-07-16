using System;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Application.Inventory
{

public static class ResidentInventoryActionErrors
{
    public static readonly DomainError StackNotCarriedByActor = new DomainError(
        "inventory.resident.stack_not_carried",
        "The selected stack is not carried or equipped by the acting resident.");

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

}
