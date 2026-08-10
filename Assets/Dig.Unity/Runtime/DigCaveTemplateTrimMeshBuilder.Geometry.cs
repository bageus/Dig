using System.Collections.Generic;
using UnityEngine;

namespace Dig.Unity
{
    internal static partial class DigCaveTemplateTrimMeshBuilder
    {
        private static void AddPlaneRibbon(
            Vector2 start,
            Vector2 end,
            float z,
            float thickness,
            int submesh,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<List<int>> triangles)
        {
            Vector2 direction = end - start;
            if (direction.sqrMagnitude <= 0.000001f)
            {
                return;
            }

            Vector2 perpendicular = new Vector2(-direction.y, direction.x)
                .normalized
                * (thickness * 0.5f);
            AddDoubleSidedQuad(
                new Vector3(start.x - perpendicular.x, start.y - perpendicular.y, z),
                new Vector3(end.x - perpendicular.x, end.y - perpendicular.y, z),
                new Vector3(end.x + perpendicular.x, end.y + perpendicular.y, z),
                new Vector3(start.x + perpendicular.x, start.y + perpendicular.y, z),
                Vector3.forward,
                submesh,
                vertices,
                normals,
                triangles);
        }

        private static void AddDoubleSidedQuad(
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector3 d,
            Vector3 normal,
            int submesh,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<List<int>> triangles)
        {
            AddQuad(
                a,
                b,
                c,
                d,
                normal,
                submesh,
                vertices,
                normals,
                triangles);
            AddQuad(
                d,
                c,
                b,
                a,
                -normal,
                submesh,
                vertices,
                normals,
                triangles);
        }

        private static void AddQuad(
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector3 d,
            Vector3 normal,
            int submesh,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<List<int>> triangles)
        {
            int start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(d);
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
    }
}
