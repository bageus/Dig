using System;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Combat;
using Dig.Domain.Core;

namespace Dig.Unity
{

internal sealed partial class DigAgentSession
{
    private bool IsResidentCombatActiveOrThreatened(EntityId residentId, long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (_combatRepository == null || _combatOnlyActors.Contains(residentId))
        {
            return false;
        }

        // Combat acquisition must run before resident autonomy. Previously enemy
        // acquisition happened later in the movement loop, after Eat/Sleep/Study
        // and work systems had already advanced for the same tick.
        foreach (EntityId enemyId in _combatOnlyActors
            .OrderBy(value => value.ToString(), StringComparer.Ordinal))
        {
            AgentState? enemy = _repository.Get(enemyId);
            if (enemy != null && enemy.IsAlive)
            {
                EnsureAutonomousEnemyIntent(enemy);
            }
        }

        CombatState combat = _combatRepository.Get();
        if (combat.GetActiveIntent(residentId) != null)
        {
            return true;
        }

        CombatIntentSnapshot? incoming = combat.CreateIntentSnapshot()
            .Where(intent => intent.IsActive
                && intent.TargetEntityId == residentId
                && _combatOnlyActors.Contains(intent.ActorId))
            .OrderBy(intent => intent.ActorId.ToString(), StringComparer.Ordinal)
            .FirstOrDefault();
        if (incoming == null)
        {
            return false;
        }

        EnsureResidentSelfDefense(residentId, incoming.ActorId);
        return _combatRepository.Get().GetActiveIntent(residentId) != null;
    }
}

}