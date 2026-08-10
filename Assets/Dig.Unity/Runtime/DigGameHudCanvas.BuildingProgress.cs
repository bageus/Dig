using Dig.Presentation.Buildings;
using UnityEngine;
using UnityEngine.UI;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    private void ShowBuildingProgress(BuildingWorldViewModel building)
    {
        string signature = $"building-progress:{building.Id}:{building.Version}:"
            + $"{building.Status}:{building.CompletedWork}:{building.RequiredWork}";
        if (!ApplyContextSignature(signature))
        {
            return;
        }

        BeginBottomLayout(142f);
        RectTransform section = CreateSection(
            "Building Progress",
            _bottomContent!,
            building.Name.ToUpperInvariant(),
            preferredWidth: 900f);
        Text details = CreateText(
            "Details",
            section,
            $"{building.Status} · Work {building.CompletedWork}/{building.RequiredWork}",
            18,
            TextAnchor.MiddleCenter);
        details.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;
    }

}

}
