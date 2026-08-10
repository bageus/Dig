using System;
using System.Collections.Generic;
using Dig.Presentation.Combat;
using UnityEngine;

namespace Dig.Unity
{

public sealed partial class DigAgentRenderer
{
    public void RenderCombatHealthBars(
        IReadOnlyList<CombatantHealthBarViewModel> healthBars,
        Camera? camera)
    {
        if (healthBars == null)
        {
            throw new ArgumentNullException(nameof(healthBars));
        }

        Dictionary<string, CombatantHealthBarViewModel> byId =
            new Dictionary<string, CombatantHealthBarViewModel>(StringComparer.Ordinal);
        for (int index = 0; index < healthBars.Count; index++)
        {
            CombatantHealthBarViewModel value = healthBars[index];
            if (!byId.TryAdd(value.EntityId, value))
            {
                throw new InvalidOperationException(
                    "A combatant can have only one health-bar projection.");
            }
        }

        foreach (KeyValuePair<string, DigAgentVisual> pair in _agents)
        {
            byId.TryGetValue(pair.Key, out CombatantHealthBarViewModel? health);
            pair.Value.SetCombatHealth(health, camera);
        }
    }
}

}
