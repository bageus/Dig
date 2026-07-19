using System;
using System.Collections.Generic;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    internal static class DigTerrainChunkMeshBuilder
    {
        private const float HalfExtent = 0.47f;
        private const float RockHeight = 0.82f;
        private const float Roughness = 0.012f;

        internal static DigTerrainChunkMeshData Build(
            IReadOnlyList<DigCellVisual> cells,
            ISet<Vector2Int> solidCells,
            ISet<Vector2Int> cutawayCells,
            ISet<Vector2Int> protectedCells)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<DigTerrainMaterialKey> keys = new List<DigTerrainMaterialKey>();
            List<List<int>> triangles = new List<List<int>>();
            Dictionary<DigTerrainMaterialKey, int> submeshes =
                new Dictionary<DigTerrainMaterialKey, int>();

            for (int index = 0; index < cells.Count; index++)
            {
                WorldCellViewModel model = cells[index].Model;
                Vector2Int cell = new Vector2Int(model.X, model.Y);
                if (!model.IsSolid || cutawayCells.Contains(cell))
                {
                    continue;
                }

                DigTerrainMaterialKey key = ResolveKey(
                    model,
                    protectedCells.Contains(cell));
                int submesh = GetSubmesh(key, keys, triangles, submeshes);
                AddCell(
                    model.X,
                    model.Y,
                    submesh,
                    vertices,
                    normals,
                    triangles,
                    solidCells,
                    cutawayCells);
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
            WorldCellViewModel cell,
            bool isProtected)
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
            return new DigTerrainMaterialKey(cell.MaterialId, state, shade);
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

        private static void AddCell(
            int x,
            int y,
            int submesh,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<List<int>> triangles,
            ISet<Vector2Int> solidCells,
            ISet<Vector2Int> cutawayCells)
        {
            float minX = x - HalfExtent;
            float maxX = x + HalfExtent;
            float minY = 0f;
            float maxY = RockHeight;
            float minZ = y - HalfExtent;
            float maxZ = y + HalfExtent;

            AddFace(
                x,
                y,
                1,
                Vector3.down,
                new Vector3(minX, minY, minZ),
                new Vector3(maxX, minY, minZ),
                new Vector3(maxX, minY, maxZ),
                new Vector3(minX, minY, maxZ),
                submesh,
                vertices,
                normals,
                triangles);
            AddFace(
                x,
                y,
                2,
                Vector3.up,
                new Vector3(minX, maxY, minZ),
                new Vector3(minX, maxY, maxZ),
                new Vector3(maxX, maxY, maxZ),
                new Vector3(maxX, maxY, minZ),
                submesh,
                vertices,
                normals,
                triangles);

            if (!IsRenderedSolid(x - 1, y, solidCells, cutawayCells))
            {
                AddFace(
                    x,
                    y,
                    3,
                    Vector3.left,
                    new Vector3(minX, minY, minZ),
                    new Vector3(minX, minY, maxZ),
                    new Vector3(minX, maxY, maxZ),
                    new Vector3(minX, maxY, minZ),
                    submesh,
                    vertices,
                    normals,
                    triangles);
            }

            if (!IsRenderedSolid(x + 1, y, solidCells, cutawayCells))
            {
                AddFace(
                    x,
                    y,
                    4,
                    Vector3.right,
                    new Vector3(maxX, minY, minZ),
                    new Vector3(maxX, maxY, minZ),
                    new Vector3(maxX, maxY, maxZ),
                    new Vector3(maxX, minY, maxZ),
                    submesh,
                    vertices,
                    normals,
                    triangles);
            }

            if (!IsRenderedSolid(x, y - 1, solidCells, cutawayCells))
            {
                AddFace(
                    x,
                    y,
                    5,
                    Vector3.back,
                    new Vector3(minX, minY, minZ),
                    new Vector3(minX, maxY, minZ),
                    new Vector3(maxX, maxY, minZ),
                    new Vector3(maxX, minY, minZ),
                    submesh,
                    vertices,
                    normals,
                    triangles);
            }

            if (!IsRenderedSolid(x, y + 1, solidCells, cutawayCells))
            {
                AddFace(
                    x,
                    y,
                    6,
                    Vector3.forward,
                    new Vector3(minX, minY, maxZ),
                    new Vector3(maxX, minY, maxZ),
                    new Vector3(maxX, maxY, maxZ),
                    new Vector3(minX, maxY, maxZ),
                    submesh,
                    vertices,
                    normals,
                    triangles);
            }
        }

        private static bool IsRenderedSolid(
            int x,
            int y,
            ISet<Vector2Int> solidCells,
            ISet<Vector2Int> cutawayCells)
        {
            Vector2Int cell = new Vector2Int(x, y);
            return solidCells.Contains(cell) && !cutawayCells.Contains(cell);
        }

        private static void AddFace(
            int x,
            int y,
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
            Vector3 offset = normal * ResolveOffset(x, y, salt);
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

        private static float ResolveOffset(int x, int y, int salt)
        {
            unchecked
            {
                uint hash = (uint)(x * 73856093)
                    ^ (uint)(y * 19349663)
                    ^ (uint)(salt * 83492791);
                hash ^= hash >> 13;
                hash *= 1274126177u;
                float normalized = (hash & 1023u) / 1023f;
                return (normalized - 0.5f) * Roughness;
            }
        }
    }
}
