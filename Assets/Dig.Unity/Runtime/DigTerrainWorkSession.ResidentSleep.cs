using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Presentation.Agents;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private void ExecuteSleepAction(
        AgentState agent,
        AgentDecision decision,
        AgentBehaviorPolicy policy,
        long tick)
    {
        BuildingFacilitiesState facilities = _residentFacilities!.Get();
        AgentActionSnapshot? active = agent.CreateSnapshot(tick).ActiveAction;
        AgentActivityTarget target;
        if (active?.IntentKind == AgentIntentKind.Sleep
            && active.Value.Target.HasValue
            && IsValidSleepTarget(agent.Id, active.Value.Target.Value, facilities))
        {
            target = active.Value.Target.Value;
        }
        else
        {
            facilities.ReleaseForAgent(agent.Id, tick);
            target = ResolveSleepTarget(agent, facilities, tick);
            _residentFacilities.Save(facilities);
            PublishFacilityEvents(facilities);
        }

        RequireResidentNeeds(agent.ApplyDecision(decision, policy, target, tick));
        if (target.Kind == AgentActivityTargetKind.Bed)
        {
            BuildingFacilitySnapshot? facility = facilities.Get(target.EntityId);
            if (facility == null || agent.Position != facility.Definition.Position)
            {
                return;
            }
        }

        Result<bool> progressed = agent.AdvanceTargetedAction(policy, tick);
        if (progressed.IsFailure)
        {
            throw new InvalidOperationException(progressed.Error!.ToString());
        }

        if (!progressed.Value)
        {
            return;
        }

        if (target.Kind == AgentActivityTargetKind.Bed)
        {
            facilities.ReleaseForAgent(agent.Id, tick);
            _residentFacilities.Save(facilities);
            PublishFacilityEvents(facilities);
        }

        RequireResidentNeeds(agent.CompleteTargetedAction(policy, tick));
    }

    private AgentActivityTarget ResolveSleepTarget(
        AgentState agent,
        BuildingFacilitiesState facilities,
        long tick)
    {
        if (TryLoadBuildingPlacementNavigation(out NavigationSnapshot navigation))
        {
            NavigationPathfinder pathfinder = new NavigationPathfinder();
            BuildingFacilitySnapshot? selected = null;
            int selectedCost = int.MaxValue;
            foreach (BuildingFacilitySnapshot candidate in facilities
                .FindAvailable(BuildingFacilityKind.Bed, agent.Id))
            {
                PathResult path = pathfinder.FindPath(
                    navigation,
                    new PathRequest(
                        agent.Position,
                        candidate.Definition.Position,
                        navigation.NavigationVersion));
                if (!path.Succeeded || path.Path == null)
                {
                    continue;
                }

                int cost = path.Path.TotalCost;
                if (selected == null
                    || cost < selectedCost
                    || (cost == selectedCost
                        && string.Compare(
                            candidate.Definition.Id.ToString(),
                            selected.Definition.Id.ToString(),
                            StringComparison.Ordinal) < 0))
                {
                    selected = candidate;
                    selectedCost = cost;
                }
            }

            if (selected != null)
            {
                Result reserved = facilities.Reserve(
                    selected.Definition.Id,
                    agent.Id,
                    tick);
                if (reserved.IsSuccess)
                {
                    return new AgentActivityTarget(
                        AgentActivityTargetKind.Bed,
                        selected.Definition.Id);
                }
            }
        }

        return new AgentActivityTarget(
            AgentActivityTargetKind.FloorSleep,
            agent.Id);
    }

    private bool TryPlanResidentSleepMovement(
        AgentViewModel agent,
        NavigationSnapshot navigation,
        IDictionary<string, CellId> movement)
    {
        if (_residentFacilities == null || _productionAgents == null)
        {
            return false;
        }

        AgentSnapshot? state = _productionAgents.Get(EntityId.Parse(agent.Id))?
            .CreateSnapshot(tick: 0);
        AgentActivityTarget? target = state?.ActiveAction?.Target;
        if (state?.ActiveAction?.IntentKind != AgentIntentKind.Sleep
            || !target.HasValue
            || target.Value.Kind != AgentActivityTargetKind.Bed)
        {
            return false;
        }

        BuildingFacilitySnapshot? facility = _residentFacilities.Get()
            .Get(target.Value.EntityId);
        if (facility == null)
        {
            return false;
        }

        CellId start = new CellId(agent.CellX, agent.CellY, agent.CellZ);
        PathResult path = new NavigationPathfinder().FindPath(
            navigation,
            new PathRequest(
                start,
                facility.Definition.Position,
                navigation.NavigationVersion));
        if (!path.Succeeded || path.Path == null)
        {
            return true;
        }

        movement[agent.Id] = path.Path.Cells.Count > 1
            ? path.Path.Cells[1]
            : facility.Definition.Position;
        return true;
    }

    private void SynchronizeResidentSleepFacilities(long tick)
    {
        if (_residentFacilities == null || _buildingsRepository == null)
        {
            return;
        }

        BuildingFacilitiesState facilities = _residentFacilities.Get();
        BuildingSnapshot[] tents = _buildingsRepository.Get().GetAll()
            .Where(value => value.Status == BuildingStatus.Completed
                && value.Definition.Id == CampfireProductionContent.TentBuildingId)
            .OrderBy(value => value.Id.ToString(), StringComparer.Ordinal)
            .ToArray();
        Dictionary<EntityId, BuildingSnapshot> tentsById = tents.ToDictionary(
            value => value.Id);
        EntityId[] registeredBuildings = facilities.GetAllFacilities()
            .Select(value => value.Definition.BuildingId)
            .Distinct()
            .ToArray();
        foreach (EntityId buildingId in registeredBuildings)
        {
            if (!tentsById.ContainsKey(buildingId))
            {
                facilities.RemoveByBuilding(buildingId, tick);
            }
        }

        if (TryLoadBuildingPlacementNavigation(out NavigationSnapshot navigation))
        {
            foreach (BuildingSnapshot tent in tents)
            {
                CellId[] positions = tent.Footprint
                    .Where(navigation.IsWalkable)
                    .Where(HasFullStandingSupport)
                    .Distinct()
                    .OrderBy(value => value)
                    .Take(2)
                    .ToArray();
                if (positions.Length < 2)
                {
                    facilities.RemoveByBuilding(tent.Id, tick);
                    continue;
                }

                BuildingFacilitySnapshot[] existing = facilities.GetAllFacilities()
                    .Where(value => value.Definition.BuildingId == tent.Id)
                    .OrderBy(value => value.Definition.Id.ToString(), StringComparer.Ordinal)
                    .ToArray();
                EntityId[] ids =
                {
                    CreateTentSlotId(tent.Id, 0),
                    CreateTentSlotId(tent.Id, 1),
                };
                bool exact = existing.Length == 2
                    && existing.Select(value => value.Definition.Id).SequenceEqual(ids)
                    && existing.Select(value => value.Definition.Position)
                        .SequenceEqual(positions);
                if (exact)
                {
                    continue;
                }

                facilities.RemoveByBuilding(tent.Id, tick);
                for (int index = 0; index < positions.Length; index++)
                {
                    Result added = facilities.Add(new BuildingFacilityDefinition(
                        ids[index],
                        tent.Id,
                        BuildingFacilityKind.Bed,
                        positions[index]));
                    RequireResidentNeeds(added);
                }
            }
        }

        _residentFacilities.Save(facilities);
        PublishFacilityEvents(facilities);
    }


    private void ReleaseInterruptedSleepReservation(
        AgentState agent,
        AgentDecision decision,
        long tick)
    {
        AgentActionSnapshot? action = agent.CreateSnapshot(tick).ActiveAction;
        if (decision.SelectedIntent == AgentIntentKind.Sleep
            || action?.Target?.Kind != AgentActivityTargetKind.Bed)
        {
            return;
        }

        BuildingFacilitiesState facilities = _residentFacilities!.Get();
        facilities.ReleaseForAgent(agent.Id, tick);
        _residentFacilities.Save(facilities);
        PublishFacilityEvents(facilities);
    }

    private bool IsValidSleepTarget(
        EntityId agentId,
        AgentActivityTarget target,
        BuildingFacilitiesState facilities)
    {
        return target.Kind == AgentActivityTargetKind.FloorSleep
            ? target.EntityId == agentId
            : target.Kind == AgentActivityTargetKind.Bed
                && facilities.IsReservedBy(
                    target.EntityId,
                    agentId,
                    BuildingFacilityKind.Bed);
    }


    private static EntityId CreateTentSlotId(EntityId buildingId, int slot)
    {
        byte[] bytes = Guid.ParseExact(buildingId.ToString(), "N").ToByteArray();
        bytes[14] ^= 0x54;
        bytes[15] ^= (byte)(0x70 + slot);
        return new EntityId(new Guid(bytes));
    }

}

}
