using System;
using System.Collections.Generic;
using Dig.Application.World;
using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigCaveRoomPreviewRenderer : MonoBehaviour
    {
        private const int EdgeCount = 12;
        private readonly List<LineRenderer> _edges = new List<LineRenderer>(EdgeCount);
        private Transform? _root;
        private Material? _validMaterial;
        private Material? _invalidMaterial;

        internal void Show(
            CaveRoomPreset preset,
            CellId entrance,
            bool valid)
        {
            if (preset == null)
            {
                throw new ArgumentNullException(nameof(preset));
            }

            EnsureResources();
            Vector3[] corners = CreateCorners(preset, entrance);
            Vector2Int[] connections =
            {
                new Vector2Int(0, 1),
                new Vector2Int(1, 2),
                new Vector2Int(2, 3),
                new Vector2Int(3, 0),
                new Vector2Int(4, 5),
                new Vector2Int(5, 6),
                new Vector2Int(6, 7),
                new Vector2Int(7, 4),
                new Vector2Int(0, 4),
                new Vector2Int(1, 5),
                new Vector2Int(2, 6),
                new Vector2Int(3, 7),
            };
            Material material = valid ? _validMaterial! : _invalidMaterial!;
            for (int index = 0; index < connections.Length; index++)
            {
                LineRenderer edge = _edges[index];
                edge.sharedMaterial = material;
                edge.enabled = true;
                edge.SetPosition(0, corners[connections[index].x]);
                edge.SetPosition(1, corners[connections[index].y]);
            }
        }

        internal void Clear()
        {
            for (int index = 0; index < _edges.Count; index++)
            {
                _edges[index].enabled = false;
            }
        }

        private static Vector3[] CreateCorners(
            CaveRoomPreset preset,
            CellId entrance)
        {
            int baseMinX = entrance.X - ((preset.BaseWidth - 1) / 2);
            int topMinX = entrance.X - ((preset.TopWidth - 1) / 2);
            float baseLeft = baseMinX - 0.5f;
            float baseRight = baseMinX + preset.BaseWidth - 0.5f;
            float topLeft = topMinX - 0.5f;
            float topRight = topMinX + preset.TopWidth - 0.5f;
            float bottom = -entrance.Y - 0.5f;
            float top = -entrance.Y + preset.Height - 0.5f;
            float firstDepth = DigTunnelProjection.CellWorldPosition(
                new SpatialCellId(entrance.X, entrance.Y, 0)).z;
            float lastDepth = DigTunnelProjection.CellWorldPosition(
                new SpatialCellId(entrance.X, entrance.Y, preset.Depth - 1)).z;
            float halfDepth = Mathf.Abs(DigTunnelProjection.DepthSpacing) * 0.47f;
            float front = Mathf.Max(firstDepth, lastDepth) + halfDepth;
            float back = Mathf.Min(firstDepth, lastDepth) - halfDepth;
            return new[]
            {
                new Vector3(baseLeft, bottom, front),
                new Vector3(baseRight, bottom, front),
                new Vector3(topRight, top, front),
                new Vector3(topLeft, top, front),
                new Vector3(baseLeft, bottom, back),
                new Vector3(baseRight, bottom, back),
                new Vector3(topRight, top, back),
                new Vector3(topLeft, top, back),
            };
        }

        private void EnsureResources()
        {
            if (_root == null)
            {
                _root = new GameObject("Cave Room Preview").transform;
                _root.SetParent(transform, worldPositionStays: true);
                _root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            }

            if (_validMaterial == null || _invalidMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                {
                    shader = Shader.Find("Unlit/Color");
                }

                if (shader == null)
                {
                    throw new InvalidOperationException(
                        "Cave room preview requires an unlit shader.");
                }

                _validMaterial = CreateMaterial(
                    shader,
                    "Valid Cave Room Preview",
                    new Color(0.25f, 1f, 0.42f, 1f));
                _invalidMaterial = CreateMaterial(
                    shader,
                    "Invalid Cave Room Preview",
                    new Color(1f, 0.18f, 0.16f, 1f));
            }

            while (_edges.Count < EdgeCount)
            {
                GameObject edgeObject = new GameObject(
                    $"Cave room preview edge {_edges.Count + 1}");
                edgeObject.transform.SetParent(_root, worldPositionStays: true);
                LineRenderer edge = edgeObject.AddComponent<LineRenderer>();
                edge.positionCount = 2;
                edge.useWorldSpace = true;
                edge.widthMultiplier = 0.075f;
                edge.numCapVertices = 2;
                edge.enabled = false;
                _edges.Add(edge);
            }
        }

        private static Material CreateMaterial(
            Shader shader,
            string name,
            Color color)
        {
            return new Material(shader)
            {
                name = name,
                color = color,
            };
        }

        private void OnDestroy()
        {
            if (_validMaterial != null)
            {
                Destroy(_validMaterial);
            }

            if (_invalidMaterial != null)
            {
                Destroy(_invalidMaterial);
            }
        }
    }
}
