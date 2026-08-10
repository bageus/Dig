using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dig.Unity.Tests
{

public sealed class ContextHoverLayoutPlayModeTests
{
    private GameObject? _root;
    private DigGameHudCanvas? _hud;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("Context hover layout test", typeof(RectTransform));
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
    public void Context_hover_keeps_content_and_icon_geometry_stable()
    {
        Invoke("BeginBottomLayout", 98f);
        RectTransform contextPanel = Require("Context Panel");
        RectTransform content = Require("Context Panel/Context Content");
        RectTransform hoverPanel = Require(
            "Context Panel/Context Hover Information");
        Text hoverText = Require(
            "Context Panel/Context Hover Information/Context Hover Text")
            .GetComponent<Text>();

        RectTransform icon = new GameObject(
            "Representative Context Icon",
            typeof(RectTransform),
            typeof(Image),
            typeof(LayoutElement)).GetComponent<RectTransform>();
        icon.SetParent(content, worldPositionStays: false);
        LayoutElement iconLayout = icon.GetComponent<LayoutElement>();
        iconLayout.minWidth = 64f;
        iconLayout.preferredWidth = 64f;
        iconLayout.minHeight = 48f;
        iconLayout.preferredHeight = 48f;
        iconLayout.flexibleWidth = 0f;
        iconLayout.flexibleHeight = 0f;

        ForceLayout(contextPanel, content);
        LayoutSnapshot empty = Capture(contextPanel, content, icon);
        Assert.That(hoverPanel.gameObject.activeSelf, Is.True);
        Assert.That(hoverText.text, Is.Empty);
        Assert.That(content.offsetMax.y, Is.EqualTo(-52f).Within(0.001f));

        Invoke("SetProductionHoverInfo", "Stone", "Required: 2 stone");
        ForceLayout(contextPanel, content);
        LayoutSnapshot hovered = Capture(contextPanel, content, icon);
        Assert.That(hoverText.text, Is.EqualTo("Stone\nRequired: 2 stone"));
        AssertStable(empty, hovered);

        Invoke("ClearProductionHoverInfo");
        ForceLayout(contextPanel, content);
        LayoutSnapshot cleared = Capture(contextPanel, content, icon);
        Assert.That(hoverPanel.gameObject.activeSelf, Is.True);
        Assert.That(hoverText.text, Is.Empty);
        AssertStable(empty, cleared);
    }

    private static void ForceLayout(
        RectTransform contextPanel,
        RectTransform content)
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contextPanel);
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();
    }

    private static LayoutSnapshot Capture(
        RectTransform panel,
        RectTransform content,
        RectTransform icon)
    {
        return new LayoutSnapshot(
            panel.rect.size,
            content.rect.size,
            content.offsetMin,
            content.offsetMax,
            icon.anchoredPosition,
            icon.rect.size);
    }

    private static void AssertStable(LayoutSnapshot expected, LayoutSnapshot actual)
    {
        AssertVector(expected.PanelSize, actual.PanelSize, "context panel size");
        AssertVector(expected.ContentSize, actual.ContentSize, "content size");
        AssertVector(expected.ContentOffsetMin, actual.ContentOffsetMin, "content offsetMin");
        AssertVector(expected.ContentOffsetMax, actual.ContentOffsetMax, "content offsetMax");
        AssertVector(expected.IconPosition, actual.IconPosition, "icon position");
        AssertVector(expected.IconSize, actual.IconSize, "icon size");
    }

    private static void AssertVector(Vector2 expected, Vector2 actual, string label)
    {
        Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.001f), label + " x");
        Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.001f), label + " y");
    }

    private RectTransform Require(string path)
    {
        Transform? value = _root!.transform.Find(path);
        Assert.That(value, Is.Not.Null, path);
        return (RectTransform)value!;
    }

    private void Invoke(string name, params object[] arguments)
    {
        MethodInfo? method = typeof(DigGameHudCanvas).GetMethod(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, name);
        method!.Invoke(_hud, arguments);
    }

    private readonly struct LayoutSnapshot
    {
        public LayoutSnapshot(
            Vector2 panelSize,
            Vector2 contentSize,
            Vector2 contentOffsetMin,
            Vector2 contentOffsetMax,
            Vector2 iconPosition,
            Vector2 iconSize)
        {
            PanelSize = panelSize;
            ContentSize = contentSize;
            ContentOffsetMin = contentOffsetMin;
            ContentOffsetMax = contentOffsetMax;
            IconPosition = iconPosition;
            IconSize = iconSize;
        }

        public Vector2 PanelSize { get; }

        public Vector2 ContentSize { get; }

        public Vector2 ContentOffsetMin { get; }

        public Vector2 ContentOffsetMax { get; }

        public Vector2 IconPosition { get; }

        public Vector2 IconSize { get; }
    }
}

}
