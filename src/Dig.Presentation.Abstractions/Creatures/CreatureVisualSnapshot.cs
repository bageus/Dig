using System;

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
        long version)
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
}
}