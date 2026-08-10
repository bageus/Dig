using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dig.Domain.World;
using Dig.Presentation.World;
using NUnit.Framework;
using UnityEngine;

namespace Dig.Unity.Tests
{
public sealed class PartialExcavationGeometryPlayModeTests
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
    public void Completed_quarter_removes_combined_rock_and_survives_erase()
    {
        _root = new GameObject("Partial excavation geometry test");
        DigWorldRenderer renderer = _root.AddComponent<DigWorldRenderer>();
        renderer.Render(World(isSolid: true, designated: true, version: 1));
        int fullVertices = TerrainVertexCount(renderer);
        Assert.That(fullVertices, Is.GreaterThan(0));

        CellId cell = new CellId(0, 0, 0);
        ExcavationQuarterProgressSnapshot partial =
            new ExcavationQuarterProgressSnapshot(
                new ExcavationWorkTarget(cell, cell.Z),
                ExcavationQuarter.UpperLeft);
        Synchronize(renderer, new[] { partial });

        DigCellVisual visual = _root.GetComponentsInChildren<DigCellVisual>(true)
            .Single(value => value.Model.X == 0
                && value.Model.Y == 0
                && value.Model.Z == 0);
        Assert.That(TerrainVertexCount(renderer), Is.EqualTo(0));
        Assert.That(Quarter(visual, "UpperLeft").activeSelf, Is.False);
        Assert.That(Quarter(visual, "LowerLeft").activeSelf, Is.True);
        Assert.That(Quarter(visual, "UpperRight").activeSelf, Is.True);
        Assert.That(Quarter(visual, "LowerRight").activeSelf, Is.True);

        renderer.Render(World(isSolid: true, designated: false, version: 2));
        Synchronize(renderer, new[] { partial });

        Assert.That(TerrainVertexCount(renderer), Is.EqualTo(0));
        Assert.That(Quarter(visual, "UpperLeft").activeSelf, Is.False);
        Assert.That(Quarter(visual, "LowerLeft").activeSelf, Is.True);
    }

    private static GameObject Quarter(DigCellVisual visual, string name)
    {
        Transform? child = visual.transform.Find($"Rock {name}");
        Assert.That(child, Is.Not.Null, name);
        return child!.gameObject;
    }

    private static void Synchronize(
        DigWorldRenderer renderer,
        IReadOnlyList<ExcavationQuarterProgressSnapshot> progress)
    {
        MethodInfo? method = typeof(DigWorldRenderer).GetMethod(
            "SynchronizeExcavationQuarterProgress",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method!.Invoke(renderer, new object[] { progress });
    }

    private static int TerrainVertexCount(DigWorldRenderer renderer)
    {
        Type? type = typeof(DigWorldRenderer).Assembly.GetType(
            "Dig.Unity.DigTerrainChunkRenderer");
        Assert.That(type, Is.Not.Null);
        Component? component = renderer.GetComponent(type!);
        Assert.That(component, Is.Not.Null);
        PropertyInfo? property = type!.GetProperty(
            "VertexCount",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null);
        return (int)property!.GetValue(component)!;
    }

    private static WorldViewModel World(
        bool isSolid,
        bool designated,
        long version)
    {
        WorldCellViewModel cell = new WorldCellViewModel(
            0,
            0,
            0,
            isSolid ? "test.rock" : "material.empty",
            isSolid,
            isExplored: true,
            isDesignated: designated,
            hardness: isSolid ? 100 : 0,
            damage: 0,
            temperature: 20,
            worldVersion: version);
        return new WorldViewModel(
            1,
            1,
            WorldSize.RequiredDepth,
            1,
            version,
            new[]
            {
                new WorldChunkViewModel(0, 0, 0, version, new[] { cell }),
            });
    }
}
}
