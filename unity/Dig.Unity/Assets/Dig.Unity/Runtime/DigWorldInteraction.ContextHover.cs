using System;
using System.Linq;
using Dig.Domain.Content;

namespace Dig.Unity
{

public sealed partial class DigWorldInteraction
{
    private void SetHostileTargetHoverInfo(DigCreatureVisual creature)
    {
        if (creature == null)
        {
            throw new ArgumentNullException(nameof(creature));
        }

        EnemyCombatDefinition? definition =
            CaveEncounterCombatContent.EnemyDefinitions.FirstOrDefault(value =>
                string.Equals(
                    value.SpeciesId,
                    creature.Model.SpeciesId,
                    StringComparison.Ordinal));
        _hud?.SetWorldTargetHoverInfo(
            definition?.DisplayName ?? creature.Model.SpeciesId);
    }

    private void SetMushroomTargetHoverInfo()
    {
        _hud?.SetWorldTargetHoverInfo(DigWorldTargetDisplayNames.Mushroom);
    }

    private void SetBarrelTargetHoverInfo()
    {
        _hud?.SetWorldTargetHoverInfo(DigWorldTargetDisplayNames.Barrel);
    }
}

internal static class DigWorldTargetDisplayNames
{
    internal const string Mushroom = "Гриб";
    internal const string Barrel = "Бочка";
}

}
