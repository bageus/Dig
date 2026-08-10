using Dig.Presentation.Combat;
using UnityEngine;

namespace Dig.Unity
{

public sealed partial class DigAgentVisual
{
    private DigCombatHealthBar? _combatHealthBar;

    internal void SetCombatHealth(
        CombatantHealthBarViewModel? health,
        Camera? camera)
    {
        if (health == null || !health.IsVisible)
        {
            if (_combatHealthBar != null)
            {
                _combatHealthBar.Configure(0, 1, false, camera, 1.45f);
            }
            return;
        }

        if (_combatHealthBar == null)
        {
            GameObject root = new GameObject("CombatHealthBar");
            root.transform.SetParent(transform, false);
            _combatHealthBar = root.AddComponent<DigCombatHealthBar>();
        }

        _combatHealthBar.Configure(
            health.CurrentHealth,
            health.MaximumHealth,
            health.IsVisible && Model.IsAlive,
            camera,
            verticalOffset: 1.45f);
    }
}

}
