using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dig.Unity
{
    internal sealed class DigRepresentativeBuildingMeshFactory : IDisposable
    {
        private readonly Dictionary<DigRepresentativeBuildingShape, Mesh> _meshes =
            new Dictionary<DigRepresentativeBuildingShape, Mesh>();

        internal Mesh Resolve(DigRepresentativeBuildingShape shape)
        {
            if (_meshes.TryGetValue(shape, out Mesh? mesh))
            {
                return mesh;
            }

            mesh = shape switch
            {
                DigRepresentativeBuildingShape.Box => CreateBox(),
                DigRepresentativeBuildingShape.Pyramid => CreatePyramid(),
                DigRepresentativeBuildingShape.Octahedron => CreateOctahedron(),
                DigRepresentativeBuildingShape.Wedge => CreateWedge(),
                _ => throw new ArgumentOutOfRangeException(nameof(shape)),
            };
            mesh.name = $"Dig Representative {shape}";
            mesh.hideFlags = HideFlags.HideAndDontSave;
            _meshes.Add(shape, mesh);
            return mesh;
        }

        internal static int TriangleCount(DigRepresentativeBuildingShape shape)
        {
            return shape switch
            {
                DigRepresentativeBuildingShape.Box => 12,
                DigRepresentativeBuildingShape.Pyramid => 6,
                DigRepresentativeBuildingShape.Octahedron => 8,
                DigRepresentativeBuildingShape.Wedge => 8,
                _ => 0,
            };
        }

        public void Dispose()
        {
            foreach (Mesh mesh in _meshes.Values)
            {
                if (mesh != null)
                {
                    UnityEngine.Object.Destroy(mesh);
                }
            }

            _meshes.Clear();
        }

        private static Mesh CreateBox()
        {
            Vector3[] vertices =
            {
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f),
            };
            int[] triangles =
            {
                0, 2, 1, 0, 3, 2,
                5, 6, 4, 6, 7, 4,
                4, 7, 0, 7, 3, 0,
                1, 2, 5, 2, 6, 5,
                3, 7, 2, 7, 6, 2,
                4, 0, 5, 0, 1, 5,
            };
            return Create(vertices, triangles);
        }

        private static Mesh CreatePyramid()
        {
            Vector3[] vertices =
            {
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(0f, 0.5f, 0f),
            };
            int[] triangles =
            {
                0, 1, 4,
                1, 2, 4,
                2, 3, 4,
                3, 0, 4,
                0, 3, 2,
                0, 2, 1,
            };
            return Create(vertices, triangles);
        }

        private static Mesh CreateOctahedron()
        {
            Vector3[] vertices =
            {
                new Vector3(0f, 0.5f, 0f),
                new Vector3(0f, -0.5f, 0f),
                new Vector3(-0.5f, 0f, 0f),
                new Vector3(0.5f, 0f, 0f),
                new Vector3(0f, 0f, -0.5f),
                new Vector3(0f, 0f, 0.5f),
            };
            int[] triangles =
            {
                0, 4, 3,
                0, 3, 5,
                0, 5, 2,
                0, 2, 4,
                1, 3, 4,
                1, 5, 3,
                1, 2, 5,
                1, 4, 2,
            };
            return Create(vertices, triangles);
        }

        private static Mesh CreateWedge()
        {
            Vector3[] vertices =
            {
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
            };
            int[] triangles =
            {
                0, 1, 2, 1, 3, 2,
                2, 3, 4, 3, 5, 4,
                0, 2, 4, 0, 4, 1,
                1, 4, 5, 1, 5, 3,
            };
            return Create(vertices, triangles);
        }

        private static Mesh Create(Vector3[] vertices, int[] triangles)
        {
            Mesh mesh = new Mesh
            {
                vertices = vertices,
                triangles = triangles,
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
