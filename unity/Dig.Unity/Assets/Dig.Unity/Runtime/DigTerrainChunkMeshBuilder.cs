using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dig.Unity
{
    internal static class DigTerrainChunkMeshBuilder
    {
        private const float HalfExtent = 0.47f;
        private const float FrontRockDepth = 0.82f;
        private const float DepthLayerScale = 0.94f;
        private const float Roughness = 0.012f;

        internal static DigTerrainChunkMeshData Build(
            DigTerrainRenderChunk chunk,
            DigTerrainRenderSnapshot snapshot)
        {
            if (chunk == null)
            {
                throw new ArgumentNullException(nameof(chunk));
            }

            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<DigTerrainMaterialKey> keys = new List<DigTerrainMaterialKey>();
            List<List<int>> triangles = new List<List<int>>();
            Dictionary<DigTerrainMaterialKey, int> submeshes =
                new Dictionary<DigTerrainMaterialKey, int>();

            for (int index = 0; index < chunk.Cells.Count; index++)
            {
                DigTerrainRenderCell cell = chunk.Cells[index];
                if (!cell.IsSolid || snapshot.IsCutaway(cell.Key))
                {
                    continue;
                }

                AddCell(
                    cell,
                    snapshot.IsProtected(cell.Key),
                    vertices,
                    normals,
                    keys,
                    triangles,
                    submeshes,
                    snapshot);
            }

            int[][] triangleArrays = new int[triangles.Count][];
            for (int index = 0; index < triangles.Count; index++)
            {
                triangleArrays[index] = triangles[index].ToArray();
            }

            return new DigTerrainChunkMeshData(
                vertices.ToArray(),
                normals.ToArray(),
                triangleArrays,
                keys.ToArray());
        }

        private static DigTerrainMaterialKey ResolveKey(
            DigTerrainRenderCell cell,
            bool isProtected,
            DigTerrainSurfaceRole role)
        {
            DigTerrainSurfaceState state;
            if (!cell.IsExplored)
            {
                state = DigTerrainSurfaceState.Unexplored;
            }
            else if (isProtected)
            {
                state = DigTerrainSurfaceState.Protected;
            }
            else if (cell.IsDesignated)
            {
                state = DigTerrainSurfaceState.Designated;
            }
            else
            {
                state = DigTerrainSurfaceState.Solid;
            }

            byte shade = state == DigTerrainSurfaceState.Solid
                ? (byte)Mathf.Clamp(cell.Hardness / 32, 0, 7)
                : (byte)0;
            return new DigTerrainMaterialKey(cell.MaterialId, state, role, shade);
        }

        private static int GetSubmesh(
            DigTerrainMaterialKey key,
            List<DigTerrainMaterialKey> keys,
            List<List<int>> triangles,
            Dictionary<DigTerrainMaterialKey, int> submeshes)
        {
            if (submeshes.TryGetValue(key, out int existing))
            {
                return existing;
            }

            int index = keys.Count;
            keys.Add(key);
            triangles.Add(new List<int>());
            submeshes.Add(key, index);
            return index;
        }

        private static int GetFaceSubmesh(
            DigTerrainRenderCell cell,
            bool isProtected,
            DigTerrainCellKey neighbour,
            DigTerrainSurfaceRole role,
            DigTerrainRenderSnapshot snapshot,
            List<DigTerrainMaterialKey> keys,
            List<List<int>> triangles,
            Dictionary<DigTerrainMaterialKey, int> submeshes)
        {
            DigTerrainSurfaceRole resolvedRole = snapshot.IsCutaway(neighbour)
                ? DigTerrainSurfaceRole.FreshCut
                : role;
            return GetSubmesh(
                ResolveKey(cell, isProtected, resolvedRole),
                keys,
                triangles,
                submeshes);
        }

        private static void AddCell(
            DigTerrainRenderCell cell,
            bool isProtected,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<DigTerrainMaterialKey> keys,
            List<List<int>> triangles,
            Dictionary<DigTerrainMaterialKey, int> submeshes,
            DigTerrainRenderSnapshot snapshot)
        {
            DigTerrainCellKey position = cell.Key;
            float minX = position.X - HalfExtent;
            float maxX = position.X + HalfExtent;
            ResolveDepthExtents(position.Z, out float minDepth, out float maxDepth);
            float minVertical = position.Y - HalfExtent;
            float maxVertical = position.Y + HalfExtent;

            DigTerrainCellKey front = position.Offset(0, 0, 1);
            if (!snapshot.IsRenderedSolid(front))
            {
                AddFace(
                    position,
                    1,
                    Vector3.down,
                    new Vector3(minX, minDepth, minVertical),
                    new Vector3(maxX, minDepth, minVertical),
                    new Vector3(maxX, minDepth, maxVertical),
                    new Vector3(minX, minDepth, maxVertical),
                    GetFaceSubmesh(
                        cell,
                        isProtected,
                        front,
                        DigTerrainSurfaceRole.Wall,
                        snapshot,
                        keys,
                        triangles,
                        submeshes),
                    vertices,
                    normals,
                    triangles);
            }

            DigTerrainCellKey back = position.Offset(0, 0, -1);
            if (!snapshot.IsRenderedSolid(back))
            {
                AddFace(
                    position,
                    2,
                    Vector3.up,
                    new Vector3(minX, maxDepth, minVertical),
                    new Vector3(minX, maxDepth, maxVertical),
                    new Vector3(maxX, maxDepth, maxVertical),
                    new Vector3(maxX, maxDepth, minVertical),
                    GetFaceSubmesh(
                        cell,
                        isProtected,
                        back,
                        DigTerrainSurfaceRole.Wall,
                        snapshot,
                        keys,
                        triangles,
                        submeshes),
                    vertices,
                    normals,
                    triangles);
            }

            DigTerrainCellKey left = position.Offset(-1, 0, 0);
            if (!snapshot.IsRenderedSolid(left))
            {
                AddFace(
                    position,
                    3,
                    Vector3.left,
                    new Vector3(minX, minDepth, minVertical),
                    new Vector3(minX, minDepth, maxVertical),
                    new Vector3(minX, maxDepth, maxVertical),
                    new Vector3(minX, maxDepth, minVertical),
                    GetFaceSubmesh(
                        cell,
                        isProtected,
                        left,
                        DigTerrainSurfaceRole.Wall,
                        snapshot,
                        keys,
                        triangles,
                        submeshes),
                    vertices,
                    normals,
                    triangles);
            }

            DigTerrainCellKey right = position.Offset(1, 0, 0);
            if (!snapshot.IsRenderedSolid(right))
            {
                AddFace(
                    position,
                    4,
                    Vector3.right,
                    new Vector3(maxX, minDepth, minVertical),
                    new Vector3(maxX, maxDepth, minVertical),
                    new Vector3(maxX, maxDepth, maxVertical),
                    new Vector3(maxX, minDepth, maxVertical),
                    GetFaceSubmesh(
                        cell,
                        isProtected,
                        right,
                        DigTerrainSurfaceRole.Wall,
                        snapshot,
                        keys,
                        triangles,
                        submeshes),
                    vertices,
                    normals,
                    triangles);
            }

            DigTerrainCellKey above = position.Offset(0, -1, 0);
            if (!snapshot.IsRenderedSolid(above))
            {
                AddFace(
                    position,
                    5,
                    Vector3.back,
                    new Vector3(minX, minDepth, minVertical),
                    new Vector3(minX, maxDepth, minVertical),
                    new Vector3(maxX, maxDepth, minVertical),
                    new Vector3(maxX, minDepth, minVertical),
                    GetFaceSubmesh(
                        cell,
                        isProtected,
                        above,
                        DigTerrainSurfaceRole.Floor,
                        snapshot,
                        keys,
                        triangles,
                        submeshes),
                    vertices,
                    normals,
                    triangles);
            }

            DigTerrainCellKey below = position.Offset(0, 1, 0);
            if (!snapshot.IsRenderedSolid(below))
            {
                AddFace(
                    position,
                    6,
                    Vector3.forward,
                    new Vector3(minX, minDepth, maxVertical),
                    new Vector3(maxX, minDepth, maxVertical),
                    new Vector3(maxX, maxDepth, maxVertical),
                    new Vector3(minX, maxDepth, maxVertical),
                    GetFaceSubmesh(
                        cell,
                        isProtected,
                        below,
                        DigTerrainSurfaceRole.Ceiling,
                        snapshot,
                        keys,
                        triangles,
                        submeshes),
                    vertices,
                    normals,
                    triangles);
            }
        }

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
