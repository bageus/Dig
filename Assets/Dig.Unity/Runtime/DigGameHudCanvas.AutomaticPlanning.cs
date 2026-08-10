using Dig.Domain.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    private Button? _automaticPlanningButton;
    private Text? _automaticPlanningLabel;

    private void CreateAutomaticPlanningButton()
    {
        _automaticPlanningButton = CreateButton(
            "Automatic Planning Toggle",
            _clockPanel!,
            "AUTO",
            ToggleSelectedResidentAutomaticPlanning,
            preferredHeight: 36f);
        RectTransform rect = (RectTransform)_automaticPlanningButton.transform;
        SetCenteredRect(rect, new Vector2(38f, 32f), new Vector2(-57f, -46f));
        LayoutElement layout = _automaticPlanningButton.GetComponent<LayoutElement>();
        layout.ignoreLayout = true;
        _automaticPlanningLabel = _automaticPlanningButton.GetComponentInChildren<Text>();
        _automaticPlanningButton.gameObject.SetActive(false);
    }

    private string ResolveAutomaticPlanningState(
        string? selectedId,
        out bool visible,
        out bool enabled)
    {
        enabled = true;
        visible = selectedId != null
            && _simulation!.TryGetResidentAutomaticPlanning(selectedId, out enabled);
        return $"{visible}:{enabled}";
    }

    private void RefreshAutomaticPlanningButton(bool visible, bool enabled)
    {
        _automaticPlanningButton!.gameObject.SetActive(visible);
        if (!visible)
        {
            return;
        }

        _automaticPlanningLabel!.text = enabled ? "AUTO\nON" : "AUTO\nOFF";
        _automaticPlanningLabel.color = enabled
            ? new Color(0.94f, 1f, 0.94f, 1f)
            : new Color(0.76f, 0.80f, 0.86f, 1f);
        SetButtonActive(_automaticPlanningButton, enabled);
    }

    private void ToggleSelectedResidentAutomaticPlanning()
    {
        string? selectedId = _agentRenderer!.SelectedCount == 1
            ? _agentRenderer.SelectedAgentId
            : null;
        if (selectedId == null
            || !_simulation!.TryGetResidentAutomaticPlanning(
                selectedId,
                out bool enabled))
        {
            return;
        }

        Result result = _simulation.SetResidentAutomaticPlanning(
            selectedId,
            !enabled);
        if (result.IsFailure)
        {
            SetStatus(result.Error!.Message);
            return;
        }

        InvalidateAll();
        SetStatus(!enabled
            ? "Automatic job planning enabled for the selected dwarf."
            : "Automatic job planning disabled; manual orders remain available.");
    }
}

}
