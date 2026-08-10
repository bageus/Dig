using System;
using System.Collections;
using System.Linq;
using Dig.Domain.Content;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.Inventory;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

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

    [Test]
    public void Inventory_grid_and_bottom_hud_use_paired_rows_and_shared_height()
    {
        Assert.That(
            DigGameHudCanvas.ResolveInventoryGrid(4),
            Is.EqualTo(new Vector2Int(2, 2)));
        Assert.That(
            DigGameHudCanvas.ResolveInventoryGrid(6),
            Is.EqualTo(new Vector2Int(3, 2)));
        Assert.That(
            DigGameHudCanvas.ResolveBottomHudHeight(720f),
            Is.EqualTo(172.8f).Within(0.01f));
    }

    [Test]
    public void Inventory_grid_orders_slots_top_bottom_by_column()
    {
        _root = new GameObject("Inventory Grid Order Test", typeof(RectTransform));
        RectTransform root = (RectTransform)_root.transform;
        root.sizeDelta = new Vector2(300f, 100f);
        GridLayoutGroup grid = _root.AddComponent<GridLayoutGroup>();
        DigGameHudCanvas.ConfigureInventoryGrid(grid, columns: 3, cellWidth: 52f);
        RectTransform[] slots = Enumerable.Range(0, 6)
            .Select(index =>
            {
                GameObject slot = new GameObject($"Slot {index + 1}", typeof(RectTransform));
                RectTransform rect = (RectTransform)slot.transform;
                rect.SetParent(root, worldPositionStays: false);
                return rect;
            })
            .ToArray();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);

        Assert.That(grid.startAxis, Is.EqualTo(GridLayoutGroup.Axis.Vertical));
        Assert.That(grid.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedRowCount));
        Assert.That(grid.constraintCount, Is.EqualTo(2));
        Assert.That(slots[0].anchoredPosition.x,
            Is.EqualTo(slots[1].anchoredPosition.x).Within(0.01f));
        Assert.That(slots[0].anchoredPosition.y, Is.GreaterThan(slots[1].anchoredPosition.y));
        Assert.That(slots[2].anchoredPosition.x, Is.GreaterThan(slots[0].anchoredPosition.x));
        Assert.That(slots[2].anchoredPosition.y,
            Is.EqualTo(slots[0].anchoredPosition.y).Within(0.01f));
    }

    [Test]
    public void Campfire_has_same_plane_side_candidates_for_every_orientation()
    {
        CellId origin = new CellId(8, 4, 1);
        foreach (Dig.Domain.Buildings.BuildingOrientation orientation in
            Enum.GetValues(typeof(Dig.Domain.Buildings.BuildingOrientation)))
        {
            CellId[] sideCandidates = CampfireBuildingBoxContent.Definition.Building
                .ResolveWorkPositions(origin, orientation)
                .Where(value => value.Y == origin.Y && value.Z == origin.Z)
                .ToArray();

            Assert.That(sideCandidates, Has.Length.EqualTo(2));
            Assert.That(sideCandidates.All(value => value.X != origin.X), Is.True);
        }
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
