using Dig.Domain.Core;

namespace Dig.Domain.Ecology
{

public static class LivingMaterialErrors
{
    public static readonly DomainError NotFound = new DomainError(
        "ecology.living_material.not_found",
        "The living material creature was not found.");

    public static readonly DomainError AlreadyExists = new DomainError(
        "ecology.living_material.already_exists",
        "The living material creature or linked item already exists.");

    public static readonly DomainError InvalidState = new DomainError(
        "ecology.living_material.invalid_state",
        "The living material state does not allow this transition.");

    public static readonly DomainError InvalidMovement = new DomainError(
        "ecology.living_material.invalid_movement",
        "The movement target is outside the creature movement region, X/Z step, or wander radius.");

    public static readonly DomainError ReproductionConflict = new DomainError(
        "ecology.living_material.reproduction_conflict",
        "The living material reproduction plan is stale or conflicts with current state.");

    public static readonly DomainError InvalidSnapshot = new DomainError(
        "ecology.living_material.invalid_snapshot",
        "The living material ecology snapshot is invalid.");
}

}
