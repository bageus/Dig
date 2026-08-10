using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Combat;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Presentation.Creatures;

namespace Dig.Unity
{

internal sealed partial class DigAgentSession
{
    internal IReadOnlyList<CreatureVisualSnapshot> LoadEnemyCreatures()
    {
        CombatState? combat = _combatRepository?.Get();
        List<CreatureVisualSnapshot> result = new List<CreatureVisualSnapshot>(
            _enemyDefinitions.Count);
        foreach (KeyValuePair<EntityId, EnemyCombatDefinition> pair
            in _enemyDefinitions.OrderBy(value => value.Key.ToString(), StringComparer.Ordinal))
        {
            AgentState? actor = _repository.Get(pair.Key);
            if (actor == null)
            {
                continue;
            }

            AgentSnapshot snapshot = actor.CreateSnapshot(_tick);
            CombatExecutionSnapshot? execution = combat?.GetActiveExecution(actor.Id);
            bool engaged = combat != null && IsCombatEngaged(combat, actor.Id);
            bool moving = execution != null
                && (execution.Stage == CombatExecutionStage.Approach
                    || execution.Stage == CombatExecutionStage.Retreat);
            moving |= _lastEnemyPatrolMoveTicks.TryGetValue(
                actor.Id,
                out long patrolTick)
                && patrolTick == _tick;
            bool attacking = execution != null
                && (execution.Stage == CombatExecutionStage.WindUp
                    || execution.Stage == CombatExecutionStage.ResolveAttack);
            bool impact = _lastCombatImpactTicks.TryGetValue(actor.Id, out long impactTick)
                && _tick - impactTick <= 1;
            VukerIndividualSnapshot? vuker = _vukerEcology.GetIndividual(actor.Id);
            CreatureLifecycleVisualStage lifecycle = vuker?.Lifecycle
                == VukerLifecycleStage.Child
                    ? CreatureLifecycleVisualStage.Child
                    : CreatureLifecycleVisualStage.Adult;
            CreatureDisposition disposition = vuker?.Disposition
                == VukerDisposition.Tamed
                    ? CreatureDisposition.Tamed
                    : CreatureDisposition.Hostile;
            bool growing = vuker?.Lifecycle == VukerLifecycleStage.Child;
            double growthProgress = growing && vuker != null
                ? Math.Max(
                    0d,
                    Math.Min(
                        1d,
                        (double)(_tick - vuker.BirthTick)
                            / Math.Max(1L, vuker.MaturityTick - vuker.BirthTick)))
                : 0d;
            result.Add(new CreatureVisualSnapshot(
                actor.Id.ToString(),
                pair.Value.SpeciesId,
                lifecycle,
                disposition,
                snapshot.IsAlive,
                snapshot.Position.X,
                snapshot.Position.Y,
                snapshot.Position.Z,
                moving,
                attacking && (vuker == null || vuker.IsCombatEligible),
                impact,
                isGrowing: growing,
                isSpecialAction: vuker?.KidnapReservedBy.HasValue == true,
                actionProgress: growthProgress,
                version: checked(
                    snapshot.Version
                    + (combat?.Version ?? 0)
                    + (vuker?.Version ?? 0)),
                activityVariantId: vuker?.Disposition == VukerDisposition.Tamed
                    ? "tamed_guard"
                    : string.Empty,
                currentHealth: snapshot.Needs.Health.Points,
                maximumHealth: pair.Value.MaximumHealth,
                showHealthBar: engaged));
        }
        return result;
    }


}

}
