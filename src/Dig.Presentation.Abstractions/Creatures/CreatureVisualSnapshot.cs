using System;
using Dig.Domain.Navigation;

namespace Dig.Presentation.Creatures
{
public sealed class CreatureVisualSnapshot
{
    public CreatureVisualSnapshot(
        string creatureId,
        string speciesId,
        CreatureLifecycleVisualStage lifecycleStage,
        CreatureDisposition disposition,
        bool isAlive,
        int cellX,
        int cellY,
        int cellZ,
        bool isMoving,
        bool isAttacking,
        bool showImpact,
        bool isGrowing,
        bool isSpecialAction,
        double actionProgress,
        long version,
        string activityVariantId = "",
        int currentHealth = 0,
        int maximumHealth = 0,
        bool showHealthBar = false,
        int surfaceU = SurfacePose.CellCentre,
        int surfaceV = SurfacePose.CellCentre)
    {
        if (string.IsNullOrWhiteSpace(creatureId))
            throw new ArgumentException("Creature id is required.", nameof(creatureId));
        if (string.IsNullOrWhiteSpace(speciesId))
            throw new ArgumentException("Species id is required.", nameof(speciesId));
        if (!Enum.IsDefined(typeof(CreatureLifecycleVisualStage), lifecycleStage)
            || !Enum.IsDefined(typeof(CreatureDisposition), disposition))
            throw new ArgumentOutOfRangeException(nameof(lifecycleStage));
        if (cellX < 0 || cellY < 0 || cellZ < 0 || cellZ > 3)
            throw new ArgumentOutOfRangeException(nameof(cellX));
        if (actionProgress < 0d || actionProgress > 1d || version < 0)
            throw new ArgumentOutOfRangeException(nameof(actionProgress));
        if (maximumHealth < 0 || currentHealth < 0
            || (maximumHealth > 0 && currentHealth > maximumHealth)
            || (showHealthBar && maximumHealth <= 0))
            throw new ArgumentOutOfRangeException(nameof(currentHealth));
        if (surfaceU < 0 || surfaceU > SurfacePose.UnitsPerCell
            || surfaceV < 0 || surfaceV > SurfacePose.UnitsPerCell)
            throw new ArgumentOutOfRangeException(nameof(surfaceU));

        CreatureId = creatureId.Trim();
        SpeciesId = speciesId.Trim();
        LifecycleStage = lifecycleStage;
        Disposition = disposition;
        IsAlive = isAlive;
        CellX = cellX;
        CellY = cellY;
        CellZ = cellZ;
        IsMoving = isMoving;
        IsAttacking = isAttacking;
        ShowImpact = showImpact;
        IsGrowing = isGrowing;
        IsSpecialAction = isSpecialAction;
        ActionProgress = actionProgress;
        Version = version;
        ActivityVariantId = activityVariantId?.Trim() ?? string.Empty;
        CurrentHealth = currentHealth;
        MaximumHealth = maximumHealth;
        ShowHealthBar = showHealthBar;
        SurfaceU = surfaceU;
        SurfaceV = surfaceV;
    }

    public string CreatureId { get; }
    public string SpeciesId { get; }
    public CreatureLifecycleVisualStage LifecycleStage { get; }
    public CreatureDisposition Disposition { get; }
    public bool IsAlive { get; }
    public int CellX { get; }
    public int CellY { get; }
    public int CellZ { get; }
    public bool IsMoving { get; }
    public bool IsAttacking { get; }
    public bool ShowImpact { get; }
    public bool IsGrowing { get; }
    public bool IsSpecialAction { get; }
    public double ActionProgress { get; }
    public long Version { get; }
    public string ActivityVariantId { get; }
    public int CurrentHealth { get; }
    public int MaximumHealth { get; }
    public bool ShowHealthBar { get; }
    public int SurfaceU { get; }
    public int SurfaceV { get; }
}
}
