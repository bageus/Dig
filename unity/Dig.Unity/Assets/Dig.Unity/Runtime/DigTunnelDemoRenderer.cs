using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Presentation.Rendering;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigTunnelDemoRenderer : MonoBehaviour
    {
        private readonly Dictionary<SpatialCellId, DigTunnelCellVisual> _cells =
            new Dictionary<SpatialCellId, DigTunnelCellVisual>();
        private Transform? _cellProxyRoot;
        private Transform? _movementSurfaceRoot;
        private Transform? _routeRoot;
        private DigRenderMaterialLibrary? _materialLibrary;
        private LineRenderer? _route;
        private int _volumeSignature;
        private bool _digInteractionActive;

        internal void Initialize(TunnelNavigationVolume volume)
        {
            if (volume == null)
            {
                throw new ArgumentNullException(nameof(volume));
            }

            EnsureRouteResources();
            int signature = CalculateSignature(volume);
            if (_volumeSignature == signature)
            {
                return;
            }

            _volumeSignature = signature;
            ReconcileCellProxies(volume);
            RebuildMovementSurfaces(volume);
        }

        internal void SetDigInteractionActive(bool active)
        {
            _digInteractionActive = active;
            foreach (DigTunnelCellVisual visual in _cells.Values)
            {
                Collider? collider = visual.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = active;
                }
            }

            if (_movementSurfaceRoot != null)
            {
                Collider[] surfaces =
                    _movementSurfaceRoot.GetComponentsInChildren<Collider>();
                for (int index = 0; index < surfaces.Length; index++)
                {
                    surfaces[index].enabled = !active;
                }
            }
        }

        internal bool TryGetMovementTarget(
            RaycastHit hit,
            out DigTunnelMovementDestination destination)
        {
            DigTunnelMovementSurface? surface = _digInteractionActive
                || hit.collider == null
                ? null
                : hit.collider.GetComponent<DigTunnelMovementSurface>();
            if (surface == null)
            {
                destination = default;
                return false;
            }

            destination = surface.Resolve(hit.point);
            return true;
        }

        internal bool TryGetCell(RaycastHit hit, out DigTunnelCellVisual cell)
        {
            cell = hit.collider == null
                ? null!
                : hit.collider.GetComponentInParent<DigTunnelCellVisual>();
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
        }

        internal void ShowRoute(TunnelPath? path, float destinationOffsetX = 0f)
        {
            EnsureRouteResources();
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
                SpatialCellId cell = path.Cells[index];
                Vector3 position = DigTunnelProjection.RouteWorldPosition(cell);
                if (index == path.Cells.Count - 1)
                {
                    position.x += destinationOffsetX;
                }

                _route.SetPosition(index, position);
            }
        }

        private void ReconcileCellProxies(TunnelNavigationVolume volume)
        {
            EnsureRoots();
            HashSet<SpatialCellId> visible = new HashSet<SpatialCellId>(volume.Cells);
            SpatialCellId[] removed = _cells.Keys
                .Where(cell => !visible.Contains(cell))
                .ToArray();
            for (int index = 0; index < removed.Length; index++)
            {
                DigTunnelCellVisual visual = _cells[removed[index]];
                _cells.Remove(removed[index]);
                Destroy(visual.gameObject);
            }

            foreach (SpatialCellId cell in volume.Cells)
            {
                if (_cells.ContainsKey(cell))
                {
                    continue;
                }

                GameObject proxy = new GameObject($"Dig cell proxy {cell}");
                proxy.transform.SetParent(_cellProxyRoot, worldPositionStays: true);
                proxy.transform.SetPositionAndRotation(
                    DigTunnelProjection.CellWorldPosition(cell),
                    Quaternion.identity);
                BoxCollider collider = proxy.AddComponent<BoxCollider>();
                collider.size = new Vector3(0.94f, 0.94f, 0.50f);
                collider.enabled = _digInteractionActive;
                DigTunnelCellVisual visual = proxy.AddComponent<DigTunnelCellVisual>();
                visual.Configure(cell, volume.IsVerticalTunnel(cell));
                _cells.Add(cell, visual);
            }
        }

        private void RebuildMovementSurfaces(TunnelNavigationVolume volume)
        {
            EnsureRoots();
            if (_movementSurfaceRoot != null)
            {
                _movementSurfaceRoot.gameObject.SetActive(false);
                Destroy(_movementSurfaceRoot.gameObject);
            }

            _movementSurfaceRoot = new GameObject("Freeform Tunnel Movement Surfaces").transform;
            _movementSurfaceRoot.SetParent(transform, worldPositionStays: true);
            CreateSurfaceRuns(
                volume.Cells.OrderBy(cell => cell.Z)
                    .ThenBy(cell => cell.Y)
                    .ThenBy(cell => cell.X),
                DigTunnelMovementSurfaceKind.Horizontal,
                sameLine: (left, right) => left.Y == right.Y && left.Z == right.Z,
                consecutive: (left, right) => right.X == left.X + 1);
            CreateSurfaceRuns(
                volume.VerticalCells.OrderBy(cell => cell.Z)
                    .ThenBy(cell => cell.X)
                    .ThenBy(cell => cell.Y),
                DigTunnelMovementSurfaceKind.Vertical,
                sameLine: (left, right) => left.X == right.X && left.Z == right.Z,
                consecutive: (left, right) => right.Y == left.Y + 1);
        }

        private void CreateSurfaceRuns(
            IEnumerable<SpatialCellId> orderedCells,
            DigTunnelMovementSurfaceKind kind,
            Func<SpatialCellId, SpatialCellId, bool> sameLine,
            Func<SpatialCellId, SpatialCellId, bool> consecutive)
        {
            List<SpatialCellId> run = new List<SpatialCellId>();
            foreach (SpatialCellId cell in orderedCells)
            {
                if (run.Count > 0
                    && (!sameLine(run[run.Count - 1], cell)
                        || !consecutive(run[run.Count - 1], cell)))
                {
                    CreateMovementSurface(run, kind);
                    run.Clear();
                }

                run.Add(cell);
            }

            if (run.Count > 0)
            {
                CreateMovementSurface(run, kind);
            }
        }

        private void CreateMovementSurface(
            IReadOnlyCollection<SpatialCellId> run,
            DigTunnelMovementSurfaceKind kind)
        {
            SpatialCellId[] cells = run.OrderBy(cell => cell).ToArray();
            Bounds bounds = ResolveSurfaceBounds(cells, kind);
            GameObject surfaceObject = new GameObject(
                $"{kind} movement surface {cells[0]}..{cells[cells.Length - 1]}");
            surfaceObject.transform.SetParent(_movementSurfaceRoot, worldPositionStays: true);
            surfaceObject.transform.SetPositionAndRotation(bounds.center, Quaternion.identity);
            BoxCollider collider = surfaceObject.AddComponent<BoxCollider>();
            collider.size = bounds.size;
            collider.enabled = !_digInteractionActive;
            DigTunnelMovementSurface surface =
                surfaceObject.AddComponent<DigTunnelMovementSurface>();
            surface.Configure(kind, cells);
        }

        private static Bounds ResolveSurfaceBounds(
            IReadOnlyList<SpatialCellId> cells,
            DigTunnelMovementSurfaceKind kind)
        {
            Vector3 first = DigTunnelProjection.CellWorldPosition(cells[0]);
            Vector3 last = DigTunnelProjection.CellWorldPosition(cells[cells.Count - 1]);
            if (kind == DigTunnelMovementSurfaceKind.Horizontal)
            {
                float width = Mathf.Abs(last.x - first.x) + 0.94f;
                return new Bounds(
                    new Vector3((first.x + last.x) * 0.5f, first.y, first.z),
                    new Vector3(width, 0.94f, 0.50f));
            }

            float height = Mathf.Abs(last.y - first.y) + 0.94f;
            return new Bounds(
                new Vector3(first.x, (first.y + last.y) * 0.5f, first.z),
                new Vector3(0.94f, height, 0.50f));
        }

        private void EnsureRoots()
        {
            if (_cellProxyRoot != null)
            {
                return;
            }

            _cellProxyRoot = new GameObject("Tunnel Dig Cell Proxies").transform;
            _cellProxyRoot.SetParent(transform, worldPositionStays: true);
        }

        private void EnsureRouteResources()
        {
            if (_route != null)
            {
                return;
            }

            _materialLibrary = GetComponent<DigRenderMaterialLibrary>();
            if (_materialLibrary == null)
            {
                _materialLibrary = gameObject.AddComponent<DigRenderMaterialLibrary>();
            }

            Material routeMaterial = _materialLibrary.Resolve(
                RenderMaterialSemantic.Overlay,
                RenderSurfaceKind.Overlay,
                Color.white);
            Color routeColor = new Color(0.35f, 0.95f, 1f, 1f);
            _routeRoot = new GameObject("Tunnel Route Overlay").transform;
            _routeRoot.SetParent(transform, worldPositionStays: true);
            GameObject routeObject = new GameObject("Selected tunnel route");
            routeObject.transform.SetParent(_routeRoot, worldPositionStays: true);
            _route = routeObject.AddComponent<LineRenderer>();
            _route.sharedMaterial = routeMaterial;
            _route.startColor = routeColor;
            _route.endColor = routeColor;
            _route.useWorldSpace = true;
            _route.widthMultiplier = 0.09f;
            _route.numCapVertices = 3;
            _route.numCornerVertices = 3;
            _route.enabled = false;
        }

        private static int CalculateSignature(TunnelNavigationVolume volume)
        {
            unchecked
            {
                int hash = 17;
                foreach (SpatialCellId cell in volume.Cells)
                {
                    hash = (hash * 31) + cell.GetHashCode();
                    hash = (hash * 31) + (volume.IsVerticalTunnel(cell) ? 1 : 0);
                }

                return hash;
            }
        }

    }
}
