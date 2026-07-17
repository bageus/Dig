using System;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Jobs;
using Xunit;

namespace Dig.Tests
{

public sealed class JobAssignmentReportJournalTests
{
    private static readonly EntityId JobId = Id(1);
    private static readonly EntityId AgentId = Id(2);
    private static readonly EntityId ToolStackId = Id(3);

    [Fact]
    public void Assignment_handler_records_successful_decision_in_journal()
    {
        InMemoryJobRepository jobs = CreateJobs();
        InMemoryJobCandidateProvider candidates = new InMemoryJobCandidateProvider();
        candidates.SetCandidates(JobId, new[]
        {
            new JobCandidate(
                AgentId,
                skillLevel: 5_000,
                distanceCost: 2,
                isAvailable: true,
                toolReadiness: JobToolReadiness.Equipped,
                toolStackId: ToolStackId),
        });
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();

        JobAssignmentReport report = new AssignAvailableJobsHandler(
            jobs,
            candidates,
            journal,
            assignmentReportSink: journal).Handle(
                new AssignAvailableJobsCommand(tick: 4));

        JobAssignmentReport stored = journal.JobAssignmentReports[JobId];
        JobAssignment assignment = Assert.Single(stored.Assignments);
        Assert.Equal(report.Tick, stored.Tick);
        Assert.Equal(JobId, assignment.JobId);
        Assert.Equal(AgentId, assignment.AgentId);
        Assert.Equal(JobToolPreparationOutcome.AlreadyEquipped, assignment.ToolPreparation);
        Assert.Equal(ToolStackId, assignment.ToolStackId);
    }

    [Fact]
    public void Journal_replaces_job_diagnostic_and_presenter_reads_indexed_report()
    {
        InMemoryJobRepository jobs = CreateJobs();
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        journal.Record(CreateReport(
            tick: 2,
            JobToolPreparationOutcome.Suggested));
        journal.Record(CreateReport(
            tick: 7,
            JobToolPreparationOutcome.Switched));
        JobOverlayPresenter presenter = new JobOverlayPresenter(
            new GetJobsHandler(jobs),
            new GetJobReservationsHandler(jobs));

        JobOverlayViewModel model = Assert.Single(
            presenter.LoadIndexed(journal.JobAssignmentReports));
        JobAssignmentDiagnosticViewModel diagnostic =
            Assert.IsType<JobAssignmentDiagnosticViewModel>(model.AssignmentDiagnostic);

        Assert.Equal(7, diagnostic.Tick);
        Assert.Equal(JobToolPreparationOutcome.Switched, diagnostic.ToolPreparation);
        Assert.Equal(ToolStackId.ToString(), diagnostic.ToolStackId);
        Assert.False(diagnostic.IsFailure);
    }

    private static InMemoryJobRepository CreateJobs()
    {
        InMemoryJobRepository jobs = new InMemoryJobRepository();
        DigJobDefinition definition = new DigJobDefinition(
            JobId,
            new DigJobTarget(new CellId(4, 5)),
            priority: 500,
            createdTick: 0,
            retryPolicy: JobRetryPolicy.Default);
        Assert.True(jobs.Get().Add(definition).IsSuccess);
        Assert.True(jobs.Get().MakeAvailable(JobId, tick: 0).IsSuccess);
        return jobs;
    }

    private static JobAssignmentReport CreateReport(
        long tick,
        JobToolPreparationOutcome outcome)
    {
        return new JobAssignmentReport(
            tick,
            new[]
            {
                new JobAssignment(
                    JobId,
                    AgentId,
                    score: 10_000,
                    toolPreparation: outcome,
                    toolStackId: ToolStackId),
            },
            Array.Empty<JobAssignmentFailure>());
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}
}
