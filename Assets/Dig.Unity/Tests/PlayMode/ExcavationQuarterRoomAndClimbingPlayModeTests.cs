using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dig.Application.World;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dig.Unity.Tests
{
public sealed class ExcavationQuarterRoomAndClimbingPlayModeTests
{
    private readonly List<UnityEngine.Object> _owned =
        new List<UnityEngine.Object>();

    [TearDown]
    public void TearDown()
    {
        for (int index = _owned.Count - 1; index >= 0; index--)
        {
            if (_owned[index] != null)
            {
                UnityEngine.Object.DestroyImmediate(_owned[index]);
            }
        }

        _owned.Clear();
    }

    [UnityTest]
    public IEnumerator Upper_quarters_form_one_horizontal_world_band_under_rotated_root()
    {
        GameObject root = Own(new GameObject("Side-view root"));
        root.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        GameObject cellObject = Own(GameObject.CreatePrimitive(PrimitiveType.Cube));
        cellObject.transform.SetParent(root.transform, worldPositionStays: false);
        DigCellVisual visual = cellObject.AddComponent<DigCellVisual>();
        visual.Configure(
            Cell(ExcavationQuarter.UpperLeft | ExcavationQuarter.UpperRight),
            Color.gray);
        yield return null;

        Transform upperLeft = RequireChild(visual.transform, "Rock UpperLeft");
        Transform upperRight = RequireChild(visual.transform, "Rock UpperRight");
        Transform lowerLeft = RequireChild(visual.transform, "Rock LowerLeft");
        Transform lowerRight = RequireChild(visual.transform, "Rock LowerRight");

        Assert.That(upperLeft.gameObject.activeSelf, Is.False);
        Assert.That(upperRight.gameObject.activeSelf, Is.False);
        Assert.That(lowerLeft.gameObject.activeSelf, Is.True);
        Assert.That(lowerRight.gameObject.activeSelf, Is.True);
        Assert.That(upperLeft.position.y, Is.EqualTo(upperRight.position.y).Within(0.001f));
        Assert.That(lowerLeft.position.y, Is.EqualTo(lowerRight.position.y).Within(0.001f));
        Assert.That(upperLeft.position.y, Is.GreaterThan(lowerLeft.position.y));
        Assert.That(lowerLeft.position.z, Is.EqualTo(lowerRight.position.z).Within(0.001f));
        Assert.That(Mathf.Abs(lowerLeft.position.x - lowerRight.position.x),
            Is.GreaterThan(0.45f));

        Bounds lowerBounds = lowerLeft.GetComponent<Renderer>().bounds;
        Assert.That(lowerBounds.size.z, Is.GreaterThan(lowerBounds.size.y * 1.7f));
    }

    [Test]
    public void Unsupported_mining_and_partial_support_require_climbing_immediately()
    {
        MethodInfo posture = typeof(DigAgentRenderer).GetMethod(
            "RequiresClimbingWorkPose",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(
                typeof(DigAgentRenderer).FullName,
                "RequiresClimbingWorkPose");
        Assert.That(posture.Invoke(null, new object[] { false, false, true }), Is.True);
        Assert.That(posture.Invoke(null, new object[] { false, true, true }), Is.False);
        Assert.That(posture.Invoke(null, new object[] { true, false, true }), Is.False);
        Assert.That(posture.Invoke(null, new object[] { false, false, false }), Is.False);

        GameObject agentRoot = Own(GameObject.CreatePrimitive(PrimitiveType.Capsule));
        DigAgentVisual agent = agentRoot.AddComponent<DigAgentVisual>();
        Material material = Own(new Material(RequireShader()));
        Invoke(agent, "InitializeSimple", Agent("climber", new CellId(4, 4)), material, material);
        SetField(agent, "_duration", 1f);
        Invoke(agent, "SetWorkTarget", new CellId(5, 4), true, true, false);

        Assert.That(GetField<bool>(agent, "_climbingWorkPose"), Is.True);
        Assert.That(GetField<bool>(agent, "_toolWorkActive"), Is.False);
        Assert.That(agent.transform.forward.z, Is.LessThan(-0.9f));
    }

    [UnityTest]
    public IEnumerator Medium_room_preview_builds_visible_full_size_geometry()
    {
        CaveRoomPlanResult planned = new CaveRoomPlanner().Plan(
            CreateWorld(horizontalTunnelY: 9),
            new ExcavationBoundaryPolicy(20, 14, 2),
            CaveRoomPresetKind.Medium,
            new CellId(10, 9));
        Assert.That(planned.Succeeded, Is.True, planned.Detail);

        GameObject root = Own(new GameObject("Medium room preview fixture"));
        root.AddComponent<DigRenderMaterialLibrary>();
        DigOverlayManager overlays = root.AddComponent<DigOverlayManager>();
        DigCaveRoomPreviewRenderer preview = root.AddComponent<DigCaveRoomPreviewRenderer>();
        Invoke(preview, "Initialize", overlays);
        Invoke(
            preview,
            "Show",
            CaveRoomPresetCatalog.Get(CaveRoomPresetKind.Medium),
            new CellId(10, 9),
            planned);
        yield return null;

        MeshFilter fill = root.GetComponentsInChildren<MeshFilter>(includeInactive: true)
            .Single(value => value.gameObject.name == "Cave room preview fill");
        MeshRenderer renderer = fill.GetComponent<MeshRenderer>();
        Assert.That(renderer.enabled, Is.True);
        Assert.That(fill.sharedMesh, Is.Not.Null);
        Assert.That(fill.sharedMesh.vertexCount, Is.EqualTo(8));
        Assert.That(fill.sharedMesh.bounds.size.x, Is.EqualTo(8f).Within(0.01f));
        Assert.That(fill.sharedMesh.bounds.size.y, Is.EqualTo(3f).Within(0.01f));
    }

    private static WorldCellViewModel Cell(ExcavationQuarter completed)
    {
        return new WorldCellViewModel(
            x: 0,
            y: 0,
            z: 0,
            materialId: "test.rock",
            isSolid: true,
            isExplored: true,
            isDesignated: true,
            hardness: 100,
            damage: 0,
            temperature: 20,
            worldVersion: 1,
            completedExcavationQuarters: completed,
            excavationCutPattern: ExcavationCutPattern.HorizontalRows);
    }

    private static AgentViewModel Agent(string id, CellId cell)
    {
        return new AgentViewModel(
            id: id,
            name: id,
            version: 1,
            isAlive: true,
            cellX: cell.X,
            cellY: cell.Y,
            nutrition: 100,
            alertness: 100,
            mood: 100,
            health: 100,
            scheduledActivity: "Work",
            activeIntent: "Dig",
            actionElapsedTicks: 0,
            actionRequiredTicks: 0,
            decisionReason: string.Empty,
            decisionExplanation: string.Empty,
            utilityOptions: Array.Empty<AgentUtilityOptionViewModel>(),
            cellZ: cell.Z);
    }

    private static WorldSnapshot CreateWorld(int horizontalTunnelY)
    {
        MaterialId rock = new MaterialId("test.rock");
        MaterialId air = new MaterialId("test.air");
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(rock, isSolid: true, hardness: 100),
            new MaterialDefinition(air, isSolid: false, hardness: 0),
        });
        WorldState world = WorldState.CreateFilled(
            new WorldSize(20, 14),
            chunkSize: 5,
            materials,
            rock,
            explored: true).Value;
        CellState empty = new CellState(
            air,
            CellDesignation.None,
            isExplored: true,
            damage: 0,
            temperature: 20);
        List<TerrainChange> changes = Enumerable.Range(1, 18)
            .Select(x => new TerrainChange(
                new CellId(x, horizontalTunnelY),
                empty))
            .ToList();
        world.ApplyTerrainChanges(changes, tick: 1);
        return world.CreateSnapshot();
    }

    private T Own<T>(T value) where T : UnityEngine.Object
    {
        _owned.Add(value);
        return value;
    }

    private static Transform RequireChild(Transform root, string name)
    {
        Transform child = root.Cast<Transform>().Single(value => value.name == name);
        return child;
    }

    private static Shader RequireShader()
    {
        Shader? shader = Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Standard")
            ?? Shader.Find("Sprites/Default");
        Assert.That(shader, Is.Not.Null, "A test shader is required.");
        return shader!;
    }

    private static object Invoke(object target, string name, params object?[] arguments)
    {
        MethodInfo? method = target.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .SingleOrDefault(value => value.Name == name
                && value.GetParameters().Length == arguments.Length);
        Assert.That(method, Is.Not.Null, name);
        return method!.Invoke(target, arguments)!;
    }

    private static T GetField<T>(object target, string name)
    {
        FieldInfo? field = target.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        return (T)field!.GetValue(target)!;
    }

    private static void SetField<T>(object target, string name, T value)
    {
        FieldInfo? field = target.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        field!.SetValue(target, value);
    }
}
}
