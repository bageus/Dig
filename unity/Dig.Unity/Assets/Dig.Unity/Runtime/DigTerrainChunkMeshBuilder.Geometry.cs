using System.Collections.Generic;
using UnityEngine;

namespace Dig.Unity
{
    internal static partial class DigTerrainChunkMeshBuilder
    {
        private static void ResolveDepthExtents(
            int z,
            out float minDepth,
            out float maxDepth)
        {
            if (z == 0)
            {
                minDepth = 0f;
                maxDepth = FrontRockDepth;
                return;
            }

            float center = DigTunnelProjection.DepthOrigin
                + (z * DigTunnelProjection.DepthSpacing);
            float half = Mathf.Abs(DigTunnelProjection.DepthSpacing)
                * DepthLayerScale
                * 0.5f;
            minDepth = center - half;
            maxDepth = center + half;
        }

        private static void AddFace(
            DigTerrainCellKey cell,
            int salt,
            Vector3 normal,
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector3 d,
            int submesh,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<List<int>> triangles)
        {
            Vector3 offset = normal * ResolvePlaneOffset(cell, salt);
            int start = vertices.Count;
            vertices.Add(a + offset);
            vertices.Add(b + offset);
            vertices.Add(c + offset);
            vertices.Add(d + offset);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            triangles[submesh].Add(start);
            triangles[submesh].Add(start + 1);
            triangles[submesh].Add(start + 2);
            triangles[submesh].Add(start);
            triangles[submesh].Add(start + 2);
            triangles[submesh].Add(start + 3);
        }

        private static float ResolvePlaneOffset(DigTerrainCellKey cell, int salt)
        {
            unchecked
            {
                // Every coplanar neighbour must receive the same normal offset.
                // Hashing all cell coordinates made adjacent quads sit on slightly
                // different planes and revealed the hidden grid under lighting.
                int plane = salt switch
                {
                    1 => cell.Z,
                    2 => cell.Z,
                    3 => cell.X,
                    4 => cell.X,
                    5 => cell.Y,
                    6 => cell.Y,
                    _ => 0,
                };
                uint hash = (uint)(plane * 19349663)
                    ^ (uint)(cell.Z * 83492791)
                    ^ (uint)(salt * 1640531513);
                hash ^= hash >> 13;
                hash *= 1274126177u;
                float normalized = (hash & 1023u) / 1023f;
                return (normalized - 0.5f) * Roughness;
            }
        }
    }
}
