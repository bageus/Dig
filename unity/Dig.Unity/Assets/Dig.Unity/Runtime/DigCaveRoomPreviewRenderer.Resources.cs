using System;
using Dig.Presentation.Overlays;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigCaveRoomPreviewRenderer
    {
        private void EnsureResources()
        {
            if (_overlays == null)
            {
                throw new InvalidOperationException("Cave room preview requires DigOverlayManager.");
            }

            if (_root == null)
            {
                _root = new GameObject("Cave Room Preview").transform;
                _root.SetParent(transform, worldPositionStays: true);
                _root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                _overlays.RegisterLayer(OverlayLayerKind.Preview, _root);
            }

            EnsureFillRenderer();
            while (_edges.Count < EdgeCount)
            {
                CreateEdge("box");
            }

            while (_edges.Count < TotalEdgeCount)
            {
                CreateEdge("invalid cross");
            }
        }

        private void EnsureFillRenderer()
        {
            if (_fillRenderer != null)
            {
                return;
            }

            GameObject fillObject = new GameObject("Cave room preview fill");
            fillObject.transform.SetParent(_root, worldPositionStays: true);
            _fillFilter = fillObject.AddComponent<MeshFilter>();
            _fillRenderer = fillObject.AddComponent<MeshRenderer>();
            _fillMesh = new Mesh { name = "Cave room preview fill mesh" };
            _fillMesh.MarkDynamic();
            _fillFilter.sharedMesh = _fillMesh;
            _overlays!.ConfigureRenderer(
                _fillRenderer,
                OverlayLayerKind.Preview,
                OverlaySemanticKind.PreviewValid);
            _fillProperties = new MaterialPropertyBlock();
            _fillProperties.SetColor("_BaseColor", RoomPreviewColor);
            _fillProperties.SetColor("_Color", RoomPreviewColor);
            _fillRenderer.SetPropertyBlock(_fillProperties);
            _fillRenderer.enabled = false;
        }

        private void CreateEdge(string role)
        {
            GameObject edgeObject = new GameObject(
                $"Cave room preview {role} edge {_edges.Count + 1}");
            edgeObject.transform.SetParent(_root, worldPositionStays: true);
            LineRenderer edge = edgeObject.AddComponent<LineRenderer>();
            edge.positionCount = 2;
            edge.useWorldSpace = true;
            edge.enabled = false;
            _edges.Add(edge);
        }

        private void OnDestroy()
        {
            if (_fillMesh != null)
            {
                Destroy(_fillMesh);
            }

            if (_root != null && _overlays != null)
            {
                _overlays.UnregisterLayer(OverlayLayerKind.Preview, _root);
            }
        }
    }
}
