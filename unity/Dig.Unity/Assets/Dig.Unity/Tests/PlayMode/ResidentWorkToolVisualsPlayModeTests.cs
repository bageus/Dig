using System;
using System.Collections;
using System.Linq;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.Inventory;
using Dig.Presentation.Jobs;
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

    [UnityTest]
    public IEnumerator Hover_survives_hand_visual_rebuild_without_destroyed_renderer_access()
    {
        _root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        _root.name = "Hovered Resident Hand Rebuild Test";
        DigAgentVisual agent = _root.AddComponent<DigAgentVisual>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Standard");
        Assert.That(shader, Is.Not.Null);
        _material = new Material(shader!);

        agent.InitializeSimple(CreateAgent("resident.hover"), _material, _material);
        agent.SetEquipment(
            new ResidentEquipmentViewModel(
                "resident.hover",
                "stack.club",
                "weapon.club"),
            _material);
        agent.SetHovered(true);
        agent.SetWorkTarget(
            new CellId(1, 0, 0),
            climbingWork: false,
            workToolVisualKind: ResidentWorkToolVisualKind.Pickaxe,
            animateToolWork: true);

        yield return null;

        DigAgentEquipmentVisual hand =
            _root.GetComponentInChildren<DigAgentEquipmentVisual>(true);
        Assert.That(hand.CurrentItemId, Is.EqualTo(DigAgentVisual.PickaxeVisualId));
        agent.SetHovered(false);

        yield return null;

        LogAssert.NoUnexpectedReceived();
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

    private static AgentViewModel CreateAgent(string id)
    {
        return new AgentViewModel(
            id: id,
            name: id,
            version: 1,
            isAlive: true,
            cellX: 0,
            cellY: 0,
            nutrition: 100,
            alertness: 100,
            mood: 100,
            health: 100,
            scheduledActivity: "Work",
            activeIntent: "Idle",
            actionElapsedTicks: 0,
            actionRequiredTicks: 0,
            decisionReason: string.Empty,
            decisionExplanation: string.Empty,
            utilityOptions: Array.Empty<AgentUtilityOptionViewModel>());
    }
}
}
