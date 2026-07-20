using System;
using System.Collections.Generic;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    internal static partial class DigCaveTemplateTrimMeshBuilder
    {
        private const float CellHalfExtent = 0.48f;

        internal static DigCaveTemplateTrimMeshData Build(
            CaveTemplateTrimInstanceViewModel instance,
            TerrainVisualDetailLevel detailLevel)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (!Enum.IsDefined(typeof(TerrainVisualDetailLevel), detailLevel))
            {
                throw new ArgumentOutOfRangeException(nameof(detailLevel));
            }

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<CaveTemplateTrimRole> roles = new List<CaveTemplateTrimRole>();
            List<List<int>> triangles = new List<List<int>>();
            Dictionary<CaveTemplateTrimRole, int> submeshes =
                new Dictionary<CaveTemplateTrimRole, int>();

            float thickness = 0.055f + (instance.Variant * 0.008f);
            float frontZ = DigTunnelProjection.DepthOrigin
                - (DigTunnelProjection.DepthSpacing * 0.36f);
            float deepestCenter = DigTunnelProjection.DepthOrigin
                + ((instance.Depth - 1) * DigTunnelProjection.DepthSpacing);
            float backZ = deepestCenter
                + (DigTunnelProjection.DepthSpacing * 0.46f);

            AddOutline(
                instance.Rows,
                frontZ,
                thickness * 1.25f,
                CaveTemplateTrimRole.Entrance,
                vertices,
                normals,
                roles,
                triangles,
                submeshes);
            if (detailLevel != TerrainVisualDetailLevel.Marker)
            {
                for (int index = 0; index < instance.ArchDepths.Count; index++)
                {
                    if (detailLevel == TerrainVisualDetailLevel.Reduced
                        && index % 2 != 0
                        && index != instance.ArchDepths.Count - 1)
                    {
                        continue;
                    }

                    float z = DigTunnelProjection.DepthOrigin
                        + (instance.ArchDepths[index]
                            * DigTunnelProjection.DepthSpacing);
                    AddOutline(
                        instance.Rows,
                        z,
                        thickness,
                        CaveTemplateTrimRole.Arch,
                        vertices,
                        normals,
                        roles,
                        triangles,
                        submeshes);
                }

                AddSideWalls(
                    instance.Rows,
                    frontZ,
                    backZ,
                    vertices,
                    normals,
                    roles,
                    triangles,
                    submeshes);
                if (instance.HasBackWall)
                {
                    AddBackWall(
                        instance.Rows,
                        backZ,
                        vertices,
                        normals,
                        roles,
                        triangles,
                        submeshes);
                }
            }

            int[][] triangleArrays = new int[triangles.Count][];
            for (int index = 0; index < triangles.Count; index++)
            {
                triangleArrays[index] = triangles[index].ToArray();
            }

            return new DigCaveTemplateTrimMeshData(
                vertices.ToArray(),
                normals.ToArray(),
                triangleArrays,
                roles.ToArray());
        }

        private static void AddOutline(
            IReadOnlyList<CaveTemplateTrimRowViewModel> rows,
            float z,
            float thickness,
            CaveTemplateTrimRole role,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<CaveTemplateTrimRole> roles,
            List<List<int>> triangles,
            Dictionary<CaveTemplateTrimRole, int> submeshes)
        {
            int submesh = GetSubmesh(role, roles, triangles, submeshes);
            for (int index = 0; index < rows.Count; index++)
            {
                CaveTemplateTrimRowViewModel row = rows[index];
                ResolveRowBounds(
                    row,
                    out float left,
                    out float right,
                    out float bottom,
                    out float top);
                AddPlaneRibbon(
                    new Vector2(left, bottom),
                    new Vector2(left, top),
                    z,
                    thickness,
                    submesh,
                    vertices,
                    normals,
                    triangles);
                AddPlaneRibbon(
                    new Vector2(right, bottom),
                    new Vector2(right, top),
                    z,
                    thickness,
                    submesh,
                    vertices,
                    normals,
                    triangles);

                if (index + 1 < rows.Count)
                {
                    CaveTemplateTrimRowViewModel next = rows[index + 1];
                    ResolveRowBounds(
                        next,
                        out float nextLeft,
                        out float nextRight,
                        out float nextBottom,
                        out _);
                    AddPlaneRibbon(
                        new Vector2(left, top),
                        new Vector2(nextLeft, nextBottom),
                        z,
                        thickness,
                        submesh,
                        vertices,
                        normals,
                        triangles);
                    AddPlaneRibbon(
                        new Vector2(right, top),
                        new Vector2(nextRight, nextBottom),
                        z,
                        thickness,
                        submesh,
                        vertices,
                        normals,
                        triangles);
                }
            }

            CaveTemplateTrimRowViewModel bottomRow = rows[0];
            ResolveRowBounds(
                bottomRow,
                out float bottomLeft,
                out float bottomRight,
                out float bottomY,
                out _);
            AddPlaneRibbon(
                new Vector2(bottomLeft, bottomY),
                new Vector2(bottomRight, bottomY),
                z,
                thickness,
                submesh,
                vertices,
                normals,
                triangles);

            CaveTemplateTrimRowViewModel topRow = rows[rows.Count - 1];
            ResolveRowBounds(
                topRow,
                out float topLeft,
                out float topRight,
                out _,
                out float topY);
            AddPlaneRibbon(
                new Vector2(topLeft, topY),
                new Vector2(topRight, topY),
                z,
                thickness,
                submesh,
                vertices,
                normals,
                triangles);
        }

        private static void AddSideWalls(
            IReadOnlyList<CaveTemplateTrimRowViewModel> rows,
            float frontZ,
            float backZ,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<CaveTemplateTrimRole> roles,
            List<List<int>> triangles,
            Dictionary<CaveTemplateTrimRole, int> submeshes)
        {
            int submesh = GetSubmesh(
                CaveTemplateTrimRole.SideWall,
                roles,
                triangles,
                submeshes);
            for (int index = 0; index < rows.Count; index++)
            {
                ResolveRowBounds(
                    rows[index],
                    out float left,
                    out float right,
                    out float bottom,
                    out float top);
                AddDoubleSidedQuad(
                    new Vector3(left, bottom, frontZ),
                    new Vector3(left, bottom, backZ),
                    new Vector3(left, top, backZ),
                    new Vector3(left, top, frontZ),
                    Vector3.left,
                    submesh,
                    vertices,
                    normals,
                    triangles);
                AddDoubleSidedQuad(
                    new Vector3(right, bottom, backZ),
                    new Vector3(right, bottom, frontZ),
                    new Vector3(right, top, frontZ),
                    new Vector3(right, top, backZ),
                    Vector3.right,
                    submesh,
                    vertices,
                    normals,
                    triangles);
            }
        }

        private static void AddBackWall(
            IReadOnlyList<CaveTemplateTrimRowViewModel> rows,
            float z,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<CaveTemplateTrimRole> roles,
            List<List<int>> triangles,
            Dictionary<CaveTemplateTrimRole, int> submeshes)
        {
            int submesh = GetSubmesh(
                CaveTemplateTrimRole.BackWall,
                roles,
                triangles,
                submeshes);
            for (int index = 0; index < rows.Count; index++)
            {
                ResolveRowBounds(
                    rows[index],
                    out float left,
                    out float right,
                    out float bottom,
                    out float top);
                AddDoubleSidedQuad(
                    new Vector3(left, bottom, z),
                    new Vector3(right, bottom, z),
                    new Vector3(right, top, z),
                    new Vector3(left, top, z),
                    Vector3.forward,
                    submesh,
                    vertices,
                    normals,
                    triangles);
            }
        }

        private static void ResolveRowBounds(
            CaveTemplateTrimRowViewModel row,
            out float left,
            out float right,
            out float bottom,
            out float top)
        {
            left = row.MinX - CellHalfExtent;
            right = row.MaxX + CellHalfExtent;
            float centerY = -row.Y;
            bottom = centerY - CellHalfExtent;
            top = centerY + CellHalfExtent;
        }

        private static int GetSubmesh(
            CaveTemplateTrimRole role,
            List<CaveTemplateTrimRole> roles,
            List<List<int>> triangles,
            Dictionary<CaveTemplateTrimRole, int> submeshes)
        {
            if (submeshes.TryGetValue(role, out int existing))
            {
                return existing;
            }

            int index = roles.Count;
            roles.Add(role);
            triangles.Add(new List<int>());
            submeshes.Add(role, index);
            return index;
        }
    }
}
