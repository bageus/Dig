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

        private static readonly Vector2[] CellItemOffsets =
        {
            new Vector2(-0.24f, -0.10f),
            new Vector2(0.24f, -0.10f),
            new Vector2(-0.12f, 0.13f),
            new Vector2(0.12f, 0.13f),
            new Vector2(0f, 0f),
        };

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
            Dictionary<(int X, int Y, int Z), int> cellSlots =
                new Dictionary<(int X, int Y, int Z), int>();
            for (int index = 0; index < items.Count; index++)
            {
                WorldItemViewModel item = items[index];
                visible.Add(item.StackId);
                if (!_visuals.TryGetValue(item.StackId, out DigWorldItemVisual? visual))
                {
                    visual = AcquireVisual();
                    _visuals.Add(item.StackId, visual);
                }

                (int X, int Y, int Z) cell = (item.CellX, item.CellY, item.CellZ);
                cellSlots.TryGetValue(cell, out int slot);
                cellSlots[cell] = slot + 1;

                ItemStackVisualLayoutViewModel layout = _layoutPresenter.Present(item);
                visual.Configure(item, layout, Resolve(item.ItemId));
                visual.SetCellOffset(ResolveCellOffset(slot));
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

        private static Vector2 ResolveCellOffset(int slot)
        {
            if (slot < CellItemOffsets.Length)
            {
                return CellItemOffsets[slot];
            }

            int ringSlot = slot - CellItemOffsets.Length;
            float angle = ringSlot * 2.39996323f;
            float radius = Mathf.Min(0.32f, 0.20f + ((ringSlot / 6) * 0.04f));
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
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
