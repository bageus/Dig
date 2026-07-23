using System;
using System.Collections.Generic;
using Dig.Domain.World;
using Dig.Presentation.Overlays;
using UnityEngine;

namespace Dig.Unity
{
[DisallowMultipleComponent]
public sealed partial class DigWorldOverlayRenderer : MonoBehaviour
{
    private const int MaximumDiagnosticMarkers = 256;
    private const float DesignationFaceOffset = 0.015f;
    private readonly List<GameObject> _selection = new List<GameObject>();
    private readonly List<GameObject> _designations = new List<GameObject>();
    private readonly List<GameObject> _buildingFootprints = new List<GameObject>();
    private readonly List<GameObject> _storageDemand = new List<GameObject>();
    private readonly List<GameObject> _deposits = new List<GameObject>();
    private readonly List<GameObject> _fog = new List<GameObject>();
    private readonly List<GameObject> _dirtyChunks = new List<GameObject>();
    private readonly List<GameObject> _navigation = new List<GameObject>();
    private readonly Dictionary<Vector2Int, long> _chunkVersions =
        new Dictionary<Vector2Int, long>();
    private DigOverlayManager? _overlays;
    private DigAgentRenderer? _agents;
    private DigBuildingRenderer? _buildings;
    private DigWorldRenderer? _world;
    private Transform? _selectionRoot;
    private Transform? _designationRoot;
    private Transform? _previewRoot;
    private Transform? _reservationRoot;
    private Transform? _diagnosticRoot;

    internal void Initialize(
        DigOverlayManager overlays,
        DigAgentRenderer agents,
        DigBuildingRenderer buildings,
        DigWorldRenderer world)
    {
        _overlays = overlays ?? throw new ArgumentNullException(nameof(overlays));
        _agents = agents ?? throw new ArgumentNullException(nameof(agents));
        _buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _selectionRoot = CreateRoot("Selection Overlay", OverlayLayerKind.Selection);
        _designationRoot = CreateRoot("Designation Overlay", OverlayLayerKind.Designation);
        _previewRoot = CreateRoot("Building Footprint Overlay", OverlayLayerKind.Preview);
        _reservationRoot = CreateRoot("Reservation Overlay", OverlayLayerKind.Reservations);
        _diagnosticRoot = CreateRoot("World Diagnostic Overlay", OverlayLayerKind.Diagnostics);
    }

    private Transform CreateRoot(string name, OverlayLayerKind layer)
    {
        Transform root = new GameObject(name).transform;
        root.SetParent(transform, worldPositionStays: false);
        _overlays!.RegisterLayer(layer, root);
        return root;
    }

    private GameObject Acquire(
        List<GameObject> pool,
        int index,
        Transform root,
        string name,
        OverlayLayerKind layer,
        OverlaySemanticKind semantic)
    {
        while (pool.Count <= index)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = name + " " + pool.Count;
            marker.transform.SetParent(root, worldPositionStays: false);
            Collider collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            _overlays!.ConfigureRenderer(
                marker.GetComponent<Renderer>(),
                layer,
                semantic);
            pool.Add(marker);
        }

        GameObject value = pool[index];
        value.SetActive(true);
        return value;
    }

    private static void HideRemainder(List<GameObject> pool, int activeCount)
    {
        for (int index = activeCount; index < pool.Count; index++)
        {
            pool[index].SetActive(false);
        }
    }

    private static void PlaceCell(
        GameObject marker,
        int x,
        int y,
        float elevation,
        float scale = 0.72f)
    {
        marker.transform.position = new Vector3(x, elevation, y);
        marker.transform.rotation = Quaternion.identity;
        marker.transform.localScale = new Vector3(scale, 0.035f, scale);
    }

    private static void PlaceCellAtDepth(
        GameObject marker,
        int x,
        int y,
        int z,
        float elevation,
        float scale = 0.72f)
    {
        int visibleFaceZ = z > 0 ? z - 1 : z;
        Vector3 center = DigTunnelProjection.CellWorldPosition(
            new CellId(x, y, visibleFaceZ));
        marker.transform.position = center + new Vector3(
            0f,
            elevation,
            DigTunnelProjection.RockCellHalfExtent + DesignationFaceOffset);
        marker.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        marker.transform.localScale = new Vector3(scale, 0.035f, scale);
        marker.SetActive(false);
    }
}
}