using System;
using System.Collections.Generic;
using Dig.Presentation.Navigation;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigNavigationRouteRenderer : MonoBehaviour
    {
        private readonly Dictionary<string, LineRenderer> _routes =
            new Dictionary<string, LineRenderer>(StringComparer.Ordinal);
        private Transform? _root;
        private Material? _material;
        private bool _visible = true;

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

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.F4) || _root == null)
            {
                return;
            }

            _visible = !_visible;
            _root.gameObject.SetActive(_visible);
        }

        private void Configure(LineRenderer line)
        {
            line.sharedMaterial = _material;
            line.useWorldSpace = true;
            line.widthMultiplier = 0.075f;
            line.numCapVertices = 3;
            line.numCornerVertices = 3;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
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

            foreach (string id in removed)
            {
                LineRenderer line = _routes[id];
                _routes.Remove(id);
                Destroy(line.gameObject);
            }
        }

        private void EnsureResources()
        {
            if (_root == null)
            {
                _root = new GameObject("Navigation Routes [F4]").transform;
                _root.SetParent(transform, worldPositionStays: false);
            }

            if (_material != null)
            {
                return;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                throw new InvalidOperationException("No supported route shader was found.");
            }

            _material = new Material(shader)
            {
                name = "Dig Navigation Route",
                color = new Color(0.40f, 0.92f, 1f, 1f),
            };
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }
        }
    }
}
