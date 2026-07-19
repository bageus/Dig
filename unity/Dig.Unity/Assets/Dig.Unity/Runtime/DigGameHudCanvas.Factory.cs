using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    private static Font? _font;

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject target = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)target.transform;
        rect.SetParent(parent, worldPositionStays: false);
        rect.localScale = Vector3.one;
        return rect;
    }

    private static RectTransform CreatePanel(
        string name,
        Transform parent,
        Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return rect;
    }

    private static Text CreateText(
        string name,
        Transform parent,
        string value,
        int fontSize,
        TextAnchor alignment)
    {
        RectTransform rect = CreateRect(name, parent);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = GetFont();
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.color = Color.white;
        text.alignment = alignment;
        text.alignByGeometry = true;
        text.supportRichText = false;
        text.text = value;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private static Button CreateButton(
        string name,
        Transform parent,
        string label,
        Action callback,
        float preferredHeight = 38f)
    {
        RectTransform rect = CreatePanel(
            name,
            parent,
            new Color(0.15f, 0.20f, 0.27f, 1f));
        LayoutElement element = rect.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = preferredHeight;
        element.minHeight = preferredHeight;
        Button button = rect.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.72f, 0.78f, 0.88f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.55f, 0.58f, 0.62f, 0.72f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.05f;
        button.colors = colors;
        button.onClick.AddListener(() => callback());
        Text text = CreateText(
            "Label",
            rect,
            label,
            20,
            TextAnchor.MiddleCenter);
        Stretch(text.rectTransform, 8f, 3f, -8f, -3f);
        text.raycastTarget = false;
        Outline outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.82f);
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = true;
        return button;
    }

    private static Button CreateRoomIconButton(
        string name,
        Transform parent,
        Vector2 iconSize,
        Action callback)
    {
        Button button = CreateButton(
            name,
            parent,
            string.Empty,
            callback,
            preferredHeight: 52f);
        RectTransform icon = CreateRect("Room Icon", button.transform);
        icon.anchorMin = new Vector2(0.5f, 0.5f);
        icon.anchorMax = new Vector2(0.5f, 0.5f);
        icon.pivot = new Vector2(0.5f, 0.5f);
        icon.sizeDelta = iconSize;
        icon.anchoredPosition = Vector2.zero;

        const float thickness = 3f;
        CreateIconBar(
            "Top",
            icon,
            new Vector2(iconSize.x, thickness),
            new Vector2(0f, (iconSize.y - thickness) * 0.5f));
        CreateIconBar(
            "Bottom",
            icon,
            new Vector2(iconSize.x, thickness),
            new Vector2(0f, -(iconSize.y - thickness) * 0.5f));
        CreateIconBar(
            "Left",
            icon,
            new Vector2(thickness, Mathf.Max(thickness, iconSize.y - (thickness * 2f))),
            new Vector2(-(iconSize.x - thickness) * 0.5f, 0f));
        CreateIconBar(
            "Right",
            icon,
            new Vector2(thickness, Mathf.Max(thickness, iconSize.y - (thickness * 2f))),
            new Vector2((iconSize.x - thickness) * 0.5f, 0f));
        return button;
    }

    private static void CreateIconBar(
        string name,
        Transform parent,
        Vector2 size,
        Vector2 position)
    {
        RectTransform bar = CreatePanel(name, parent, Color.white);
        bar.anchorMin = new Vector2(0.5f, 0.5f);
        bar.anchorMax = new Vector2(0.5f, 0.5f);
        bar.pivot = new Vector2(0.5f, 0.5f);
        bar.sizeDelta = size;
        bar.anchoredPosition = position;
        bar.GetComponent<Image>().raycastTarget = false;
    }

    private static RectTransform CreateSection(
        string name,
        Transform parent,
        string title,
        float preferredWidth)
    {
        RectTransform section = CreatePanel(
            name,
            parent,
            new Color(0.08f, 0.11f, 0.15f, 0.96f));
        LayoutElement element = section.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = preferredWidth;
        element.flexibleWidth = 1f;
        VerticalLayoutGroup layout = section.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 6, 8);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        if (!string.IsNullOrWhiteSpace(title))
        {
            Text heading = CreateText(
                "Heading",
                section,
                title,
                16,
                TextAnchor.MiddleCenter);
            LayoutElement headingLayout = heading.gameObject.AddComponent<LayoutElement>();
            headingLayout.preferredHeight = 24f;
        }

        return section;
    }

    private static void ClearChildren(RectTransform parent)
    {
        for (int index = parent.childCount - 1; index >= 0; index--)
        {
            GameObject child = parent.GetChild(index).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
    }

    private static void Anchor(
        RectTransform rect,
        float minX,
        float minY,
        float maxX,
        float maxY,
        float left,
        float bottom,
        float right,
        float top)
    {
        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, top);
    }

    private static void Stretch(
        RectTransform rect,
        float left,
        float bottom,
        float right,
        float top)
    {
        Anchor(rect, 0f, 0f, 1f, 1f, left, bottom, right, top);
    }

    private static Font GetFont()
    {
        if (_font == null)
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null)
            {
                throw new InvalidOperationException("Unity built-in runtime font was not found.");
            }
        }

        return _font;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(StandaloneInputModule));
        eventSystem.transform.SetParent(null);
    }

    private static void SetButtonActive(Button? button, bool active)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = active
                ? new Color(0.90f, 0.54f, 0.13f, 1f)
                : new Color(0.15f, 0.20f, 0.27f, 1f);
        }
    }
}

}