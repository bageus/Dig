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
    public static readonly DomainError UnitLocationRequiresSingleItem = new DomainError(
        "inventory.unit_location_requires_single_item",
        "World and resident inventory locations accept exactly one physical item entity.");
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
        "Only a single available tool can be held.");
    public static readonly DomainError ToolNotCarried = new DomainError(
        "inventory.tool_not_carried",
        "The tool must remain in the acting resident inventory.");
    public static readonly DomainError ToolSlotOccupied = new DomainError(
        "inventory.tool_slot_occupied",
        "The resident already holds another item.");
    public static readonly DomainError ToolSwitchUnsafe = new DomainError(
        "inventory.tool_switch_unsafe",
        "The currently held item cannot be safely released.");
    public static readonly DomainError HeldItemAlreadyExists = new DomainError(
        "inventory.held.already_exists",
        "The resident already has a held item reference.");
    public static readonly DomainError HeldItemNotFound = new DomainError(
        "inventory.held.not_found",
        "The resident does not have a held item reference.");
    public static readonly DomainError HeldItemStackNotCarried = new DomainError(
        "inventory.held.stack_not_carried",
        "The held stack must stay in the resident inventory.");
    public static readonly DomainError HeldItemReferenceInvalid = new DomainError(
        "inventory.held.reference_invalid",
        "The held item reference is stale or inconsistent with the stack.");
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
    public static readonly DomainError ResidentSlotClaimConflict = new DomainError(
        "inventory.resident.slot_claim_conflict",
        "The hauling job already owns a different resident slot capacity claim.");
    public static readonly DomainError ResidentSlotClaimStale = new DomainError(
        "inventory.resident.slot_claim_stale",
        "A resident slot capacity claim no longer matches the active inventory layout.");
}

}