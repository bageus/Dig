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
        "The creature or linked item identity already exists.");

    public static readonly DomainError InvalidState = new DomainError(
        "ecology.living_material.invalid_state",
        "The living material transition is not valid from the current state.");

    public static readonly DomainError InvalidMovement = new DomainError(
        "ecology.living_material.invalid_movement",
        "The movement target is outside the creature flat plane or wander radius.");

    public static readonly DomainError ReproductionConflict = new DomainError(
        "ecology.living_material.reproduction_conflict",
        "The reproduction plan no longer matches the parent state.");

    public static readonly DomainError InvalidSnapshot = new DomainError(
        "ecology.living_material.invalid_snapshot",
        "The saved living material ecology snapshot is invalid.");
}

}
