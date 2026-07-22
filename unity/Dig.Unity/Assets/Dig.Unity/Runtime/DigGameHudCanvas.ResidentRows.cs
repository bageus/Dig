using System;
using System.Collections.Generic;
using Dig.Domain.Agents;
using Dig.Presentation.Agents;
using UnityEngine;
using UnityEngine.UI;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    private void BuildResidentCompactRow(
        Transform parent,
        ResidentRosterRowViewModel resident)
    {
        RectTransform compact = CreateRect("Compact", parent);
        LayoutElement compactLayout = compact.gameObject.AddComponent<LayoutElement>();
        compactLayout.preferredHeight = CompactResidentContentHeight;
        compactLayout.flexibleHeight = 0f;
        HorizontalLayoutGroup layout = compact.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = false;

        ResidentSexIndicator sex = resident.Sex;
        string sexMarker = sex == ResidentSexIndicator.Female
            ? "F"
            : sex == ResidentSexIndicator.Male ? "M" : "?";
        Text name = CreateText(
            "Name",
            compact,
            $"{resident.Name} ({sexMarker})",
            14,
            TextAnchor.MiddleLeft);
        name.color = sex == ResidentSexIndicator.Female
            ? new Color(1f, 0.51f, 0.73f, 1f)
            : sex == ResidentSexIndicator.Male
                ? new Color(0.44f, 0.72f, 1f, 1f)
                : new Color(0.76f, 0.76f, 0.76f, 1f);
        name.horizontalOverflow = HorizontalWrapMode.Overflow;
        name.resizeTextForBestFit = true;
        name.resizeTextMinSize = 10;
        name.resizeTextMaxSize = 14;
        LayoutElement nameLayout = name.gameObject.AddComponent<LayoutElement>();
        nameLayout.minWidth = 72f;
        nameLayout.flexibleWidth = 1f;
        nameLayout.preferredHeight = 27f;

        CreateCompactHealthBar(compact, resident.Health);
        CreateScheduleIndicator(compact, resident);
        CreateMoodIndicator(compact, resident.MoodFace);
    }

    private static void CreateCompactHealthBar(
        Transform parent,
        ResidentNeedViewModel health)
    {
        RectTransform bar = CreatePanel(
            "Health",
            parent,
            new Color(0.035f, 0.05f, 0.07f, 1f));
        LayoutElement layout = bar.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 72f;
        layout.minWidth = 58f;
        RectTransform fill = CreatePanel("Fill", bar, NeedColor(health.Band));
        Anchor(
            fill,
            0f,
            0f,
            health.Percent / 100f,
            1f,
            1f,
            4f,
            -1f,
            -4f);
        Text label = CreateText(
            "Value",
            bar,
            $"HP {health.Percent}",
            10,
            TextAnchor.MiddleCenter);
        Stretch(label.rectTransform, 1f, 1f, -1f, -1f);
        label.raycastTarget = false;
    }

    private static void CreateScheduleIndicator(
        Transform parent,
        ResidentRosterRowViewModel resident)
    {
        Color background = resident.IsIdleAtWork
            ? new Color(0.70f, 0.12f, 0.12f, 1f)
            : ScheduleColor(resident.ScheduledActivity);
        RectTransform marker = CreatePanel("Schedule", parent, background);
        LayoutElement layout = marker.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 30f;
        layout.minWidth = 30f;
        string value = resident.IsIdleAtWork
            ? "W!"
            : ScheduleLabel(resident.ScheduledActivity);
        Text text = CreateText("Label", marker, value, 11, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform, 1f, 1f, -1f, -1f);
        text.raycastTarget = false;
    }

    private static void CreateMoodIndicator(
        Transform parent,
        ResidentMoodFace mood)
    {
        RectTransform marker = CreatePanel(
            "Mood",
            parent,
            mood == ResidentMoodFace.Sad
                ? new Color(0.58f, 0.18f, 0.18f, 1f)
                : mood == ResidentMoodFace.Joy
                    ? new Color(0.16f, 0.52f, 0.24f, 1f)
                    : new Color(0.30f, 0.34f, 0.40f, 1f));
        LayoutElement layout = marker.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 30f;
        layout.minWidth = 30f;
        string value = mood == ResidentMoodFace.Sad
            ? ":("
            : mood == ResidentMoodFace.Joy ? ":)" : ":|";
        Text text = CreateText("Label", marker, value, 11, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform, 1f, 1f, -1f, -1f);
        text.raycastTarget = false;
    }

    private static void BuildResidentDetails(
        Transform parent,
        ResidentRosterRowViewModel resident)
    {
        Text status = CreateText(
            "Status",
            parent,
            FormatActivity(resident.Activity),
            12,
            TextAnchor.MiddleLeft);
        status.horizontalOverflow = HorizontalWrapMode.Overflow;
        status.resizeTextForBestFit = true;
        status.resizeTextMinSize = 9;
        status.resizeTextMaxSize = 12;
        LayoutElement statusLayout = status.gameObject.AddComponent<LayoutElement>();
        statusLayout.preferredHeight = ResidentStatusHeight;
        statusLayout.flexibleHeight = 0f;
        CreateNeedMetric(parent, "Health", resident.Health);
        CreateNeedMetric(parent, "Nutrition", resident.Nutrition);
        CreateNeedMetric(parent, "Alertness", resident.Alertness);
        CreateNeedMetric(parent, "Mood", resident.Mood);
        BuildTopSkillList(parent, resident.Skills);
    }

    private static void CreateNeedMetric(
        Transform parent,
        string label,
        ResidentNeedViewModel need)
    {
        RectTransform row = CreateRect(label, parent);
        LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = ResidentNeedMetricHeight;
        rowLayout.flexibleHeight = 0f;
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 4f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = false;
        Text name = CreateText("Label", row, label, 10, TextAnchor.MiddleLeft);
        name.gameObject.AddComponent<LayoutElement>().preferredWidth = 63f;
        RectTransform bar = CreatePanel(
            "Bar",
            row,
            new Color(0.025f, 0.035f, 0.05f, 1f));
        LayoutElement barLayout = bar.gameObject.AddComponent<LayoutElement>();
        barLayout.flexibleWidth = 1f;
        RectTransform fill = CreatePanel("Fill", bar, NeedColor(need.Band));
        Anchor(fill, 0f, 0f, need.Percent / 100f, 1f, 1f, 3f, -1f, -3f);
        Text value = CreateText("Value", bar, need.Percent.ToString(), 10, TextAnchor.MiddleCenter);
        Stretch(value.rectTransform, 1f, 1f, -1f, -1f);
        value.raycastTarget = false;
    }

    private static string FormatActivity(ResidentActivityDescriptor activity)
    {
        string progress = activity.ProgressMaximum > 0
            ? $" {activity.ProgressCurrent}/{activity.ProgressMaximum}"
            : string.Empty;
        string blocked = string.IsNullOrWhiteSpace(activity.BlockReasonCode)
            ? string.Empty
            : $" · {activity.BlockReasonCode}";
        return $"Status: {activity.Kind}{progress}{blocked}";
    }

    private static string ScheduleLabel(ScheduleActivity activity)
    {
        return activity switch
        {
            ScheduleActivity.Work => "W",
            ScheduleActivity.Sleep => "S",
            ScheduleActivity.Free => "F",
            _ => "R",
        };
    }

    private static Color ScheduleColor(ScheduleActivity activity)
    {
        return activity switch
        {
            ScheduleActivity.Work => new Color(0.82f, 0.49f, 0.12f, 1f),
            ScheduleActivity.Sleep => new Color(0.30f, 0.24f, 0.52f, 1f),
            ScheduleActivity.Free => new Color(0.18f, 0.50f, 0.34f, 1f),
            _ => new Color(0.22f, 0.38f, 0.58f, 1f),
        };
    }

    private static Color NeedColor(ResidentNeedBand band)
    {
        return band switch
        {
            ResidentNeedBand.Critical => new Color(0.78f, 0.16f, 0.13f, 1f),
            ResidentNeedBand.Warning => new Color(0.92f, 0.48f, 0.10f, 1f),
            _ => new Color(0.24f, 0.72f, 0.28f, 1f),
        };
    }

    private static string ShortSkill(string skillId)
    {
        int separator = skillId.LastIndexOf('.');
        string value = separator >= 0 ? skillId.Substring(separator + 1) : skillId;
        return value.Length <= 10 ? value : value.Substring(0, 10);
    }
}

}
