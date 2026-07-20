using System;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    internal sealed class DigCaveTemplateTrimMeshData
    {
        internal DigCaveTemplateTrimMeshData(
            Vector3[] vertices,
            Vector3[] normals,
            int[][] triangles,
            CaveTemplateTrimRole[] materialRoles)
        {
            Vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
            Normals = normals ?? throw new ArgumentNullException(nameof(normals));
            Triangles = triangles ?? throw new ArgumentNullException(nameof(triangles));
            MaterialRoles = materialRoles
                ?? throw new ArgumentNullException(nameof(materialRoles));
            if (Vertices.Length != Normals.Length)
            {
                throw new ArgumentException(
                    "Cave template trim vertices and normals must match.");
            }

            if (Triangles.Length != MaterialRoles.Length)
            {
                throw new ArgumentException(
                    "Cave template trim submeshes and roles must match.");
            }
        }

        internal Vector3[] Vertices { get; }

        internal Vector3[] Normals { get; }

        internal int[][] Triangles { get; }

        internal CaveTemplateTrimRole[] MaterialRoles { get; }
    }
}
