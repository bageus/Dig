using System;
using UnityEngine;

namespace Dig.Unity
{

[DisallowMultipleComponent]
public sealed class DigBuildingBoxLabel : MonoBehaviour
{
    private const string BuildingBoxPrefix = "building_box.";
    private static readonly Vector3 LabelOffset = new Vector3(0f, 0.38f, -0.18f);

    private DigWorldItemVisual? _visual;
    private TextMesh? _label;
    private string _itemId = string.Empty;

    private void Awake()
    {
        _visual = GetComponent<DigWorldItemVisual>();
    }

    private void LateUpdate()
    {
        if (_visual == null)
        {
            _visual = GetComponent<DigWorldItemVisual>();
        }

        string itemId = _visual?.Model?.ItemId ?? string.Empty;
        if (!DigWorldItemVisualPolicy.IsBuildingBox(itemId))
        {
            if (_label != null) _label.gameObject.SetActive(false);
            _itemId = string.Empty;
            return;
        }

        EnsureLabel();
        _label!.gameObject.SetActive(true);
        if (!string.Equals(_itemId, itemId, StringComparison.Ordinal))
        {
            _itemId = itemId;
            _label.text = ResolveBuildingName(itemId);
        }

        _label.transform.localPosition = LabelOffset;
        Camera camera = Camera.main;
        if (camera != null)
        {
            _label.transform.rotation = camera.transform.rotation;
        }
    }

    internal static string ResolveBuildingName(string itemId)
    {
        return itemId switch
        {
            "building_box.campfire" => "Campfire",
            "building_box.tent" => "Tent",
            "building_box.stone_mason" => "Stone mason workshop",
            "building_box.wood_workshop" => "Wooden workshop",
            "building_box.wooden_door" => "Wooden door",
            "building_box.ladder" => "Ladder",
            "building_box.farm" => "Farm",
            "building_box.border_stone" => "Border stone",
            "building_box.press_trap" => "Press trap",
            "building_box.stone_door" => "Stone door",
            _ => HumanizeBuildingBoxId(itemId),
        };
    }

    private static string HumanizeBuildingBoxId(string itemId)
    {
        if (!itemId.StartsWith(BuildingBoxPrefix, StringComparison.Ordinal))
        {
            return string.Empty;
        }

        string value = itemId.Substring(BuildingBoxPrefix.Length).Replace('_', ' ');
        return value.Length == 0
            ? string.Empty
            : char.ToUpperInvariant(value[0]) + value.Substring(1);
    }

    private void EnsureLabel()
    {
        if (_label != null) return;

        GameObject labelObject = new GameObject("Building name label");
        labelObject.layer = 2;
        labelObject.transform.SetParent(transform, worldPositionStays: false);
        _label = labelObject.AddComponent<TextMesh>();
        _label.anchor = TextAnchor.LowerCenter;
        _label.alignment = TextAlignment.Center;
        _label.fontSize = 48;
        _label.characterSize = 0.045f;
        _label.fontStyle = FontStyle.Bold;
        _label.color = Color.white;
        _label.richText = false;
        MeshRenderer renderer = _label.GetComponent<MeshRenderer>();
        renderer.sortingOrder = 20;
    }
}

[RequireComponent(typeof(DigBuildingBoxLabel))]
public sealed partial class DigWorldItemVisual
{
}

}
