using System;
using System.Collections;
using System.Linq;
using Dig.Domain.Content;
using Dig.Domain.Inventory;
using Dig.Presentation.Agents;
using Dig.Presentation.Inventory;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dig.Unity.Tests
{
public sealed class BasketInventoryLifecyclePlayModeTests
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
    public IEnumerator Loaded_cargo_uses_back_basket_and_empty_projection_hides_it()
    {
        _root = new GameObject("Basket Inventory Lifecycle Test");
        DigAgentRenderer renderer = _root.AddComponent<DigAgentRenderer>();
        renderer.Render(new[] { Resident() }, movementDuration: 0.1f);
        yield return null;

        ResidentInventoryAttachmentViewModel attachment =
            new ResidentInventoryAttachmentViewModel(
                "resident.basket-test",
                "20000000000000000000000000000001",
                ResidentInventoryExpansionContent.BasketItemId.ToString(),
                InventoryExpansionGroup.Cargo,
                tier: 1,
                visualAttachmentId: "visual.resident.basket");
        renderer.RenderInventoryAttachments(new[] { attachment });
        yield return null;

        DigResidentInventoryAttachmentVisual visual = _root
            .GetComponentsInChildren<DigResidentInventoryAttachmentVisual>(true)
            .Single();
        Assert.That(visual.gameObject.activeSelf, Is.True);
        Assert.That(visual.transform.localPosition.z, Is.LessThan(0f));
        string[] parts = visual.GetComponentsInChildren<Transform>(true)
            .Select(value => value.name)
            .ToArray();
        Assert.That(parts, Does.Contain("Basket Bottom"));
        Assert.That(parts, Does.Contain("Basket Handle Top"));
        Assert.That(visual.GetComponentsInChildren<Collider>(true)
            .All(value => !value.enabled), Is.True);

        renderer.RenderInventoryAttachments(
            Array.Empty<ResidentInventoryAttachmentViewModel>());
        yield return null;

        Assert.That(visual.gameObject.activeSelf, Is.False);
    }

    private static AgentViewModel Resident()
    {
        return new AgentViewModel(
            id: "resident.basket-test",
            name: "Basket Test",
            version: 1,
            isAlive: true,
            cellX: 1,
            cellY: 1,
            nutrition: 100,
            alertness: 100,
            mood: 100,
            health: 100,
            scheduledActivity: "Work",
            activeIntent: "Wait",
            actionElapsedTicks: 0,
            actionRequiredTicks: 0,
            decisionReason: "test",
            decisionExplanation: "test",
            utilityOptions: Array.Empty<AgentUtilityOptionViewModel>());
    }
}
}
