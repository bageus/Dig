using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Ecology;

namespace Dig.Presentation.Creatures
{

public sealed class LivingMaterialCreatureVisualProjector
{
    public IReadOnlyList<CreatureVisualSnapshot> Project(
        IReadOnlyCollection<LivingMaterialSnapshot> creatures)
    {
        if (creatures == null)
        {
            throw new ArgumentNullException(nameof(creatures));
        }

        return creatures
            .Where(value => value.IsFree && value.Cell.HasValue)
            .OrderBy(value => value.CreatureId.ToString(), StringComparer.Ordinal)
            .Select(ProjectOne)
            .ToArray();
    }

    private static CreatureVisualSnapshot ProjectOne(LivingMaterialSnapshot creature)
    {
        bool moving = creature.Activity == LivingMaterialActivity.Moving
            || creature.Activity == LivingMaterialActivity.Blocked;
        bool special = creature.Activity == LivingMaterialActivity.HamsterSearching
            || creature.Activity == LivingMaterialActivity.HamsterSleeping
            || creature.Activity == LivingMaterialActivity.ReleaseDormant;
        double progress = creature.ActivityStepsRemaining <= 0
            ? 0d
            : 1d / (creature.ActivityStepsRemaining + 1d);
        return new CreatureVisualSnapshot(
            creature.CreatureId.ToString(),
            creature.Species == LivingMaterialSpecies.Hamster
                ? "creature.hamster"
                : "creature.grub",
            CreatureLifecycleVisualStage.Adult,
            CreatureDisposition.Neutral,
            isAlive: true,
            creature.Cell!.Value.X,
            creature.Cell.Value.Y,
            creature.Cell.Value.Z,
            isMoving: moving,
            isAttacking: false,
            showImpact: false,
            isGrowing: false,
            isSpecialAction: special,
            actionProgress: progress,
            version: creature.Version);
    }
}

}
