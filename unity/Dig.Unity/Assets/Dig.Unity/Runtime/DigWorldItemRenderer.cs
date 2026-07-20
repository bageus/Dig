using System;
using System.Collections.Generic;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigWorldItemRenderer : MonoBehaviour
    {
        private const string CatalogResourcePath = "Dig/VisualCatalogs/Items";
        private const int MaximumPooledRoots = 64;

        [SerializeField]
        private DigItemVisualCatalog? visualCatalog;

        private readonly Dictionary<string, DigWorldItemVisual> _visuals =
            new Dictionary<string, DigWorldItemVisual>(StringComparer.Ordinal);
        private readonly Stack<DigWorldItemVisual> _pool =
            new Stack<DigWorldItemVisual>();
        private readonly ItemStackVisualLayoutPresenter _layoutPresenter =
            new ItemStackVisualLayoutPresenter();
        private Transform? _root;

        internal int ActiveStackCount => _visuals.Count;
        internal int PooledRootCount => _pool.Count;

        private void Awake()
        {
            if (visualCatalog == null)
            {
                visualCatalog = Resources.Load<DigItemVisualCatalog>(CatalogResourcePath);
            }

            DigVisualCatalogDiagnostics.LogValidation(
                visualCatalog,
                this,
                "Items");
        }

        public void SetVisualCatalog(DigItemVisualCatalog? catalog)
        {
            visualCatalog = catalog;
            DigVisualCatalogDiagnostics.LogValidation(
                visualCatalog,
                this,
                "Items");
            foreach (DigWorldItemVisual visual in _visuals.Values)
            {
                visual.InvalidateAsset();
            }
        }

        public void Render(IReadOnlyList<WorldItemViewModel> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            EnsureRoot();
            HashSet<string> visible = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < items.Count; index++)
            {
                WorldItemViewModel item = items[index];
                visible.Add(item.StackId);
                if (!_visuals.TryGetValue(item.StackId, out DigWorldItemVisual? visual))
                {
                    visual = AcquireVisual();
                    _visuals.Add(item.StackId, visual);
                }

                ItemStackVisualLayoutViewModel layout = _layoutPresenter.Present(item);
                visual.Configure(item, layout, Resolve(item.ItemId));
            }

            RemoveMissing(visible);
        }

        public bool TryGetItem(RaycastHit hit, out DigWorldItemVisual visual)
        {
            DigWorldItemVisual? candidate = hit.collider == null
                ? null
                : hit.collider.GetComponentInParent<DigWorldItemVisual>();
            if (candidate != null
                && candidate.Model.IsInteractive
                && _visuals.TryGetValue(
                    candidate.Model.StackId,
                    out DigWorldItemVisual? tracked)
                && ReferenceEquals(candidate, tracked))
            {
                visual = candidate;
                return true;
            }

            visual = null!;
            return false;
        }

        private DigItemVisualResolution Resolve(string itemId)
        {
            if (visualCatalog != null)
            {
                return visualCatalog.ResolveItem(itemId);
            }

            return new DigItemVisualResolution(
                DigVisualAsset.CreateRuntimeFallback(itemId, Color.magenta),
                icon: null,
                DigItemCarrySocketPolicy.None,
                new Vector3(0.34f, 0.34f, 0.34f),
                new Vector3(0.28f, 0.28f, 0.28f),
                DigItemRotationPolicy.StackQuarterTurns,
                DigItemColliderPolicy.InteractiveOnly,
                maxVisibleInstances: 4,
                hasProfile: false);
        }

        private DigWorldItemVisual AcquireVisual()
        {
            DigWorldItemVisual visual;
            if (_pool.Count > 0)
            {
                visual = _pool.Pop();
                visual.gameObject.SetActive(true);
            }
            else
            {
                GameObject root = new GameObject("World Item Stack");
                root.transform.SetParent(_root, worldPositionStays: false);
                visual = root.AddComponent<DigWorldItemVisual>();
            }

            visual.transform.SetParent(_root, worldPositionStays: false);
            return visual;
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

            for (int index = 0; index < removed.Count; index++)
            {
                string id = removed[index];
                DigWorldItemVisual visual = _visuals[id];
                _visuals.Remove(id);
                if (_pool.Count < MaximumPooledRoots)
                {
                    visual.PrepareForPool();
                    _pool.Push(visual);
                }
                else
                {
                    Destroy(visual.gameObject);
                }
            }
        }

        private void EnsureRoot()
        {
            if (_root != null)
            {
                return;
            }

            _root = new GameObject("World Item Visuals").transform;
            _root.SetParent(transform, worldPositionStays: false);
        }
    }
}
