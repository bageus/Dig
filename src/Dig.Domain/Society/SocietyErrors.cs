using Dig.Domain.Core;

namespace Dig.Domain.Society
{

public static class SocietyErrors
{
    public static readonly DomainError UnknownResident = new DomainError(
        "society.resident.unknown",
        "The resident is not registered in Society.");

    public static readonly DomainError DuplicateResident = new DomainError(
        "society.resident.duplicate",
        "The resident is already registered in Society.");

    public static readonly DomainError ResidentDead = new DomainError(
        "society.resident.dead",
        "The resident is dead.");

    public static readonly DomainError InvalidParents = new DomainError(
        "society.family.invalid_parents",
        "Birth requires two distinct living registered parents with matching sexes.");

    public static readonly DomainError FamilyCycle = new DomainError(
        "society.family.cycle",
        "The family graph contains a cycle.");

    public static readonly DomainError CloseRelatives = new DomainError(
        "society.family.close_relatives",
        "Close relatives cannot form a partnership or reproduce.");

    public static readonly DomainError PartnershipBlocked = new DomainError(
        "society.partnership.blocked",
        "The partnership requirements are not satisfied.");

    public static readonly DomainError AlreadyPartnered = new DomainError(
        "society.partnership.already_partnered",
        "One of the residents already has another partner.");

    public static readonly DomainError PregnancyBlocked = new DomainError(
        "society.pregnancy.blocked",
        "The reproduction requirements are not satisfied.");

    public static readonly DomainError PregnancyMissing = new DomainError(
        "society.pregnancy.missing",
        "The mother does not have the expected pregnancy.");

    public static readonly DomainError PregnancyNotDue = new DomainError(
        "society.pregnancy.not_due",
        "The pregnancy has not reached its due tick.");

    public static readonly DomainError InvalidTick = new DomainError(
        "society.tick.invalid",
        "The tick is earlier than the resident or state timeline allows.");
}
}
