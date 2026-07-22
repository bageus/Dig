using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;
using Dig.Presentation.Jobs;
using Dig.Presentation.World;

namespace Dig.Unity
{
    internal sealed class DigJobSession
    {
        private readonly AdvanceJobHandler _advanceHandler;
        private readonly JobOverlayPresenter _presenter;
        private readonly InMemoryJobRepository _repository;

        private DigJobSession(
            AdvanceJobHandler advanceHandler,
            JobOverlayPresenter presenter,
            InMemoryJobRepository repository)
        {
            _advanceHandler = advanceHandler;
            _presenter = presenter;
            _repository = repository;
        }

        public static DigJobSession CreateDemo(
            WorldViewModel world,
            IReadOnlyList<AgentViewModel> agents,
            InMemoryExecutionJournal journal)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (agents == null)
            {
                throw new ArgumentNullException(nameof(agents));
            }

            WorldCellViewModel[] targets = world.Chunks
                .SelectMany(chunk => chunk.Cells)
                .Where(cell => cell.IsDesignated)
                .OrderBy(cell => cell.Y)
                .ThenBy(cell => cell.X)
                .ToArray();
            if (targets.Length == 0 || agents.Count == 0)
            {
                throw new InvalidOperationException(
                    "The job overlay demo requires designated cells and residents.");
            }

            InMemoryJobRepository repository = new InMemoryJobRepository();
            InMemoryJobCandidateProvider candidates = new InMemoryJobCandidateProvider();
            CreateDigJobHandler create = new CreateDigJobHandler(repository, journal);
            for (int index = 0; index < targets.Length; index++)
            {
                EntityId jobId = EntityId.Parse(
                    $"400000000000000000000000000000{index + 1:D2}");
                WorldCellViewModel target = targets[index];
                DigJobDefinition definition = new DigJobDefinition(
                    jobId,
                    new DigJobTarget(new Dig.Domain.World.CellId(target.X, target.Y)),
                    priority: 800 - (index * 50),
                    createdTick: 0,
                    new JobRetryPolicy(maximumRetries: 2, retryDelayTicks: 3));
                Result created = create.Handle(new CreateDigJobCommand(
                    definition,
                    makeAvailable: true));
                if (created.IsFailure)
                {
                    throw new InvalidOperationException(created.Error!.ToString());
                }

                candidates.SetCandidates(jobId, CreateCandidates(agents, target));
            }

            AssignAvailableJobsHandler assign = new AssignAvailableJobsHandler(
                repository,
                candidates,
                journal);
            assign.Handle(new AssignAvailableJobsCommand(tick: 0));
            AdvanceJobHandler advance = new AdvanceJobHandler(repository, journal);
            foreach (JobSnapshot job in repository.Get().GetAll())
            {
                if (job.Status == JobStatus.Claimed)
                {
                    Result started = advance.Handle(new AdvanceJobCommand(job.Id, tick: 0));
                    if (started.IsFailure)
                    {
                        throw new InvalidOperationException(started.Error!.ToString());
                    }
                }
            }

            return new DigJobSession(
                advance,
                new JobOverlayPresenter(
                    new GetJobsHandler(repository),
                    new GetJobReservationsHandler(repository)),
                repository);
        }

        public IReadOnlyList<JobOverlayViewModel> LoadView()
        {
            return _presenter.Load();
        }

        public Result Advance(long tick)
        {
            if (tick < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tick));
            }

            if (tick % 3 != 0)
            {
                return Result.Success();
            }

            foreach (JobSnapshot job in _repository.Get().GetAll())
            {
                if (job.Status != JobStatus.Claimed
                    && job.Status != JobStatus.InProgress)
                {
                    continue;
                }

                Result result = _advanceHandler.Handle(new AdvanceJobCommand(job.Id, tick));
                if (result.IsFailure)
                {
                    return result;
                }
            }

            return Result.Success();
        }

        private static IReadOnlyList<JobCandidate> CreateCandidates(
            IReadOnlyList<AgentViewModel> agents,
            WorldCellViewModel target)
        {
            JobCandidate[] candidates = new JobCandidate[agents.Count];
            for (int index = 0; index < agents.Count; index++)
            {
                AgentViewModel agent = agents[index];
                int distance = Math.Abs(agent.CellX - target.X)
                    + Math.Abs(agent.CellY - target.Y);
                candidates[index] = new JobCandidate(
                    EntityId.Parse(agent.Id),
                    skillLevel: 5_000 - (index * 250),
                    distanceCost: distance,
                    isAvailable: agent.IsAvailableForAutomaticPlanning);
            }

            return candidates;
        }
    }
}
