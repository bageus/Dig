using System;
using System.Collections.Generic;

namespace Dig.Presentation.Creatures
{
public sealed class CreatureVisualPresenter
{
    private const ulong OffsetBasis = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;
    private static readonly IReadOnlyDictionary<string, SpeciesDefinition> Species =
        CreateSpecies();

    public CreatureAppearanceViewModel PresentAppearance(CreatureVisualSnapshot snapshot)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        bool fallback = !Species.TryGetValue(snapshot.SpeciesId, out SpeciesDefinition? definition);
        definition ??= new SpeciesDefinition(
            CreatureVisualFamily.SmallCreature,
            "creature.rig.fallback",
            "fallback");
        string stage = snapshot.LifecycleStage.ToString().ToLowerInvariant();
        string variant = definition.VariantId + "." + stage;
        ulong hash = Hash(snapshot.CreatureId, snapshot.SpeciesId,
            (int)snapshot.LifecycleStage, (int)snapshot.Disposition);
        int bodyPalette = (int)(hash % 8UL);
        int accentPalette = (int)((hash / 8UL) % 6UL);
        CreatureMarkerShape marker = snapshot.Disposition switch
        {
            CreatureDisposition.Tamed => CreatureMarkerShape.Shield,
            CreatureDisposition.Hostile => CreatureMarkerShape.Spikes,
            _ => CreatureMarkerShape.Ring,
        };
        long version = ToVersion(Hash(snapshot.CreatureId, snapshot.SpeciesId,
            definition.RigId, variant, bodyPalette, accentPalette,
            (int)marker, snapshot.Version));
        return new CreatureAppearanceViewModel(
            snapshot.CreatureId,
            snapshot.SpeciesId,
            definition.RigId,
            variant,
            definition.Family,
            snapshot.LifecycleStage,
            snapshot.Disposition,
            marker,
            bodyPalette,
            accentPalette,
            fallback,
            version);
    }

    public CreatureActionVisualViewModel PresentAction(CreatureVisualSnapshot snapshot)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        CreatureActionVisualState state;
        if (!snapshot.IsAlive) state = CreatureActionVisualState.Death;
        else if (snapshot.ShowImpact) state = CreatureActionVisualState.Hit;
        else if (snapshot.IsAttacking) state = CreatureActionVisualState.Attack;
        else if (snapshot.IsSpecialAction) state = CreatureActionVisualState.Special;
        else if (snapshot.IsGrowing) state = CreatureActionVisualState.Growth;
        else if (snapshot.IsMoving) state = CreatureActionVisualState.Move;
        else state = CreatureActionVisualState.Idle;

        double progress = state == CreatureActionVisualState.Death
            ? 1d
            : snapshot.ActionProgress;
        bool looping = state == CreatureActionVisualState.Idle
            || state == CreatureActionVisualState.Move;
        long version = ToVersion(Hash(snapshot.CreatureId,
            (int)state, (int)Math.Round(progress * 1000d),
            looping ? 1 : 0, snapshot.Version));
        return new CreatureActionVisualViewModel(
            snapshot.CreatureId, state, progress, looping, version);
    }

    public CreatureLodViewModel PresentLod(double cameraDistance, bool isVisible)
    {
        if (cameraDistance < 0d)
            throw new ArgumentOutOfRangeException(nameof(cameraDistance));
        if (!isVisible || cameraDistance > 80d)
            return new CreatureLodViewModel(CreatureLodTier.Hidden,
                CreatureAnimationUpdatePolicy.Frozen, 30, renderBody: false);
        if (cameraDistance > 42d)
            return new CreatureLodViewModel(CreatureLodTier.Far,
                CreatureAnimationUpdatePolicy.Reduced, 12, renderBody: true);
        if (cameraDistance > 18d)
            return new CreatureLodViewModel(CreatureLodTier.Mid,
                CreatureAnimationUpdatePolicy.Reduced, 4, renderBody: true);
        return new CreatureLodViewModel(CreatureLodTier.Near,
            CreatureAnimationUpdatePolicy.EveryFrame, 1, renderBody: true);
    }

    private static IReadOnlyDictionary<string, SpeciesDefinition> CreateSpecies()
    {
        return new Dictionary<string, SpeciesDefinition>(StringComparer.Ordinal)
        {
            ["creature.hamster"] = Small("hamster"),
            ["creature.larva"] = Small("larva"),
            ["enemy.plant.poison"] = Plant("poison"),
            ["enemy.plant.fire"] = Plant("fire"),
            ["enemy.vuker"] = Vuker("common"),
            ["enemy.vuker.sulfur"] = Vuker("sulfur"),
            ["enemy.spider"] = Arachnid("spider"),
            ["creature.spider.egg"] = Arachnid("spider-egg"),
            ["enemy.demon.swallower"] = Demon("swallower"),
            ["enemy.demon.lava"] = Demon("lava"),
            ["enemy.troll"] = Biped("troll"),
            ["enemy.goblin"] = Biped("goblin"),
        };
    }

    private static SpeciesDefinition Plant(string variant) =>
        new SpeciesDefinition(CreatureVisualFamily.Plant, "creature.rig.plant", variant);
    private static SpeciesDefinition Vuker(string variant) =>
        new SpeciesDefinition(CreatureVisualFamily.Vuker, "creature.rig.vuker", variant);
    private static SpeciesDefinition Arachnid(string variant) =>
        new SpeciesDefinition(CreatureVisualFamily.Arachnid, "creature.rig.arachnid", variant);
    private static SpeciesDefinition Biped(string variant) =>
        new SpeciesDefinition(CreatureVisualFamily.Biped, "creature.rig.biped", variant);
    private static SpeciesDefinition Demon(string variant) =>
        new SpeciesDefinition(CreatureVisualFamily.LargeDemon, "creature.rig.demon", variant);
    private static SpeciesDefinition Small(string variant) =>
        new SpeciesDefinition(CreatureVisualFamily.SmallCreature, "creature.rig.small", variant);

    private static ulong Hash(string first, params object[] values)
    {
        ulong hash = HashText(OffsetBasis, first);
        for (int index = 0; index < values.Length; index++)
            hash = HashText(hash, values[index]?.ToString() ?? string.Empty);
        return hash;
    }

    private static ulong HashText(ulong hash, string value)
    {
        for (int index = 0; index < value.Length; index++)
        {
            hash ^= value[index];
            hash *= Prime;
        }
        return hash;
    }

    private static long ToVersion(ulong value) =>
        (long)(value & (ulong)long.MaxValue);

    private sealed class SpeciesDefinition
    {
        internal SpeciesDefinition(
            CreatureVisualFamily family,
            string rigId,
            string variantId)
        {
            Family = family;
            RigId = rigId;
            VariantId = variantId;
        }

        internal CreatureVisualFamily Family { get; }
        internal string RigId { get; }
        internal string VariantId { get; }
    }
}
}