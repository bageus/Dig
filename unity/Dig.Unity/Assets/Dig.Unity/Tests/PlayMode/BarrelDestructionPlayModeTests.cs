using System.Collections;
using System.Reflection;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Domain.WorldObjects;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dig.Unity.PlayModeTests
{

public sealed class BarrelDestructionPlayModeTests
{
    [UnityTest]
    public IEnumerator Four_supported_barrels_render_shorter_than_resident_and_destroyed_one_disappears()
    {
        GameObject root = new GameObject("Barrel Play Mode fixture");
        root.AddComponent<DigRenderMaterialLibrary>();
        DigBarrelRenderer renderer = root.AddComponent<DigBarrelRenderer>();
        BarrelDefinitionId definitionId = new BarrelDefinitionId("world.barrel.wooden");
        ItemId stone = new ItemId("material.stone");
        BarrelSnapshot[] barrels =
        {
            Snapshot("f1000000000000000000000000000001", definitionId, stone, new CellId(2, 3, 0)),
            Snapshot("f1000000000000000000000000000002", definitionId, stone, new CellId(5, 3, 0)),
            Snapshot("f1000000000000000000000000000003", definitionId, stone, new CellId(2, 8, 2)),
            Snapshot("f1000000000000000000000000000004", definitionId, stone, new CellId(5, 8, 2)),
        };

        InvokeRender(renderer, barrels);
        yield return null;

        DigBarrelVisual[] visuals = Object.FindObjectsOfType<DigBarrelVisual>();
        Assert.That(visuals, Has.Length.EqualTo(4));
        foreach (DigBarrelVisual visual in visuals)
        {
            BoxCollider collider = visual.GetComponent<BoxCollider>();
            Assert.That(collider.enabled, Is.True);
            Assert.That(collider.size.y, Is.LessThan(1.2f));
        }

        InvokeRender(renderer, new[] { barrels[1], barrels[2], barrels[3] });
        yield return null;
        Assert.That(Object.FindObjectsOfType<DigBarrelVisual>(), Has.Length.EqualTo(3));

        Object.Destroy(root);
        yield return null;
    }

    private static BarrelSnapshot Snapshot(
        string id,
        BarrelDefinitionId definitionId,
        ItemId contents,
        CellId cell)
    {
        return new BarrelSnapshot(
            EntityId.Parse(id),
            definitionId,
            cell,
            BarrelLifecycle.Supported,
            contents,
            contentsGeneration: 0,
            contentsMaterialized: false,
            fallSourceCell: null,
            fallLandingCell: null,
            version: 0);
    }

    private static void InvokeRender(DigBarrelRenderer renderer, BarrelSnapshot[] barrels)
    {
        MethodInfo method = typeof(DigBarrelRenderer).GetMethod(
            "Render",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(typeof(DigBarrelRenderer).FullName, "Render");
        method.Invoke(renderer, new object[] { barrels });
    }
}

}