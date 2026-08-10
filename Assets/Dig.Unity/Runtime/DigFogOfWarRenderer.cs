using System;
using System.Collections.Generic;
using Dig.Domain.Exploration;
using Dig.Domain.World;
using Dig.Presentation.World;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{
[DisallowMultipleComponent]
public sealed class DigFogOfWarRenderer : MonoBehaviour
{
    private Mesh? _mesh;
    private Material? _material;
    private Mesh? _memoryMesh;

    public void Render(WorldViewModel world)
    {
        if (world == null) throw new ArgumentNullException(nameof(world));
        EnsureView();
        List<Vector3> vertices = new List<Vector3>();
        List<Color> colors = new List<Color>();
        List<int> triangles = new List<int>();
        foreach (WorldChunkViewModel chunk in world.Chunks)
        foreach (WorldCellViewModel cell in chunk.Cells)
        {
            if (cell.Visibility == CellVisibility.Visible) continue;
            Color color = cell.Visibility == CellVisibility.Unexplored
                ? new Color(0.01f, 0.012f, 0.018f, 1f)
                : new Color(0.025f, 0.03f, 0.045f, 0.58f);
            AddQuad(new CellId(cell.X, cell.Y, cell.Z), color, vertices, colors, triangles);
        }
        _mesh!.Clear();
        _mesh.SetVertices(vertices); _mesh.SetColors(colors); _mesh.SetTriangles(triangles, 0);
        _mesh.RecalculateBounds();
    }

    public void RenderItemMemory(IReadOnlyList<WorldItemMemoryViewModel> markers)
    {
        if (markers == null) throw new ArgumentNullException(nameof(markers));
        EnsureView();
        List<Vector3> vertices = new List<Vector3>();
        List<Color> colors = new List<Color>();
        List<int> triangles = new List<int>();
        foreach (WorldItemMemoryViewModel marker in markers)
            AddMemoryMarker(marker, vertices, colors, triangles);
        _memoryMesh!.Clear(); _memoryMesh.SetVertices(vertices);
        _memoryMesh.SetColors(colors); _memoryMesh.SetTriangles(triangles, 0);
        _memoryMesh.RecalculateBounds();
    }

    private void EnsureView()
    {
        if (_mesh != null) return;
        GameObject root = new GameObject("Fog of War");
        root.transform.SetParent(transform, false);
        MeshFilter filter = root.AddComponent<MeshFilter>();
        MeshRenderer renderer = root.AddComponent<MeshRenderer>();
        _mesh = new Mesh { name = "Fog of War Cells" };
        _mesh.MarkDynamic(); filter.sharedMesh = _mesh;
        Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent");
        _material = new Material(shader) { name = "Fog of War Material" };
        renderer.sharedMaterial = _material;
        renderer.sortingOrder = 900;
        GameObject memory = new GameObject("Remembered World Items");
        memory.transform.SetParent(root.transform, false);
        MeshFilter memoryFilter = memory.AddComponent<MeshFilter>();
        MeshRenderer memoryRenderer = memory.AddComponent<MeshRenderer>();
        _memoryMesh = new Mesh { name = "Remembered World Item Markers" };
        _memoryMesh.MarkDynamic(); memoryFilter.sharedMesh = _memoryMesh;
        memoryRenderer.sharedMaterial = _material; memoryRenderer.sortingOrder = 901;
    }

    private static void AddMemoryMarker(
        WorldItemMemoryViewModel marker, List<Vector3> vertices,
        List<Color> colors, List<int> triangles)
    {
        Vector3 center = DigTunnelProjection.CellWorldPosition(
            new CellId(marker.CellX, marker.CellY, marker.CellZ));
        center.z += 0.49f;
        int start = vertices.Count;
        float radius = 0.18f;
        vertices.Add(center + new Vector3(0, radius, 0));
        vertices.Add(center + new Vector3(radius, 0, 0));
        vertices.Add(center + new Vector3(0, -radius, 0));
        vertices.Add(center + new Vector3(-radius, 0, 0));
        Color color = new Color(0.9f, 0.72f, 0.25f, 0.72f);
        for (int index = 0; index < 4; index++) colors.Add(color);
        triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
        triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
    }

    private static void AddQuad(
        CellId cell, Color color, List<Vector3> vertices, List<Color> colors, List<int> triangles)
    {
        Vector3 center = DigTunnelProjection.CellWorldPosition(cell);
        center.z += 0.48f;
        int start = vertices.Count;
        vertices.Add(center + new Vector3(-0.505f, -0.505f, 0));
        vertices.Add(center + new Vector3(0.505f, -0.505f, 0));
        vertices.Add(center + new Vector3(0.505f, 0.505f, 0));
        vertices.Add(center + new Vector3(-0.505f, 0.505f, 0));
        for (int index = 0; index < 4; index++) colors.Add(color);
        triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 1);
        triangles.Add(start); triangles.Add(start + 3); triangles.Add(start + 2);
    }
}
}
