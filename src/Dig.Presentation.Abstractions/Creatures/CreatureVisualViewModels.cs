using System;

namespace Dig.Presentation.Creatures
{
public sealed class CreatureAppearanceViewModel
{
    public CreatureAppearanceViewModel(
        string creatureId,
        string speciesId,
        string rigId,
        string variantId,
        CreatureVisualFamily family,
        CreatureLifecycleVisualStage lifecycleStage,
        CreatureDisposition disposition,
        CreatureMarkerShape markerShape,
        int bodyPaletteIndex,
        int accentPaletteIndex,
        bool isFallback,
        long version)
    {
        if (string.IsNullOrWhiteSpace(creatureId)
            || string.IsNullOrWhiteSpace(speciesId)
            || string.IsNullOrWhiteSpace(rigId)
            || string.IsNullOrWhiteSpace(variantId))
            throw new ArgumentException("Creature appearance identifiers are required.");
        if (!Enum.IsDefined(typeof(CreatureVisualFamily), family)
            || !Enum.IsDefined(typeof(CreatureLifecycleVisualStage), lifecycleStage)
            || !Enum.IsDefined(typeof(CreatureDisposition), disposition)
            || !Enum.IsDefined(typeof(CreatureMarkerShape), markerShape))
            throw new ArgumentOutOfRangeException(nameof(family));
        if (bodyPaletteIndex < 0 || bodyPaletteIndex > 7
            || accentPaletteIndex < 0 || accentPaletteIndex > 5
            || version < 0)
            throw new ArgumentOutOfRangeException(nameof(bodyPaletteIndex));

        CreatureId = creatureId.Trim();
        SpeciesId = speciesId.Trim();
        RigId = rigId.Trim();
        VariantId = variantId.Trim();
        Family = family;
        LifecycleStage = lifecycleStage;
        Disposition = disposition;
        MarkerShape = markerShape;
        BodyPaletteIndex = bodyPaletteIndex;
        AccentPaletteIndex = accentPaletteIndex;
        IsFallback = isFallback;
        Version = version;
    }

    public string CreatureId { get; }
    public string SpeciesId { get; }
    public string RigId { get; }
    public string VariantId { get; }
    public CreatureVisualFamily Family { get; }
    public CreatureLifecycleVisualStage LifecycleStage { get; }
    public CreatureDisposition Disposition { get; }
    public CreatureMarkerShape MarkerShape { get; }
    public int BodyPaletteIndex { get; }
    public int AccentPaletteIndex { get; }
    public bool IsFallback { get; }
    public long Version { get; }
}

public sealed class CreatureActionVisualViewModel
{
    public CreatureActionVisualViewModel(
        string creatureId,
        CreatureActionVisualState state,
        double normalizedProgress,
        bool isLooping,
        long version)
    {
        if (string.IsNullOrWhiteSpace(creatureId))
            throw new ArgumentException("Creature id is required.", nameof(creatureId));
        if (!Enum.IsDefined(typeof(CreatureActionVisualState), state)
            || normalizedProgress < 0d || normalizedProgress > 1d || version < 0)
            throw new ArgumentOutOfRangeException(nameof(state));
        CreatureId = creatureId.Trim();
        State = state;
        NormalizedProgress = normalizedProgress;
        IsLooping = isLooping;
        Version = version;
    }

    public string CreatureId { get; }
    public CreatureActionVisualState State { get; }
    public double NormalizedProgress { get; }
    public bool IsLooping { get; }
    public long Version { get; }
}

public sealed class CreatureLodViewModel
{
    public CreatureLodViewModel(
        CreatureLodTier tier,
        CreatureAnimationUpdatePolicy animationPolicy,
        int updateIntervalFrames,
        bool renderBody)
    {
        if (!Enum.IsDefined(typeof(CreatureLodTier), tier)
            || !Enum.IsDefined(typeof(CreatureAnimationUpdatePolicy), animationPolicy))
            throw new ArgumentOutOfRangeException(nameof(tier));
        if (updateIntervalFrames < 1)
            throw new ArgumentOutOfRangeException(nameof(updateIntervalFrames));
        Tier = tier;
        AnimationPolicy = animationPolicy;
        UpdateIntervalFrames = updateIntervalFrames;
        RenderBody = renderBody;
    }

    public CreatureLodTier Tier { get; }
    public CreatureAnimationUpdatePolicy AnimationPolicy { get; }
    public int UpdateIntervalFrames { get; }
    public bool RenderBody { get; }
}
}