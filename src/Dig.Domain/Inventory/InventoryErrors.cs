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
    public static readonly DomainError ToolNotCarried = new DomainError(
        "inventory.tool_not_carried",
        "The tool must be carried by the acting resident before it can be equipped.");
    public static readonly DomainError ToolSlotOccupied = new DomainError(
        "inventory.tool_slot_occupied",
        "The resident already has an equipped tool.");
    public static readonly DomainError ToolSwitchUnsafe = new DomainError(
        "inventory.tool_switch_unsafe",
        "The currently equipped item cannot be safely returned to the resident inventory.");
    public static readonly DomainError ResidentSlotOccupied = new DomainError(
        "inventory.resident.slot_occupied",
        "The selected resident inventory slot already contains another stack.");
    public static readonly DomainError ResidentSlotOutOfRange = new DomainError(
        "inventory.resident.slot_out_of_range",
        "The selected slot does not exist in the resident's active inventory layout.");
    public static readonly DomainError ResidentSlotCategoryRejected = new DomainError(
        "inventory.resident.slot_category_rejected",
        "The item category is not accepted by the selected inventory compartment.");
    public static readonly DomainError InventoryExpansionMainOnly = new DomainError(
        "inventory.resident.expansion_main_only",
        "Inventory expansions can only occupy a resident Main slot.");
    public static readonly DomainError ResidentInventoryCapacityExceeded = new DomainError(
        "inventory.resident.capacity_exceeded",
        "The resident inventory does not have a compatible free slot.");
    public static readonly DomainError ResidentInventoryLayoutInvalid = new DomainError(
        "inventory.resident.layout_invalid",
        "The resident inventory contains duplicate or invalid slot locations.");
    public static readonly DomainError ResidentInventorySpillRequired = new DomainError(
        "inventory.resident.spill_required",
        "Removing this active expansion requires a transactional compartment spill.");
}

}
