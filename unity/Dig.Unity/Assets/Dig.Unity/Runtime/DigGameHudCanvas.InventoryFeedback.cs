using System;
using System.Linq;
using Dig.Domain.Inventory;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    private const float SpillConfirmationSeconds = 4f;
    private string? _pendingSpillStackId;
    private float _pendingSpillUntil;

    private void ShowInventorySlotFeedback(
        ResidentInventoryLayoutSlotViewModel slot)
    {
        if (_statusText == null)
        {
            return;
        }

        ResidentInventoryExpansionFeedbackViewModel? expansion =
            LoadExpansionFeedback(slot);
        if (expansion == null)
        {
            if (!slot.IsEmpty)
            {
                _statusText.text = BuildItemTooltip(slot);
            }

            return;
        }

        string state = expansion.IsActive ? "активно" : "неактивно";
        string group = expansion.Group == InventoryExpansionGroup.Cargo
            ? "грузовых ячеек"
            : "оружейных ячеек";
        string categories = expansion.AcceptedCategories.Count == 0
            ? "—"
            : string.Join(", ", expansion.AcceptedCategories);
        string penalty = expansion.SpeedPenaltyPercent == 0
            ? "без штрафа скорости"
            : $"штраф скорости -{expansion.SpeedPenaltyPercent}% при заполнении";
        string spill = expansion.RequiresSpillConfirmation
            ? $" · ⚠ при выбрасывании прольётся стеков: {expansion.OccupiedSlotCount}"
            : string.Empty;
        _statusText.text =
            $"{expansion.DisplayName} · T{expansion.Tier} · {state} · "
            + $"+{expansion.AddedSlots} {group} · фильтр: {categories} · "
            + $"{penalty} · visual: {expansion.VisualAttachmentId}{spill}";
    }

    private void HideInventorySlotFeedback()
    {
        if (_statusText != null)
        {
            _statusText.text = _status;
        }
    }

    private bool ConfirmExpansionSpill(
        ResidentInventoryLayoutSlotViewModel slot)
    {
        ResidentInventoryExpansionFeedbackViewModel? expansion =
            LoadExpansionFeedback(slot);
        if (expansion == null || !expansion.RequiresSpillConfirmation)
        {
            ClearSpillConfirmation();
            return true;
        }

        float now = Time.unscaledTime;
        if (string.Equals(
                _pendingSpillStackId,
                expansion.StackId,
                StringComparison.Ordinal)
            && now <= _pendingSpillUntil)
        {
            ClearSpillConfirmation();
            return true;
        }

        _pendingSpillStackId = expansion.StackId;
        _pendingSpillUntil = now + SpillConfirmationSeconds;
        SetStatus(
            $"⚠ {expansion.DisplayName}: выбрасывание прольёт "
            + $"{expansion.OccupiedSlotCount} стек(ов) из "
            + $"{expansion.Group}. Повторите действие для подтверждения.");
        return false;
    }

    private bool RequiresExpansionSpillConfirmation(
        ResidentInventoryLayoutSlotViewModel slot)
    {
        return LoadExpansionFeedback(slot)?.RequiresSpillConfirmation == true;
    }

    private ResidentInventoryExpansionFeedbackViewModel? LoadExpansionFeedback(
        ResidentInventoryLayoutSlotViewModel slot)
    {
        if (slot.StackId == null
            || _terrainSession == null
            || _agentRenderer?.SelectedAgentId == null)
        {
            return null;
        }

        return _terrainSession.LoadResidentExpansionFeedback(
            _agentRenderer.SelectedAgentId,
            slot.StackId);
    }

    private void ClearSpillConfirmation()
    {
        _pendingSpillStackId = null;
        _pendingSpillUntil = 0f;
    }

    private static string BuildItemTooltip(
        ResidentInventoryLayoutSlotViewModel slot)
    {
        string quantities = $"доступно {slot.AvailableQuantity}/{slot.Quantity}";
        string reserved = slot.ReservedQuantity > 0
            ? $" · зарезервировано {slot.ReservedQuantity}"
            : string.Empty;
        string held = slot.HeldQuantity > 0
            ? $" · в руках {slot.HeldQuantity}"
            : string.Empty;
        return $"{slot.DisplayName} · {quantities}{reserved}{held}";
    }
}

}
