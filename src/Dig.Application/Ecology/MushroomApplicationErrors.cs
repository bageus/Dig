using Dig.Domain.Core;

namespace Dig.Application.Ecology
{

public static class MushroomApplicationErrors
{
    public static readonly DomainError JobTypeUnsupported = new DomainError(
        "ecology.mushroom.job_type_unsupported",
        "The job is not a mushroom chopping job.");

    public static readonly DomainError JobNotReady = new DomainError(
        "ecology.mushroom.job_not_ready",
        "The mushroom job is not at the required stage.");

    public static readonly DomainError JobWorkerMissing = new DomainError(
        "ecology.mushroom.job_worker_missing",
        "The mushroom job has no assigned worker.");

    public static readonly DomainError GenerationConflict = new DomainError(
        "ecology.mushroom.generation_conflict",
        "The mushroom grew or was chopped before this job could commit.");

    public static readonly DomainError UnknownDropItem = new DomainError(
        "ecology.mushroom.drop_item_unknown",
        "The mushroom drop item is not present in the Inventory catalog.");
}

}
