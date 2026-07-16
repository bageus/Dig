using System;
using System.Collections.Generic;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigWorldItemRenderer : MonoBehaviour
    {
        private readonly Dictionary<string, DigWorldItemVisual> _visuals =
            new Dictionary<string, DigWorldItemVisual>(StringComparer.Ordinal);
        private Transform? _root;
        private Material? _resourceMaterial;
        private Material? _boxMaterial;

        public void Render(IReadOnlyList<WorldItemViewModel> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            EnsureResources();
            HashSet<string> visible = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < items.Count; index++)
            {
                WorldItemViewModel item = items[index];
                visible.Add(item.StackId);
                if (!_visuals.TryGetValue(item.StackId, out DigWorldItemVisual? visual))
                {
                    visual = CreateVisual(item);
                    _visuals.Add(item.StackId, visual);
                }

                visual.Configure(
                    item,
                    item.IsBuildingBox ? _boxMaterial! : _resourceMaterial!);
            }

            RemoveMissing(visible);
        }

        public bool TryGetItem(RaycastHit hit, out DigWorldItemVisual visual)
        {
            DigWorldItemVisual? candidate = hit.collider.GetComponentInParent<DigWorldItemVisual>();
            if (candidate != null
                && candidate.Model.IsInteractive
                && _visuals.TryGetValue(candidate.Model.StackId, out DigWorldItemVisual? tracked)
                && ReferenceEquals(candidate, tracked))
            {
                visual = candidate;
                return true;
            }

            visual = null!;
            return false;
        }

        private DigWorldItemVisual CreateVisual(WorldItemViewModel item)
        {
            PrimitiveType primitive = item.IsBuildingBox
                ? PrimitiveType.Cube
                : PrimitiveType.Sphere;
            GameObject target = GameObject.CreatePrimitive(primitive);
            target.transform.SetParent(_root, worldPositionStays: false);
            return target.AddComponent<DigWorldItemVisual>();
        }

        private void RemoveMissing(HashSet<string> visible)
        {
            List<string> removed = new List<string>();
            foreach (KeyValuePair<string, DigWorldItemVisual> pair in _visuals)
            {
                if (!visible.Contains(pair.Key))
                {
                    removed.Add(pair.Key);
                }
            }

            foreach (string id in removed)
            {
                DigWorldItemVisual visual = _visuals[id];
                _visuals.Remove(id);
                Destroy(visual.gameObject);
            }
        }

        private void EnsureResources()
        {
            if (_root == null)
            {
                _root = new GameObject("World Item Visuals").transform;
                _root.SetParent(transform, worldPositionStays: false);
            }

            if (_resourceMaterial != null && _boxMaterial != null)
            {
                return;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                throw new InvalidOperationException("No supported item shader was found.");
            }

            _resourceMaterial = new Material(shader)
            {
                name = "Dig World Resource",
                color = new Color(0.56f, 0.62f, 0.70f, 1f),
            };
            _boxMaterial = new Material(shader)
            {
                name = "Dig BuildingBox",
                color = new Color(0.84f, 0.58f, 0.24f, 1f),
            };
        }

        private void OnDestroy()
        {
            if (_resourceMaterial != null)
            {
                Destroy(_resourceMaterial);
            }

            if (_boxMaterial != null)
            {
                Destroy(_boxMaterial);
            }
        }
    }
}
