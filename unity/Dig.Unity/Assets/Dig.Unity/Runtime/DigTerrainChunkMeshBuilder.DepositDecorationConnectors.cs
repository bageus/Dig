using System.Collections.Generic;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    internal static partial class DigTerrainChunkMeshBuilder
    {
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
            AddDepositTriangle(a, b, c, normal, submesh, vertices, normals, triangles);
            AddDepositTriangle(a, c, d, normal, submesh, vertices, normals, triangles);
        }
    }
}
