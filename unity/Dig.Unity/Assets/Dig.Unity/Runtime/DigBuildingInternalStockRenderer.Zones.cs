using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Presentation.Buildings;
using UnityEngine;

namespace Dig.Unity
{

public sealed partial class DigBuildingInternalStockRenderer
{
    private void RenderZones(
        BuildingWorldViewModel building,
        ISet<string> visible)
    {
        BuildingFootprintCellViewModel input = ResolveInternalZoneCell(building);
        BuildingFootprintCellViewModel output = ResolveOutputZoneCell(building);
        RenderBay(
            building.Id + ":input",
            "Internal Storage Zone ",
            input,
            "internal.stock.zone",
            visible);
        RenderBay(
            building.Id + ":output",
            "Finished Output Zone ",
            output,
            "finished.output.zone",
            visible);
    }

    private void RenderBay(
        string key,
        string namePrefix,
        BuildingFootprintCellViewModel cell,
        string materialId,
        ISet<string> visible)
    {
        visible.Add(key);
        if (!_bays.TryGetValue(key, out DigBuildingInternalStockBayVisual? bay))
        {
            GameObject root = new GameObject(namePrefix + key);
            root.transform.SetParent(_root, worldPositionStays: true);
            bay = root.AddComponent<DigBuildingInternalStockBayVisual>();
            bay.Initialize(ResolveMaterial(materialId));
            _bays.Add(key, bay);
        }

        bay.gameObject.SetActive(true);
        bay.SetPosition(
            DigTunnelProjection.ResidentWorldPosition(cell.X, cell.Y, cell.Z)
            + new Vector3(
                0f,
                DigTunnelProjection.ResidentFootSink,
                VisibleDepthOffset));
    }

    private static BuildingFootprintCellViewModel ResolveInternalZoneCell(
        BuildingWorldViewModel building)
    {
        BuildingFootprintCellViewModel row = ResolveZoneRow(building);
        int leftEdge = building.Footprint.Min(value => value.X);
        return new BuildingFootprintCellViewModel(leftEdge - 1, row.Y, row.Z);
    }

    private static BuildingFootprintCellViewModel ResolveOutputZoneCell(
        BuildingWorldViewModel building)
    {
        BuildingFootprintCellViewModel row = ResolveZoneRow(building);
        int rightEdge = building.Footprint.Max(value => value.X);
        return new BuildingFootprintCellViewModel(rightEdge + 1, row.Y, row.Z);
    }

    private static BuildingFootprintCellViewModel ResolveZoneRow(
        BuildingWorldViewModel building)
    {
        return building.Footprint
            .OrderBy(value => Math.Abs(value.Y - building.OriginY))
            .ThenBy(value => Math.Abs(value.Z - building.OriginZ))
            .ThenBy(value => value.Y)
            .ThenBy(value => value.Z)
            .First();
    }
}

}
