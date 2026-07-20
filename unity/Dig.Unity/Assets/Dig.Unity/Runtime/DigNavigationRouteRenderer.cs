using System;
using System.Collections.Generic;
using Dig.Presentation.Navigation;
using Dig.Presentation.Overlays;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigNavigationRouteRenderer : MonoBehaviour
    {
        private readonly Dictionary<string, LineRenderer> _routes =
            new Dictionary<string, LineRenderer>(StringComparer.Ordinal);
        private Transform? _root;
        private DigOverlayManager? _overlays;

        internal void Initialize(DigOverlayManager overlays)
        {
            _overlays = overlays
                ?? throw new ArgumentNullException(nameof(overlays));
        }

        public void Render(IReadOnlyList<RouteViewModel> routes)
        {
            if (routes == null)
            {
                throw new ArgumentNullException(nameof(routes));
            }

            EnsureResources();
            HashSet<string> visibleIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < routes.Count; index++)
            {
                RouteViewModel route = routes[index];
                visibleIds.Add(route.JobId);
                if (!_routes.TryGetValue(route.JobId, out LineRenderer? line))
                {
                    GameObject routeObject = new GameObject($"Route {route.JobId}");
                    routeObject.transform.SetParent(_root, worldPositionStays: false);
                    line = routeObject.AddComponent<LineRenderer>();
                    Configure(line);
                    _routes.Add(route.JobId, line);
                }

                Apply(line, route);
            }

            RemoveMissing(visibleIds);
        }

        private void Configure(LineRenderer line)
        {
            _overlays!.ConfigureLineRenderer(
                line,
                OverlayLayerKind.Routes,
                OverlaySemanticKind.Route);
            line.useWorldSpace = true;
        }

        private static void Apply(LineRenderer line, RouteViewModel route)
        {
            bool render = route.Succeeded && route.Cells.Count > 0;
            line.enabled = render;
            if (!render)
            {
                line.positionCount = 0;
                return;
            }

            line.positionCount = route.Cells.Count;
            for (int index = 0; index < route.Cells.Count; index++)
            {
                RouteCellViewModel cell = route.Cells[index];
                line.SetPosition(index, new Vector3(cell.X, 0.32f, cell.Y));
            }
        }

        private void RemoveMissing(HashSet<string> visibleIds)
        {
            List<string> removed = new List<string>();
            foreach (KeyValuePair<string, LineRenderer> pair in _routes)
            {
                if (!visibleIds.Contains(pair.Key))
                {
                    removed.Add(pair.Key);
                }
            }

            for (int index = 0; index < removed.Count; index++)
            {
                string id = removed[index];
                LineRenderer line = _routes[id];
                _routes.Remove(id);
                Destroy(line.gameObject);
            }
        }

        private void EnsureResources()
        {
            if (_overlays == null)
            {
                throw new InvalidOperationException(
                    "Navigation route renderer requires DigOverlayManager.");
            }

            if (_root != null)
            {
                return;
            }

            _root = new GameObject("Navigation Routes").transform;
            _root.SetParent(transform, worldPositionStays: false);
            _overlays.RegisterLayer(OverlayLayerKind.Routes, _root);
        }

        private void OnDestroy()
        {
            if (_root != null && _overlays != null)
            {
                _overlays.UnregisterLayer(OverlayLayerKind.Routes, _root);
            }
        }
    }
}
