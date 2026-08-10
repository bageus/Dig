using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Presentation.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dig.Unity.Tests
{
public sealed class ExcavationContinuationPlayModeTests
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

    [UnityTest]
    public IEnumerator Twelve_opened_cells_keep_geometry_cursor_and_route_in_sync()
    {
        const int cellCount = 12;
        _root = new GameObject("Twelve-cell excavation continuation");
        DigWorldRenderer renderer = _root.AddComponent<DigWorldRenderer>();

        for (int openedCount = 1; openedCount <= cellCount; openedCount++)
        {
            renderer.Render(World(cellCount, openedCount));
            Invoke(renderer, "SetTunnelDigInteractionActive", true);
            yield return null;

            DigCellVisual[] visuals = _root
                .GetComponentsInChildren<DigCellVisual>(true)
                .OrderBy(value => value.Model.X)
                .ToArray();
            Assert.That(visuals, Has.Length.EqualTo(cellCount));
            for (int index = 0; index < visuals.Length; index++)
            {
                DigCellVisual visual = visuals[index];
                Collider collider = visual.GetComponent<Collider>();
                if (index < openedCount)
                {
                    Assert.That(visual.Model.IsExcavationOpen, Is.True);
                    Assert.That(visual.Model.IsDesignated, Is.False);
                    Assert.That(visual.transform.localScale, Is.EqualTo(Vector3.zero));
                    Assert.That(collider.enabled, Is.False);
                }
                else
                {
                    Assert.That(visual.Model.IsExcavationOpen, Is.False);
                    Assert.That(visual.Model.IsDesignated, Is.True);
                    Assert.That(collider.enabled, Is.True);
                }
            }

            CellId[] open = Enumerable.Range(0, openedCount)
                .Select(x => new CellId(x, 0, 0))
                .ToArray();
            TunnelNavigationVolume volume = new TunnelNavigationVolume(
                width: cellCount,
                height: 1,
                depth: 1,
                openCells: open,
                verticalCells: Array.Empty<CellId>(),
                supportedCells: open);
            TunnelPathResult route = volume.FindPath(open[0], open[open.Length - 1]);
            Assert.That(route.Succeeded, Is.True);
            Assert.That(route.Path!.Cells, Has.Count.EqualTo(openedCount));
            Assert.That(route.Path.Cells, Is.EqualTo(open));
        }
    }

    private static WorldViewModel World(int cellCount, int openedCount)
    {
        WorldCellViewModel[] cells = Enumerable.Range(0, cellCount)
            .Select(x => Cell(x, x < openedCount, openedCount))
            .ToArray();
        return new WorldViewModel(
            width: cellCount,
            height: 1,
            depth: WorldSize.RequiredDepth,
            chunkSize: cellCount,
            version: openedCount,
            chunks: new[]
            {
                new WorldChunkViewModel(0, 0, 0, openedCount, cells),
            });
    }

    private static WorldCellViewModel Cell(int x, bool opened, long version)
    {
        return new WorldCellViewModel(
            x: x,
            y: 0,
            z: 0,
            materialId: opened ? "test.air" : "test.rock",
            isSolid: !opened,
            isExplored: true,
            isDesignated: !opened,
            hardness: opened ? 0 : 100,
            damage: 0,
            temperature: 20,
            worldVersion: version,
            completedExcavationQuarters: opened
                ? ExcavationQuarter.All
                : ExcavationQuarter.None,
            excavationCutPattern: opened
                ? ExcavationCutPattern.HorizontalRows
                : ExcavationCutPattern.None);
    }

    private static object Invoke(object target, string name, params object[] arguments)
    {
        MethodInfo? method = target.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .SingleOrDefault(value => value.Name == name
                && value.GetParameters().Length == arguments.Length);
        Assert.That(method, Is.Not.Null, name);
        return method!.Invoke(target, arguments)!;
    }
}
}
