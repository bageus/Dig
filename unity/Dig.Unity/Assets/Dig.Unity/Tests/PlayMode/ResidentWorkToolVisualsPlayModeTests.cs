using System.Collections;
using System.Linq;
using Dig.Domain.Inventory;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dig.Unity.Tests
{

public sealed class ResidentWorkToolVisualsPlayModeTests
{
    private GameObject? _root;
    private Material? _material;

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
        {
            Object.DestroyImmediate(_root);
        }

        if (_material != null)
        {
            Object.DestroyImmediate(_material);
        }
    }

    [UnityTest]
    public IEnumerator Right_hand_switches_club_pickaxe_axe_hammer_club_and_empty()
    {
        _root = new GameObject("Resident Right Hand Tool Test");
        DigAgentEquipmentVisual visual =
            _root.AddComponent<DigAgentEquipmentVisual>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Standard");
        Assert.That(shader, Is.Not.Null);
        _material = new Material(shader!);

        yield return ConfigureAndRequire(
            visual,
            "weapon.club",
            "Club Head");
        yield return ConfigureAndRequire(
            visual,
            DigAgentVisual.PickaxeVisualId,
            "Pickaxe Head");
        yield return ConfigureAndRequire(
            visual,
            DigAgentVisual.AxeVisualId,
            "Axe Blade");
        yield return ConfigureAndRequire(
            visual,
            DigAgentVisual.HammerVisualId,
            "Hammer Head");
        yield return ConfigureAndRequire(
            visual,
            "weapon.club",
            "Club Head");

        visual.Clear();
        yield return null;

        Assert.That(visual.CurrentItemId, Is.Null);
        Assert.That(visual.transform.childCount, Is.Zero);
    }

    private IEnumerator ConfigureAndRequire(
        DigAgentEquipmentVisual visual,
        string itemId,
        string expectedPart)
    {
        visual.Configure(
            itemId,
            EquipmentAppearanceKind.Generic,
            _material!);
        yield return null;

        string[] parts = visual.GetComponentsInChildren<Transform>(true)
            .Select(value => value.name)
            .ToArray();
        Assert.That(visual.CurrentItemId, Is.EqualTo(itemId));
        Assert.That(parts, Does.Contain(expectedPart));
        Assert.That(
            visual.GetComponentsInChildren<Collider>(true),
            Is.Empty);
    }
}
}
