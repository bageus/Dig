using System.Collections.ObjectModel;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Jobs;

namespace Dig.Infrastructure.InMemory;

public sealed class InMemoryJobRepository : IJobRepository
{
    private JobSystem _jobs;

    public InMemoryJobRepository(JobSystem? jobs = null)
    {
        _jobs = jobs ?? new JobSystem();
    }

    public JobSystem Get()
    {
        return _jobs;
    }

    public void Save(JobSystem jobs)
    {
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
    }
}

public sealed class InMemoryJobCandidateProvider : IJobCandidateProvider
{
    private readonly Dictionary<EntityId, List<JobCandidate>> _candidates =
        new Dictionary<EntityId, List<JobCandidate>>();

    public void SetCandidates(EntityId jobId, IEnumerable<JobCandidate> candidates)
    {
        if (jobId.IsEmpty)
        {
            throw new ArgumentException("Job id cannot be empty.", nameof(jobId));
        }

        if (candidates is null)
        {
            throw new ArgumentNullException(nameof(candidates));
        }

        _candidates[jobId] = candidates
            .OrderBy(candidate => candidate.AgentId.ToString(), StringComparer.Ordinal)
            .ToList();
    }

    public IReadOnlyCollection<JobCandidate> GetCandidates(JobSnapshot job, long tick)
    {
        if (job is null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        JobCandidate[] values = _candidates.TryGetValue(job.Id, out List<JobCandidate>? candidates)
            ? candidates.ToArray()
            : Array.Empty<JobCandidate>();
        return new ReadOnlyCollection<JobCandidate>(values);
    }
}
