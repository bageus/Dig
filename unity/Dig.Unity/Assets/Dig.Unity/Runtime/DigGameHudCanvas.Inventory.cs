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
    private const int InventoryColumns = 3;
    private const float InventoryCellWidth = 82f;
    private const float InventoryCellHeight = 76f;

    private void BuildInventoryContext(ResidentInventoryLayoutViewModel inventory)
    {
        BeginBottomLayout();
        BuildCompartmentIfActive(
            inventory,
            ResidentInventoryCompartment.Weapon,
            $"WEAPON · {inventory.WeaponCapacity}");
        BuildCompartment(
            inventory,
            ResidentInventoryCompartment.Main,
            "MAIN · 6");
        BuildCompartmentIfActive(
            inventory,
            ResidentInventoryCompartment.Cargo,
            $"CARGO · {inventory.CargoCapacity}");

        if (inventory.MoveSpeedMultiplier < 1d)
        {
            _statusText!.text = _status
                + $"  ·  Loaded speed: {inventory.MoveSpeedMultiplier:P0}";
        }
        else
        {
            _statusText!.text = _status;
        }
    }

    private void BuildCompartmentIfActive(
        ResidentInventoryLayoutViewModel inventory,
        ResidentInventoryCompartment compartment,
        string title)
    {
        if (inventory.GetCompartment(compartment).Count > 0)
        {
            BuildCompartment(inventory, compartment, title);
        }
    }

    private void BuildCompartment(
        ResidentInventoryLayoutViewModel inventory,
        ResidentInventoryCompartment compartment,
        string title)
    {
        IReadOnlyList<ResidentInventoryLayoutSlotViewModel> models =
            inventory.GetCompartment(compartment);
        if (models.Count == 0)
        {
            return;
        }

        int rows = Mathf.CeilToInt(models.Count / (float)InventoryColumns);
        float gridHeight = (rows * InventoryCellHeight) + ((rows - 1) * 6f);
        RectTransform section = CreateSection(
            compartment.ToString(),
            _bottomContent!,
            title,
            preferredWidth: 286f);
        RectTransform slots = CreateRect("Slot Grid", section);
        LayoutElement gridElement = slots.gameObject.AddComponent<LayoutElement>();
        gridElement.preferredHeight = gridHeight;
        gridElement.minHeight = gridHeight;
        GridLayoutGroup grid = slots.gameObject.AddComponent<GridLayoutGroup>();
        grid.padding = new RectOffset(6, 6, 0, 0);
        grid.cellSize = new Vector2(InventoryCellWidth, InventoryCellHeight);
        grid.spacing = new Vector2(6f, 6f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = InventoryColumns;
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
        Button button = rect.gameObject.AddComponent<Button>();
        button.interactable = !slot.IsEmpty;
        DigInventorySlotPointer pointer =
            rect.gameObject.AddComponent<DigInventorySlotPointer>();
        pointer.Clicked = eventData => HandleInventorySlotClick(slot, eventData);
        pointer.Hovered = () => ShowInventorySlotFeedback(slot);
        pointer.Exited = HideInventorySlotFeedback;

        string marker = ResolveSlotMarker(slot);
        string quantity = slot.Quantity > 1 ? $" ×{slot.Quantity}" : string.Empty;
        string reservation = slot.ReservedQuantity > 0
            ? $"\nR:{slot.ReservedQuantity}"
            : string.Empty;
        string held = slot.IsHeld ? $"\nHeld:{slot.HeldQuantity}" : string.Empty;
        string active = slot.IsActiveExpansion ? " ★" : string.Empty;
        string warning = RequiresExpansionSpillConfirmation(slot) ? " ⚠" : string.Empty;
        string name = slot.IsEmpty
            ? $"{slot.SlotIndex + 1}\n·"
            : $"{marker}{active}{warning}\n{ShortName(slot.DisplayName)}"
                + $"{quantity}{reservation}{held}";
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
            if (!ConfirmExpansionSpill(slot))
            {
                InvalidateAll();
                return;
            }

            if (slot.CanDrop)
            {
                _interaction!.DropResidentInventoryLayoutSlot(slot);
            }
            else
            {
                SetStatus(slot.IsHeld
                    ? "The held item remains in its original slot."
                    : "A reserved item cannot be dropped.");
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
                    ? "This item is already held."
                    : "This item cannot be used now.");
            }
        }
        else if (slot.CanStartPlacement)
        {
            _interaction!.ActivateResidentInventoryLayoutSlot(slot);
        }
        else if (!slot.IsHeld)
        {
            if (!ConfirmExpansionSpill(slot))
            {
                InvalidateAll();
                return;
            }

            _interaction!.SelectResidentInventoryLayoutSlot(slot);
        }
        else
        {
            SetStatus("The held item remains in its original slot.");
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