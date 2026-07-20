using System;
using System.Collections.Generic;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigTunnelDemoRenderer : MonoBehaviour
    {
        private readonly Dictionary<SpatialCellId, DigTunnelCellVisual> _cells =
            new Dictionary<SpatialCellId, DigTunnelCellVisual>();
        private readonly HashSet<SpatialCellId> _hiddenHitTargets =
            new HashSet<SpatialCellId>();
        private readonly Material?[] _depthMaterials = new Material?[4];
        private Transform? _root;
        private Material? _selectedMaterial;
        private Material? _routeMaterial;
        private LineRenderer? _route;
        private DigTunnelCellVisual? _selected;

        internal void Initialize(TunnelNavigationVolume volume)
        {
            if (volume == null)
            {
                throw new ArgumentNullException(nameof(volume));
            }

            EnsureResources();
            TunnelDemoLayout? layout = volume.DemoLayout;
            foreach (SpatialCellId cell in volume.Cells)
            {
                if (_cells.ContainsKey(cell))
                {
                    continue;
                }

                bool vertical = volume.IsVerticalTunnel(cell);
                bool hiddenHitTarget = vertical || cell.Z == 0;
                GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.name = hiddenHitTarget
                    ? $"Tunnel hit target {cell}"
                    : $"Walkable plane {cell}";
                target.transform.SetParent(_root, worldPositionStays: false);
                target.transform.SetPositionAndRotation(
                    vertical
                        ? DigTunnelProjection.CellWorldPosition(cell)
                        : DigTunnelProjection.FloorWorldPosition(cell),
                    Quaternion.identity);
                target.transform.localScale = vertical
                    ? new Vector3(0.72f, 0.72f, 0.48f)
                    : new Vector3(
                        0.84f,
                        hiddenHitTarget ? 0.14f : DigTunnelProjection.FloorThickness,
                        DigTunnelProjection.FloorDepth);
                DigTunnelCellVisual visual = target.AddComponent<DigTunnelCellVisual>();
                visual.Configure(
                    cell,
                    vertical,
                    ResolveMaterial(cell));
                ConfigureInteractionCollider(target, cell, vertical, layout);
                Renderer renderer = target.GetComponent<Renderer>();
                renderer.enabled = !hiddenHitTarget;
                _cells.Add(cell, visual);
                if (hiddenHitTarget)
                {
                    _hiddenHitTargets.Add(cell);
                }
            }
        }

        internal bool TryGetCell(RaycastHit hit, out DigTunnelCellVisual cell)
        {
            cell = hit.collider == null
                ? null!
                : hit.collider.GetComponent<DigTunnelCellVisual>();
            return cell != null;
        }

        internal bool TryGetCell(
            SpatialCellId cell,
            out DigTunnelCellVisual visual)
        {
            return _cells.TryGetValue(cell, out visual!);
        }

        internal void Select(DigTunnelCellVisual? cell)
        {
            if (_selected != null)
            {
                Renderer previous = _selected.GetComponent<Renderer>();
                previous.sharedMaterial = ResolveMaterial(_selected.Cell);
                previous.enabled = !_hiddenHitTargets.Contains(_selected.Cell);
            }

            _selected = cell;
            if (_selected != null)
            {
                Renderer current = _selected.GetComponent<Renderer>();
                current.sharedMaterial = _selectedMaterial;
                current.enabled = true;
            }
        }

        internal void ShowRoute(TunnelPath? path)
        {
            EnsureResources();
            if (path == null || path.Cells.Count < 2)
            {
                _route!.positionCount = 0;
                _route.enabled = false;
                return;
            }

            _route!.enabled = true;
            _route.positionCount = path.Cells.Count;
            for (int index = 0; index < path.Cells.Count; index++)
            {
                _route.SetPosition(
                    index,
                    DigTunnelProjection.RouteWorldPosition(path.Cells[index]));
            }
        }

        private static void ConfigureInteractionCollider(
            GameObject target,
            SpatialCellId cell,
            bool vertical,
            TunnelDemoLayout? layout)
        {
            BoxCollider collider = target.GetComponent<BoxCollider>();
            Vector3 center = DigTunnelProjection.CellWorldPosition(cell);
            float height = 0.94f;
            if (!vertical && IsNaturalCaveFloor(cell, layout))
            {
                float top = DigTunnelProjection.CellWorldPosition(
                    new SpatialCellId(
                        cell.X,
                        layout!.CaveCeilingY + 1,
                        cell.Z)).y
                    + DigTunnelProjection.RockCellHalfExtent;
                float bottom = DigTunnelProjection.WalkSurfaceY(layout.CaveFloorY);
                center.y = (top + bottom) * 0.5f;
                height = Mathf.Max(0.94f, top - bottom);
            }

            SetWorldColliderBounds(
                target.transform,
                collider,
                center,
                new Vector3(0.94f, height, 0.50f));
        }

        private static bool IsNaturalCaveFloor(
            SpatialCellId cell,
            TunnelDemoLayout? layout)
        {
            return layout != null
                && cell.Y == layout.CaveFloorY
                && cell.X >= layout.CaveMinX
                && cell.X <= layout.CaveMaxX;
        }

        private static void SetWorldColliderBounds(
            Transform target,
            BoxCollider collider,
            Vector3 worldCenter,
            Vector3 worldSize)
        {
            Vector3 scale = target.lossyScale;
            collider.center = target.InverseTransformPoint(worldCenter);
            collider.size = new Vector3(
                worldSize.x / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
                worldSize.y / Mathf.Max(0.001f, Mathf.Abs(scale.y)),
                worldSize.z / Mathf.Max(0.001f, Mathf.Abs(scale.z)));
        }

        private Material ResolveMaterial(SpatialCellId cell)
        {
            return _depthMaterials[cell.Z]!;
        }

        private void EnsureResources()
        {
            if (_root == null)
            {
                _root = new GameObject("Layered Tunnel Targets").transform;
                _root.SetParent(transform, worldPositionStays: false);
            }

            if (_depthMaterials[0] != null)
            {
                return;
            }

            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null)
            {
                lit = Shader.Find("Standard");
            }

            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlit == null)
            {
                unlit = Shader.Find("Unlit/Color");
            }

            if (lit == null || unlit == null)
            {
                throw new InvalidOperationException(
                    "Tunnel rendering requires lit and unlit shaders.");
            }

            Color[] colors =
            {
                new Color(0.20f, 0.52f, 0.66f, 1f),
                new Color(0.25f, 0.62f, 0.46f, 1f),
                new Color(0.70f, 0.50f, 0.22f, 1f),
                new Color(0.55f, 0.34f, 0.68f, 1f),
            };
            for (int index = 0; index < _depthMaterials.Length; index++)
            {
                _depthMaterials[index] = CreateMaterial(
                    lit,
                    $"Tunnel depth {index}",
                    colors[index]);
            }

            _selectedMaterial = CreateMaterial(
                lit,
                "Selected tunnel destination",
                Color.white);
            _routeMaterial = CreateMaterial(
                unlit,
                "Tunnel route",
                new Color(0.35f, 0.95f, 1f, 1f));
            GameObject routeObject = new GameObject("Selected tunnel route");
            routeObject.transform.SetParent(_root, worldPositionStays: false);
            _route = routeObject.AddComponent<LineRenderer>();
            _route.sharedMaterial = _routeMaterial;
            _route.useWorldSpace = true;
            _route.widthMultiplier = 0.09f;
            _route.numCapVertices = 3;
            _route.numCornerVertices = 3;
            _route.enabled = false;
        }

        private static Material CreateMaterial(
            Shader shader,
            string materialName,
            Color color)
        {
            return new Material(shader)
            {
                name = materialName,
                color = color,
            };
        }

        private void OnDestroy()
        {
            for (int index = 0; index < _depthMaterials.Length; index++)
            {
                if (_depthMaterials[index] != null)
                {
                    Destroy(_depthMaterials[index]);
                }
            }

            DestroyMaterial(_selectedMaterial);
            DestroyMaterial(_routeMaterial);
        }

        private void DestroyMaterial(Material? material)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }
    }
}
