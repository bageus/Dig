using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public static class InventoryErrors
{
    public static readonly DomainError StackAlreadyExists = new DomainError(
        "inventory.stack_already_exists",
        "An item stack with the same id already exists.");

    public static readonly DomainError StackNotFound = new DomainError(
        "inventory.stack_not_found",
        "The requested item stack does not exist.");

    public static readonly DomainError InvalidQuantity = new DomainError(
        "inventory.invalid_quantity",
        "The requested item quantity is invalid.");

    public static readonly DomainError InsufficientAvailableQuantity = new DomainError(
        "inventory.insufficient_available_quantity",
        "The item stack does not have enough unreserved quantity.");

    public static readonly DomainError ReservationNotFound = new DomainError(
        "inventory.reservation_not_found",
        "The hauling job does not own the requested item quantity reservation.");

    public static readonly DomainError StackSizeExceeded = new DomainError(
        "inventory.stack_size_exceeded",
        "The quantity exceeds the item definition maximum stack size.");

    public static readonly DomainError SplitIdRequired = new DomainError(
        "inventory.split_id_required",
        "A unique stack id is required for a partial move.");

    public static readonly DomainError ToolRequired = new DomainError(
        "inventory.tool_required",
        "Only a single unreserved tool can be equipped.");

    public static readonly DomainError ToolSlotOccupied = new DomainError(
        "inventory.tool_slot_occupied",
        "The resident already has an equipped tool.");
}

}