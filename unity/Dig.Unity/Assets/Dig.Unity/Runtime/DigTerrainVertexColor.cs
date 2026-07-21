using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dig.Unity
{
internal static class DigTerrainVertexColor
{
    internal static Color[] Build(
        IReadOnlyList<Vector3> vertices,
        IReadOnlyList<Vector3> normals,
        IReadOnlyList<int[]> triangles,
        IReadOnlyList<DigTerrainMaterialKey> materialKeys)
    {
        if (vertices == null) throw new ArgumentNullException(nameof(vertices));
        if (normals == null) throw new ArgumentNullException(nameof(normals));
        if (triangles == null) throw new ArgumentNullException(nameof(triangles));
        if (materialKeys == null) throw new ArgumentNullException(nameof(materialKeys));
        if (vertices.Count != normals.Count)
            throw new ArgumentException("Terrain vertices and normals must align.");
        if (triangles.Count != materialKeys.Count)
            throw new ArgumentException("Terrain submeshes and material keys must align.");

        Color[] colors = new Color[vertices.Count];
        for (int index = 0; index < colors.Length; index++) colors[index] = Color.white;
        for (int submesh = 0; submesh < triangles.Count; submesh++)
        {
            Color baseColor = ResolveBaseColor(materialKeys[submesh]);
            int[] indices = triangles[submesh];
            for (int index = 0; index < indices.Length; index++)
            {
                int vertex = indices[index];
                colors[vertex] = ApplyAmbientOcclusion(
                    baseColor,
                    vertices[vertex],
                    normals[vertex]);
            }
        }
        return colors;
    }

    private static Color ApplyAmbientOcclusion(
        Color color,
        Vector3 position,
        Vector3 normal)
    {
        float upward = Mathf.Clamp01(Vector3.Dot(normal.normalized, Vector3.forward));
        float downward = Mathf.Clamp01(Vector3.Dot(normal.normalized, Vector3.back));
        float orientation = 0.78f + (upward * 0.16f) - (downward * 0.08f);
        float variation = 0.94f + (StableNoise(position) * 0.06f);
        float factor = Mathf.Clamp(orientation * variation, 0.62f, 1f);
        return new Color(color.r * factor, color.g * factor, color.b * factor, color.a);
    }

    private static float StableNoise(Vector3 position)
    {
        unchecked
        {
            int x = Mathf.RoundToInt(position.x * 1000f);
            int y = Mathf.RoundToInt(position.y * 1000f);
            int z = Mathf.RoundToInt(position.z * 1000f);
            uint hash = (uint)(x * 73856093) ^ (uint)(y * 19349663)
                ^ (uint)(z * 83492791);
            hash ^= hash >> 13;
            hash *= 1274126177u;
            return (hash & 1023u) / 1023f;
        }
    }

    private static Color ResolveBaseColor(DigTerrainMaterialKey key)
    {
        if (key.HasVisibleDeposit) return ResolveDepositColor(key.DepositId);
        switch (key.State)
        {
            case DigTerrainSurfaceState.Unexplored:
                return new Color(0.08f, 0.09f, 0.12f, 1f);
            case DigTerrainSurfaceState.Designated:
                return new Color(0.95f, 0.47f, 0.12f, 1f);
            case DigTerrainSurfaceState.Protected:
                return new Color(0.18f, 0.22f, 0.28f, 1f);
        }
        float hardness = key.Shade / 7f;
        Color result = Color.Lerp(
            new Color(0.48f, 0.36f, 0.25f, 1f),
            new Color(0.32f, 0.36f, 0.42f, 1f),
            hardness);
        if (key.Role == DigTerrainSurfaceRole.Floor)
            result = Color.Lerp(result, Color.white, 0.08f);
        else if (key.Role == DigTerrainSurfaceRole.Ceiling)
            result = Color.Lerp(result, Color.black, 0.12f);
        else if (key.Role == DigTerrainSurfaceRole.FreshCut)
            result = Color.Lerp(result, new Color(0.62f, 0.43f, 0.28f, 1f), 0.22f);
        return result;
    }

    private static Color ResolveDepositColor(string stableId)
    {
        string id = stableId.ToLowerInvariant();
        if (id.Contains("gold")) return new Color(1f, 0.72f, 0.18f, 1f);
        if (id.Contains("crystal")) return new Color(0.30f, 0.82f, 1f, 1f);
        if (id.Contains("coal")) return new Color(0.12f, 0.14f, 0.18f, 1f);
        if (id.Contains("iron")) return new Color(0.68f, 0.34f, 0.20f, 1f);
        return new Color(0.58f, 0.62f, 0.68f, 1f);
    }
}
}
