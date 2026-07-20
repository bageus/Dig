using System.Collections.Generic;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    internal static partial class DigTerrainChunkMeshBuilder
    {
        private const float DepositSurfaceInset = 0.045f;
        private const float DepositConnectorWidth = 0.035f;

        private static void AddDepositDecorations(
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
            if (isProtected || !cell.IsExplored || cell.IsDesignated)
            {
                return;
            }

            if (!snapshot.TryGetDepositDecoration(
                    cell.Key,
                    out TerrainDepositDecorationCellViewModel? decoration)
                || decoration == null)
            {
                return;
            }

            DigTerrainCellKey position = cell.Key;
            float minX = position.X - HalfExtent;
            float maxX = position.X + HalfExtent;
            ResolveDepthExtents(position.Z, out float minDepth, out float maxDepth);
            float minVertical = position.Y - HalfExtent;
            float maxVertical = position.Y + HalfExtent;
            float depthHalf = Mathf.Max(
                DepositSurfaceInset,
                ((maxDepth - minDepth) * 0.5f) - DepositSurfaceInset);
            float planarHalf = HalfExtent - DepositSurfaceInset;

            if (!snapshot.IsRenderedSolid(position.Offset(0, 0, 1)))
            {
                AddDepositFace(
                    cell,
                    decoration,
                    DigTerrainSurfaceRole.Wall,
                    new Vector3(position.X, minDepth, position.Y),
                    Vector3.down,
                    Vector3.right,
                    Vector3.forward,
                    planarHalf,
                    planarHalf,
                    TerrainDepositConnection.NegativeX,
                    TerrainDepositConnection.PositiveX,
                    TerrainDepositConnection.NegativeY,
                    TerrainDepositConnection.PositiveY,
                    vertices,
                    normals,
                    keys,
                    triangles,
                    submeshes,
                    catalog,
                    detailLevel);
            }

            if (!snapshot.IsRenderedSolid(position.Offset(0, 0, -1)))
            {
                AddDepositFace(
                    cell,
                    decoration,
                    DigTerrainSurfaceRole.Wall,
                    new Vector3(position.X, maxDepth, position.Y),
                    Vector3.up,
                    Vector3.right,
                    Vector3.forward,
                    planarHalf,
                    planarHalf,
                    TerrainDepositConnection.NegativeX,
                    TerrainDepositConnection.PositiveX,
                    TerrainDepositConnection.NegativeY,
                    TerrainDepositConnection.PositiveY,
                    vertices,
                    normals,
                    keys,
                    triangles,
                    submeshes,
                    catalog,
                    detailLevel);
            }

            if (!snapshot.IsRenderedSolid(position.Offset(-1, 0, 0)))
            {
                AddDepositFace(
                    cell,
                    decoration,
                    DigTerrainSurfaceRole.Wall,
                    new Vector3(minX, (minDepth + maxDepth) * 0.5f, position.Y),
                    Vector3.left,
                    Vector3.up,
                    Vector3.forward,
                    depthHalf,
                    planarHalf,
                    TerrainDepositConnection.PositiveZ,
                    TerrainDepositConnection.NegativeZ,
                    TerrainDepositConnection.NegativeY,
                    TerrainDepositConnection.PositiveY,
                    vertices,
                    normals,
                    keys,
                    triangles,
                    submeshes,
                    catalog,
                    detailLevel);
            }

            if (!snapshot.IsRenderedSolid(position.Offset(1, 0, 0)))
            {
                AddDepositFace(
                    cell,
                    decoration,
                    DigTerrainSurfaceRole.Wall,
                    new Vector3(maxX, (minDepth + maxDepth) * 0.5f, position.Y),
                    Vector3.right,
                    Vector3.up,
                    Vector3.forward,
                    depthHalf,
                    planarHalf,
                    TerrainDepositConnection.PositiveZ,
                    TerrainDepositConnection.NegativeZ,
                    TerrainDepositConnection.NegativeY,
                    TerrainDepositConnection.PositiveY,
                    vertices,
                    normals,
                    keys,
                    triangles,
                    submeshes,
                    catalog,
                    detailLevel);
            }

            if (!snapshot.IsRenderedSolid(position.Offset(0, -1, 0)))
            {
                AddDepositFace(
                    cell,
                    decoration,
                    DigTerrainSurfaceRole.Floor,
                    new Vector3(position.X, (minDepth + maxDepth) * 0.5f, minVertical),
                    Vector3.back,
                    Vector3.right,
                    Vector3.up,
                    planarHalf,
                    depthHalf,
                    TerrainDepositConnection.NegativeX,
                    TerrainDepositConnection.PositiveX,
                    TerrainDepositConnection.PositiveZ,
                    TerrainDepositConnection.NegativeZ,
                    vertices,
                    normals,
                    keys,
                    triangles,
                    submeshes,
                    catalog,
                    detailLevel);
            }

            if (!snapshot.IsRenderedSolid(position.Offset(0, 1, 0)))
            {
                AddDepositFace(
                    cell,
                    decoration,
                    DigTerrainSurfaceRole.Ceiling,
                    new Vector3(position.X, (minDepth + maxDepth) * 0.5f, maxVertical),
                    Vector3.forward,
                    Vector3.right,
                    Vector3.up,
                    planarHalf,
                    depthHalf,
                    TerrainDepositConnection.NegativeX,
                    TerrainDepositConnection.PositiveX,
                    TerrainDepositConnection.PositiveZ,
                    TerrainDepositConnection.NegativeZ,
                    vertices,
                    normals,
                    keys,
                    triangles,
                    submeshes,
                    catalog,
                    detailLevel);
            }
        }

        private static void AddDepositFace(
            DigTerrainRenderCell cell,
            TerrainDepositDecorationCellViewModel decoration,
            DigTerrainSurfaceRole role,
            Vector3 surfaceCenter,
            Vector3 normal,
            Vector3 tangent,
            Vector3 bitangent,
            float tangentHalf,
            float bitangentHalf,
            TerrainDepositConnection negativeTangent,
            TerrainDepositConnection positiveTangent,
            TerrainDepositConnection negativeBitangent,
            TerrainDepositConnection positiveBitangent,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<DigTerrainMaterialKey> keys,
            List<List<int>> triangles,
            Dictionary<DigTerrainMaterialKey, int> submeshes,
            DigTerrainVisualCatalog? catalog,
            TerrainVisualDetailLevel detailLevel)
        {
            ResolveRotatedAxes(
                tangent,
                bitangent,
                decoration.RotationQuarterTurns,
                out Vector3 shapeTangent,
                out Vector3 shapeBitangent);
            float offset = 0.034f;
            Vector3 center = surfaceCenter
                + normal * 0.018f
                + shapeTangent * (decoration.OffsetBandX * offset)
                + shapeBitangent * (decoration.OffsetBandY * offset);
            int submesh = GetSubmesh(
                ResolveDepositKey(cell, role, decoration),
                keys,
                triangles,
                submeshes);
            DigTerrainDepositShape shape = catalog?.ResolveDepositShape(
                decoration.VisibleDepositId)
                ?? ResolveFallbackDepositShape(decoration.VisibleDepositId);
            AddDepositCluster(
                center,
                normal,
                shapeTangent,
                shapeBitangent,
                decoration,
                shape,
                detailLevel,
                submesh,
                vertices,
                normals,
                triangles);

            int connectorBudget = detailLevel == TerrainVisualDetailLevel.Full
                ? TerrainDepositDecorationCellViewModel.MaximumConnectorsPerFace
                : detailLevel == TerrainVisualDetailLevel.Reduced ? 1 : 0;
            if (connectorBudget > 0)
            {
                AddDepositConnectors(
                    center,
                    normal,
                    tangent,
                    bitangent,
                    tangentHalf,
                    bitangentHalf,
                    negativeTangent,
                    positiveTangent,
                    negativeBitangent,
                    positiveBitangent,
                    decoration,
                    connectorBudget,
                    submesh,
                    vertices,
                    normals,
                    triangles);
            }
        }

        private static DigTerrainMaterialKey ResolveDepositKey(
            DigTerrainRenderCell cell,
            DigTerrainSurfaceRole role,
            TerrainDepositDecorationCellViewModel decoration)
        {
            return new DigTerrainMaterialKey(
                cell.MaterialId,
                DigTerrainSurfaceState.Solid,
                role,
                shade: 0,
                decoration.VisibleDepositId,
                decoration.State,
                decoration.DamageBand,
                decoration.Connections);
        }

        private static DigTerrainDepositShape ResolveFallbackDepositShape(
            string stableId)
        {
            unchecked
            {
                uint hash = 2166136261u;
                for (int index = 0; index < stableId.Length; index++)
                {
                    hash ^= stableId[index];
                    hash *= 16777619u;
                }

                return (DigTerrainDepositShape)(hash % 5u);
            }
        }

        private static void ResolveRotatedAxes(
            Vector3 tangent,
            Vector3 bitangent,
            byte rotation,
            out Vector3 resultTangent,
            out Vector3 resultBitangent)
        {
            switch (rotation)
            {
                case 1:
                    resultTangent = bitangent;
                    resultBitangent = -tangent;
                    break;
                case 2:
                    resultTangent = -tangent;
                    resultBitangent = -bitangent;
                    break;
                case 3:
                    resultTangent = -bitangent;
                    resultBitangent = tangent;
                    break;
                default:
                    resultTangent = tangent;
                    resultBitangent = bitangent;
                    break;
            }
        }
    }
}
