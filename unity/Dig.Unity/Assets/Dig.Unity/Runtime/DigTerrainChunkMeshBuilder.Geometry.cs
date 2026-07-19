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
            Vector3 offset = normal * ResolveOffset(cell, salt);
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

        private static float ResolveOffset(DigTerrainCellKey cell, int salt)
        {
            unchecked
            {
                uint hash = (uint)(cell.X * 73856093)
                    ^ (uint)(cell.Y * 19349663)
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
