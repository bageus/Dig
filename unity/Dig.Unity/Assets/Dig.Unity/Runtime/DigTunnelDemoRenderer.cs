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
        private readonly Material?[] _depthMaterials = new Material?[4];
        private Transform? _root;
        private Material? _verticalMaterial;
        private Material? _caveMaterial;
        private Material? _selectedMaterial;
        private Material? _routeMaterial;
        private LineRenderer? _route;
        private DigTunnelCellVisual? _selected;
        private bool _caveShellCreated;

        internal void Initialize(TunnelNavigationVolume volume)
        {
            if (volume == null)
            {
                throw new ArgumentNullException(nameof(volume));
            }

            EnsureResources();
            foreach (SpatialCellId cell in volume.Cells)
            {
                if (_cells.ContainsKey(cell))
                {
                    continue;
                }

                bool vertical = volume.IsVerticalTunnel(cell);
                GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.name = vertical
                    ? $"Shaft {cell}"
                    : $"Walkable plane {cell}";
                target.transform.SetParent(_root, worldPositionStays: false);
                target.transform.localPosition = DigTunnelProjection.CellLocalPosition(cell);
                target.transform.localScale = vertical
                    ? new Vector3(0.38f, 0.38f, 0.88f)
                    : new Vector3(
                        0.84f,
                        Mathf.Abs(DigTunnelProjection.DepthSpacing) * 0.82f,
                        0.10f);
                DigTunnelCellVisual visual = target.AddComponent<DigTunnelCellVisual>();
                visual.Configure(
                    cell,
                    vertical,
                    ResolveMaterial(cell, vertical));
                _cells.Add(cell, visual);
            }

            if (!_caveShellCreated && volume.DemoLayout != null)
            {
                CreateCaveShell(volume.DemoLayout, volume.Depth);
                _caveShellCreated = true;
            }
        }

        internal bool TryGetCell(RaycastHit hit, out DigTunnelCellVisual cell)
        {
            cell = hit.collider == null
                ? null!
                : hit.collider.GetComponent<DigTunnelCellVisual>();
            return cell != null;
        }

        internal void Select(DigTunnelCellVisual? cell)
        {
            if (_selected != null)
            {
                _selected.GetComponent<Renderer>().sharedMaterial = ResolveMaterial(
                    _selected.Cell,
                    _selected.IsVerticalTunnel);
            }

            _selected = cell;
            if (_selected != null)
            {
                _selected.GetComponent<Renderer>().sharedMaterial = _selectedMaterial;
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
                Vector3 position = DigTunnelProjection.CellLocalPosition(path.Cells[index]);
                position.z -= 0.14f;
                _route.SetPosition(index, position);
            }
        }

        private Material ResolveMaterial(SpatialCellId cell, bool vertical)
        {
            return vertical ? _verticalMaterial! : _depthMaterials[cell.Z]!;
        }

        private void CreateCaveShell(TunnelDemoLayout layout, int depth)
        {
            Vector3 front = DigTunnelProjection.CellLocalPosition(
                new SpatialCellId(layout.CaveMinX, layout.CaveFloorY, 0));
            Vector3 back = DigTunnelProjection.CellLocalPosition(
                new SpatialCellId(layout.CaveMinX, layout.CaveFloorY, depth - 1));
            float centerX = (layout.CaveMinX + layout.CaveMaxX) * 0.5f;
            float centerDepth = (front.y + back.y) * 0.5f;
            float depthSpan = Mathf.Abs(front.y - back.y)
                + (Mathf.Abs(DigTunnelProjection.DepthSpacing) * 0.82f);
            float centerY = (layout.CaveCeilingY + layout.CaveFloorY) * 0.5f;
            float caveWidth = layout.CaveWidth;
            float caveHeight = layout.CaveHeight;

            CreateShellPart(
                "Cave ceiling",
                new Vector3(centerX, centerDepth, layout.CaveCeilingY),
                new Vector3(caveWidth, depthSpan, 0.12f));
            CreateShellPart(
                "Cave left wall",
                new Vector3(layout.CaveMinX - 0.48f, centerDepth, centerY),
                new Vector3(0.12f, depthSpan, caveHeight));
            CreateShellPart(
                "Cave right wall",
                new Vector3(layout.CaveMaxX + 0.48f, centerDepth, centerY),
                new Vector3(0.12f, depthSpan, caveHeight));
            CreateShellPart(
                "Cave back wall",
                new Vector3(centerX, Mathf.Min(front.y, back.y) - 0.30f, centerY),
                new Vector3(caveWidth, 0.10f, caveHeight));
        }

        private void CreateShellPart(string name, Vector3 position, Vector3 scale)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(_root, worldPositionStays: false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = _caveMaterial;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        private void EnsureResources()
        {
            if (_root == null)
            {
                _root = new GameObject("Layered Tunnel Demo").transform;
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
                throw new InvalidOperationException("Tunnel rendering requires lit and unlit shaders.");
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

            _verticalMaterial = CreateMaterial(
                lit,
                "Vertical tunnel",
                new Color(0.92f, 0.72f, 0.18f, 1f));
            _caveMaterial = CreateMaterial(
                lit,
                "Lower cave shell",
                new Color(0.24f, 0.21f, 0.20f, 1f));
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
            _route.useWorldSpace = false;
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

            DestroyMaterial(_verticalMaterial);
            DestroyMaterial(_caveMaterial);
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
