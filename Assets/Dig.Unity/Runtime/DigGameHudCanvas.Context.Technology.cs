using System;
using UnityEngine;
using UnityEngine.UI;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    private string? _technologyDescriptionId;

    internal void ShowTechnologyDescription(string technologyId)
    {
        if (string.IsNullOrWhiteSpace(technologyId))
        {
            throw new ArgumentException("Technology id is required.", nameof(technologyId));
        }

        _technologyDescriptionId = technologyId.Trim();
        _lastContextSignature = string.Empty;
        if (_initialized)
        {
            RefreshContextPanel();
        }
    }

    private void ShowTechnologyDescriptionPanel(string technologyId)
    {
        string signature = "technology:" + technologyId;
        if (!ApplyContextSignature(signature))
        {
            return;
        }

        BeginBottomLayout(142f);
        RectTransform section = CreateSection(
            "Technology Description",
            _bottomContent!,
            "TECHNOLOGY DISCOVERED",
            preferredWidth: 900f);
        Text details = CreateText(
            "Description",
            section,
            $"{technologyId} is unlocked and available in the research catalogue.",
            18,
            TextAnchor.MiddleCenter);
        details.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
        CreateButton(
            "Close Technology",
            section,
            "Close",
            DismissTechnologyDescription,
            preferredHeight: 38f);
    }

    internal void DismissTechnologyDescription()
    {
        _technologyDescriptionId = null;
        _lastContextSignature = string.Empty;
    }
}

}
