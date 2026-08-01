using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Presentation.Buildings;
using Dig.Presentation.Production;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{

[DisallowMultipleComponent]
public sealed partial class DigBuildingInternalStockRenderer : MonoBehaviour
{
    private const string CatalogResourcePath = "Dig/VisualCatalogs/Items";
    private const float VisibleDepthOffset = 0.12f;
    private readonly Dictionary<string, DigWorldItemVisual> _units =
        new Dictionary<string, DigWorldItemVisual>(StringComparer.Ordinal);
    private readonly ItemStackVisualLayoutPresenter _layoutPresenter =
        new ItemStackVisualLayoutPresenter();

    [SerializeField]
    private DigItemVisualCatalog? visualCatalog;
    private readonly Dictionary<string, Material> _materials =
        new Dictionary<string, Material>(StringComparer.Ordinal);
    private readonly Dictionary<string, DigBuildingInternalStockBayVisual> _bays =
        new Dictionary<string, DigBuildingInternalStockBayVisual>(StringComparer.Ordinal);
    private Transform? _root;

    internal int ActiveUnitCount => _units.Values.Count(value => value.gameObject.activeSelf);
    internal int ActiveBayCount => _bays.Values.Count(value => value.gameObject.activeSelf);

    private void Awake()
    {
        if (visualCatalog == null)
        {
            visualCatalog = Resources.Load<DigItemVisualCatalog>(CatalogResourcePath);
        }

        DigVisualCatalogDiagnostics.LogValidation(visualCatalog, this, "Building stock items");
    }

    public void SetVisualCatalog(DigItemVisualCatalog? catalog)
    {
        visualCatalog = catalog;
        foreach (DigWorldItemVisual visual in _units.Values)
        {
            visual.InvalidateAsset();
        }
    }

    internal void Render(
        IReadOnlyList<BuildingProductionViewModel> production,
        IReadOnlyList<BuildingWorldViewModel> buildings,
        IReadOnlyList<BuildingInternalStockUnitViewModel> stockUnits)
    {
        if (production == null || buildings == null || stockUnits == null)
        {
            throw new ArgumentNullException(nameof(production));
        }

        EnsureRoot();
        Dictionary<string, BuildingWorldViewModel> buildingById = buildings
            .ToDictionary(value => value.Id, StringComparer.Ordinal);
        Dictionary<string, BuildingProductionViewModel> productionByBuilding = production
            .ToDictionary(value => value.BuildingId.ToString(), StringComparer.Ordinal);
        HashSet<string> visible = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> visibleBays = new HashSet<string>(StringComparer.Ordinal);

        foreach (BuildingProductionViewModel model in production)
        {
            if (buildingById.TryGetValue(
                model.BuildingId.ToString(),
                out BuildingWorldViewModel? building))
            {
                RenderZones(building, visibleBays);
            }
        }

        foreach (BuildingInternalStockUnitViewModel unit in stockUnits)
        {
            string buildingId = unit.BuildingId.ToString();
            if (!buildingById.TryGetValue(buildingId, out BuildingWorldViewModel? building)
                || !productionByBuilding.TryGetValue(
                    buildingId,
                    out BuildingProductionViewModel? model))
            {
                continue;
            }

            int stockIndex = model.Stocks
                .Select((stock, index) => new { stock, index })
                .Where(value => value.stock.ItemId == unit.ItemId)
                .Select(value => value.index)
                .DefaultIfEmpty(0)
                .First();
            RenderUnit(building, unit, stockIndex, visible);
        }

        RemoveMissing(visible, visibleBays);
    }

    private void RenderUnit(
        BuildingWorldViewModel building,
        BuildingInternalStockUnitViewModel unit,
        int stockIndex,
        ISet<string> visible)
    {
        string key = unit.VisualKey;
        visible.Add(key);
        if (!_units.TryGetValue(key, out DigWorldItemVisual? visual))
        {
            GameObject root = new GameObject("Internal stock item " + key);
            root.transform.SetParent(_root, worldPositionStays: true);
            visual = root.AddComponent<DigWorldItemVisual>();
            _units.Add(key, visual);
        }

        WorldItemViewModel item = new WorldItemViewModel(
            unit.StackId,
            unit.ItemId.ToString(),
            quantity: 1,
            reservedQuantity: unit.IsAvailable ? 0 : 1,
            cellX: ResolveInternalZoneCell(building).X,
            cellY: ResolveInternalZoneCell(building).Y,
            cellZ: ResolveInternalZoneCell(building).Z,
            interactionProfile: unit.InteractionProfile);
        DigItemVisualResolution resolution = DigWorldItemVisualPolicy.Resolve(
            visualCatalog,
            item.ItemId);
        visual.gameObject.SetActive(true);
        visual.Configure(item, _layoutPresenter.Present(item), resolution);

        DigBuildingInternalStockVisual marker =
            visual.GetComponent<DigBuildingInternalStockVisual>()
            ?? visual.gameObject.AddComponent<DigBuildingInternalStockVisual>();
        marker.Initialize(
            unit.BuildingId.ToString(),
            unit.ItemId.ToString(),
            unit.StackId);
        ApplyUnitTransform(visual, building, stockIndex, unit.UnitIndex);
    }

    private static void ApplyUnitTransform(
        DigWorldItemVisual visual,
        BuildingWorldViewModel building,
        int stockIndex,
        int unitIndex)
    {
        int column = unitIndex % 2;
        int layer = unitIndex / 2;
        float pileX = -0.24f + (stockIndex * 0.16f) + (column * 0.07f);
        BuildingFootprintCellViewModel anchor = ResolveInternalZoneCell(building);
        visual.PlaceOnFloor(
            new Dig.Domain.World.CellId(anchor.X, anchor.Y, anchor.Z),
            new Vector2(pileX, stockIndex * 0.008f));
        visual.transform.position += Vector3.up * (layer * 0.12f);
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
            Destroy(_units[key].gameObject);
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
