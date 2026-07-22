using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    private readonly struct ManagementColumn
    {
        public ManagementColumn(string label, float width)
        {
            Label = label;
            Width = width;
        }

        public string Label { get; }
        public float Width { get; }
    }

    private void BeginManagementOverlay(
        string title,
        IReadOnlyList<string> tabs,
        int activeTab,
        Action<int> selectTab)
    {
        ClearChildren(_managementOverlay!);
        Button close = CreateButton(
            "Close",
            _managementOverlay!,
            "X",
            CloseManagementOverlay,
            preferredHeight: 42f);
        RectTransform closeRect = (RectTransform)close.transform;
        Anchor(closeRect, 0f, 1f, 0f, 1f, 10f, -52f, 52f, -10f);

        Text heading = CreateText(
            "Title",
            _managementOverlay!,
            title,
            24,
            TextAnchor.MiddleCenter);
        Anchor(heading.rectTransform, 0f, 1f, 1f, 1f, 64f, -50f, -64f, -8f);
        heading.horizontalOverflow = HorizontalWrapMode.Overflow;

        RectTransform tabRow = CreateRect("Tabs", _managementOverlay!);
        Anchor(tabRow, 0f, 1f, 1f, 1f, 10f, -94f, -10f, -56f);
        HorizontalLayoutGroup tabLayout =
            tabRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        tabLayout.spacing = 5f;
        tabLayout.childControlHeight = true;
        tabLayout.childControlWidth = true;
        tabLayout.childForceExpandHeight = true;
        tabLayout.childForceExpandWidth = true;
        for (int index = 0; index < tabs.Count; index++)
        {
            int captured = index;
            Button button = CreateButton(
                "Tab " + tabs[index],
                tabRow,
                tabs[index],
                () => selectTab(captured),
                preferredHeight: 38f);
            SetButtonActive(button, index == activeTab);
        }

        RectTransform viewport = CreatePanel(
            "Table Viewport",
            _managementOverlay!,
            new Color(0f, 0f, 0f, 0.22f));
        Anchor(viewport, 0f, 0f, 1f, 1f, 10f, 10f, -10f, -102f);
        viewport.gameObject.AddComponent<RectMask2D>();
        _managementTableContent = CreateRect("Table Content", viewport);
        _managementTableContent.anchorMin = new Vector2(0f, 1f);
        _managementTableContent.anchorMax = new Vector2(0f, 1f);
        _managementTableContent.pivot = new Vector2(0f, 1f);
        _managementTableContent.anchoredPosition = Vector2.zero;
        VerticalLayoutGroup rows =
            _managementTableContent.gameObject.AddComponent<VerticalLayoutGroup>();
        rows.spacing = 2f;
        rows.childControlHeight = true;
        rows.childControlWidth = true;
        rows.childForceExpandHeight = false;
        rows.childForceExpandWidth = true;
        ContentSizeFitter fitter =
            _managementTableContent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = _managementTableContent;
        scroll.horizontal = true;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;
    }

    private void BuildManagementHeader(IReadOnlyList<ManagementColumn> columns)
    {
        float width = Mathf.Max(1f, columns.Sum(value => value.Width));
        _managementTableContent!.sizeDelta = new Vector2(width, 0f);
        RectTransform row = CreateManagementRow("Header", 34f, header: true);
        for (int index = 0; index < columns.Count; index++)
        {
            CreateManagementTextCell(
                row,
                columns[index].Label,
                columns[index].Width,
                TextAnchor.MiddleCenter,
                header: true);
        }
    }

    private RectTransform CreateManagementRow(
        string name,
        float height,
        bool header = false)
    {
        int index = _managementTableContent!.childCount;
        Color color = header
            ? new Color(0.17f, 0.22f, 0.29f, 1f)
            : index % 2 == 0
                ? new Color(0.075f, 0.095f, 0.125f, 0.98f)
                : new Color(0.10f, 0.12f, 0.15f, 0.98f);
        RectTransform row = CreatePanel(name, _managementTableContent, color);
        LayoutElement rowElement = row.gameObject.AddComponent<LayoutElement>();
        rowElement.preferredHeight = height;
        rowElement.minHeight = height;
        rowElement.flexibleHeight = 0f;
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 1f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = false;
        return row;
    }

    private static Text CreateManagementTextCell(
        Transform parent,
        string value,
        float width,
        TextAnchor alignment = TextAnchor.MiddleLeft,
        bool header = false)
    {
        Text text = CreateText(
            "Cell",
            parent,
            value,
            header ? 13 : 12,
            alignment);
        LayoutElement element = text.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = width;
        element.minWidth = width;
        element.flexibleWidth = 0f;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 8;
        text.resizeTextMaxSize = header ? 13 : 12;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        return text;
    }

    private static void CreateManagementBarCell(
        Transform parent,
        int value,
        int maximum,
        float width,
        Color color)
    {
        RectTransform cell = CreatePanel(
            "Metric",
            parent,
            new Color(0.025f, 0.035f, 0.05f, 1f));
        LayoutElement element = cell.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = width;
        element.minWidth = width;
        element.flexibleWidth = 0f;
        float progress = maximum <= 0 ? 0f : Mathf.Clamp01(value / (float)maximum);
        RectTransform fill = CreatePanel("Fill", cell, color);
        Anchor(fill, 0f, 0f, progress, 1f, 1f, 2f, -1f, -2f);
        Text label = CreateText(
            "Value",
            cell,
            maximum > 0 ? value + "/" + maximum : value.ToString(),
            11,
            TextAnchor.MiddleCenter);
        Stretch(label.rectTransform, 2f, 1f, -2f, -1f);
        label.raycastTarget = false;
    }

    private void BuildManagementEmptyState(string message)
    {
        float width = Mathf.Max(720f, _managementTableContent!.sizeDelta.x);
        _managementTableContent.sizeDelta = new Vector2(width, 0f);
        RectTransform row = CreateManagementRow("Empty", 72f);
        CreateManagementTextCell(
            row,
            message,
            width,
            TextAnchor.MiddleCenter);
    }
}

}
