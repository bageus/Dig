using System;
using System.Collections.Generic;
using Dig.Domain.World;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    internal sealed class DigTunnelInfrastructureRenderer : MonoBehaviour
    {
        private readonly Dictionary<string, GameObject> _visuals =
            new Dictionary<string, GameObject>(StringComparer.Ordinal);
        private readonly HashSet<string> _visibleInstances =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> _removedInstances = new List<string>();
        private Transform? _root;
        private Mesh? _cubeMesh;
        private Material? _woodMaterial;
        private Material? _stoneMaterial;

        internal int InstanceCount => _visuals.Count;

        internal void Render(TunnelInfrastructureVisualVolumeViewModel volume)
        {
            if (volume == null)
            {
                throw new ArgumentNullException(nameof(volume));
            }

            EnsureResources();
            _visibleInstances.Clear();
            for (int index = 0; index < volume.Instances.Count; index++)
            {
                TunnelInfrastructureVisualViewModel instance = volume.Instances[index];
                _visibleInstances.Add(instance.InstanceId);
                if (!_visuals.ContainsKey(instance.InstanceId))
                {
                    _visuals.Add(instance.InstanceId, CreateVisual(instance));
                }
            }

            RemoveMissingInstances();
        }

        internal bool TryGetVisual(string instanceId, out GameObject visual)
        {
            if (_visuals.TryGetValue(instanceId, out GameObject? value))
            {
                visual = value;
                return true;
            }

            visual = null!;
            return false;
        }

        private GameObject CreateVisual(TunnelInfrastructureVisualViewModel instance)
        {
            GameObject visual = new GameObject(
                $"Tunnel infrastructure {instance.InstanceId}");
            visual.transform.SetParent(_root, worldPositionStays: false);
            visual.transform.localPosition = ResolveRootPosition(
                instance.Cell,
                instance.Kind);
            switch (instance.Kind)
            {
                case TunnelInfrastructureVisualKind.WoodenSupport:
                    BuildWoodenSupport(visual.transform);
                    break;
                case TunnelInfrastructureVisualKind.JunctionStoneTrim:
                    BuildJunctionStoneTrim(visual.transform);
                    break;
                case TunnelInfrastructureVisualKind.StoneFloorTrim:
                    BuildStoneFloorLayer(visual.transform);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(instance));
            }

            return visual;
        }

        private void BuildWoodenSupport(Transform parent)
        {
            const float frontZ = 0f;
            CreatePart(
                parent,
                "Wooden support left post",
                new Vector3(-0.43f, 0.46f, frontZ),
                new Vector3(0.12f, 0.92f, 0.10f),
                _woodMaterial!);
            CreatePart(
                parent,
                "Wooden support right post",
                new Vector3(0.43f, 0.46f, frontZ),
                new Vector3(0.12f, 0.92f, 0.10f),
                _woodMaterial!);
            CreatePart(
                parent,
                "Wooden support crossbeam",
                new Vector3(0f, 0.88f, frontZ),
                new Vector3(0.98f, 0.14f, 0.10f),
                _woodMaterial!);
        }

        private void BuildJunctionStoneTrim(Transform parent)
        {
            const float railLength = 0.88f;
            const float railWidth = 0.10f;
            const float edge = 0.18f;
            const float height = 0.06f;
            CreatePart(
                parent,
                "Stone trim front",
                new Vector3(0f, height * 0.5f, edge),
                new Vector3(railLength, height, railWidth),
                _stoneMaterial!);
            CreatePart(
                parent,
                "Stone trim back",
                new Vector3(0f, height * 0.5f, -edge),
                new Vector3(railLength, height, railWidth),
                _stoneMaterial!);
            CreatePart(
                parent,
                "Stone trim left",
                new Vector3(-0.39f, height * 0.5f, 0f),
                new Vector3(railWidth, height, 0.46f),
                _stoneMaterial!);
            CreatePart(
                parent,
                "Stone trim right",
                new Vector3(0.39f, height * 0.5f, 0f),
                new Vector3(railWidth, height, 0.46f),
                _stoneMaterial!);
        }

        private void BuildStoneFloorLayer(Transform parent)
        {
            CreatePart(
                parent,
                "Stone reinforced floor surface",
                new Vector3(0f, 0.025f, 0f),
                new Vector3(0.96f, 0.05f, 0.96f),
                _stoneMaterial!);
        }

        private void CreatePart(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject part = new GameObject(name);
            part.transform.SetParent(parent, worldPositionStays: false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            MeshFilter filter = part.AddComponent<MeshFilter>();
            filter.sharedMesh = _cubeMesh;
            MeshRenderer renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
        }

        private static Vector3 ResolveRootPosition(
            CellId cell,
            TunnelInfrastructureVisualKind kind)
        {
            float depth = kind == TunnelInfrastructureVisualKind.WoodenSupport
                ? DigTunnelProjection.DepthOrigin
                : DigTunnelProjection.DepthOrigin
                    + (cell.Z * DigTunnelProjection.DepthSpacing);
            return new Vector3(
                cell.X,
                DigTunnelProjection.WalkSurfaceY(cell.Y),
                depth);
        }

        private void EnsureResources()
        {
            if (_root == null)
            {
                _root = new GameObject("Tunnel Infrastructure Visuals").transform;
                _root.SetParent(transform, worldPositionStays: false);
            }

            _cubeMesh ??= CreateCubeMesh();
            if (_woodMaterial == null || _stoneMaterial == null)
            {
                Shader shader = ResolveShader();
                _woodMaterial = new Material(shader)
                {
                    name = "Tunnel wooden support material",
                    color = new Color(0.46f, 0.27f, 0.12f, 1f),
                };
                _stoneMaterial = new Material(shader)
                {
                    name = "Tunnel junction stone trim material",
                    color = new Color(0.48f, 0.50f, 0.54f, 1f),
                };
            }
        }

        private static Shader ResolveShader()
        {
            Shader? shader = Shader.Find("Dig/Stylized Unlit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Standard");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "Tunnel infrastructure visuals require a compatible shader.");
            }

            return shader;
        }

        private static Mesh CreateCubeMesh()
        {
            Mesh mesh = new Mesh
            {
                name = "Tunnel infrastructure cube",
                vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f, -0.5f),
                    new Vector3(0.5f, -0.5f, -0.5f),
                    new Vector3(0.5f, 0.5f, -0.5f),
                    new Vector3(-0.5f, 0.5f, -0.5f),
                    new Vector3(-0.5f, -0.5f, 0.5f),
                    new Vector3(0.5f, -0.5f, 0.5f),
                    new Vector3(0.5f, 0.5f, 0.5f),
                    new Vector3(-0.5f, 0.5f, 0.5f),
                },
                triangles = new[]
                {
                    0, 2, 1, 0, 3, 2,
                    1, 2, 6, 1, 6, 5,
                    5, 6, 7, 5, 7, 4,
                    4, 7, 3, 4, 3, 0,
                    3, 7, 6, 3, 6, 2,
                    4, 0, 1, 4, 1, 5,
                },
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void RemoveMissingInstances()
        {
            _removedInstances.Clear();
            foreach (string instanceId in _visuals.Keys)
            {
                if (!_visibleInstances.Contains(instanceId))
                {
                    _removedInstances.Add(instanceId);
                }
            }

            for (int index = 0; index < _removedInstances.Count; index++)
            {
                string instanceId = _removedInstances[index];
                GameObject visual = _visuals[instanceId];
                _visuals.Remove(instanceId);
                Destroy(visual);
            }
        }

        private void OnDestroy()
        {
            if (_woodMaterial != null)
            {
                Destroy(_woodMaterial);
            }

            if (_stoneMaterial != null)
            {
                Destroy(_stoneMaterial);
            }

            if (_cubeMesh != null)
            {
                Destroy(_cubeMesh);
            }
        }
    }
}
