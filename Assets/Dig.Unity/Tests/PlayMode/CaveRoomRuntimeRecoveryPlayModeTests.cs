using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dig.Application.World;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.Jobs;
using Dig.Presentation.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dig.Unity.Tests
{
public sealed class CaveRoomRuntimeRecoveryPlayModeTests
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
    public IEnumerator Idle_resident_keeps_climbing_when_support_is_partially_cut()
    {
        CellId current = new CellId(2, 1, 0);
        WorldSnapshot world = CreateUnsupportedWorld(current);
        TunnelNavigationVolume tunnel = new TunnelNavigationVolume(
            width: 5,
            height: 4,
            depth: 1,
            openCells: new[] { current },
            verticalCells: new[] { current },
            supportedCells: Array.Empty<CellId>());
        GameObject root = Own(new GameObject("Unsupported resident fixture"));
        DigAgentRenderer renderer = root.AddComponent<DigAgentRenderer>();
        renderer.Render(new[] { Agent("unsupported", current) }, movementDuration: 0f);
        yield return null;

        Invoke(
            renderer,
            "SynchronizeWorkFacing",
            Array.Empty<JobOverlayViewModel>(),
            tunnel,
            world);
        DigAgentVisual visual = root.GetComponentInChildren<DigAgentVisual>();

        Assert.That(visual, Is.Not.Null);
        Assert.That(GetField<bool>(visual, "_climbingWorkPose"), Is.True);
        Assert.That(GetField<CellId?>(visual, "_workTargetCell"), Is.EqualTo(current));
        Assert.That(GetField<bool>(visual, "_toolWorkActive"), Is.False);
        Assert.That(visual.transform.forward.z, Is.LessThan(-0.9f));
    }

    [UnityTest]
    public IEnumerator Medium_completed_room_renders_at_world_room_height_under_rotated_root()
    {
        CaveRoomPlanResult planned = new CaveRoomPlanner().Plan(
            CreateRoomWorld(horizontalTunnelY: 9),
            new ExcavationBoundaryPolicy(20, 14, 2),
            CaveRoomPresetKind.Medium,
            new CellId(10, 9));
        Assert.That(planned.Succeeded, Is.True, planned.Detail);
        CaveTemplateTrimVolumeViewModel volume = new CaveTemplateTrimPresenter().Present(
            new[] { planned.Plan! });
        GameObject root = Own(new GameObject("Rotated side-view root"));
        root.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        Type rendererType = typeof(DigAgentRenderer).Assembly.GetType(
            "Dig.Unity.DigCaveTemplateTrimRenderer")
            ?? throw new TypeLoadException("Dig.Unity.DigCaveTemplateTrimRenderer");
        Component renderer = root.AddComponent(rendererType);

        Invoke(renderer, "Render", volume, null);
        yield return null;

        Transform trimRoot = root.transform.Cast<Transform>()
            .Single(value => value.name == "Cave Template Trim Visuals");
        MeshFilter mesh = trimRoot.GetComponentsInChildren<MeshFilter>()
            .Single(value => value.sharedMesh != null);
        Renderer meshRenderer = mesh.GetComponent<Renderer>();
        Assert.That(GetProperty<int>(renderer, "InstanceCount"), Is.EqualTo(1));
        Assert.That(mesh.sharedMesh.vertexCount, Is.GreaterThan(0));
        Assert.That(Quaternion.Angle(trimRoot.rotation, Quaternion.identity),
            Is.LessThan(0.01f));
        Assert.That(trimRoot.position, Is.EqualTo(Vector3.zero));
        Assert.That(meshRenderer.bounds.center.y, Is.LessThan(-6f));
        Assert.That(meshRenderer.bounds.center.y, Is.GreaterThan(-11f));
    }

    [UnityTest]
    public IEnumerator Small_middle_row_removes_inner_halves_on_both_boundaries()
    {
        CaveRoomPlanResult planned = new CaveRoomPlanner().Plan(
            CreateRoomWorld(horizontalTunnelY: 9),
            new ExcavationBoundaryPolicy(20, 14, 2),
            CaveRoomPresetKind.Small,
            new CellId(10, 9));
        Assert.That(planned.Succeeded, Is.True, planned.Detail);
        CaveRoomExcavationTarget left = planned.Plan!.ExcavationTargets.Single(
            target => target.Cell == new CellId(8, 8, 0));
        CaveRoomExcavationTarget right = planned.Plan.ExcavationTargets.Single(
            target => target.Cell == new CellId(12, 8, 0));
        Assert.That(left.RequiredQuarters, Is.EqualTo(
            ExcavationQuarter.UpperRight | ExcavationQuarter.LowerRight));
        Assert.That(right.RequiredQuarters, Is.EqualTo(
            ExcavationQuarter.UpperLeft | ExcavationQuarter.LowerLeft));

        GameObject root = Own(new GameObject("Symmetric half-cell fixture"));
        root.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        DigCellVisual leftVisual = CreateCellVisual(root.transform, left);
        DigCellVisual rightVisual = CreateCellVisual(root.transform, right);
        yield return null;

        Assert.That(Child(leftVisual, "Rock UpperLeft").gameObject.activeSelf, Is.True);
        Assert.That(Child(leftVisual, "Rock LowerLeft").gameObject.activeSelf, Is.True);
        Assert.That(Child(leftVisual, "Rock UpperRight").gameObject.activeSelf, Is.False);
        Assert.That(Child(rightVisual, "Rock UpperLeft").gameObject.activeSelf, Is.False);
        Assert.That(Child(rightVisual, "Rock UpperRight").gameObject.activeSelf, Is.True);
        Assert.That(Child(rightVisual, "Rock LowerRight").gameObject.activeSelf, Is.True);
    }

    private DigCellVisual CreateCellVisual(
        Transform parent,
        CaveRoomExcavationTarget target)
    {
        GameObject targetObject = Own(GameObject.CreatePrimitive(PrimitiveType.Cube));
        targetObject.transform.SetParent(parent, worldPositionStays: false);
        DigCellVisual visual = targetObject.AddComponent<DigCellVisual>();
        visual.Configure(Cell(target.Cell, target.RequiredQuarters), Color.gray);
        return visual;
    }

    private static WorldCellViewModel Cell(CellId cell, ExcavationQuarter completed)
    {
        return new WorldCellViewModel(
            cell.X, cell.Y, cell.Z,
            "test.rock", true, true, true,
            100, 0, 20, 1,
            completed,
            ExcavationCutPattern.VerticalColumns);
    }

    private static WorldSnapshot CreateUnsupportedWorld(CellId current)
    {
        WorldState world = CreateFilledWorld(new WorldSize(5, 4));
        Assert.That(world.Excavate(current, Air, tick: 1).IsSuccess, Is.True);
        CellId support = new CellId(current.X, current.Y + 1, current.Z);
        Assert.That(world.SetDigDesignation(support, true, tick: 2).IsSuccess, Is.True);
        Assert.That(world.CommitExcavationQuarter(
            support,
            ExcavationQuarter.UpperLeft,
            ExcavationCutPattern.HorizontalRows,
            Air,
            tick: 3).IsSuccess, Is.True);
        return world.CreateSnapshot();
    }

    private static WorldSnapshot CreateRoomWorld(int horizontalTunnelY)
    {
        WorldState world = CreateFilledWorld(new WorldSize(20, 14));
        CellState empty = new CellState(
            Air, CellDesignation.None, true, 0, 20);
        TerrainChange[] tunnel = Enumerable.Range(1, 18)
            .Select(x => new TerrainChange(new CellId(x, horizontalTunnelY), empty))
            .ToArray();
        Assert.That(world.ApplyTerrainChanges(tunnel, tick: 1).IsSuccess, Is.True);
        return world.CreateSnapshot();
    }

    private static WorldState CreateFilledWorld(WorldSize size)
    {
        return WorldState.CreateFilled(
            size,
            chunkSize: 5,
            new MaterialCatalog(new[]
            {
                new MaterialDefinition(Rock, true, 100),
                new MaterialDefinition(Air, false, 0),
            }),
            Rock,
            explored: true).Value;
    }

    private static AgentViewModel Agent(string id, CellId cell)
    {
        return new AgentViewModel(
            id, id, 1, true,
            cell.X, cell.Y,
            100, 100, 100, 100,
            "Work", "Idle", 0, 0,
            string.Empty, string.Empty,
            Array.Empty<AgentUtilityOptionViewModel>(),
            cell.Z);
    }

    private T Own<T>(T value) where T : UnityEngine.Object
    {
        _owned.Add(value);
        return value;
    }

    private static Transform Child(Component root, string name)
    {
        return root.transform.Cast<Transform>().Single(value => value.name == name);
    }

    private static object Invoke(object target, string name, params object?[] arguments)
    {
        MethodInfo? method = target.GetType().GetMethods(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
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

    private static T GetProperty<T>(object target, string name)
    {
        PropertyInfo? property = target.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, name);
        return (T)property!.GetValue(target)!;
    }

    private static readonly MaterialId Rock = new MaterialId("test.rock");
    private static readonly MaterialId Air = new MaterialId("test.air");
}
}
