using Dig.Domain.Core;

namespace Dig.Domain.Jobs;

public static class JobErrors
{
    public static readonly DomainError AlreadyExists = new DomainError(
        "jobs.already_exists",
        "A job with the same id already exists.");

    public static readonly DomainError NotFound = new DomainError(
        "jobs.not_found",
        "The requested job does not exist.");

    public static readonly DomainError InvalidStatus = new DomainError(
        "jobs.invalid_status",
        "The job cannot perform that transition from its current status.");

    public static readonly DomainError DependenciesIncomplete = new DomainError(
        "jobs.dependencies_incomplete",
        "The job cannot become available until all dependencies are completed.");

    public static readonly DomainError AgentUnavailable = new DomainError(
        "jobs.agent_unavailable",
        "The agent already owns another active job reservation.");

    public static readonly DomainError ReservationConflict = new DomainError(
        "jobs.reservation_conflict",
        "One or more required resources are reserved by another job.");

    public static readonly DomainError RetryNotReady = new DomainError(
        "jobs.retry_not_ready",
        "The blocked job has not reached its retry tick.");

    public static readonly DomainError RetryLimitReached = new DomainError(
        "jobs.retry_limit_reached",
        "The job exhausted its bounded retry policy.");

    public static readonly DomainError CandidateUnavailable = new DomainError(
        "jobs.candidate_unavailable",
        "No eligible candidate is available for the job.");
}
