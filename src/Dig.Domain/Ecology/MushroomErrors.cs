using Dig.Domain.Core;

namespace Dig.Domain.Ecology
{

public static class MushroomErrors
{
    public static readonly DomainError AlreadyExists = new DomainError(
        "ecology.mushroom.already_exists",
        "A mushroom site with the same id already exists.");

    public static readonly DomainError CellAlreadyOccupied = new DomainError(
        "ecology.mushroom.cell_already_occupied",
        "A mushroom growth site already owns the requested cell.");

    public static readonly DomainError NotFound = new DomainError(
        "ecology.mushroom.not_found",
        "The requested mushroom site does not exist.");

    public static readonly DomainError NotVisible = new DomainError(
        "ecology.mushroom.not_visible",
        "The mushroom is absent and cannot be chopped.");

    public static readonly DomainError ChopAlreadyActive = new DomainError(
        "ecology.mushroom.chop_already_active",
        "The mushroom already has an active chopping job.");

    public static readonly DomainError ChopNotActive = new DomainError(
        "ecology.mushroom.chop_not_active",
        "The mushroom has no active chopping job.");

    public static readonly DomainError ChopOwnerMismatch = new DomainError(
        "ecology.mushroom.chop_owner_mismatch",
        "The chopping job or worker does not own this mushroom attempt.");

    public static readonly DomainError ChopIncomplete = new DomainError(
        "ecology.mushroom.chop_incomplete",
        "The mushroom has not received all required swings.");

    public static readonly DomainError InvalidRestore = new DomainError(
        "ecology.mushroom.restore_invalid",
        "The mushroom snapshot is invalid.");
}

}
