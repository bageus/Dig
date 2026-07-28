using Dig.Domain.Core;

namespace Dig.Application.Inventory
{

public static class ResidentInventoryConsumableErrors
{
    public static readonly DomainError EffectOwnerUnavailable = new DomainError(
        "inventory.consumable.effect_owner_unavailable",
        "The consumable interaction is available, but no authoritative effect owner is registered for this item.");
}
}
