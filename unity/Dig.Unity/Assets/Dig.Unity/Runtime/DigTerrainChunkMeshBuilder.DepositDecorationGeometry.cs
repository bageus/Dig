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
