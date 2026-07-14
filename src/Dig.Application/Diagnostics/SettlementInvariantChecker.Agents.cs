using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Application.Diagnostics;

public sealed partial class SettlementInvariantChecker
{
    private void CheckAgentTargets(long tick)
    {
        InventoryState inventory = Require(_currentInventory);
        BuildingFacilitiesState facilities = Require(_currentFacilities);
        foreach (AgentState agentState in _agents.GetAll())
        {
            AgentSnapshot agent = agentState.CreateSnapshot(tick);
            AgentActivityTarget? target = agent.ActiveAction?.Target;
            if (!target.HasValue)
            {
                continue;
            }

            if (target.Value.Kind == AgentActivityTargetKind.Food)
            {
                if (!inventory.HasReservation(
                    target.Value.EntityId,
                    agent.Id,
                    minimumQuantity: 1))
                {
                    Add(
                        "agents.food_target_unreserved",
                        "Active food action has no matching item reservation.",
                        agent.Id);
                }

                continue;
            }

            BuildingFacilityKind expected = target.Value.Kind == AgentActivityTargetKind.Bed
                ? BuildingFacilityKind.Bed
                : BuildingFacilityKind.Leisure;
            if (!facilities.TryGetInspection(
                    target.Value.EntityId,
                    out BuildingFacilityInspection facility)
                || facility.Definition.Kind != expected
                || facility.ReservedAgentId != agent.Id)
            {
                Add(
                    "agents.facility_target_unreserved",
                    "Active facility action has no matching facility reservation.",
                    agent.Id);
            }
        }
    }

    public void VisitFacilityReservation(BuildingFacilityReservation reservation)
    {
        if (!_facilityAgents.Add(reservation.AgentId))
        {
            Add(
                "facilities.agent_multiple_reservations",
                "One resident owns multiple facility reservations.",
                reservation.AgentId);
        }

        AgentState? agentState = _agents.Get(reservation.AgentId);
        AgentActivityTarget? target = agentState?.CreateSnapshot(0).ActiveAction?.Target;
        if (!target.HasValue || target.Value.EntityId != reservation.FacilityId)
        {
            Add(
                "facilities.orphan_reservation",
                "Facility reservation has no matching active resident action.",
                reservation.FacilityId);
        }
    }
}
