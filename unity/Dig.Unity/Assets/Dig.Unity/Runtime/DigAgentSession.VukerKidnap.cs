using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Agents;
using Dig.Domain.Agents;
using Dig.Domain.Combat;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Factions;
using Dig.Domain.World;

namespace Dig.Unity
{

internal sealed partial class DigAgentSession
{
    internal Result RequestVukerKidnap(EntityId residentId, EntityId childId)
    {
        AgentState? resident = _repository.Get(residentId);
        AgentState? child = _repository.Get(childId);
        if (resident == null
            || child == null
            || !resident.IsAlive
            || !child.IsAlive
            || !_residentSexes.ContainsKey(residentId))
        {
            return Result.Failure(VukerEcologyErrors.KidnapUnavailable);
        }

        Result reserved = _vukerEcology.ReserveKidnap(childId, residentId, _tick);
        if (reserved.IsFailure)
        {
            return reserved;
        }

        PlanAgentTunnelRouteReport route = MoveResidentThroughTunnel(
            residentId.ToString(),
            child.Position);
        if (route.Result.IsFailure)
        {
            _vukerEcology.CancelKidnap(
                childId,
                residentId,
                _tick,
                route.Result.Error!.Code);
            AppendVukerEvents();
            return route.Result;
        }

        _vukerKidnapOrders[childId] = new VukerKidnapOrder(
            residentId,
            childId,
            child.Position);
        TryCompleteVukerKidnap(childId);
        AppendVukerEvents();
        return Result.Success();
    }

    internal PlanAgentTunnelRouteReport MoveTamedVukerThroughTunnel(
        EntityId vukerId,
        CellId destination)
    {
        if (!_vukerEcology.IsTamed(vukerId))
        {
            return new PlanAgentTunnelRouteReport(
                Result.Failure(VukerEcologyErrors.InvalidLifecycle),
                path: null);
        }

        return MoveResidentThroughTunnel(vukerId.ToString(), destination);
    }

    private void CompleteVukerKidnapOrders()
    {
        EntityId[] childIds = _vukerKidnapOrders.Keys
            .OrderBy(value => value.ToString(), StringComparer.Ordinal)
            .ToArray();
        for (int index = 0; index < childIds.Length; index++)
        {
            TryCompleteVukerKidnap(childIds[index]);
        }

        AppendVukerEvents();
    }

    private void TryCompleteVukerKidnap(EntityId childId)
    {
        if (!_vukerKidnapOrders.TryGetValue(
            childId,
            out VukerKidnapOrder? order))
        {
            return;
        }

        AgentState? resident = _repository.Get(order.ResidentId);
        AgentState? child = _repository.Get(order.ChildId);
        if (resident == null || child == null || !resident.IsAlive || !child.IsAlive)
        {
            _vukerEcology.CancelKidnap(
                order.ChildId,
                order.ResidentId,
                _tick,
                "kidnap_actor_unavailable");
            _vukerKidnapOrders.Remove(childId);
            return;
        }

        if (resident.Position != child.Position)
        {
            return;
        }

        Result tamed = _vukerEcology.CommitTame(
            child.Id,
            resident.Id,
            _tick);
        if (tamed.IsFailure)
        {
            _vukerKidnapOrders.Remove(childId);
            return;
        }

        FactionState factions = _combatFactions!.Get();
        Result assigned = factions.AssignMember(child.Id, ResidentFaction);
        if (assigned.IsFailure)
        {
            throw new InvalidOperationException(assigned.Error!.ToString());
        }
        _combatFactions.Save(factions);
        CancelCombatForTamedVuker(child.Id);
        _vukerKidnapOrders.Remove(childId);
    }

    private void CancelCombatForTamedVuker(EntityId vukerId)
    {
        if (_combatRepository == null)
        {
            return;
        }

        CombatState combat = _combatRepository.Get();
        CombatIntentSnapshot? intent = combat.GetActiveIntent(vukerId);
        if (intent != null)
        {
            combat.CancelIntent(intent.IntentId, "vuker_tamed", _tick);
        }
        else
        {
            CombatExecutionSnapshot? execution = combat.GetActiveExecution(vukerId);
            if (execution != null)
            {
                combat.CancelExecution(execution.ExecutionId, _tick, "vuker_tamed");
            }
        }
        _combatRepository.Save(combat);
        _combatJournal?.Append(combat.DequeueUncommittedEvents());
    }
}

}
