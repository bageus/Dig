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
        text.color = Color.white;
        text.alignment = alignment;
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
            new Color(0.18f, 0.22f, 0.28f, 0.92f));
        LayoutElement element = rect.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = preferredHeight;
        element.minHeight = preferredHeight;
        Button button = rect.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        colors.pressedColor = new Color(0.78f, 0.82f, 0.90f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        button.onClick.AddListener(() => callback());
        Text text = CreateText(
            "Label",
            rect,
            label,
            18,
            TextAnchor.MiddleCenter);
        Stretch(text.rectTransform, 6f, 2f, -6f, -2f);
        text.raycastTarget = false;
        return button;
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
            new Color(0.11f, 0.14f, 0.18f, 0.82f));
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
        Text heading = CreateText(
            "Heading",
            section,
            title,
            16,
            TextAnchor.MiddleCenter);
        LayoutElement headingLayout = heading.gameObject.AddComponent<LayoutElement>();
        headingLayout.preferredHeight = 24f;
        return section;
    }

    private static void ClearChildren(RectTransform parent)
    {
        for (int index = parent.childCount - 1; index >= 0; index--)
        {
            Destroy(parent.GetChild(index).gameObject);
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
                ? new Color(0.86f, 0.55f, 0.18f, 0.96f)
                : new Color(0.18f, 0.22f, 0.28f, 0.92f);
        }
    }
}

}
