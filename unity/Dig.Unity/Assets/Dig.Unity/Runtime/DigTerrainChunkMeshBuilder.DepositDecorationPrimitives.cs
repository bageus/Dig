using System.Collections.Generic;
using UnityEngine;

namespace Dig.Unity
{
    internal static partial class DigTerrainChunkMeshBuilder
    {
        private static void AddDepositPyramid(
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
            AddDepositTriangle(a, b, tip, normal, submesh, vertices, normals, triangles);
            AddDepositTriangle(b, c, tip, normal, submesh, vertices, normals, triangles);
            AddDepositTriangle(c, d, tip, normal, submesh, vertices, normals, triangles);
            AddDepositTriangle(d, a, tip, normal, submesh, vertices, normals, triangles);
        }

        private static void AddDepositFlatQuad(
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
            AddDepositTriangle(a, b, c, normal, submesh, vertices, normals, triangles);
            AddDepositTriangle(a, c, d, normal, submesh, vertices, normals, triangles);
        }

        private static void AddDepositTriangle(
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
