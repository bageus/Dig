using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Ecology;
using Dig.Domain.Agents;
using Dig.Domain.Content;
using Dig.Domain.Navigation;
using Dig.Domain.Combat;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.World;

namespace Dig.Unity
{

internal sealed partial class DigAgentSession
{
    private const int TamedVukerDislocationRadius = 2;
    private readonly VukerEcologyState _vukerEcology =
        new VukerEcologyState(DemoIdentitySeed);
    private readonly Dictionary<EntityId, VukerKidnapOrder> _vukerKidnapOrders =
        new Dictionary<EntityId, VukerKidnapOrder>();
    private VukerCaveRegionResolver? _vukerRegions;
    private VukerBirthPlanner? _vukerBirthPlanner;

    internal VukerEcologySnapshot LoadVukerEcology() =>
        _vukerEcology.CaptureSnapshot();

    private void InitializeVukerEcology(TunnelNavigationVolume tunnelVolume)
    {
        _vukerRegions = new VukerCaveRegionResolver(tunnelVolume);
        _vukerBirthPlanner = new VukerBirthPlanner(_vukerRegions);
    }

    private void RegisterInitialVukerAdult(EntityId id, CellId position)
    {
        VukerRegionKey region = ResolveVukerRegion(position);
        Result registered = _vukerEcology.RegisterAdult(
            id,
            region,
            position,
            VukerDisposition.Wild,
            tick: 0);
        if (registered.IsFailure)
        {
            throw new InvalidOperationException(registered.Error!.ToString());
        }
    }

    private void FormInitialVukerPair()
    {
        _vukerEcology.Advance(0);
        AppendVukerEvents();
    }

    private void AdvanceVukerEcology()
    {
        if (_vukerRegions == null || _vukerBirthPlanner == null)
        {
            return;
        }

        foreach (VukerIndividualSnapshot individual in _vukerEcology.GetIndividuals())
        {
            AgentState? actor = _repository.Get(individual.EntityId);
            if (actor == null)
            {
                continue;
            }

            VukerRegionKey region = ResolveVukerRegion(actor.Position);
            Result synchronized = _vukerEcology.SynchronizeActor(
                actor.Id,
                region,
                actor.Position,
                actor.IsAlive,
                _tick);
            if (synchronized.IsFailure)
            {
                throw new InvalidOperationException(synchronized.Error!.ToString());
            }
        }

        IReadOnlyList<VukerPairSnapshot> due = _vukerEcology.Advance(_tick);
        HashSet<CellId> occupied = new HashSet<CellId>(
            _repository.GetAll()
                .Where(value => value.IsAlive)
                .Select(value => value.Position));
        for (int index = 0; index < due.Count; index++)
        {
            VukerPairSnapshot pair = due[index];
            Result<VukerBirthPlan> planned = _vukerBirthPlanner.Plan(
                _vukerEcology,
                pair,
                occupied,
                _tick);
            if (planned.IsFailure)
            {
                _vukerEcology.RecordBirthBlocked(
                    pair.PairId,
                    planned.Error!.Code,
                    _tick);
                continue;
            }

            VukerBirthPlan birth = planned.Value;
            if (_repository.Get(birth.ChildId) != null)
            {
                _vukerEcology.RecordBirthBlocked(
                    pair.PairId,
                    "ecology.vuker.child_identity_collision",
                    _tick);
                continue;
            }

            AddEnemy(
                birth.ChildId,
                CaveEncounterCombatContent.CaveMonster,
                birth.Position,
                _combatFactions!.Get());
            Result committed = _vukerEcology.CommitBirth(
                birth.PairId,
                birth.ChildId,
                birth.Region,
                birth.Position,
                _tick);
            if (committed.IsFailure)
            {
                throw new InvalidOperationException(committed.Error!.ToString());
            }

            occupied.Add(birth.Position);
        }

        AppendVukerEvents();
    }

    internal bool IsWildVukerChild(EntityId entityId)
    {
        return _vukerEcology.IsWildChild(entityId);
    }

    internal bool IsTamedVuker(EntityId entityId)
    {
        return _vukerEcology.IsTamed(entityId);
    }

    private bool CanVukerInitiateCombat(EntityId entityId)
    {
        VukerIndividualSnapshot? vuker = _vukerEcology.GetIndividual(entityId);
        return vuker == null
            || (vuker.IsCombatEligible
                && vuker.Disposition == VukerDisposition.Wild);
    }

    private bool IsVukerKidnapReserved(EntityId entityId)
    {
        return _vukerEcology.GetIndividual(entityId)?.KidnapReservedBy.HasValue
            == true;
    }

    private bool ShouldYieldEnemyIdleToManualMovement(EntityId entityId)
    {
        return _vukerEcology.IsTamed(entityId)
            && _manualTunnelMovements.ContainsKey(entityId);
    }

    private bool TryAdvanceTamedVukerAutoReturn(
        AgentState vuker,
        out Result result)
    {
        result = Result.Success();
        if (!_vukerEcology.IsTamed(vuker.Id))
        {
            return false;
        }

        AgentState? nearest = _repository.GetAll()
            .Where(value => value.IsAlive && _residentSexes.ContainsKey(value.Id))
            .OrderBy(value => CombatSpatialMath.Distance3D(
                vuker.Position,
                value.Position))
            .ThenBy(value => value.Id.ToString(), StringComparer.Ordinal)
            .FirstOrDefault();
        if (nearest == null)
        {
            return true;
        }

        int distance = CombatSpatialMath.Distance3D(vuker.Position, nearest.Position);
        if (distance <= TamedVukerDislocationRadius)
        {
            return true;
        }

        TunnelPathResult path = TunnelVolume.FindPath(vuker.Position, nearest.Position);
        if (!path.Succeeded || path.Path == null || path.Path.Cells.Count < 2)
        {
            return true;
        }

        CellId target = path.Path.Cells[1];
        if (!IsMovementStepDue(
            vuker,
            target,
            ResidentMovementCommandSource.Automatic,
            repeatedManualCommand: false,
            remainingPathSteps: path.Path.Cells.Count - 1))
        {
            return true;
        }

        result = MoveThroughTunnelTraffic(vuker, target);
        if (result.IsSuccess && vuker.Position == target)
        {
            _lastEnemyPatrolMoveTicks[vuker.Id] = _tick;
        }
        return true;
    }

    private VukerRegionKey ResolveVukerRegion(CellId position)
    {
        if (_vukerRegions == null
            || !_vukerRegions.TryResolveKey(position, out VukerRegionKey region))
        {
            throw new InvalidOperationException(
                "A Vuker actor must occupy a supported connected cave cell.");
        }

        return region;
    }

    private void AppendVukerEvents()
    {
        _combatJournal?.Append(_vukerEcology.DequeueUncommittedEvents());
    }

    private sealed class VukerKidnapOrder
    {
        internal VukerKidnapOrder(
            EntityId residentId,
            EntityId childId,
            CellId targetCell)
        {
            ResidentId = residentId;
            ChildId = childId;
            TargetCell = targetCell;
        }

        internal EntityId ResidentId { get; }
        internal EntityId ChildId { get; }
        internal CellId TargetCell { get; }
    }

}

}
