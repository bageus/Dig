using System;
using System.Collections.Generic;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigRockVolumeRenderer : MonoBehaviour
    {
        private readonly List<Mesh> _meshes = new List<Mesh>();
        private readonly List<GameObject> _layers = new List<GameObject>();
        private readonly HashSet<SpatialCellId> _excavatedCells =
            new HashSet<SpatialCellId>();
        private Transform? _root;
        private Material? _material;
        private TunnelNavigationVolume? _volume;

        internal void Initialize(TunnelNavigationVolume volume)
        {
            _volume = volume ?? throw new ArgumentNullException(nameof(volume));
            EnsureResources();
            Rebuild();
        }

        internal void SetExcavatedCells(
            IReadOnlyCollection<SpatialCellId> excavatedCells)
        {
            if (excavatedCells == null)
            {
                throw new ArgumentNullException(nameof(excavatedCells));
            }

            HashSet<SpatialCellId> next = new HashSet<SpatialCellId>();
            foreach (SpatialCellId cell in excavatedCells)
            {
                if (cell.Z > 0)
                {
                    next.Add(cell);
                }
            }

            if (_excavatedCells.SetEquals(next))
            {
                return;
            }

            _excavatedCells.Clear();
            _excavatedCells.UnionWith(next);
            if (_volume != null)
            {
                Rebuild();
            }
        }

        private void Rebuild()
        {
            if (_volume == null)
            {
                return;
            }

            for (int index = 0; index < _layers.Count; index++)
            {
                Destroy(_layers[index]);
            }

            for (int index = 0; index < _meshes.Count; index++)
            {
                Destroy(_meshes[index]);
            }

            _layers.Clear();
            _meshes.Clear();
            for (int z = 1; z < _volume.Depth; z++)
            {
                CreateDepthMesh(_volume, z);
            }
        }

        private void CreateDepthMesh(TunnelNavigationVolume volume, int z)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            for (int y = 0; y < volume.Height; y++)
            {
                for (int x = 0; x < volume.Width; x++)
                {
                    SpatialCellId cell = new SpatialCellId(x, y, z);
                    if (!IsSolidRock(volume, cell))
                    {
                        continue;
                    }

                    AddCube(
                        DigTunnelProjection.CellWorldPosition(cell),
                        new Vector3(
                            0.96f,
                            0.96f,
                            Mathf.Abs(DigTunnelProjection.DepthSpacing) * 0.94f),
                        vertices,
                        triangles);
                }
            }

            if (vertices.Count == 0)
            {
                return;
            }

            GameObject layer = new GameObject($"Solid rock depth {z}");
            layer.transform.SetParent(_root, worldPositionStays: true);
            layer.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Mesh mesh = new Mesh
            {
                name = $"Solid rock depth {z}",
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
            };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, submesh: 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            layer.AddComponent<MeshFilter>().sharedMesh = mesh;
            layer.AddComponent<MeshRenderer>().sharedMaterial = _material;
            _layers.Add(layer);
            _meshes.Add(mesh);
        }

        private bool IsSolidRock(
            TunnelNavigationVolume volume,
            SpatialCellId cell)
        {
            if (_excavatedCells.Contains(cell))
            {
                return false;
            }

            TunnelDemoLayout layout = volume.DemoLayout
                ?? throw new InvalidOperationException("The tunnel demo layout is required.");
            if (cell.Y < layout.SurfaceY || volume.IsOpen(cell))
            {
                return false;
            }

            bool caveInterior = cell.X >= layout.CaveMinX
                && cell.X <= layout.CaveMaxX
                && cell.Y > layout.CaveCeilingY
                && cell.Y <= layout.CaveFloorY;
            return !caveInterior;
        }

        private static void AddCube(
            Vector3 center,
            Vector3 size,
            ICollection<Vector3> vertices,
            ICollection<int> triangles)
        {
            Vector3 half = size * 0.5f;
            Vector3[] corners =
            {
                center + new Vector3(-half.x, -half.y, -half.z),
                center + new Vector3(half.x, -half.y, -half.z),
                center + new Vector3(half.x, half.y, -half.z),
                center + new Vector3(-half.x, half.y, -half.z),
                center + new Vector3(-half.x, -half.y, half.z),
                center + new Vector3(half.x, -half.y, half.z),
                center + new Vector3(half.x, half.y, half.z),
                center + new Vector3(-half.x, half.y, half.z),
            };
            AddFace(vertices, triangles, corners[0], corners[3], corners[2], corners[1]);
            AddFace(vertices, triangles, corners[4], corners[5], corners[6], corners[7]);
            AddFace(vertices, triangles, corners[0], corners[4], corners[7], corners[3]);
            AddFace(vertices, triangles, corners[1], corners[2], corners[6], corners[5]);
            AddFace(vertices, triangles, corners[3], corners[7], corners[6], corners[2]);
            AddFace(vertices, triangles, corners[0], corners[1], corners[5], corners[4]);
        }

        private static void AddFace(
            ICollection<Vector3> vertices,
            ICollection<int> triangles,
            Vector3 first,
            Vector3 second,
            Vector3 third,
            Vector3 fourth)
        {
            int start = vertices.Count;
            vertices.Add(first);
            vertices.Add(second);
            vertices.Add(third);
            vertices.Add(fourth);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }

        private void EnsureResources()
        {
            if (_root == null)
            {
                _root = new GameObject("Solid Rock Volume").transform;
                _root.SetParent(transform, worldPositionStays: true);
                _root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
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
                throw new InvalidOperationException("No supported rock shader was found.");
            }

            _material = new Material(shader)
            {
                name = "Unexcavated rock volume",
                color = new Color(0.30f, 0.27f, 0.25f, 1f),
            };
        }

        private void OnDestroy()
        {
            for (int index = 0; index < _meshes.Count; index++)
            {
                Destroy(_meshes[index]);
            }

            if (_material != null)
            {
                Destroy(_material);
            }
        }
    }
}
