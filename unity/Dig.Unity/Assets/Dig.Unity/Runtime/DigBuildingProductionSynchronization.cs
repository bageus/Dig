using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Jobs;
using Dig.Application.Production;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.Production;
using Dig.Domain.Technology;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;
using Dig.Presentation.Production;
using Dig.Presentation.Navigation;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    internal void SynchronizeBuildingProduction(
        long tick,
        IReadOnlyList<AgentViewModel> agents)
    {
        EnsureBuildingProductionInitialized();
        SynchronizeProductionWorkstationRegistrations(tick);
        SynchronizeRequiredProductionInputDelivery(tick);
        RecoverBlockedBuildingSupplyJobs(tick);
        if (!TryLoadBuildingPlacementNavigation(out NavigationSnapshot navigation))
        {
            return;
        }

        ResolveEligibleDeferredSupplyJobs(tick, agents, navigation);
        CreateEligibleSupplyJobs(tick, agents, navigation);
        CreateEligibleFoodDependencyJobs(tick, agents, navigation);
        PrepareEligibleProductionOrders(tick, navigation);
        AssignProductionJobs(tick, agents, navigation);
    }

    internal Result AdvanceBuildingProduction(
        long tick,
        IReadOnlyList<AgentViewModel> agents)
    {
        EnsureBuildingProductionInitialized();
        Dictionary<string, AgentViewModel> agentsById = agents.ToDictionary(
            value => value.Id,
            StringComparer.Ordinal);
        foreach (JobSnapshot job in _jobRepository.Get().GetAll())
        {
            if (!IsActive(job)
                || !job.AssignedAgentId.HasValue
                || !agentsById.TryGetValue(
                    job.AssignedAgentId.Value.ToString(),
                    out AgentViewModel? worker))
            {
                continue;
            }

            Result result = Result.Success();
            if (job.Definition is ProductionWorkJobDefinition production)
            {
                result = AdvanceProductionJob(job, production, worker, tick);
            }
            else if (job.Definition is BuildingSupplyJobDefinition supply)
            {
                result = AdvanceSupplyJob(job, supply, worker, tick);
            }

            if (result.IsFailure)
            {
                return result;
            }
        }

        return Result.Success();
    }

    internal bool TryPlanBuildingSupplyMovement(
        JobSnapshot job,
        AgentViewModel agent,
        NavigationSnapshot navigation,
        IDictionary<string, CellId> movement,
        long tick)
    {
        if (job.Definition is not BuildingSupplyJobDefinition supply)
        {
            return false;
        }

        EnsureBuildingProductionInitialized();
        CellId target = supply.WorkPosition;
        if (job.Stage == JobStageKind.AcquireItem)
        {
            ItemReservationAllocation? pending = FindPendingSupplyAllocation(
                job.Id,
                supply);
            if (pending.HasValue)
            {
                ItemStackSnapshot? source = _buildingInventoryRepository!.Get()
                    .GetStack(pending.Value.StackId);
                if (source?.Location.Kind == ItemLocationKind.World
                    && source.Location.HasCell)
                {
                    target = source.Location.CellId;
                }
            }
        }

        PlanBuildingProductionRoute(
            _buildingSupplyRoutes,
            job,
            agent,
            target,
            navigation,
            movement);
        if (_buildingSupplyRoutes.TryGetValue(
                job.Id,
                out BuildingProductionRoutePlan? route)
            && !route.Path.Succeeded)
        {
            _cancelBuildingSupply!.Handle(new CancelBuildingSupplyCommand(
                job.Id,
                "route_unavailable",
                tick));
            _buildingSupplyRoutes.Remove(job.Id);
        }

        return true;
    }

    internal IReadOnlyList<RouteViewModel> LoadBuildingProductionRoutes()
    {
        return PresentBuildingProductionRoutes(
                _buildingProductionRoutes,
                "Production")
            .Concat(PresentBuildingProductionRoutes(
                _buildingSupplyRoutes,
                "Building supply"))
            .ToArray();
    }

    private void SynchronizeProductionWorkstationRegistrations(long tick)
    {
        BuildingSupplyState supply = _buildingSupplyRepository!.Get();
        foreach (BuildingSnapshot building in _buildingsRepository!.Get().GetAll())
        {
            if (building.Status == BuildingStatus.Completed
                && _productionContent!.ContainsWorkstation(building.Definition.Id))
            {
                Result registered = supply.Register(
                    building.Id,
                    _productionContent.GetWorkstation(building.Definition.Id),
                    tick);
                if (registered.IsFailure)
                {
                    throw new InvalidOperationException(registered.Error!.ToString());
                }
            }
        }

        _buildingSupplyRepository.Save(supply);
    }

    private void PrepareEligibleProductionOrders(
        long tick,
        NavigationSnapshot navigation)
    {
        foreach (BuildingSnapshot building in _buildingsRepository!.Get().GetAll())
        {
            if (building.Status != BuildingStatus.Completed
                || !_productionContent!.ContainsWorkstation(building.Definition.Id)
                || ShouldWaitForSupplyBeforeProduction(building.Id)
                || _buildingSupplyRepository!.Get().Get(
                    building.Id,
                    _buildingInventoryRepository!.Get().CreateSnapshot())?.HasActiveSupply
                    == true
                || _productionRepository!.Get().HasActiveOrder(building.Id)
                || _productionRepository.Get().GetNextQueued(building.Id) == null)
            {
                continue;
            }

            IReadOnlyCollection<CellId> reachable = GetProductionReachableCells(
                navigation,
                building.WorkPosition);
            EntityId jobId = NextProductionEntityId(
                'f',
                ref _nextProductionJobSequence);
            Result prepared = _prepareProduction!.Handle(
                new PrepareProductionOrderCommand(
                    jobId,
                    building.Id,
                    reachable,
                    priority: 700,
                    tick));
            if (prepared.IsFailure
                && prepared.Error != InventoryErrors.InsufficientAvailableQuantity
                && prepared.Error != ProductionErrors.QueueBlocked)
            {
                continue;
            }
        }
    }

    private void AssignProductionJobs(
        long tick,
        IReadOnlyList<AgentViewModel> agents,
        NavigationSnapshot navigation)
    {
        bool available = false;
        foreach (JobSnapshot job in _jobRepository.Get().GetAll())
        {
            if (job.Status != JobStatus.Available
                || job.Definition is not ProductionWorkJobDefinition production)
            {
                continue;
            }

            RecipeDefinition recipe = _productionContent!.GetRecipe(production.RecipeId);
            AgentSkillId skill = recipe.MaterialSteps.Count == 0
                ? AgentSkillCatalog.Logistics
                : recipe.MaterialSteps[0].SkillId;
            JobCandidate[] candidates = agents
                .Select(agent => new JobCandidate(
                    EntityId.Parse(agent.Id),
                    _productionAgents!.Get(EntityId.Parse(agent.Id))?
                        .CreateSnapshot(tick)
                        .GetSkillLevel(skill) ?? 0,
                    Distance(agent, production.WorkPosition),
                    IsAvailableForAutomaticWork(agent)
                        && BuildingSupplyReachability.IsConnected(
                            navigation,
                            new CellId(agent.CellX, agent.CellY, agent.CellZ),
                            production.WorkPosition)))
                .ToArray();
            _productionCandidates!.SetCandidates(job.Id, candidates);
            available = true;
        }

        if (available)
        {
            _productionAssignment!.Handle(new AssignAvailableJobsCommand(tick));
        }
    }

    private void CreateEligibleSupplyJobs(
        long tick,
        IReadOnlyList<AgentViewModel> agents,
        NavigationSnapshot navigation)
    {
        CellId[] revealed = GetProductionRevealedCells();
        BuildingSupplyState supply = _buildingSupplyRepository!.Get();
        InventorySnapshot inventory = _buildingInventoryRepository!.Get().CreateSnapshot();
        foreach (BuildingSupplySnapshot snapshot in supply.GetAll(inventory))
        {
            ProductionOrderSnapshot? queued = _productionRepository!.Get()
                .GetNextQueued(snapshot.BuildingId);
            BuildingSnapshot? building = _buildingsRepository!.Get().Get(
                snapshot.BuildingId);
            if (building == null
                || building.Status != BuildingStatus.Completed
                || snapshot.HasActiveSupply
                || HasNonTerminalResolvedBuildingSupplyJob(snapshot.BuildingId)
                || HasNonTerminalProductionWorkJob(snapshot.BuildingId)
                || ShouldYieldSupplyTurnToRunnableProduction(snapshot, queued)
                || snapshot.Stocks.All(value =>
                    !value.DeliveryEnabled || value.Missing == 0))
            {
                continue;
            }

            CellId[] reachable = GetProductionReachableCells(
                navigation,
                building.WorkPosition).ToArray();
            AgentViewModel[] candidates = agents
                .Where(value => IsAvailableForAutomaticWork(value)
                    && reachable.Contains(new CellId(
                        value.CellX,
                        value.CellY,
                        value.CellZ)))
                .OrderBy(value => Distance(value, building.WorkPosition))
                .ThenBy(value => value.Id, StringComparer.Ordinal)
                .ToArray();
            foreach (AgentViewModel agent in candidates)
            {
                EntityId jobId = NextProductionEntityId(
                    'd',
                    ref _nextSupplyJobSequence);
                EntityId[] transit = Enumerable.Range(0, 12)
                    .Select(_ => NextProductionEntityId(
                        'c',
                        ref _nextSupplyTransitSequence))
                    .ToArray();
                EntityId[] deposits = Enumerable.Range(0, snapshot.Stocks.Count)
                    .Select(_ => NextProductionEntityId(
                        'b',
                        ref _nextSupplyDepositSequence))
                    .ToArray();
                Result created = _createBuildingSupply!.Handle(
                    new CreateBuildingSupplyJobCommand(
                        jobId,
                        snapshot.BuildingId,
                        EntityId.Parse(agent.Id),
                        revealed,
                        reachable,
                        transit,
                        deposits,
                        priority: 650,
                        tick,
                        targetItemIds: queued?.Recipe.Inputs
                            .Select(value => value.ItemId)
                            .Distinct()
                            .OrderBy(value => value)
                            .ToArray()));
                if (created.IsSuccess)
                {
                    break;
                }
            }
        }
    }

}

}
