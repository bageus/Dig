using System;
using System.Collections.Generic;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigWorldItemRenderer : MonoBehaviour
    {
        private readonly Dictionary<string, GameObject> _visuals =
            new Dictionary<string, GameObject>(StringComparer.Ordinal);
        private Transform? _root;
        private Material? _material;

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
                if (!_visuals.TryGetValue(item.StackId, out GameObject? visual))
                {
                    visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    visual.transform.SetParent(_root, worldPositionStays: false);
                    visual.GetComponent<Renderer>().sharedMaterial = _material;
                    _visuals.Add(item.StackId, visual);
                }

                Apply(visual, item);
            }

            RemoveMissing(visible);
        }

        private void RemoveMissing(HashSet<string> visible)
        {
            List<string> removed = new List<string>();
            foreach (KeyValuePair<string, GameObject> pair in _visuals)
            {
                if (!visible.Contains(pair.Key))
                {
                    removed.Add(pair.Key);
                }
            }

            foreach (string id in removed)
            {
                GameObject visual = _visuals[id];
                _visuals.Remove(id);
                Destroy(visual);
            }
        }

        private static void Apply(GameObject visual, WorldItemViewModel item)
        {
            float scale = 0.22f + (Mathf.Clamp(item.Quantity, 1, 50) * 0.006f);
            visual.name = $"World item {item.ItemId} x{item.Quantity}";
            visual.transform.localPosition = new Vector3(
                item.CellX,
                0.24f,
                item.CellY);
            visual.transform.localScale = new Vector3(scale, scale, scale);
        }

        private void EnsureResources()
        {
            if (_root == null)
            {
                _root = new GameObject("World Item Visuals").transform;
                _root.SetParent(transform, worldPositionStays: false);
            }

            if (_material != null)
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

            _material = new Material(shader)
            {
                name = "Dig World Resource",
                color = new Color(0.56f, 0.62f, 0.70f, 1f),
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
