using System;
using System.Linq;
using Dig.Domain.Content;
using UnityEngine;

namespace Dig.Unity
{

public sealed partial class DigWorldInteraction
{
    private void SetGeneralWorldTargetHoverInfo(RaycastHit[] hits)
    {
        for (int index = 0; index < hits.Length; index++)
        {
            RaycastHit hit = hits[index];
            if (_itemRenderer!.TryGetItem(hit, out DigWorldItemVisual item))
            {
                _hud?.SetWorldTargetHoverInfo(item.Model.DisplayName);
                return;
            }

            if (_buildingRenderer!.TryGetBuilding(hit, out DigBuildingVisual building))
            {
                SetBuildingTargetHoverInfo(building);
                return;
            }

            if (_creatureRenderer != null
                && _creatureRenderer.TryGetCreature(hit, out DigCreatureVisual creature)
                && creature.Model.Disposition == Dig.Presentation.Creatures.CreatureDisposition.Hostile)
            {
                SetHostileTargetHoverInfo(creature);
                return;
            }

            DigAgentVisual? resident = hit.collider == null
                ? null
                : hit.collider.GetComponentInParent<DigAgentVisual>();
            if (resident != null)
            {
                _hud?.SetWorldTargetHoverInfo(resident.Model.Name);
                return;
            }
        }

        if (TryResolveAgentNearPointer(out DigAgentVisual nearbyResident))
        {
            _hud?.SetWorldTargetHoverInfo(nearbyResident.Model.Name);
        }
    }

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

    private void SetBuildingTargetHoverInfo(DigBuildingVisual building)
    {
        if (building == null)
        {
            throw new ArgumentNullException(nameof(building));
        }

        _hud?.SetWorldTargetHoverInfo(building.Model.Name);
    }
}

internal static class DigWorldTargetDisplayNames
{
    internal const string Mushroom = "Гриб";
    internal const string Barrel = "Бочка";
}

}
