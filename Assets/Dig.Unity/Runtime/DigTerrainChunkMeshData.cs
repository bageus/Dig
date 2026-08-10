using System;
using UnityEngine;

namespace Dig.Unity
{
    internal sealed class DigTerrainChunkMeshData
    {
        internal DigTerrainChunkMeshData(
            Vector3[] vertices,
            Vector3[] normals,
            Color[] colors,
            int[][] triangles,
            DigTerrainMaterialKey[] materialKeys)
        {
            Vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
            Normals = normals ?? throw new ArgumentNullException(nameof(normals));
            Colors = colors ?? throw new ArgumentNullException(nameof(colors));
            Triangles = triangles ?? throw new ArgumentNullException(nameof(triangles));
            MaterialKeys = materialKeys
                ?? throw new ArgumentNullException(nameof(materialKeys));
            if (vertices.Length != normals.Length || vertices.Length != colors.Length)
            {
                throw new ArgumentException(
                    "Terrain chunk vertices, normals and colors must have equal lengths.");
            }

            if (triangles.Length != materialKeys.Length)
            {
                throw new ArgumentException(
                    "Terrain chunk submeshes and material keys must have equal lengths.");
            }
        }

        internal Vector3[] Vertices { get; }
        internal Vector3[] Normals { get; }
        internal Color[] Colors { get; }
        internal int[][] Triangles { get; }
        internal DigTerrainMaterialKey[] MaterialKeys { get; }

        internal int TriangleCount
        {
            get
            {
                int count = 0;
                for (int index = 0; index < Triangles.Length; index++)
                {
                    count += Triangles[index].Length / 3;
                }

                return count;
            }
        }
    }
}
