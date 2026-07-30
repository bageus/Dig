using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Presentation.Buildings;
using Dig.Presentation.Production;
using UnityEngine;

namespace Dig.Unity
{

[DisallowMultipleComponent]
public sealed partial class DigBuildingInternalStockRenderer : MonoBehaviour
{
    private const float VisibleDepthOffset = 0.12f;
    private readonly Dictionary<string, GameObject> _units =
        new Dictionary<string, GameObject>(StringComparer.Ordinal);
    private readonly Dictionary<string, Material> _materials =
        new Dictionary<string, Material>(StringComparer.Ordinal);
    private readonly Dictionary<string, DigBuildingInternalStockBayVisual> _bays =
        new Dictionary<string, DigBuildingInternalStockBayVisual>(StringComparer.Ordinal);
    private Transform? _root;

    internal int ActiveUnitCount => _units.Values.Count(value => value.activeSelf);
    internal int ActiveBayCount => _bays.Values.Count(value => value.gameObject.activeSelf);

    internal void Render(
        IReadOnlyList<BuildingProductionViewModel> production,
        IReadOnlyList<BuildingWorldViewModel> buildings)
    {
        if (production == null)
        {
            throw new ArgumentNullException(nameof(production));
        }

        if (buildings == null)
        {
            throw new ArgumentNullException(nameof(buildings));
        }

        EnsureRoot();
        Dictionary<string, BuildingWorldViewModel> buildingById = buildings
            .ToDictionary(value => value.Id, StringComparer.Ordinal);
        HashSet<string> visible = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> visibleBays = new HashSet<string>(StringComparer.Ordinal);
        for (int productionIndex = 0;
            productionIndex < production.Count;
            productionIndex++)
        {
            BuildingProductionViewModel model = production[productionIndex];
            if (!buildingById.TryGetValue(
                model.BuildingId.ToString(),
                out BuildingWorldViewModel? building))
            {
                continue;
            }

            RenderZones(building, visibleBays);
            for (int stockIndex = 0; stockIndex < model.Stocks.Count; stockIndex++)
            {
                BuildingStockIconViewModel stock = model.Stocks[stockIndex];
                RenderStock(building, stock, stockIndex, visible);
            }
        }

        RemoveMissing(visible, visibleBays);
    }

    private void RenderStock(
        BuildingWorldViewModel building,
        BuildingStockIconViewModel stock,
        int stockIndex,
        ISet<string> visible)
    {
        if (stock.ItemId.ToString().IndexOf("hamster", StringComparison.Ordinal) >= 0)
        {
            return;
        }

        for (int unitIndex = 0; unitIndex < stock.Current; unitIndex++)
        {
            string key = building.Id + ":" + stock.ItemId + ":" + unitIndex;
            visible.Add(key);
            if (!_units.TryGetValue(key, out GameObject? unit))
            {
                unit = CreateUnit(
                    building.Id,
                    stock.ItemId.ToString(),
                    key);
                _units.Add(key, unit);
            }

            unit.SetActive(true);
            ApplyUnitTransform(unit, building, stockIndex, unitIndex);
        }
    }

    internal bool TryGetStock(
        RaycastHit hit,
        out DigBuildingInternalStockVisual visual)
    {
        visual = hit.collider == null
            ? null!
            : hit.collider.GetComponentInParent<DigBuildingInternalStockVisual>();
        return visual != null;
    }

    private GameObject CreateUnit(string buildingId, string itemId, string key)
    {
        PrimitiveType primitive = ResolvePrimitive(itemId);
        GameObject unit = GameObject.CreatePrimitive(primitive);
        unit.name = "Internal Stock " + key;
        unit.transform.SetParent(_root, worldPositionStays: true);
        Renderer renderer = unit.GetComponent<Renderer>();
        renderer.sharedMaterial = ResolveMaterial(itemId);
        DigBuildingInternalStockVisual visual =
            unit.AddComponent<DigBuildingInternalStockVisual>();
        visual.Initialize(buildingId, itemId);
        return unit;
    }

    private static void ApplyUnitTransform(
        GameObject unit,
        BuildingWorldViewModel building,
        int stockIndex,
        int unitIndex)
    {
        int column = unitIndex % 2;
        int layer = unitIndex / 2;
        float pileX = -0.24f + (stockIndex * 0.16f);
        BuildingFootprintCellViewModel anchor = ResolveInternalZoneCell(building);
        Vector3 basePosition = DigTunnelProjection.ResidentWorldPosition(
            anchor.X,
            anchor.Y,
            anchor.Z) + (Vector3.up * DigTunnelProjection.ResidentFootSink);
        unit.transform.position = basePosition + new Vector3(
            pileX + (column * 0.07f),
            0.12f + (layer * 0.16f),
            VisibleDepthOffset + (stockIndex * 0.008f));
        unit.transform.localScale = ResolveScale(unit.name);
    }

