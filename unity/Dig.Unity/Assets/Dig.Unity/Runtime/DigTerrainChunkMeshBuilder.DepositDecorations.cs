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
            DigTerrainRenderSnapshot snapshot)
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
                    submeshes);
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
                    submeshes);
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
                    submeshes);
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
                    submeshes);
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
                    submeshes);
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
                    submeshes);
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
            Dictionary<DigTerrainMaterialKey, int> submeshes)
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
            AddDepositCluster(
                center,
                normal,
                shapeTangent,
                shapeBitangent,
                decoration,
                submesh,
                vertices,
                normals,
                triangles);
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
                submesh,
                vertices,
                normals,
                triangles);
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

        private static void AddDepositCluster(
            Vector3 center,
            Vector3 normal,
            Vector3 tangent,
            Vector3 bitangent,
            TerrainDepositDecorationCellViewModel decoration,
            int submesh,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<List<int>> triangles)
        {
            float scale = 0.085f + (decoration.ScaleBand * 0.014f);
            float tangentScale = decoration.Variant == 1 ? 0.76f : 1f;
            float bitangentScale = decoration.Variant == 2 ? 0.74f : 0.9f;
            float heightScale = decoration.Variant == 3 ? 1.28f : 1f;
            Vector3 a = center - tangent * (scale * tangentScale)
                - bitangent * (scale * bitangentScale);
            Vector3 b = center + tangent * (scale * tangentScale)
                - bitangent * (scale * bitangentScale);
            Vector3 c = center + tangent * (scale * tangentScale)
                + bitangent * (scale * bitangentScale);
            Vector3 d = center - tangent * (scale * tangentScale)
                + bitangent * (scale * bitangentScale);
            Vector3 tip = center + normal * (0.05f + scale * 0.42f * heightScale);
            AddDecorationTriangle(a, b, tip, normal, submesh, vertices, normals, triangles);
            AddDecorationTriangle(b, c, tip, normal, submesh, vertices, normals, triangles);
            AddDecorationTriangle(c, d, tip, normal, submesh, vertices, normals, triangles);
            AddDecorationTriangle(d, a, tip, normal, submesh, vertices, normals, triangles);
        }

        private static void AddDepositConnectors(
            Vector3 center,
            Vector3 normal,
            Vector3 tangent,
            Vector3 bitangent,
            float tangentHalf,
            float bitangentHalf,
            TerrainDepositConnection negativeTangent,
            TerrainDepositConnection positiveTangent,
            TerrainDepositConnection negativeBitangent,
            TerrainDepositConnection positiveBitangent,
            TerrainDepositDecorationCellViewModel decoration,
            int submesh,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<List<int>> triangles)
        {
            int added = 0;
            int start = decoration.RotationQuarterTurns;
            for (int step = 0;
                step < 4
                    && added
                        < TerrainDepositDecorationCellViewModel.MaximumConnectorsPerFace;
                step++)
            {
                int direction = (start + step) & 3;
                TerrainDepositConnection flag;
                Vector3 axis;
                Vector3 perpendicular;
                float extent;
                switch (direction)
                {
                    case 0:
                        flag = positiveTangent;
                        axis = tangent;
                        perpendicular = bitangent;
                        extent = tangentHalf;
                        break;
                    case 1:
                        flag = positiveBitangent;
                        axis = bitangent;
                        perpendicular = tangent;
                        extent = bitangentHalf;
                        break;
                    case 2:
                        flag = negativeTangent;
                        axis = -tangent;
                        perpendicular = bitangent;
                        extent = tangentHalf;
                        break;
                    default:
                        flag = negativeBitangent;
                        axis = -bitangent;
                        perpendicular = tangent;
                        extent = bitangentHalf;
                        break;
                }

                if ((decoration.Connections & flag) == 0)
                {
                    continue;
                }

                AddDepositConnector(
                    center,
                    normal,
                    axis,
                    perpendicular,
                    extent,
                    submesh,
                    vertices,
                    normals,
                    triangles);
                added++;
            }
        }

        private static void AddDepositConnector(
            Vector3 center,
            Vector3 normal,
            Vector3 axis,
            Vector3 perpendicular,
            float extent,
            int submesh,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<List<int>> triangles)
        {
            float start = 0.07f;
            float end = Mathf.Max(start + 0.02f, extent);
            Vector3 lift = normal * 0.008f;
            Vector3 width = perpendicular * DepositConnectorWidth;
            Vector3 a = center + axis * start - width + lift;
            Vector3 b = center + axis * end - width + lift;
            Vector3 c = center + axis * end + width + lift;
            Vector3 d = center + axis * start + width + lift;
            AddDecorationTriangle(a, b, c, normal, submesh, vertices, normals, triangles);
            AddDecorationTriangle(a, c, d, normal, submesh, vertices, normals, triangles);
        }

        private static void AddDecorationTriangle(
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector3 expectedNormal,
            int submesh,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<List<int>> triangles)
        {
            Vector3 faceNormal = Vector3.Cross(b - a, c - a).normalized;
            if (Vector3.Dot(faceNormal, expectedNormal) < 0f)
            {
                Vector3 swap = b;
                b = c;
                c = swap;
                faceNormal = -faceNormal;
            }

            int start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            normals.Add(faceNormal);
            normals.Add(faceNormal);
            normals.Add(faceNormal);
            triangles[submesh].Add(start);
            triangles[submesh].Add(start + 1);
            triangles[submesh].Add(start + 2);
        }
    }
}
