using System;
using System.Collections.Generic;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private CreateBuildingBoxRelocationHandler? _buildingBoxRelocationCreate;
        private AcquireBuildingBoxForRelocationHandler? _buildingBoxRelocationAcquire;
        private CompleteBuildingBoxRelocationHandler? _buildingBoxRelocationComplete;
        private InMemoryJobCandidateProvider? _buildingBoxRelocationCandidates;
        private AssignAvailableJobsHandler? _buildingBoxRelocationAssignment;

        private void InitializeBuildingBoxRelocationExecution(
            InMemoryExecutionJournal journal)
        {
            if (_buildingsRepository == null || _buildingInventoryRepository == null)
            {
                throw new InvalidOperationException(
                    "BuildingBox state must be initialized before relocation.");
            }

            _buildingBoxRelocationCreate = new CreateBuildingBoxRelocationHandler(
                _worldSession.Repository,
                _buildingsRepository,
                _buildingInventoryRepository,
                _jobRepository,
                journal);
            _buildingBoxRelocationAcquire = new AcquireBuildingBoxForRelocationHandler(
                _buildingInventoryRepository,
                _jobRepository,
                journal);
            _buildingBoxRelocationComplete = new CompleteBuildingBoxRelocationHandler(
                _buildingInventoryRepository,
                _jobRepository,
                journal);
            _buildingBoxRelocationCandidates = new InMemoryJobCandidateProvider();
            _buildingBoxRelocationAssignment = new AssignAvailableJobsHandler(
                _jobRepository,
                _buildingBoxRelocationCandidates,
                journal);
        }

        internal Result CreateBuildingBoxRelocation(
            EntityId stackId,
            CellId destination,
            long tick)
        {
            return CreateBuildingBoxRelocation(
                stackId,
                destination,
                GetBuildingPlacementReachableCells(),
                tick);
        }

        internal Result CreateBuildingBoxRelocation(
            EntityId stackId,
            CellId destination,
            IReadOnlyCollection<CellId> reachableCells,
            long tick)
        {
            EnsureBuildingBoxRelocationInitialized();
            if (reachableCells == null)
            {
                throw new ArgumentNullException(nameof(reachableCells));
            }
            ItemStackSnapshot? stack = _buildingInventoryRepository!.Get().GetStack(stackId);
            var definition = stack == null
                ? null
                : ResolveBuildingBoxDefinition(stack.ItemId);
            if (stack == null
                || definition?.BoxPolicy == null
                || stack.ItemId != definition.BoxPolicy.BoxItemId)
            {
                return Result.Failure(PlacementSourceUnavailable);
            }

            long sequence = checked(_nextPlacementSequence + 1);
            _nextPlacementSequence = sequence;
            return _buildingBoxRelocationCreate!.Handle(
                new CreateBuildingBoxRelocationCommand(
                    DemoId('9', sequence),
                    stackId,
                    definition.BoxPolicy.BoxItemId,
                    destination,
                    reachableCells,
                    priority: 625,
                    tick));
        }

        internal void SynchronizeBuildingBoxRelocation(
            long tick,
            IReadOnlyList<AgentViewModel> agents)
        {
            EnsureBuildingBoxRelocationInitialized();
            InventoryState inventory = _buildingInventoryRepository!.Get();
            bool hasAvailable = false;
            foreach (JobSnapshot job in _jobRepository.Get().GetAll())
            {
                if (job.Status != JobStatus.Available
                    || job.Definition is not BuildingBoxPickupJobDefinition relocation
                    || !relocation.IsRelocation)
                {
                    continue;
                }

                ItemStackSnapshot? box = inventory.GetStack(relocation.StackId);
                if (box == null)
                {
                    continue;
                }

                IReadOnlyList<JobCandidate> candidates =
                    CreateBuildingBoxAssemblyCandidates(agents, box);
                _buildingBoxRelocationCandidates!.SetCandidates(job.Id, candidates);
                hasAvailable = candidates.Count > 0 || hasAvailable;
            }

            if (hasAvailable)
            {
                _buildingBoxRelocationAssignment!.Handle(
                    new AssignAvailableJobsCommand(tick));
            }
        }

        private bool TryPlanBuildingBoxRelocationMovement(
            JobSnapshot job,
            BuildingBoxPickupJobDefinition relocation,
            AgentViewModel agent,
            NavigationSnapshot navigation,
            IDictionary<string, CellId> movement)
        {
            EnsureBuildingBoxRelocationInitialized();
            CellId start = new CellId(agent.CellX, agent.CellY, agent.CellZ);
            CellId target = ResolveBuildingBoxRelocationTarget(
                job,
                relocation,
                start,
                navigation,
                out PathResult path);
            _buildingBoxPickupRoutes[job.Id] = new BuildingBoxPickupRoutePlan(target, path);
            if (path.Succeeded)
            {
                movement[agent.Id] = path.Path!.Cells.Count > 1
                    ? path.Path.Cells[1]
                    : target;
            }

            return true;
        }

        private Result AdvanceBuildingBoxRelocation(
            JobSnapshot job,
            BuildingBoxPickupJobDefinition relocation,
            AgentViewModel agent,
            long tick)
        {
            CellId workerCell = new CellId(agent.CellX, agent.CellY, agent.CellZ);
            for (int index = 0; index < 5; index++)
            {
                JobSnapshot? current = _jobRepository.Get().Get(job.Id);
                if (current == null || current.IsTerminal)
                {
                    _buildingBoxPickupRoutes.Remove(job.Id);
                    return Result.Success();
                }

                if (!IsAtPreciseWorkPose(current, agent))
                {
                    return Result.Success();
                }

                ItemStackSnapshot? box = _buildingInventoryRepository!.Get().GetStack(
                    relocation.StackId);
                Result<BuildingBoxRelocationExecutionStepKind> evaluated =
                    BuildingBoxRelocationExecutionPolicy.Evaluate(
                        current,
                        box,
                        workerCell);
                if (evaluated.IsFailure)
                {
                    return Result.Failure(evaluated.Error!);
                }

                if (evaluated.Value == BuildingBoxRelocationExecutionStepKind.None)
                {
                    return Result.Success();
                }

                Result executed = ExecuteBuildingBoxRelocationStep(
                    evaluated.Value,
                    current,
                    workerCell,
                    tick);
                if (executed.IsFailure)
                {
                    return executed;
                }

                if (evaluated.Value
                    == BuildingBoxRelocationExecutionStepKind.CompleteRelocation)
                {
                    _buildingBoxPickupRoutes.Remove(job.Id);
                    return Result.Success();
                }
            }

            return Result.Failure(BuildingBoxPickupErrors.InvalidJobStage);
        }

        private Result ExecuteBuildingBoxRelocationStep(
            BuildingBoxRelocationExecutionStepKind step,
            JobSnapshot job,
            CellId workerCell,
            long tick)
        {
            return step switch
            {
                BuildingBoxRelocationExecutionStepKind.None => Result.Success(),
                BuildingBoxRelocationExecutionStepKind.StartJob =>
                    _advanceHandler.Handle(new AdvanceJobCommand(job.Id, tick)),
                BuildingBoxRelocationExecutionStepKind.AdvanceStage =>
                    _advanceHandler.Handle(new AdvanceJobCommand(job.Id, tick)),
                BuildingBoxRelocationExecutionStepKind.AcquireBox =>
                    AcquireAndAdvanceBuildingBoxRelocation(job.Id, workerCell, tick),
                BuildingBoxRelocationExecutionStepKind.CompleteRelocation =>
                    _buildingBoxRelocationComplete!.Handle(
                        new CompleteBuildingBoxRelocationCommand(job.Id, tick)),
                _ => Result.Failure(BuildingBoxPickupErrors.InvalidJobStage),
            };
        }

        private Result AcquireAndAdvanceBuildingBoxRelocation(
            EntityId jobId,
            CellId workerCell,
            long tick)
        {
            Result acquired = _buildingBoxRelocationAcquire!.Handle(
                new AcquireBuildingBoxForRelocationCommand(jobId, workerCell, tick));
            return acquired.IsFailure
                ? acquired
                : _advanceHandler.Handle(new AdvanceJobCommand(jobId, tick));
        }

        private void EnsureBuildingBoxRelocationInitialized()
        {
            if (_buildingBoxRelocationCreate == null
                || _buildingBoxRelocationAcquire == null
                || _buildingBoxRelocationComplete == null
                || _buildingBoxRelocationCandidates == null
                || _buildingBoxRelocationAssignment == null)
            {
                throw new InvalidOperationException(
                    "BuildingBox relocation execution is not initialized.");
            }
        }
    }
}
