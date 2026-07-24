using System.Collections.Generic;
using Dig.Domain.World;
using Dig.Presentation.Overlays;
using Dig.Presentation.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    internal sealed class DigExcavationCursorRenderer : MonoBehaviour
    {
        private const float FaceOffset = 0.025f;
        private const float MarkerThickness = 0.025f;
        private const float DesignationOverlap = 1.015f;
        private static readonly Color TunnelColor =
            new Color(0.68f, 0.86f, 0.62f, 0.32f);
        private static readonly Color DepthColor =
            new Color(0.74f, 0.62f, 0.90f, 0.28f);
        private static readonly Color EraseColor =
            new Color(0.58f, 0.58f, 0.58f, 0.42f);

        private readonly Dictionary<CellId, GameObject> _tunnelDesignations =
            new Dictionary<CellId, GameObject>();
        private readonly HashSet<CellId> _visibleDesignations =
            new HashSet<CellId>();
        private readonly List<CellId> _removedDesignations =
            new List<CellId>();
        private GameObject? _marker;
        private DigOverlayManager? _overlays;
        private MaterialPropertyBlock? _properties;
        private long _synchronizedWorldVersion = long.MinValue;

        internal void Initialize(DigOverlayManager overlays)
        {
            _overlays = overlays;
        }

        internal void Show(CellId cell, bool depth, bool erase)
        {
            _marker ??= CreateMarker("Excavation tool cursor", sortingOrder: 50);
            Color color = erase
                ? EraseColor
                : depth
                    ? DepthColor
                    : TunnelColor;
            PlaceMarker(_marker, cell, color, overlap: false);
        }

        internal void Hide()
        {
            if (_marker != null)
            {
                _marker.SetActive(false);
            }
        }

        internal void SetTunnelDesignation(CellId cell, bool active)
        {
            if (!active)
            {
                RemoveTunnelDesignation(cell);
                return;
            }

            if (!_tunnelDesignations.TryGetValue(cell, out GameObject? marker))
            {
                marker = CreateMarker(
                    $"Tunnel designation {cell.X},{cell.Y},{cell.Z}",
                    sortingOrder: 40);
                _tunnelDesignations.Add(cell, marker);
            }

            PlaceMarker(marker, cell, TunnelColor, overlap: true);
        }

        internal void SynchronizeTunnelDesignations(WorldViewModel world)
        {
            if (world.Version == _synchronizedWorldVersion)
            {
                return;
            }

            _synchronizedWorldVersion = world.Version;
            _visibleDesignations.Clear();
            foreach (WorldChunkViewModel chunk in world.Chunks)
            {
                foreach (WorldCellViewModel cell in chunk.Cells)
                {
                    if (!cell.IsDesignated || !cell.IsSolid || cell.Z != 0)
                    {
                        continue;
                    }

                    CellId id = new CellId(cell.X, cell.Y, cell.Z);
                    _visibleDesignations.Add(id);
                    SetTunnelDesignation(id, active: true);
                }
            }

            _removedDesignations.Clear();
            foreach (CellId cell in _tunnelDesignations.Keys)
            {
                if (!_visibleDesignations.Contains(cell))
                {
                    _removedDesignations.Add(cell);
                }
            }

            for (int index = 0; index < _removedDesignations.Count; index++)
            {
                RemoveTunnelDesignation(_removedDesignations[index]);
            }
        }

        internal void InvalidateDesignationSynchronization()
        {
            _synchronizedWorldVersion = long.MinValue;
        }

        private void RemoveTunnelDesignation(CellId cell)
        {
            if (!_tunnelDesignations.TryGetValue(cell, out GameObject? existing))
            {
                return;
            }

            Destroy(existing);
            _tunnelDesignations.Remove(cell);
        }

        private GameObject CreateMarker(string markerName, int sortingOrder)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = markerName;
            marker.transform.SetParent(transform, worldPositionStays: true);
            Collider collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = marker.GetComponent<Renderer>();
            _overlays!.ConfigureRenderer(
                renderer,
                OverlayLayerKind.Preview,
                OverlaySemanticKind.PreviewValid);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = sortingOrder;
            marker.SetActive(false);
            return marker;
        }

        private void PlaceMarker(
            GameObject marker,
            CellId cell,
            Color color,
            bool overlap)
        {
            Vector3 center = DigTunnelProjection.CellWorldPosition(cell);
            marker.transform.position = center + new Vector3(
                0f,
                0f,
                DigTunnelProjection.RockCellHalfExtent + FaceOffset);
            marker.transform.rotation = Quaternion.identity;
            float faceSize = overlap ? DesignationOverlap : 0.94f;
            marker.transform.localScale = new Vector3(
                faceSize,
                faceSize,
                MarkerThickness);

            Renderer renderer = marker.GetComponent<Renderer>();
            _properties ??= new MaterialPropertyBlock();
            _properties.Clear();
            _properties.SetColor("_BaseColor", color);
            _properties.SetColor("_Color", color);
            renderer.SetPropertyBlock(_properties);
            marker.SetActive(true);
        }
    }
}
