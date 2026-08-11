using System;
using UnityEngine;
using UnityEngine.UI;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    private const float ContextHoverContentOffsetMaxY = -52f;
    private RectTransform? _contextHoverPanel;
    private Text? _contextHoverText;
    private string _productionHoverTitle = string.Empty;
    private string _productionHoverDetails = string.Empty;
    private string _worldTargetHoverTitle = string.Empty;

    internal void SetProductionHoverInfo(string title, string details)
    {
        _productionHoverTitle = NormalizeHoverText(title);
        _productionHoverDetails = NormalizeHoverText(details);
        RefreshContextHoverInfo();
    }

    internal void ClearProductionHoverInfo()
    {
        if (_productionHoverTitle.Length == 0
            && _productionHoverDetails.Length == 0)
        {
            return;
        }

        _productionHoverTitle = string.Empty;
        _productionHoverDetails = string.Empty;
        RefreshContextHoverInfo();
    }

    internal void SetWorldTargetHoverInfo(string title)
    {
        string normalized = NormalizeHoverText(title);
        if (string.Equals(
                _worldTargetHoverTitle,
                normalized,
                StringComparison.Ordinal))
        {
            return;
        }

        _worldTargetHoverTitle = normalized;
        RefreshContextHoverInfo();
    }

    internal void ClearWorldTargetHoverInfo()
    {
        if (_worldTargetHoverTitle.Length == 0)
        {
            return;
        }

        _worldTargetHoverTitle = string.Empty;
        RefreshContextHoverInfo();
    }

    private void RefreshContextHoverInfo()
    {
        if (_bottomPanel == null || _bottomContent == null)
        {
            return;
        }

        bool hasProduction = _productionHoverTitle.Length > 0;
        string title = hasProduction
            ? _productionHoverTitle
            : _worldTargetHoverTitle;
        string details = hasProduction
            ? _productionHoverDetails
            : string.Empty;

        EnsureContextHoverInfo();
        _contextHoverPanel!.gameObject.SetActive(_bottomPanel.gameObject.activeSelf);
        _contextHoverText!.text = details.Length == 0
            ? title
            : title + "\n" + details;

        Vector2 offsetMax = _bottomContent.offsetMax;
        offsetMax.y = ContextHoverContentOffsetMaxY;
        _bottomContent.offsetMax = offsetMax;
    }

    private void EnsureContextHoverInfo()
    {
        if (_contextHoverPanel != null)
        {
            return;
        }

        _contextHoverPanel = CreatePanel(
            "Context Hover Information",
            _bottomPanel!,
            new Color(0.055f, 0.075f, 0.105f, 0.98f));
        Anchor(
            _contextHoverPanel,
            0f,
            1f,
            1f,
            1f,
            12f,
            -48f,
            -12f,
            -6f);
        _contextHoverPanel.SetAsLastSibling();
        Image background = _contextHoverPanel.GetComponent<Image>();
        background.raycastTarget = false;

        _contextHoverText = CreateText(
            "Context Hover Text",
            _contextHoverPanel,
            string.Empty,
            16,
            TextAnchor.MiddleCenter);
        Stretch(_contextHoverText.rectTransform, 10f, 3f, -10f, -3f);
        _contextHoverText.resizeTextForBestFit = true;
        _contextHoverText.resizeTextMinSize = 10;
        _contextHoverText.resizeTextMaxSize = 16;
        _contextHoverText.raycastTarget = false;
        _contextHoverPanel.gameObject.SetActive(false);
    }

    private static string NormalizeHoverText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim();
    }
}

}
