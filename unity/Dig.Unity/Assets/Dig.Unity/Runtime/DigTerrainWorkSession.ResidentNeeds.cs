using System;
using System.Collections.Generic;
using Dig.Domain.Buildings;
using Dig.Infrastructure.InMemory;
using Dig.Application.Agents;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Presentation.Agents;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private InMemoryBuildingFacilitiesRepository? _residentFacilities;

    internal void InitializeResidentNeedsRuntime(
        long tick,
        IReadOnlyList<AgentViewModel> agents)
    {
        _residentFacilities = new InMemoryBuildingFacilitiesRepository(
            new BuildingFacilitiesState());
        SynchronizeResidentSleepFacilities(tick);
    }

    internal AgentDecisionContext CreateResidentNeedsContext(
        AgentSnapshot agent,
        long tick)
    {
        EnsureResidentNeedsInitialized();
        SynchronizeResidentSleepFacilities(tick);
        return new AgentDecisionContext(
            foodAvailable: HasAutomaticFoodSource(agent),
            bedAvailable: true,
            workAvailable: true,
            restAvailable: true,
            escapeRouteAvailable: true,
            threatLevel: 0);
    }

    internal bool TryExecuteResidentNeedsAction(
        AgentState agent,
        AgentDecision decision,
        AgentBehaviorPolicy policy,
        long tick)
    {
        EnsureResidentNeedsInitialized();
        ReleaseInterruptedSleepReservation(agent, decision, tick);
        if (decision.SelectedIntent == AgentIntentKind.Eat)
        {
            RequireResidentNeeds(agent.ApplyDecision(decision, policy, tick));
            Result planned = EnsureAutomaticFoodPlan(agent, tick);
            if (planned.IsFailure)
            {
                throw new InvalidOperationException(planned.Error!.ToString());
            }

            return true;
        }

        if (decision.SelectedIntent != AgentIntentKind.Sleep)
        {
            return false;
        }

        ExecuteSleepAction(agent, decision, policy, tick);
        return true;
    }


    private void EnsureResidentNeedsInitialized()
    {
        if (_residentFacilities == null
            || _productionAgents == null
            || _buildingInventoryRepository == null
            || _productionRepository == null)
        {
            throw new InvalidOperationException(
                "Resident needs runtime requires initialized buildings, production and agents.");
        }
    }

    private void PublishFacilityEvents(BuildingFacilitiesState facilities)
    {
        _worldSession.Journal.Append(facilities.DequeueUncommittedEvents());
    }


    private static void RequireResidentNeeds(Result result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error!.ToString());
        }
    }

}

}
