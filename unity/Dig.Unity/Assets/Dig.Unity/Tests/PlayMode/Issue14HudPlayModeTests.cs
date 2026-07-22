using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Presentation.Agents;
using Dig.Presentation.Inventory;
using Dig.Presentation.Notifications;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dig.Unity.Tests
{

public sealed class Issue14HudPlayModeTests
{
    private GameObject? _root;
    private DigGameHudCanvas? _hud;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("Issue 14 HUD Test", typeof(RectTransform));
        DigHudOverlay overlay = _root.AddComponent<DigHudOverlay>();
        _hud = _root.AddComponent<DigGameHudCanvas>();
        Invoke("InitializeStartup", overlay);
    }

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
        {
            UnityEngine.Object.DestroyImmediate(_root);
        }

        if (EventSystem.current != null)
        {
            UnityEngine.Object.DestroyImmediate(EventSystem.current.gameObject);
        }
    }

    [Test]
    public void Notification_ticker_deduplicates_and_right_click_dismisses()
    {
        GameNotificationTicker ticker = GetField<GameNotificationTicker>(
            "_notificationTicker");
        EntityId residentId = Id(1);
        AgentNeedThresholdCrossed need = new AgentNeedThresholdCrossed(
            tick: 5,
            residentId,
            AgentNeedThresholdKind.Hunger,
            threshold: 1_500,
            previousValue: 1_500,
            currentValue: 1_499);
        ticker.Ingest(new IDomainEvent[] { need, need });
        Invoke("SetStatus", "fallback");
        Invoke("RefreshNotificationText");

        Text text = Require("Notification Ticker/Notification").GetComponent<Text>();
        StringAssert.Contains("hungry", text.text);
        Assert.That(ticker.ActiveCount, Is.EqualTo(1));

        PointerEventData click = new PointerEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Right,
        };
        Invoke("HandleNotificationClick", click);

        Assert.That(ticker.ActiveCount, Is.Zero);
        Assert.That(text.text, Is.EqualTo("fallback"));
    }

    [Test]
    public void Technology_notification_target_opens_description_panel()
    {
        Invoke("ShowTechnologyDescriptionPanel", "technology.deep_mining");

        Text heading = Require(
            "Context Panel/Context Content/Technology Description/Heading")
            .GetComponent<Text>();
        Text description = Require(
            "Context Panel/Context Content/Technology Description/Description")
            .GetComponent<Text>();

        Assert.That(heading.text, Is.EqualTo("TECHNOLOGY DISCOVERED"));
        StringAssert.Contains("technology.deep_mining", description.text);
    }

    [Test]
    public void Left_click_removes_after_navigation_attempt_but_middle_click_does_not()
    {
        GameNotificationTicker ticker = GetField<GameNotificationTicker>(
            "_notificationTicker");
        AgentNeedThresholdCrossed need = new AgentNeedThresholdCrossed(
            tick: 6,
            Id(2),
            AgentNeedThresholdKind.CriticalMood,
            threshold: 500,
            previousValue: 500,
            currentValue: 499);
        ticker.Ingest(new IDomainEvent[] { need });
        PointerEventData click = new PointerEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Middle,
        };

        Invoke("HandleNotificationClick", click);
        Assert.That(ticker.ActiveCount, Is.EqualTo(1));
        click.button = PointerEventData.InputButton.Left;
        Invoke("HandleNotificationClick", click);

        Assert.That(ticker.ActiveCount, Is.Zero);
    }

    [Test]
    public void Seventy_residents_use_sixteen_reused_row_roots()
    {
        ResidentRosterViewModel first = CreateRoster(changedRow: -1);
        Invoke("RefreshResidentRows", first);
        Transform content = Require("Roster Panel/Roster Viewport/Roster Content");
        Transform[] rows = ResidentRows(content);

        Assert.That(rows.Length, Is.EqualTo(16));
        Assert.That(rows[0].name, Is.EqualTo("Resident " + Id(1)));
        Transform unchangedRoot = rows[1];
        Transform unchangedCompact = unchangedRoot.Find("Compact");

        Invoke("RefreshResidentRows", CreateRoster(changedRow: 0));
        Transform[] updated = ResidentRows(content);

        Assert.That(updated.Length, Is.EqualTo(16));
        Assert.That(updated[1], Is.SameAs(unchangedRoot));
        Assert.That(updated[1].Find("Compact"), Is.SameAs(unchangedCompact));
    }

    [TestCase(0, 141f)]
    [TestCase(1, 176f)]
    [TestCase(2, 192f)]
    [TestCase(3, 208f)]
    [TestCase(4, 224f)]
    [TestCase(5, 240f)]
    public void Expanded_resident_row_fits_visible_metrics_without_stretching(
        int visibleSkillCount,
        float expectedHeight)
    {
        Invoke(
            "RefreshResidentRows",
            CreateRoster(
                changedRow: -1,
                expandFirst: true,
                visibleSkillCount: visibleSkillCount));
        Transform content = Require("Roster Panel/Roster Viewport/Roster Content");
        Transform row = ResidentRows(content)[0];

        LayoutElement rootLayout = row.GetComponent<LayoutElement>();
        Assert.That(rootLayout.preferredHeight, Is.EqualTo(expectedHeight));
        Assert.That(rootLayout.minHeight, Is.EqualTo(expectedHeight));
        foreach (Transform child in row)
        {
            LayoutElement childLayout = child.GetComponent<LayoutElement>();
            Assert.That(childLayout, Is.Not.Null, child.name);
            Assert.That(childLayout.flexibleHeight, Is.Zero, child.name);
        }
    }

    [Test]
    public void Resident_inventory_renders_weapon_main_cargo_in_exact_order()
    {
        List<ResidentInventoryLayoutSlotViewModel> slots =
            new List<ResidentInventoryLayoutSlotViewModel>
            {
                EmptySlot(ResidentInventoryCompartment.Weapon, 0),
            };
        for (int index = 0; index < ResidentInventoryLayoutSnapshot.MainSlotCount; index++)
        {
            slots.Add(EmptySlot(ResidentInventoryCompartment.Main, index));
        }

        slots.Add(EmptySlot(ResidentInventoryCompartment.Cargo, 0));
        ResidentInventoryLayoutViewModel inventory = new ResidentInventoryLayoutViewModel(
            Id(1).ToString(),
            inventoryVersion: 1,
            mainCapacity: ResidentInventoryLayoutSnapshot.MainSlotCount,
            cargoCapacity: 1,
            weaponCapacity: 1,
            moveSpeedMultiplier: 1d,
            slots: slots);

        Invoke("BuildInventoryContext", inventory);
        string[] sections = Require("Context Panel/Context Content")
            .Cast<Transform>()
            .Select(child => child.name)
            .ToArray();

        Assert.That(sections, Is.EqualTo(new[] { "Weapon", "Main", "Cargo" }));
    }

    private ResidentRosterViewModel CreateRoster(
        int changedRow,
        bool expandFirst = false,
        int visibleSkillCount = 0)
    {
        List<ResidentRosterRowViewModel> rows = new List<ResidentRosterRowViewModel>();
        for (int index = 0; index < 70; index++)
        {
            string id = Id(index + 1).ToString();
            ResidentNeedViewModel need = new ResidentNeedViewModel(
                index == changedRow ? 7_900 : 8_000,
                ResidentNeedBand.Healthy,
                "resident.need.test");
            ResidentSkillSetViewModel skills = index == 0
                ? new ResidentSkillSetViewModel(AgentSkillCatalog.All
                    .Take(visibleSkillCount)
                    .Select(definition => new ResidentSkillViewModel(
                        definition.Id.ToString(),
                        1_000)))
                : new ResidentSkillSetViewModel(Array.Empty<ResidentSkillViewModel>());
            rows.Add(new ResidentRosterRowViewModel(
                id,
                "Resident " + (index + 1),
                version: index == changedRow ? 2 : 1,
                isAlive: true,
                isExpanded: expandFirst && index == 0,
                ResidentSexIndicator.Unknown,
                "resident.sex.unknown",
                ScheduleActivity.Work,
                ResidentMoodFace.Joy,
                need,
                need,
                need,
                need,
                new ResidentActivityDescriptor(
                    ResidentActivityKind.Idle,
                    id,
                    "resident.activity.idle"),
                isIdleAtWork: true,
                skills));
        }

        return new ResidentRosterViewModel(rows, selectedResidentId: null);
    }

    private Transform[] ResidentRows(Transform content)
    {
        return content.Cast<Transform>()
            .Where(child => child.name.StartsWith("Resident ", StringComparison.Ordinal))
            .ToArray();
    }

    private static ResidentInventoryLayoutSlotViewModel EmptySlot(
        ResidentInventoryCompartment compartment,
        int index)
    {
        return new ResidentInventoryLayoutSlotViewModel(
            compartment,
            index,
            stackId: null,
            itemId: null,
            displayName: string.Empty,
            quantity: 0,
            reservedQuantity: 0,
            heldQuantity: 0,
            visualKind: ResidentInventorySlotVisualKind.Empty,
            isActiveExpansion: false);
    }

    private Transform Require(string path)
    {
        Transform? child = _root!.transform.Find(path);
        Assert.That(child, Is.Not.Null, path);
        return child!;
    }

    private T GetField<T>(string name)
    {
        FieldInfo? field = typeof(DigGameHudCanvas).GetField(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        return (T)field!.GetValue(_hud)!;
    }

    private void Invoke(string name, params object[] arguments)
    {
        MethodInfo? method = typeof(DigGameHudCanvas).GetMethod(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, name);
        method!.Invoke(_hud, arguments);
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}
