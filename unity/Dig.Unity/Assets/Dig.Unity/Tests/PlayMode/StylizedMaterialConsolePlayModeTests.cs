using System;
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
public sealed class StylizedMaterialConsolePlayModeTests
{
    [UnityTest]
    public IEnumerator Barrel_highlight_uses_base_color_without_console_errors()
    {
        GameObject root = new GameObject("Stylized material console fixture");
        root.AddComponent<DigRenderMaterialLibrary>();
        GameObject barrelRoot = new GameObject("Barrel");
        barrelRoot.transform.SetParent(root.transform, worldPositionStays: false);
        barrelRoot.AddComponent<BoxCollider>();
        DigBarrelVisual visual = barrelRoot.AddComponent<DigBarrelVisual>();
        BarrelSnapshot snapshot = new BarrelSnapshot(
            EntityId.Parse("fb000000000000000000000000000001"),
            new BarrelDefinitionId("world.barrel.wooden"),
            new CellId(2, 3, 0),
            BarrelLifecycle.Supported,
            new ItemId("material.stone"),
            contentsGeneration: 0,
            contentsMaterialized: false,
            fallSourceCell: null,
            fallLandingCell: null,
            version: 0);

        Invoke(visual, "Configure", snapshot);
        Invoke(visual, "SetHighlighted", true);
        yield return null;

        Renderer[] renderers = barrelRoot.GetComponentsInChildren<Renderer>();
        Assert.That(renderers, Has.Length.EqualTo(3));
        for (int index = 0; index < renderers.Length; index++)
        {
            Material material = renderers[index].sharedMaterial;
            Assert.That(material, Is.Not.Null);
            Assert.That(material.HasProperty("_BaseColor"), Is.True);
        }

        LogAssert.NoUnexpectedReceived();
        UnityEngine.Object.Destroy(root);
        yield return null;
    }

    private static object Invoke(object target, string name, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethod(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(target.GetType().FullName, name);
        return method.Invoke(target, arguments)!;
    }
}
}
