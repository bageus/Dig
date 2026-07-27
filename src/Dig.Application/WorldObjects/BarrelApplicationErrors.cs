using Dig.Domain.Core;

namespace Dig.Application.WorldObjects
{

public static class BarrelApplicationErrors
{
    public static readonly DomainError JobTypeUnsupported = new DomainError(
        "barrel.job_type_unsupported",
        "The job is not a barrel attack job.");

    public static readonly DomainError JobNotReady = new DomainError(
        "barrel.job_not_ready",
        "The barrel attack job is not ready for this transition.");

    public static readonly DomainError GenerationConflict = new DomainError(
        "barrel.generation_conflict",
        "The barrel changed before this job could commit.");

    public static readonly DomainError UnknownContentsItem = new DomainError(
        "barrel.contents_item_unknown",
        "The saved barrel contents item is not present in the inventory catalog.");
}

}