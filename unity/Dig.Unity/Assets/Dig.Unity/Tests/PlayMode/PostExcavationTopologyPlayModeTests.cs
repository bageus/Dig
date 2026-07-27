using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Presentation.World;
using NUnit.Framework;
using UnityEngine;

namespace Dig.Unity.Tests
{
public sealed class PostExcavationTopologyPlayModeTests
{
    private GameObject? _root;

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
        {
            UnityEngine.Object.DestroyImmediate(_root);
        }
    }

    [Test]
    public void Excavated_frontier_cell_enters_topology_and_movement_surfaces()
    {
        Assembly runtime = typeof(DigWorldRenderer).Assembly;
        Type worldType = RequireType(runtime, "Dig.Unity.DigWorldSession");
        object world = InvokeStatic(worldType, "CreateDemo", 8, 8, 4);
        TunnelNavigationVolume before =
            (TunnelNavigationVolume)Invoke(world, "CreateTunnelNavigationVolume");
        WorldViewModel view = (WorldViewModel)Invoke(world, "LoadView");
        HashSet<CellId> open = new HashSet<CellId>(before.Cells);
        CellId target = view.Chunks
            .SelectMany(chunk => chunk.Cells)
            .Where(cell => cell.IsSolid && cell.Z == 0)
            .Select(cell => new CellId(cell.X, cell.Y, cell.Z))
            .First(cell => HorizontalNeighbours(cell).Any(open.Contains));

        _root = new GameObject("Post excavation topology test");
        DigTunnelDemoRenderer renderer = _root.AddComponent<DigTunnelDemoRenderer>();
        Invoke(renderer, "Initialize", before);
        Assert.That(RenderedCells(renderer).Contains(target), Is.False);

        object first = Invoke(world, "ExcavateSpatialCell", target);
        Assert.That((bool)GetProperty(first, "IsSuccess"), Is.True);
        TunnelNavigationVolume after =
            (TunnelNavigationVolume)Invoke(world, "CreateTunnelNavigationVolume");
        Assert.That(after.IsOpen(target), Is.True);

        Invoke(renderer, "Initialize", after);
        Assert.That(RenderedCells(renderer).Contains(target), Is.True);
        Assert.That(MovementSurfaceCells(renderer).Contains(target), Is.True);

        object retry = Invoke(world, "ExcavateSpatialCell", target);
        Assert.That((bool)GetProperty(retry, "IsSuccess"), Is.True);
    }

    [Test]
    public void First_vertical_shaft_cell_accepts_horizontal_entry_transition()
    {
        CellId entry = new CellId(2, 2, 0);
        CellId shaft = new CellId(2, 3, 0);
        TunnelNavigationVolume volume = new TunnelNavigationVolume(
            width: 6,
            height: 6,
            depth: 4,
            openCells: new[] { entry, shaft },
            verticalCells: new[] { shaft });

        Assert.That(volume.CanTraverseStep(entry, shaft), Is.True);
        Assert.That(volume.FindPath(entry, shaft).Succeeded, Is.True);
    }

    [Test]
    public void Downward_excavation_selects_target_side_nearest_worker()
    {
        ExcavationApproachSide side = ExcavationApproachResolver.Resolve(
            new CellId(2, 2, 0),
            new CellId(2, 3, 0));

        Assert.That(side, Is.EqualTo(ExcavationApproachSide.Above));
        Assert.That(
            ExcavationQuarterPlanner.CandidatesFor(side),
            Is.EqualTo(ExcavationQuarter.UpperLeft | ExcavationQuarter.UpperRight));
    }

    private static IEnumerable<CellId> HorizontalNeighbours(CellId cell)
    {
        yield return new CellId(cell.X - 1, cell.Y, cell.Z);
        yield return new CellId(cell.X + 1, cell.Y, cell.Z);
        yield return new CellId(cell.X, cell.Y - 1, cell.Z);
        yield return new CellId(cell.X, cell.Y + 1, cell.Z);
    }

    private static IDictionary RenderedCells(DigTunnelDemoRenderer renderer)
    {
        FieldInfo? field = typeof(DigTunnelDemoRenderer).GetField(
            "_cells",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (IDictionary)field!.GetValue(renderer)!;
    }

    private static HashSet<CellId> MovementSurfaceCells(
        DigTunnelDemoRenderer renderer)
    {
        FieldInfo? field = typeof(DigTunnelMovementSurface).GetField(
            "_cells",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        HashSet<CellId> cells = new HashSet<CellId>();
        DigTunnelMovementSurface[] surfaces =
            renderer.GetComponentsInChildren<DigTunnelMovementSurface>(true);
        for (int index = 0; index < surfaces.Length; index++)
        {
            IEnumerable values = (IEnumerable)field!.GetValue(surfaces[index])!;
            foreach (object value in values)
            {
                cells.Add((CellId)value);
            }
        }

        return cells;
    }

    private static Type RequireType(Assembly assembly, string name)
    {
        Type? type = assembly.GetType(name);
        Assert.That(type, Is.Not.Null, name);
        return type!;
    }

    private static object InvokeStatic(
        Type type,
        string name,
        params object[] arguments)
    {
        MethodInfo method = RequireMethod(
            type,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            name,
            arguments.Length);
        return method.Invoke(null, arguments)!;
    }

    private static object Invoke(
        object target,
        string name,
        params object[] arguments)
    {
        MethodInfo method = RequireMethod(
            target.GetType(),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            name,
            arguments.Length);
        return method.Invoke(target, arguments)!;
    }

    private static MethodInfo RequireMethod(
        Type type,
        BindingFlags flags,
        string name,
        int argumentCount)
    {
        MethodInfo? method = type.GetMethods(flags)
            .SingleOrDefault(value => value.Name == name
                && value.GetParameters().Length == argumentCount);
        Assert.That(method, Is.Not.Null, name);
        return method!;
    }

    private static object GetProperty(object target, string name)
    {
        PropertyInfo? property = target.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, name);
        return property!.GetValue(target)!;
    }
}
}
