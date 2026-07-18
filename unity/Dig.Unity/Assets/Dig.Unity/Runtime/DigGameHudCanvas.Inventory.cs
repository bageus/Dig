using System;
using System.Collections.Generic;
using Dig.Domain.Inventory;
using Dig.Presentation.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    private void BuildInventoryContext(ResidentInventoryLayoutViewModel inventory)
    {
        BeginBottomLayout();
        BuildCompartment(
            inventory,
            ResidentInventoryCompartment.Weapon,
            "ОРУЖИЕ",
            preferredWidth: 360f);
        BuildCompartment(
            inventory,
            ResidentInventoryCompartment.Main,
            "ОСНОВНОЙ · 6",
            preferredWidth: 620f);
        string cargoTitle = inventory.CargoCapacity == 0
            ? "ГРУЗ · НЕТ РАСШИРЕНИЯ"
            : $"ГРУЗ · {inventory.CargoCapacity}";
        BuildCompartment(
            inventory,
            ResidentInventoryCompartment.Cargo,
            cargoTitle,
            preferredWidth: 520f);

        if (inventory.MoveSpeedMultiplier < 1d)
        {
            _statusText!.text = _status +
                $"  ·  Скорость с грузом: {inventory.MoveSpeedMultiplier:P0}";
        }
        else
        {
            _statusText!.text = _status;
        }
    }

    private void BuildCompartment(
        ResidentInventoryLayoutViewModel inventory,
        ResidentInventoryCompartment compartment,
        string title,
        float preferredWidth)
    {
        RectTransform section = CreateSection(
            compartment.ToString(),
            _bottomContent!,
            title,
            preferredWidth);
        RectTransform slots = CreateHorizontalRow("Slots", section, 92f);
        IReadOnlyList<ResidentInventoryLayoutSlotViewModel> models =
            inventory.GetCompartment(compartment);
        if (models.Count == 0)
        {
            Text none = CreateText(
                "Unavailable",
                slots,
                "—",
                28,
                TextAnchor.MiddleCenter);
            none.gameObject.AddComponent<LayoutElement>().preferredWidth = 70f;
            return;
        }

        for (int index = 0; index < models.Count; index++)
        {
            CreateInventorySlot(slots, models[index]);
        }
    }

    private void CreateInventorySlot(
        Transform parent,
        ResidentInventoryLayoutSlotViewModel slot)
    {
        RectTransform rect = CreatePanel(
            $"{slot.Compartment} {slot.SlotIndex}",
            parent,
            ResolveSlotBackground(slot));
        LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 82f;
        layout.minWidth = 64f;
        layout.flexibleWidth = 1f;
        Button button = rect.gameObject.AddComponent<Button>();
        button.interactable = !slot.IsEmpty;
        DigInventorySlotPointer pointer =
            rect.gameObject.AddComponent<DigInventorySlotPointer>();
        pointer.Clicked = eventData => HandleInventorySlotClick(slot, eventData);

        string marker = ResolveSlotMarker(slot);
        string quantity = slot.Quantity > 1 ? $" ×{slot.Quantity}" : string.Empty;
        string reservation = slot.ReservedQuantity > 0
            ? $"\nR:{slot.ReservedQuantity}"
            : string.Empty;
        string held = slot.IsHeld ? $"\nВ руках:{slot.HeldQuantity}" : string.Empty;
        string active = slot.IsActiveExpansion ? " ★" : string.Empty;
        string name = slot.IsEmpty
            ? $"{slot.SlotIndex + 1}\n·"
            : $"{marker}{active}\n{ShortName(slot.DisplayName)}{quantity}{reservation}{held}";
        Text label = CreateText(
            "Slot Label",
            rect,
            name,
            slot.IsEmpty ? 18 : 15,
            TextAnchor.MiddleCenter);
        label.color = ResolveSlotTextColor(slot);
        Stretch(label.rectTransform, 3f, 3f, -3f, -3f);
        label.raycastTarget = false;

        if (!slot.IsEmpty
            && string.Equals(
                slot.StackId,
                _interaction!.SelectedInventoryStackId,
                StringComparison.Ordinal))
        {
            Outline outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.78f, 0.18f, 1f);
            outline.effectDistance = new Vector2(3f, -3f);
        }
    }

    private void HandleInventorySlotClick(
        ResidentInventoryLayoutSlotViewModel slot,
        PointerEventData eventData)
    {
        if (slot.IsEmpty)
        {
            return;
        }

        bool altPressed = Input.GetKey(KeyCode.LeftAlt)
            || Input.GetKey(KeyCode.RightAlt);
        if (eventData.button == PointerEventData.InputButton.Right
            || eventData.clickCount >= 2)
        {
            if (slot.CanDrop)
            {
                _interaction!.DropResidentInventoryLayoutSlot(slot);
            }
            else
            {
                SetStatus(slot.IsHeld
                    ? "Предмет используется и остаётся в исходном слоте."
                    : "Зарезервированный предмет нельзя выбросить.");
            }
        }
        else if (altPressed)
        {
            if (slot.CanUse)
            {
                _interaction!.UseResidentInventoryLayoutSlot(slot);
            }
            else
            {
                SetStatus(slot.IsHeld
                    ? "Этот предмет уже находится в руках."
                    : "Предмет сейчас нельзя использовать.");
            }
        }
        else if (slot.CanStartPlacement)
        {
            _interaction!.ActivateResidentInventoryLayoutSlot(slot);
        }
        else if (!slot.IsHeld)
        {
            _interaction!.SelectResidentInventoryLayoutSlot(slot);
        }
        else
        {
            SetStatus("Предмет в руках остаётся в исходном слоте.");
        }

        InvalidateAll();
    }

    private static Color ResolveSlotBackground(
        ResidentInventoryLayoutSlotViewModel slot)
    {
        if (slot.IsEmpty)
        {
            return new Color(0.06f, 0.07f, 0.09f, 0.72f);
        }

        if (slot.IsHeld)
        {
            return new Color(0.12f, 0.34f, 0.48f, 0.96f);
        }

        if (slot.ReservedQuantity > 0)
        {
            return new Color(0.42f, 0.18f, 0.18f, 0.92f);
        }

        return slot.IsActiveExpansion
            ? new Color(0.35f, 0.28f, 0.10f, 0.96f)
            : new Color(0.16f, 0.20f, 0.25f, 0.96f);
    }

    private static Color ResolveSlotTextColor(
        ResidentInventoryLayoutSlotViewModel slot)
    {
        return slot.VisualKind switch
        {
            ResidentInventorySlotVisualKind.Tool =>
                new Color(0.35f, 0.84f, 1f, 1f),
            ResidentInventorySlotVisualKind.BuildingBox =>
                new Color(1f, 0.78f, 0.24f, 1f),
            ResidentInventorySlotVisualKind.CargoExpansion =>
                new Color(1f, 0.58f, 0.24f, 1f),
            ResidentInventorySlotVisualKind.WeaponExpansion =>
                new Color(0.58f, 0.68f, 1f, 1f),
            ResidentInventorySlotVisualKind.Generic =>
                new Color(0.65f, 0.92f, 0.55f, 1f),
            _ => new Color(0.55f, 0.58f, 0.62f, 1f),
        };
    }

    private static string ResolveSlotMarker(
        ResidentInventoryLayoutSlotViewModel slot)
    {
        return slot.VisualKind switch
        {
            ResidentInventorySlotVisualKind.Tool => "◆",
            ResidentInventorySlotVisualKind.BuildingBox => "■",
            ResidentInventorySlotVisualKind.CargoExpansion => "●",
            ResidentInventorySlotVisualKind.WeaponExpansion => "▲",
            ResidentInventorySlotVisualKind.Generic => "○",
            _ => "·",
        };
    }

    private static string ShortName(string value)
    {
        const int maximumLength = 12;
        if (string.IsNullOrWhiteSpace(value) || value.Length <= maximumLength)
        {
            return value;
        }

        return value.Substring(0, maximumLength - 1) + "…";
    }
}

}
