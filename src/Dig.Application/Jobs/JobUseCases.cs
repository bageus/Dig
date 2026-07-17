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
    private readonly IJobToolPreparationService? _toolPreparationService;
    private readonly IJobAssignmentReportSink? _assignmentReportSink;
    private readonly IJobToolPreparationModeSource? _toolPreparationModeSource;

    public AssignAvailableJobsHandler(
        IJobRepository repository,
        IJobCandidateProvider candidateProvider,
        IEventSink eventSink,
        IJobToolPreparationService? toolPreparationService = null,
        IJobAssignmentReportSink? assignmentReportSink = null,
        IJobToolPreparationModeSource? toolPreparationModeSource = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _candidateProvider = candidateProvider
            ?? throw new ArgumentNullException(nameof(candidateProvider));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
        _toolPreparationService = toolPreparationService;
        _assignmentReportSink = assignmentReportSink;
        _toolPreparationModeSource = toolPreparationModeSource;
    }

    public JobAssignmentReport Handle(AssignAvailableJobsCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        JobToolPreparationMode preparationMode = ResolvePreparationMode(command);
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
                Result canClaim = jobs.CanClaim(
                    job.Id,
                    candidate.AgentId,
                    candidate.ToolStackId,
                    command.Tick);
                if (canClaim.IsFailure)
                {
                    failure = canClaim.Error!;
                    if (canClaim.Error != JobErrors.AgentUnavailable
                        && canClaim.Error != JobErrors.ReservationConflict)
                    {
                        break;
                    }

                    continue;
                }

                JobToolPreparationOutcome preparation = ResolvePreparation(
                    candidate,
                    preparationMode);
                if (candidate.ToolReadiness == JobToolReadiness.SwitchAvailable
                    && preparationMode == JobToolPreparationMode.Automatic)
                {
                    if (_toolPreparationService is null)
                    {
                        failure = JobErrors.ToolPreparationUnavailable;
                        continue;
                    }

                    EntityId toolStackId = candidate.ToolStackId
                        ?? throw new InvalidOperationException(
                            "Switchable candidates require a tool stack id.");
                    Result prepared = _toolPreparationService.Prepare(
                        candidate.AgentId,
                        toolStackId,
                        command.Tick);
                    if (prepared.IsFailure)
                    {
                        failure = prepared.Error!;
                        continue;
                    }
                }

                Result claim = jobs.Claim(
                    job.Id,
                    candidate.AgentId,
                    candidate.ToolStackId,
                    command.Tick);
                if (claim.IsSuccess)
                {
                    assignments.Add(new JobAssignment(
                        job.Id,
                        candidate.AgentId,
                        JobCandidateEvaluator.Score(job, candidate),
                        preparation,
                        candidate.ToolStackId));
                    assigned = true;
                    break;
                }

                failure = claim.Error!;
                if (claim.Error != JobErrors.AgentUnavailable
                    && claim.Error != JobErrors.ReservationConflict)
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
        JobAssignmentReport report = new JobAssignmentReport(
            command.Tick,
            assignments,
            failures);
        _assignmentReportSink?.Record(report);
        return report;
    }

    private JobToolPreparationMode ResolvePreparationMode(
        AssignAvailableJobsCommand command)
    {
        JobToolPreparationMode mode = _toolPreparationModeSource?.Mode
            ?? command.ToolPreparationMode;
        if (!Enum.IsDefined(typeof(JobToolPreparationMode), mode))
        {
            throw new InvalidOperationException(
                "The tool preparation mode source returned an invalid mode.");
        }

        return mode;
    }

    private static JobToolPreparationOutcome ResolvePreparation(
        JobCandidate candidate,
        JobToolPreparationMode mode)
    {
        return candidate.ToolReadiness switch
        {
            JobToolReadiness.Equipped => JobToolPreparationOutcome.AlreadyEquipped,
            JobToolReadiness.SwitchAvailable when mode == JobToolPreparationMode.Automatic =>
                JobToolPreparationOutcome.Switched,
            JobToolReadiness.SwitchAvailable => JobToolPreparationOutcome.Suggested,
            _ => JobToolPreparationOutcome.None,
        };
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
