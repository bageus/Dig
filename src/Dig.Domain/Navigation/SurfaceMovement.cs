using System;
using Dig.Domain.World;

namespace Dig.Domain.Navigation
{

public enum SurfaceFace
{
    Floor = 0,
    NegativeX = 1,
    PositiveX = 2,
    NegativeZ = 3,
    PositiveZ = 4,
}

public enum SurfaceMoverKind
{
    Resident = 0,
    CaveMonster = 1,
    Spider = 2,
    GroundEnemy = 3,
    Hamster = 4,
    Worm = 5,
}

/// <summary>
/// A deterministic position on one exposed voxel face. Coordinates use thousandths
/// of a cell so simulation state never depends on floating point rounding.
/// </summary>
public readonly struct SurfacePose : IEquatable<SurfacePose>
{
    public const int UnitsPerCell = 1_000;
    public const int CellCentre = UnitsPerCell / 2;

    public SurfacePose(CellId cell, SurfaceFace face, int u, int v)
    {
        if (!Enum.IsDefined(typeof(SurfaceFace), face))
        {
            throw new ArgumentOutOfRangeException(nameof(face));
        }

        if (u < 0.ú×¿-¢G§²ÚîÆ­yÓAvailable = true;
            }

            if (hasAvailable)
            {
                _buildingBoxAssemblyAssignment!.Handle(new AssignAvailableJobsCommand(tick));
            }
        }

        // Keep this marker in the execution source: IsAvailableForAutomaticPlanning.
        internal bool TryPlanBuildingBoxAssemblyMovement(
            JobSnapshot job,
            AgentViewModel agent,
            NavigationSnapshot navigation,
            IDictionary<string, CellId> movement)
        {
            if (job.Definition is not BuildingBoxAssemblyJobDefinition assembly)
            {
                return false;
            }

            EnsureBuildingBoxAssemblyInitialized();
            ItemStackSnapshot? box = _buildingInventoryRepository!.Get().GetStack(
                assembly.SourceStackId);
            CellId target = ResolveBuildingBoxAssemblyTarget(job, assembly, box);
            CellId start = new CellId(agent.CellX, agent.CellY, agent.CellZ);
            PathResult path = _buildingBoxAssemblyPathfinder!.FindPath(
                navigation,
                new PathRequest(start, target, navigation.NavigationVersion));
            _buildingBoxAssemblyRoutes[job.Id] = new BuildingBoxAssemblyRoutePlan(target, path);
            if (path.Succeeded)
            {
                movement[agent.Id] = path.Path!.Cells.Count > 1
                    ? path.Path.Cells[1]
                    : target;
            }

            return true;
        }

        internal Result AdvanceBuildingBoxAssembly(
            long tick,
            IReadOnlyList<AgentViewModel> agents)
        {
            EnsureBuildingBoxAssemblyInitialized();
            Dictionary<string, AgentViewModel> agentsById = agents.ToDictionary(
                agent => agent.Id,
                StringComparer.Ordinal);
            foreach (JobSnapshot job in _jobRepository.Get().GetAll())
            {
                if (!IsActive(job)
                    || job.Definition is not BuildingBoxAssemblyJobDefinition
                    || !job.AssignedAgentId.HasValue
                    || !agentsById.TryGetValue(
                        job.AssignedAgentId.Value.ToString(),
                        out AgentViewModel? agent))
                {
                    continue;
                }

                if (!IsAtPreciseWorkPose(job, agent))
                {
                    continue;
                }

                Result advanced = AdvanceBuildingBoxAssemblyJob(job.Id, agent, tick);
                if (advanced.IsFailure)
                {
                    return advanced;
                }
            }

            return Result.Success();
        }

        internal IReadOnlyList<RouteViewModel> LoadBuildingBoxAssemblyRoutes()
        {
            List<RouteViewModel> routes = new List<RouteViewModel>();
            foreach (KeyValuePair<EntityId, BuildingBoxAssemblyRoutePlan> pair
                in _buildingBoxAssemblyRoutes.OrderBy(
                    value => value.Key.ToString(),
                    StringComparer.Ordinal))
            {
                JobSnapshot? job = _jobRepository.Get().Get(pair.Key);
                if (job == null || !job.AssignedAgentId.HasValue)
                {
                    continue;
                }

                PathResult path = pair.Value.Path;
                RouteCellViewModel[] cells = path.Path == null
                    ? Array.Empty<RouteCellViewModel>()
                    : path.Path.Cells
                        .Select(cell => new RouteCellViewModel(cell.X, cell.Y, cell.Z))
                        .ToArray();
                routes.Add(new RouteViewModel(
                    pair.Key.ToString(),
                    job.AssignedAgentId.Value.ToString(),
                    pair.Value.Target.X,
                    pair.Value.Target.Y,
                    pair.Value.Target.Z,
                    path.Succeeded,
                    "BuildingBox assembly: " + path.Diagnostics.Detail,
                    path.Path?.TotalCost ?? 0,
                    path.Diagnostics.SnapshotVersion,
                    cells));
            }

            return routes;
        }

        private Result ExecuteBuildingBoxAssemblyStep(
            BuildingBoxAssemblyExecutionStepKind step,
            BuildingBoxAssemblyJobDefinition assembly,
            BuildingSnapshot building,
            EntityId workerId,
            CellId workerCell,
            long tick)
        {
            if (step == BuildingBoxAssemblyExecutionStepKind.None)
            {
                return Result.Success();
            }

            Result<PackableBuildingExecutionState> execution =
                GetOrCreatePackableBuildingExecution(
                    assembly.Id,
                    assembly.BuildingId,
                    building.Definition.Id,
                    PackableBuildingOperationKind.Unpack,
                    building.Definition.RequiredWork);
            if (execution.IsFailure)
            {
                return Result.Failure(execution.Error!);
            }

            if (step == BuildingBoxAssemblyExecutionStepKind.StartJob)
            {
                Result started = _packableBuildingExecutions!.StartOrResume(
                    assembly.Id,
                    workerId);
                return started.IsFailure
                    ? started
                    : _advanceHandler.Handle(new AdvanceJobCommand(assembly.Id, tick));
            }

            if (step == BuildingBoxAssemblyExecutionStepKind.AddWork)
            {
                return ExecutePackableBuildingIteration(
                    assembly.Id,
                    workerId,
                    tick,
                    () => _buildingBoxAssemblyWork!.Handle(
                        new AddBuildingBoxAssemblyWorkCommand(
                            assembly.BuildingId,
                            assembly.Id,
                            workAmount: 1,
                            tick: tick)));
            }

            return ExecuteBuildingBoxAssemblyTransition(step, assembly, workerCell, tick);
        }

        private Result ExecuteBuildingBoxAssemblyTransition(
            BuildingBoxAssemblyExecutionStepKind step,
            BuildingBoxAssemblyJobDefinition assembly,
            CellId workerCell,
            long tick)
        {
            return step switch
            {
                BuildingBoxAssemblyExecutionStepKind.AcquireBox =>
                    _buildingBoxAssemblyAcquire!.Handle(
                        new AcquireBuildingBoxForAssemblyCommand(
                            assembly.BuildingId,
                            assembly.Id,
                            workerCell,
                            tick)),
                BuildingBoxAssemblyExecutionStepKind.AdvanceStage =>
                    _advanceHandler.Handle(new AdvanceJobCommand(assembly.Id, tick)),
                BuildingBoxAssemblyExecutionStepKind.CommitBoxToSite =>
                    _buildingBoxAssemblyCommit!.Handle(new CommitBuildingBoxToSiteCommand(
                        assembly.BuildingId,
                        assembly.Id,
                        tick)),
                BuildingBoxAssemblyExecutionStepKind.CompleteAssembly =>
                    _buildingBoxAssemblyComplete!.Handle(
                        new CompleteBuildingBoxAssemblyCommand(
                            assembly.BuildingId,
                            assembly.Id,
                            tick)),
                _ => throw new ArgumentOutOfRangeException(nameof(step)),
            };
        }

        private void EnsureBuildingBoxAssemblyInitialized()
        {
            if (_buildingsRepository == null
                || _buildingInventoryRepository == null
                || _buildingBoxAssemblyCandidates == null
                || _buildingBoxAssemblyAssignment == null
                || _buildingBoxAssemblyAcquire == null
                || _buildingBoxAssemblyCommit == null
                || _buildingBoxAssemblyWork == null
                || _buildingBoxAssemblyComplete == null
                || _buildingBoxAssemblyPathfinder == null
                || _packableBuildingExecutions == null
                || _campfireIterationProgression == null)
            {
                throw new InvalidOperationException(
                    "BuildingBox assembly execution is not initialized.");
            }
        }
    }
}
