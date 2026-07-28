using Dig.Domain.Core;

namespace Dig.Domain.WorldObjects
{

public static class BarrelErrors
{
    public static readonly DomainError AlreadyExists = new DomainError(
        "barrel.already_exists",
        "A barrel with this id already exists.");

    public static readonly DomainError CellAlreadyOccupied = new DomainError(
        "barrel.cell_already_occupied",
        "Another barrel already occupies this cell.");

    public static readonly DomainError NotFound = new DomainError(
        "barrel.not_found",
        "The barrel was not found.");

    public static readonly DomainError NotAttackable = new DomainError(
        "barrel.not_attackable",
        "The barrel is falling or already destroyed.");

    public static readonly DomainError VersionConflict = new DomainError(
        "barrel.version_conflict",
        "The barrel changed before this attack could commit.");

    public static readonly DomainError ContentsAlreadyMaterialized = new DomainError(
        "barrel.contents_already_materialized",
        "The barrel contents were already materialized.");

    public static readonly DomainError InvalidRestore = new DomainError(
        "barrel.invalid_restore",
        "The saved barrel state is invalid.");

    public static readonly DomainError FallNotAllowed = new DomainError(
        "barrel.fall_not_allowed",
        "Only a supported barrel may begin falling.");

    public static readonly DomainError LandingNotAllowed = new DomainError(
        "barrel.landing_not_allowed",
        "Only a falling barrel may land.");
}

}