using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Creatures;
using Dig.Presentation.Input;
using UnityEngine;

namespace Dig.Unity
{
public sealed partial class DigWorldInteraction
{
    private void ApplyAttack(ContextInputDecision decision)
    {
        if (!decision.ActorId.HasValue || !decision.TargetEntityId.HasValue)
        {
            _hud!.SetStatus("input.target.stale_or_dead");
            return;
        }

        Result<Dig.Domain.Combat.CombatIntentSnapshot> result =
            _agentSession!.IssuePlayerAttackOrder(
                decision.ActorId.Value,
                decision.TargetEntityId.Value);
        _hud!.SetStatus(result.IsSuccess
            ? "combat.order.attack_issued"
            : result.Error!.Code);
    }

    private bool TryResolveHostileCreatureHit(
        RaycastHit[] hits,
        out DigCreatureVisual creature)
    {
        for (int index = 0; index < hits.Length; index++)
        {
            if (_creatureRenderer!.TryGetCreature(hits[index], out creature)
                && creature.Model.Disposition == CreatureDisposition.Hostile)
            {
                return true;
            }

            if (_buildingRenderer!.TryGetBuilding(hits[index], out _)
                || _itemRenderer!.TryGetItem(hits[index], out _))
            {
                break;
            }
        }

        creature = null!;
        return false;
    }

    private static ContextPointerTarget BuildHostileTarget(DigCreatureVisual creature)
    {
        bool hasEntityId = EntityId.TryParse(creature.Model.CreatureId, out EntityId id);
        return new ContextPointerTarget(
            ContextWorldTargetKind.HostileResident,
            hasEntityId ? id : (EntityId?)null,
            new CellId(creature.Model.CellX, creature.Model.CellY, creature.Model.CellZ),
            isAlive: creature.Model.IsAlive);
    }
}
}
