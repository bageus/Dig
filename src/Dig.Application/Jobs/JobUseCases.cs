using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Jobs;

namespace Dig.Application.Jobs
{

public sealed class CreateDigJobHandler : ICommandHandler<CreateDigJobCommand, Result>
{
    private readonly IJobRepository _repository;
    private readonly IEventSink _eventSink;

    public CreateDigJobHandler(IJobRepository repository, IEventSink eventSink)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(CreateDigJobCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        JobSystem jobs = _repository.Get();
        Result result = jobs.Add(command.Definition);
        if (result.IsFailure)
        {
            return result;
        }

        if (command.MakeAvailable)
        {
            result = jobs.MakeAvailable(command.Definition.Id, command.Definition.CreatedTick);
        }

        _repository.Save(jobs);
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return result;
    }
}

public sealed class AssignAvailableJobsHandler
    : ICommandHandler<AssignAvailableJobsCommand, JobAssignmentReport>
{
    private readonly IJobRepository _repository;
    private readonly IJobCandidateProvider _candidateProvider;
    private readonly IEventSink _eventSink;

    public AssignAvailableJobsHandler(
        IJobRepository repository,
        IJobCandidateProvider candidateProvider,
        IEventSink eventSink)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _candidateProvider = candidateProvider
            ?? throw new ArgumentNullException(nameof(candidateProvider));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public JobAssignmentReport Handle(AssignAvailableJobsCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        JobSystem jobs = _repository.Get();
        List<JobAssignment> assignments = new List<JobAssignment>();
        List<JobAssignmentFailure> failures = new List<JobAssignmentFailure>();

        JobSnapshot[] availableJobs = jobs.GetAll()
            .Where(job => job.Status == JobStatus.Available)
            .OrderByDescending(job => job.Definition.Priority)
            .ThenBy(job => job.Definition.CreatedTick)
            .ThenBy(job => job.Id.ToString(), StringComparer.Ordinal)
            .ToArray();

        foreach (JobSnapshot job in availableJobs)
        {
            JobCandidate[] candidates = _candidateProvider
                .GetCandidates(job, command.Tick)
                .Where(candidate => candidate.IsAvailable)
                .OrderByDescending(candidate => JobCandidateEvaluator.Score(job, candidate))
                .ThenBy(candidate => candidate.AgentId.ToString(), StringComparer.Ordinal)
                .ToArray();

            DomainError failure = JobErrors.CandidateUnavailable;
            bool assigned = false;
            foreach (JobCandidate candidate in candidates)
            {
                Result claim = jobs.Claim(job.Id, candidate.AgentId, command.Tick);
                if (claim.IsSuccess)
                {
                    assignments.Add(new JobAssignment(
                        job.Id,
                        candidate.AgentId,
                        JobCandidateEvaluator.Score(job, candidate)));
                    assigned = true;
                    break;
                }

                failure = claim.Error!;
                if (claim.Error != JobErrors.AgentUnavailable)
                {
                    break;
                }
            }

            if (!assigned)
            {
                failures.Add(new JobAssignmentFailure(job.Id, failure));
            }
        }

        _repository.Save(jobs);
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return new JobAssignmentReport(command.Tick, assignments, failures);
    }
}

public sealed class AdvanceJobHandler : ICommandHandler<AdvanceJobCommand, Result>
{
    private readonly IJobRepository _repository;
    private readonly IEventSink _eventSink;

    public AdvanceJobHandler(IJobRepository repository, IEventSink eventSink)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(AdvanceJobCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        JobSystem jobs = _repository.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        Result result = job?.Status switch
        {
            JobStatus.Claimed => jobs.Start(command.JobId, command.Tick),
            JobStatus.InProgress => jobs.AdvanceStage(command.JobId, command.Tick),
            null => Result.Failure(JobErrors.NotFound),
            _ => Result.Failure(JobErrors.InvalidStatus),
        };

        _repository.Save(jobs);
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return result;
    }
}

public sealed class GetJobHandler : IQueryHandler<GetJobQuery, JobSnapshot?>
{
    private readonly IJobRepository _repository;

    public GetJobHandler(IJobRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public JobSnapshot? Handle(GetJobQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        return _repository.Get().Get(query.JobId);
    }
}

public sealed class GetJobsHandler : IQueryHandler<GetJobsQuery, IReadOnlyList<JobSnapshot>>
{
    private readonly IJobRepository _repository;

    public GetJobsHandler(IJobRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public IReadOnlyList<JobSnapshot> Handle(GetJobsQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        return _repository.Get().GetAll();
    }
}
}
