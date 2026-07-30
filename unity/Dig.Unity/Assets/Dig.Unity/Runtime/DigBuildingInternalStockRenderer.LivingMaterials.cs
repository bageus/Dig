using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Presentation.Buildings;
using Dig.Presentation.Creatures;
using UnityEngine;

namespace Dig.Unity
{

public sealed partial class DigBuildingInternalStockRenderer
{
    private readonly Dictionary<string, DigLivingMaterialTetherVisual>
        _livingMaterialTethers =
            new Dictionary<string, DigLivingMaterialTetherVisual>(StringComparer.Ordinal);

    internal int ActiveLivingMaterialTetherCount =>
        _livingMaterialTethers.Values.Count(value => value.gameObject.activeSelf);

    internal void RenderLivingMaterialTethers(
        IReadOnlyList<LivingMaterialCampfireTetherViewModel> tethers,
        IReadOnlyList<BuildingWorldViewModel> buildings)
    {
        if (tethers == null || buildings == null)
        {
            throw new ArgumentNullException(nameof(tethers));
        }

        EnsureRoot();
        Dictionary<string, BuildingWorldViewModel> buildingById = buildings
            .ToDictionary(value => value.Id, StringComparer.Ordinal);
        HashSet<string> visible = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < tethers.Count; index++)
        {
            LivingMaterialCampfireTetherViewModel model = tethers[index];
            if (!buildingById.TryGetValue(
                model.BuildingId,
                out BuildingWorldViewModel? building))
            {
                continue;
            }

            visible.Add(model.CreatureId);
            if (!_livingMaterialTethers.TryGetValue(
                model.CreatureId,
                out DigLivingMaterialTetherVisual? visual))
            {
                GameObject root = new GameObject(
                    "Campfire tether hamster " + model.CreatureId);
                root.transform.SetParent(_root, worldPositionStays: true);
                visual = root.AddComponent<DigLivingMaterialTetherVisual>();
                _livingMaterialTethers.Add(model.CreatureId, visual);
            }

            visual.gameObject.SetActive(true);
            visual.Apply(model, building, ResolveMaterial("hamster"));
        }

        string[] removed = _livingMaterialTethers.Keys
            .Where(value => !visible.Contains(value))
            .ToArray();
        for (int index = 0; index < removed.Length; index++)
        {
            string id = removed[index];
            Destroy(_livingMaterialTethers[id].gameObject);
            _livingMaterialTethers.Remove(id);
        }
    }
}

}
