using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dig.Unity.Tests
{
public sealed class WorldOwnedExcavationPlayModeTests
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
    public IEnumerator Partial_quarters_and_open_cell_use_the_same_world_snapshot()
    {
        GameObject root = Own(new GameObject("World-owned excavation geometry"));
        DigWorldRenderer renderer = root.AddComponent<DigWorldRenderer>();
        renderer.Render(World(
            isSolid: true,
            isDesignated: true,
            ExcavationQuarter.UpperLeft | ExcavationQuarter.UpperRight));
        yield return null;

        DigCellVisual partial = root.GetComponentsInChildren<DigCellVisual>(true).Single();
        Renderer[] quarterRenderers = partial
            .GetComponentsInChildren<Renderer>(true)
            .Where(value => value.gameObject.name.StartsWith("Rock ", StringComparison.Ordinal))
            .ToArray();
        Assert.That(partial.Model.CompletedExcavationQuarters,
            Is.EqualTo(ExcavationQuarter.UpperLeft | ExcavationQuarter.UpperRight));
        Assert.That(partial.GetComponent<Renderer>().enabled, Is.False);
        Assert.That(quarterRenderers, Has.Length.EqualTo(4));
        Assert.That(quarterRenderers.Count(value => value.gameObject.activeSelf), Is.EqualTo(2));
        Assert.That(quarterRenderers.All(value => value.GetComponent<Collider>() == null), Is.True);
        Assert.That(partial.GetComponent<Collider>().enabled, Is.False);

        renderer.Render(World(
            isSolid: false,
            isDesignated: false,
            ExcavationQuarter.All));
        yield return null;

        DigCellVisual opened = root.GetComponentsInChildren<DigCellVisual>(true).Single();
        Assert.That(opened.Model.IsExcavationOpen, Is.True);
        Assert.That(opened.Model.IsDesignated, Is.False);
        Assert.That(opened.transform.localScale, Is.EqualTo(Vector3.zero));
        Assert.That(
            opened.GetComponentsInChildren<Renderer>(true)
                .Where(value => value.gameObject.name.StartsWith("Rock ", StringComparison.Ordinal))
                .Any(value => value.gameObject.activeSelf),
            Is.False);
    }

    [Test]
    public void Shaft_gap_uses_climbing_visual_and_interrupt_cleans_the_pose()
    {
        CellId start = new CellId(0, 0, 0);
        CellId shaft = new CellId(1, 0, 0);
        CellId goal = new CellId(2, 0, 0);
        TunnelNavigationVolume volume = new TunnelNavigationVolume(
            width: 3,
            height: 1,
            depth: 1,
            openCells: new[] { start, shaft, goal },
            verticalCells: new[] { shaft },
            supportedCells: new[] { start, goal });
        DigAgentVisual visual = CreateAgentVisual(start, volume, "gap-climber");

        Invoke(visual, "SetModel", Agent("gap-climber", shaft), 1f);
        Assert.That(GetField<bool>(visual, "_isClimbing"), Is.True);
        Assert.That(
            GetField<TunnelTraversalKind>(visual, "_activeTraversalKind"),
            Is.EqualTo(TunnelTraversalKind.ShaftGapTraverse));

        SetField(visual, "_elapsed", 1f);
        SetField(visual, "_duration", 1f);
        Invoke(visual, "Update");
        Assert.That(GetField<bool>(visual, "_isClimbing"), Is.False);
        Assert.That(
            GetField<TunnelTraversalKind>(visual, "_activeTraversalKind"),
            Is.EqualTo(TunnelTraversalKind.Invalid));

        Invoke(visual, "SetWorkTarget", goal, true, true, false);
        Assert.That(GetField<bool>(visual, "_climbingWorkPose"), Is.True);
        Assert.That(GetField<bool>(visual, "_toolWorkActive"), Is.False);
        Invoke(visual, "SetWorkTarget", null, false, false, false);
        Assert.That(GetField<bool>(visual, "_climbingWorkPose"), Is.False);
    }

    [Test]
    public void Depth_detour_wins_and_opposite_climbers_remain_active()
    {
        CellId start = new CellId(0, 0, 0);
        CellId shaft = new CellId(1, 0, 0);
        CellId goal = new CellId(2, 0, 0);
        CellId[] open =
        {
            start,
            shaft,
            goal,
            new CellId(0, 0, 1),
            new CellId(1, 0, 1),
            new CellId(2, 0, 1),
        };
        TunnelNavigationVolume detour = new TunnelNavigationVolume(
            width: 3,
            height: 1,
            depth: 2,
            openCells: open,
            verticalCells: new[] { shaft },
            supportedCells: open.Where(value => value != shaft).ToArray());

        TunnelPathResult route = detour.FindPath(start, goal);
        Assert.That(route.Succeeded, Is.True);
        Assert.That(
            route.Path!.TraversalKinds.Contains(TunnelTraversalKind.DepthTraverse),
            Is.True);
        Assert.That(
            route.Path.TraversalKinds.Contains(TunnelTraversalKind.ShaftGapTraverse),
            Is.False);

        CellId top = new CellId(0, 0, 0);
        CellId bottom = new CellId(0, 1, 0);
        TunnelNavigationVolume shaftVolume = new TunnelNavigationVolume(
            width: 1,
            height: 2,
            depth: 1,
            openCells: new[] { top, bottom },
            verticalCells: new[] { top, bottom },
            supportedCells: Array.Empty<CellId>());
        DigAgentVisual descending = CreateAgentVisual(top, shaftVolume, "descending");
        DigAgentVisual ascending = CreateAgentVisual(bottom, shaftVolume, "ascending");

        Invoke(descending, "SetModel", Agent("descending", bottom), 1f);
        Invoke(ascending, "SetModel", Agent("ascending", top), 1f);
        Assert.That(GetField<bool>(descending, "_isClimbing"), Is.True);
        Assert.That(GetField<bool>(ascending, "_isClimbing"), Is.True);
        Assert.That(
            GetField<TunnelTraversalKind>(descending, "_activeTraversalKind"),
            Is.EqualTo(TunnelTraversalKind.VerticalClimb));
        Assert.That(
            GetField<TunnelTraversalKind>(ascending, "_activeTraversalKind"),
            Is.EqualTo(TunnelTraversalKind.VerticalClimb));
    }

    private DigAgentVisual CreateAgentVisual(
        CellId cell,
        TunnelNavigationVolume volume,
        string id)
    {
        GameObject root = Own(GameObject.CreatePrimitive(PrimitiveType.Capsule));
        root.name = id;
        DigAgentVisual visual = root.AddComponent<DigAgentVisual>();
        Material material = Own(new Material(RequireShader()));
        Invoke(visual, "InitializeSimple", Agent(id, cell), material, material);
        Invoke(visual, "SetTunnelNavigationVolume", volume);
        return visual;
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

    private static WorldViewModel World(
        bool isSolid,
        bool isDesignated,
        ExcavationQuarter completed)
    {
        WorldCellViewModel cell = new WorldCellViewModel(
            x: 0,
            y: 0,
            z: 0,
            materialId: isSolid ? "test.rock" : "test.air",
            isSolid: isSolid,
            isExplored: true,
            isDesignated: isDesignated,
            hardness: isSolid ? 100 : 0,
            damage: 0,
            temperature: 20,
            worldVersion: completed == ExcavationQuarter.All ? 2 : 1,
            completedExcavationQuarters: completed,
            excavationCutPattern: ExcavationCutPattern.HorizontalRows);
        return new WorldViewModel(
            width: 1,
            height: 1,
            depth: WorldSize.RequiredDepth,
            chunkSize: 1,
            version: cell.WorldVersion,
            chunks: new[]
            {
                new WorldChunkViewModel(0, 0, 0, cell.WorldVersion, new[] { cell }),
            });
    }

    private static Shader RequireShader()
    {
        Shader? shader = Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Sprites/Default");
        Assert.That(shader, Is.Not.Null, "A test shader is required.");
        return shader!;
    }

    private T Own<T>(T value) where T : UnityEngine.Object
    {
        _owned.Add(value);
        return value;
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