    private Material ResolveMaterial(string itemId)
    {
        string family = ResolveFamily(itemId);
        if (_materials.TryGetValue(family, out Material? material))
        {
            return material;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Standard")
            ?? throw new InvalidOperationException(
                "No supported building-zone shader was found.");
        material = new Material(shader)
        {
            name = "Dig Building Zone " + family,
        };
        DigMaterialColorUtility.SetColor(material, ResolveColor(family));
        _materials.Add(family, material);
        return material;
    }

    private static PrimitiveType ResolvePrimitive(string itemId)
    {
        string family = ResolveFamily(itemId);
        if (family == "leg")
        {
            return PrimitiveType.Cylinder;
        }

        if (family == "cap" || family == "hamster")
        {
            return PrimitiveType.Sphere;
        }

        return PrimitiveType.Cube;
    }

    private static Vector3 ResolveScale(string objectName)
    {
        if (objectName.IndexOf("mushroom_leg", StringComparison.Ordinal) >= 0)
        {
            return new Vector3(0.11f, 0.15f, 0.11f);
        }

        if (objectName.IndexOf("mushroom_cap", StringComparison.Ordinal) >= 0)
        {
            return new Vector3(0.18f, 0.10f, 0.15f);
        }

        if (objectName.IndexOf("hamster", StringComparison.Ordinal) >= 0)
        {
            return new Vector3(0.18f, 0.13f, 0.14f);
        }

        return new Vector3(0.16f, 0.12f, 0.15f);
    }

    private static string ResolveFamily(string itemId)
    {
        if (string.Equals(itemId, "internal.stock.zone", StringComparison.Ordinal))
        {
            return "internal-zone";
        }

        if (string.Equals(itemId, "finished.output.zone", StringComparison.Ordinal))
        {
            return "output-zone";
        }

        if (itemId.IndexOf("mushroom_cap", StringComparison.Ordinal) >= 0)
        {
            return "cap";
        }

        if (itemId.IndexOf("mushroom_leg", StringComparison.Ordinal) >= 0)
        {
            return "leg";
        }

        if (itemId.IndexOf("hamster", StringComparison.Ordinal) >= 0)
        {
            return "hamster";
        }

        return itemId.IndexOf("stone", StringComparison.Ordinal) >= 0
            ? "stone"
            : "generic";
    }

    private static Color ResolveColor(string family)
    {
        if (family == "internal-zone")
        {
            return new Color(0.24f, 0.42f, 0.56f, 1f);
        }

        if (family == "output-zone")
        {
            return new Color(0.62f, 0.52f, 0.20f, 1f);
        }

        if (family == "cap")
        {
            return new Color(0.75f, 0.18f, 0.14f, 1f);
        }

        if (family == "leg")
        {
            return new Color(0.82f, 0.70f, 0.46f, 1f);
        }

        if (family == "hamster")
        {
            return new Color(0.56f, 0.32f, 0.16f, 1f);
        }

        return family == "stone"
            ? new Color(0.42f, 0.48f, 0.56f, 1f)
            : new Color(0.70f, 0.70f, 0.70f, 1f);
    }

    private void EnsureRoot()
    {
        if (_root != null)
        {
            return;
        }

        GameObject root = new GameObject("Building Input And Output Zones");
        root.transform.SetParent(transform, worldPositionStays: true);
        _root = root.transform;
    }

    private void RemoveMissing(
        ISet<string> visible,
        ISet<string> visibleBays)
    {
        string[] removed = _units.Keys
            .Where(value => !visible.Contains(value))
            .ToArray();
        for (int index = 0; index < removed.Length; index++)
        {
            string key = removed[index];
            Destroy(_units[key]);
            _units.Remove(key);
        }

        string[] removedBays = _bays.Keys
            .Where(value => !visibleBays.Contains(value))
            .ToArray();
        for (int index = 0; index < removedBays.Length; index++)
        {
            string key = removedBays[index];
            Destroy(_bays[key].gameObject);
            _bays.Remove(key);
        }
    }

    private void OnDestroy()
    {
        foreach (Material material in _materials.Values)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }

        _materials.Clear();
    }
}

}
