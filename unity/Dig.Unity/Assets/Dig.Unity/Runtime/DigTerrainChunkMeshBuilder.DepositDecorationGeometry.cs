using System.Collections.Generic;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    internal static partial class DigTerrainChunkMeshBuilder
    {
        private static void AddDepositCluster(
            Vector3 center,
            Vector3 normal,
            Vector3 tangent,
            Vector3 bitangent,
            TerrainDepositDecorationCellViewModel decoration,
            DigTerrainDepositShape shape,
            TerrainVisualDetailLevel detailLevel,
            int submesh,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<List<int>> triangles)
        {
            float scale = 0.085f + (decoration.ScaleBand * 0.014f);
            float detailScale = detailLevel == TerrainVisualDetailLevel.Marker
                ? 0.78f
                : detailLevel == TerrainVisualDetailLevel.Reduced ? 0.90f : 1f;
            float damageHeight = 1f - (decoration.DamageBand * 0.08f);
            scale *= detailScale;

            switch (shape)
            {
                case DigTerrainDepositShape.Plate:
                    AddPlate(
                        center,
                        normal,
                        tangent,
                        bitangent,
                        scale,
                        decoration,
                        submesh,
                        vertices,
                        normals,
                        triangles);
                    break;
                case DigTerrainDepositShape.Crystal:
                    AddCrystal(
                        center,
                        normal,
                        tangent,
                        bitangent,
                        scale,
                        damageHeight,
                        submesh,
                        vertices,
                        normals,
                        triangles);
                    break;
                case DigTerrainDepositShape.Seam:
                    AddSeam(
                        center,
                        normal,
                        tangent,
                        bitangent,
                        scale,
                        damageHeight,
                        submesh,
                        vertices,
                        normals,
                        triangles);
                    break;
                case DigTerrainDepositShape.Pebble:
                    AddPebbles(
                        center,
                        normal,
                        tangent,
                        bitangent,
                        scale,
                        damageHeight,
                        detailLevel,
                        submesh,
                        vertices,
                        normals,
                        triangles);
                    break;
                default:
                    AddNodule(
                        center,
                        normal,
                        tangent,
                        bitangent,
                        scale,
                        decoration,
                        damageHeight,
                        submesh,
                        vertices,
                        normals,
                        triangles);
                    break;
            }
        }

        private static void AddNodule(
            Vector3 center,
            Vector3 normal,
            Vector3 tangent,
            Vector3 bitangent,
            float scale,
            TerrainDepositDecorationCellViewModel decoration,
            float damageHeight,
            int submesh,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<List<int>> triangles)
        {
            float tangentScale = decoration.Variant == 1 ? 0.76f : 1f;
            float bitangentScale = decoration.Variant == 2 ? 0.74f : 0.9f;
            float heightScale = decoration.Variant == 3 ? 1.28f : 1f;
            AddPyramid(
                center,
                normal,
                tangent,
                bitangent,
                scale * tangentScale,
                scale * bitangentScale,
                0.05f + (scale * 0.42f * heightScale * damageHeight),
                submesh,
                vertices,
                normals,
                triangles);
        }

        private static void AddPlate(
            Vector3 center,
            Vector3 normal,
            Vector3 tangent,
            Vector3 bitangent,
            float scale,
            TerrainDepositDecorationCellViewModel decoration,
            int submesh,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<List<int>> triangles)
        {
            float width = scale * (1.30f + (decoration.Variant * 0.06f));
            float height = scale * 0.52f;
            Vector3 lift = normal * (0.026f + (decoration.ScaleBand * 0.003f));
            AddFlatQuad(
                center + lift,
                normal,
                tangent,
                bitangent,
                width,
                height,
                submesh,
                vertices,
                normals,
                triangles);
        }

        private static void AddCrystal(
            Vector3 center,
            Vector3 normal,
            Vector3 tangent,
            Vector3 bitangent,
            float scale,
            float damageHeight,
            int submesh,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<List<int>> triangles)
        {
            Vector3 a = center - tangent * (scale * 0.72f)
                - bitangent * (scale * 0.48f);
            Vector3 b = center + tangent * (scale * 0.72f)
                - bitangent * (scale * 0.48f);
            Vector3 c = center + bitangent * scale;
            Vector3 tip = center + normal * (0.08f + scale * 0.95f * damageHeight);
            AddDecorationTriangle(a, b, tip, normal, submesh, vertices, normals, triangles);
            AddDecorationTriangle(b, c, tip, normal, submesh, vertices, normals, triangles);
            AddDecorationTriangle(c, a, tip, normal, submesh, vertices, normals, triangles);
        }

        private static void AddSeam(
            Vector3 center,
            Vector3 normal,
            Vector3 tangent,
            Vector3 bitangent,
            float scale,
            float damageHeight,
            int submesh,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<List<int>> triangles)
        {
            float halfLength = scale * 1.55f;
            float halfWidth = scale * 0.34f;
            Vector3 left = center - tangent * halfLength;
            Vector3 right = center + tangent * halfLength;
            Vector3 ridgeLeft = left + normal * (0.035f * damageHeight);
            Vector3 ridgeRight = right + normal * (0.035f * damageHeight);
            Vector3 a = left - bitangent * halfWidth;
            Vector3 b = right - bitangent * halfWidth;
            Vector3 c = right + bitangent * halfWidth;
            Vector3 d = left + bitangent * halfWidth;
            AddDecorationTriangle(a, b, ridgeRight, normal, submesh, vertices, normals, triangles);
            AddDecorationTriangle(a, ridgeRight, ridgeLeft, normal, submesh, vertices, normals, triangles);
            AddDecorationTriangle(ridgeLeft, ridgeRight, c, normal, submesh, vertices, normals, triangles);
            AddDecorationTriangle(ridgeLeft, c, d, normal, submesh, vertices, normals, triangles);
        }

        private static void AddPebbles(
            Vector3 center,
            Vector3 normal,
            Vector3 tangent,
            Vector3 bitangent,
            float scale,
            float damageHeight,
            TerrainVisualDetailLevel detailLevel,
            int submesh,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<List<int>> triangles)
        {
            int count = detailLevel == TerrainVisualDetailLevel.Full ? 3 : 2;
            for (int index = 0; index < count; index++)
            {
                float direction = index == 0 ? -1f : index == 1 ? 1f : 0f;
                Vector3 offset = tangent * (direction * scale * 0.68f)
                    + bitangent * ((index == 2 ? 1f : -0.25f) * scale * 0.42f);
                float pebbleScale = index == 2 ? scale * 0.55f : scale * 0.68f;
                AddPyramid(
                    center + offset,
                    normal,
                    tangent,
                    bitangent,
                    pebbleScale,
                    pebbleScale * 0.82f,
                    0.026f + (pebbleScale * 0.32f * damageHeight),
                    submesh,
                    vertices,
                    normals,
                    triangles);
            }
        }

        private static void AddPyramid(
            Vector3 center,
            Vector3 normal,
            Vector3 tangent,
            Vector3 bitangent,
            float tangentRadius,
            float bitangentRadius,
            float height,
            int submesh,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<List<int>> triangles)
        {
            Vector3 a = center - tangent * tangentRadius - bitangent * bitangentRadius;
            Vector3 b = center + tangent * tangentRadius - bitangent * bitangentRadius;
            Vector3 c = center + tangent * tangentRadius + bitangent * bitangentRadius;
            Vector3 d = center - tangent * tangentRadius + bitangent * bitangentRadius;
            Vector3 tip = center + normal * height;
            AddDecorationTriangle(a, b, tip, normal, submesh, vertices, normals, triangles);
            AddDecorationTriangle(b, c, tip, normal, submesh, vertices, normals, triangles);
            AddDecorationTriangle(c, d, tip, normal, submesh, vertices, normals, triangles);
            AddDecorationTriangle(d, a, tip, normal, submesh, vertices, normals, triangles);
        }

        private static void AddFlatQuad(
            Vector3 center,
            Vector3 normal,
            Vector3 tangent,
            Vector3 bitangent,
            float tangentRadius,
            float bitangentRadius,
            int submesh,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<List<int>> triangles)
        {
            Vector3 a = center - tangent * tangentRadius - bitangent * bitangentRadius;
            Vector3 b = center + tangent * tangentRadius - bitangent * bitangentRadius;
            Vector3 c = center + tangent * tangentRadius + bitangent * bitangentRadius;
            Vector3 d = center - tangent * tangentRadius + bitangent * bitangentRadius;
            AddDecorationTriangle(a, b, c, normal, submesh, vertices, normals, triangles);
            AddDecorationTriangle(a, c, d, normal, submesh, vertices, normals, triangles);
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
            int maximumConnectors,
            int submesh,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<List<int>> triangles)
        {
            int added = 0;
            int start = decoration.RotationQuarterTurns;
            for (int step = 0; step < 4 && added < maximumConnectors; step++)
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
