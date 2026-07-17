using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Jobs;
using Xunit;

namespace Dig.Tests
{

public sealed class JobToolPreparationModeControlTests
{
    [Fact]
    public void Control_defaults_to_automatic_and_changes_explicitly()
    {
        JobToolPreparationModeControl control = new JobToolPreparationModeControl();

        Assert.Equal(JobToolPreparationMode.Automatic, control.Mode);
        Assert.Equal("Automatic", control.Label);
        Assert.True(control.Select(JobToolPreparationMode.Suggest));
        Assert.Equal(JobToolPreparationMode.Suggest, control.Mode);
        Assert.Equal("Suggest only", control.Label);
        Assert.False(control.Select(JobToolPreparationMode.Suggest));
    }

    [Fact]
    public void Suggest_source_overrides_automatic_command_without_switching()
    {
        RecordingPreparationService preparation = new RecordingPreparationService();

        JobAssignment assignment = Assign(
            JobToolPreparationMode.Automatic,
            JobToolPreparationMode.Suggest,
            preparation);

        Assert.Equal(JobToolPreparationOutcome.Suggested, assignment.ToolPreparation);
        Assert.Equal(0, preparation.CallCount);
    }

    [Fact]
    public void Automatic_source_overrides_suggest_command_and_switches()
    {
        RecordingPreparationService preparation = new RecordingPreparationService();

        JobAssignment assignment = Assign(
            JobToolPreparationMode.Suggest,
            JobToolPreparationMode.Automatic,
            preparation);

        Assert.Equal(JobToolPreparationOutcome.Switched, assignment.ToolPreparation);
        Assert.Equal(1, preparation.CallCount);
    }

    private static JobAssignment Assign(
        JobToolPreparationMode commandMode,
        JobToolPreparationMode selectedMode,
        RecordingPreparationService preparation)
    {
        EntityId jobId = Id(1);
        EntityId agentId = Id(2);
        EntityId toolStackId = Id(3);
        JobSystem jobs = new JobSystem();
        Assert.True(jobs.Add(new DigJobDefinition(
            jobId,
            new DigJobTarget(new CellId(4, 5)),
            priority: 500,
            createdTick: 0,
            retryPolicy: JobRetryPolicy.Default)).IsSuccess);
        Assert.True(jobs.MakeAvailable(jobId, tick: 0).IsSuccess);
        InMemoryJobRepository repository = new InMemoryJobRepository(jobs);
        InMemoryJobCandidateProvider candidates = new InMemoryJobCandidateProvider();
        candidates.SetCandidates(jobId, new[]
        {
            new JobCandidate(
                agentId,
                skillLevel: 5_000,
                distanceCost: 1,
                isAvailable: true,
                JobToolReadiness.SwitchAvailable,
                toolStackId),
        });
        JobToolPreparationModeControl control = new JobToolPreparationModeControl(selectedMode);
        JobAssignmentReport report = new AssignAvailableJobsHandler(
            repository,
            candidates,
            new InMemoryExecutionJournal(),
            preparation,
            assignmentReportSink: null,
            toolPreparationModeSource: control).Handle(
                new AssignAvailableJobsCommand(tick: 5, commandMode));

        return Assert.Single(report.Assignments);
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }

    private sealed class RecordingPreparationService : IJobToolPreparationService
    {
        public int CallCount { get; private set; }

        public Result Prepare(EntityId agentId, EntityId toolStackId, long tick)
        {
            CallCount++;
            return Result.Success();
        }
    }
}
}
