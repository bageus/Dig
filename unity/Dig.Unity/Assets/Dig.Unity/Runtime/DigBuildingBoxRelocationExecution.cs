using System;
using System.Collections.Generic;
using System.Linq;
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
            EnsureBuildingBoxRelocationInitialized();
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
                    GetBuildingPlacementReachableCells(),
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
                if (box?.Location.Kind != ItemLocationKind.World
                    || !box.Location.HasCell)
                {
                    continue;
                }

                CellId source = box.Location.CellId;
                JobCandidate[] candidates = agents
                    .Where(agent => agent.IsAlive && !string.IsNullOrWhiteSpace(agent.Id))
                    .Select(agent => new JobCandidate(
                        EntityId.Parse(agent.Id),
                        skillLevel: 5_000,
                        distanceCost: Math.Abs(agent.CellX - source.X)
                            + Math.Abs(agent.CellY - source.Y)
                            + Math.Abs(agent.CellZ - source.Z),
                        isAvailable: true))
                    .ToArray();
                _buildingBoxRelocationCandidates!.SetCandidates(job.Id, candidates);
                hasAvailable = candidates.Length > 0 || hasAvailable;
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
            CellId target = ResolveBuildingBoxRelocationTarget(job, relocation);
            CellId start = new CellId(agent.CellX, agent.CellY, agent.CellZ);
            PathResult path = _buildingBoxPickupPathfinder!.FindPath(
                navigation,
                new PathRequest(start, target, navigation.NavigationVersion));
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
            CellId target = ResolveBuildingBoxRelocationTarget(job, relocation);
            if (agent.CellX != target.X || agent.CellY != target.Y || agent.CellZ != target.Z)
            {
                return Result.Success();
            }

            if (job.Status == JobStatus.Claimed)
            {
                return _advanceHandler.Handle(new AdvanceJobCommand(job.Id, tick));
            }

            if (job.Stage == JobStageKind.TravelToTarget
                || job.Stage == JobStageKind.TravelToDestination)
            {
                return _advanceHandler.Handle(new AdvanceJobCommand(job.Id, tick));
            }

            if (job.Stage == JobStageKind.AcquireItem)
            {
                Result acquired = _buildingBoxRelocationAcquire!.Handle(
                    new AcquireBuildingBoxForRelocationCommand(
                        job.Id,
                        new CellId(agent.CellX, agent.CellY, agent.CellZ),
                        tick));
                return acquired.IsSuccess
                    ? _advanceHandler.Handle(new AdvanceJobCommand(job.Id, tick))
                    : acquired;
            }

            if (job.Stage != JobStageKind.DepositItem)
            {
                return Result.Success();
            }

            Result completed = _buildingBoxRelocationComplete!.Handle(
                new CompleteBuildingBoxRelocationCommand(job.Id, tick));
            if (completed.IsSuccess)
            {
                _buildingBoxPickupRoutes.Remove(job.Id);
            }

            return completed;
        }

        private CellId ResolveBuildingBoxRelocationTarget(
            JobSnapshot job,
            BuildingBoxPickupJobDefinition relocation)
        {
            ItemStackSnapshot? box = _buildingInventoryRepository!.Get().GetStack(
                relocation.StackId);
            bool carriedByWorker = job.AssignedAgentId.HasValue
                && box?.Location == ItemLocation.InAgent(job.AssignedAgentId.Value);
            return relocation.StartsHeld
                || carriedByWorker
                || job.Stage == JobStageKind.TravelToDestination
                || job.Stage == JobStageKind.DepositItem
                    ? relocation.DestinationCell!.Value
                    : relocation.SourceCell;
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
