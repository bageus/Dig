using System;

namespace Dig.Presentation.Combat
{

public sealed class CombatantHealthBarViewModel
{
    public CombatantHealthBarViewModel(
        string entityId,
        int currentHealth,
        int maximumHealth,
        bool isVisible,
        string visibilityReason)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity id is required.", nameof(entityId));
        }

        if (maximumHealth <= 0
            || currentHealth < 0
            || currentHealth > maximumHealth)
        {
            throw new ArgumentOutOfRangeException(nameof(currentHealth));
        }

        EntityId = entityId.Trim();
        CurrentHealth = currentHealth;
        MaximumHealth = maximumHealth;
        IsVisible = isVisible;
        VisibilityReason = string.IsNullOrWhiteSpace(visibilityReason)
            ? "hidden"
            : visibilityReason.Trim();
    }

    public string EntityId { get; }
    public int CurrentHealth { get; }
    public int MaximumHealth { get; }
    public bool IsVisible { get; }
    public string VisibilityReason { get; }
    public double NormalizedHealth => (double)CurrentHealth / MaximumHealth;
}

}
