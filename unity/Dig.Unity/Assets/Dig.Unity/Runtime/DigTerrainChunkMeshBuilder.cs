using System;
using System.Collections.Generic;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    internal static partial class DigTerrainChunkMeshBuilder
    {
        private const float HalfExtent = 0.47f;
        private const float FrontRockDepth = 0.82f;
        private const float DepthLayerScale = 0.94f;
        private const float Roughness = 0.012f;

        internal static DigTerrainChunkMeshData Build(
            DigTerrainRenderChunk chunk,
            DigTerrainRenderSnapshot snapshot,
            DigTerrainVisualCatalog? catalog,
            TerrainVisualDetailLevel detailLevel)
        {
            if (chunk == null)
            {
                throw new ArgumentNullException(nameof(chunk));
            }

            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (!Enum.IsDefined(typeof(TerrainVisualDetailLevel), detailLevel))
            {
                throw new ArgumentOutOfRangeException(nameof(detailLevel));
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
                    snapshot,
                    catalog,
                    detailLevel);
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

        private static DigTerrainMaterialKey ResolveTerrainKey(
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
            return new DigTerrainMaterialKey(
                cell.MaterialId,
                state,
                role,
                shade,
                string.Empty,
                TerrainDepositVisualState.Hidden,
                depositDamageBand: 0,
                TerrainDepositConnection.None);
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
                ResolveTerrainKey(cell, isProtected, resolvedRole),
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
            DigTerrainRenderSnapshot snapshot,
            DigTerrainVisualCatalog? catalog,
            TerrainVisualDetailLevel detailLevel)
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

            AddDepositDecorations(
                cell,
                isProtected,
                vertices,
                normals,
                keys,
                triangles,
                submeshes,
                snapshot,
                catalog,
                detailLevel);
        }
    }
}
