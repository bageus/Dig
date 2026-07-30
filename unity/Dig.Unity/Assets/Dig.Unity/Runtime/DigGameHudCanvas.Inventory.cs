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
    private const int InventoryRows = 2;
    private const float InventoryCellWidth = 52f;
    private const float InventoryCellHeight = 38f;
    private const float InventoryCellSpacing = 4f;
    private const string MainCompartmentTitle = "";

    private void BuildInventoryContext(ResidentInventoryLayoutViewModel inventory)
    {
        BeginBottomLayout();
        ConfigureInventoryRootLayout();
        float cellWidth = ResolveInventoryCellWidth(inventory);
        BuildCompartmentIfActive(
            inventory,
            ResidentInventoryCompartment.Weapon,
            string.Empty,
            cellWidth);
        BuildCompartment(
            inventory,
            ResidentInventoryCompartment.Main,
            MainCompartmentTitle,
            cellWidth);
        BuildCompartmentIfActive(
            inventory,
            ResidentInventoryCompartment.Cargo,
            string.Empty,
            cellWidth);

        if (inventory.MoveSpeedMultiplier < 1d)
        {
            SetContextStatusSuffix(
                $"Loaded speed: {inventory.MoveSpeedMultiplier:P0}");
        }
        else
        {
            SetContextStatusSuffix(null);
        }
    }

    private void ConfigureInventoryRootLayout()
    {
        HorizontalLayoutGroup layout =
            _bottomContent!.gameObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
    }

    private void BuildCompartmentIfActive(
        ResidentInventoryLayoutViewModel inventory,
        ResidentInventoryCompartment compartment,
        string title,
        float cellWidth)
    {
        if (inventory.GetCompartment(compartment).Count > 0)
        {
            BuildCompartment(inventory, compartment, title, cellWidth);
        }
    }

    private void BuildCompartment(
        ResidentInventoryLayoutViewModel inventory,
        ResidentInventoryCompartment compartment,
        string title,
        float cellWidth)
    {
        IReadOnlyList<ResidentInventoryLayoutSlotViewModel> models =
            inventory.GetCompartment(compartment);
        if (models.Count == 0)
        {
            return;
        }

        Vector2Int dimensions = ResolveInventoryGrid(models.Count);
        int columns = dimensions.x;
        float gridHeight = (InventoryRows * InventoryCellHeight)
            + ((InventoryRows - 1) * InventoryCellSpacing);
        float sectionWidth = (columns * cellWidth)
            + ((columns - 1) * InventoryCellSpacing)
            + 4f;
        RectTransform section = CreateSection(
            compartment.ToString(),
            _bottomContent!,
            title,
            preferredWidth: sectionWidth);
        LayoutElement sectionElement = section.GetComponent<LayoutElement>();
        sectionElement.minWidth = 0f;
        sectionElement.preferredWidth = sectionWidth;
        sectionElement.flexibleWidth = 0f;
        VerticalLayoutGroup sectionLayout = section.GetComponent<VerticalLayoutGroup>();
        sectionLayout.padding = new RectOffset(2, 2, 4, 4);
        sectionLayout.spacing = 2f;
        sectionLayout.childAlignment = TextAnchor.UpperLeft;

        RectTransform slots = CreateRect("Slot Grid", section);
        LayoutElement gridElement = slots.gameObject.AddComponent<LayoutElement>();
        gridElement.preferredHeight = gridHeight;
        gridElement.minHeight = gridHeight;
        GridLayoutGroup grid = slots.gameObject.AddComponent<GridLayoutGroup>();
        ConfigureInventoryGrid(grid, columns, cellWidth);
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
            slot.IsEmpty ? 16 : 13,
            TextAnchor.MiddleCenter);
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 8;
        label.resizeTextMaxSize = slot.IsEmpty ? 16 : 13;
        label.color = ResolveSlotTextColor(slot);
        Stretch(label.rectTransform, 2f, 2f, -2f, -2f);
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

        bool leftClick = eventData.button == PointerEventData.InputButton.Left;
        bool altPressed = Input.GetKey(KeyCode.LeftAlt)
            || Input.GetKey(KeyCode.RightAlt);
        bool dropPressed = Input.GetKey(KeyCode.C);
        if (leftClick && altPressed)
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
        else if (leftClick && dropPressed && !slot.IsBuildingBox)
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
        else if (leftClick && slot.CanStartPlacement)
        {
            _interaction!.BeginResidentInventoryBuildingPlacement(slot);
        }
        else if (leftClick && !slot.IsHeld)
        {
            _interaction!.SelectResidentInventoryLayoutSlot(slot);
        }
        else if (leftClick)
        {
            SetStatus("The held item remains in its original slot.");
        }
        else
        {
            return;
        }

        InvalidateAll();
    }

    private Color ResolveSlotBackground(
        ResidentInventoryLayoutSlotViewModel slot)
    {
        if (slot.IsEmpty)
        {
            return new Color(0.06f, 0.07f, 0.09f, 0.72f);
        }

        if (IsBlueReservedSlot(slot))
        {
            return new Color(0.10f, 0.34f, 0.72f, 0.96f);
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

    private Color ResolveSlotTextColor(
        ResidentInventoryLayoutSlotViewModel slot)
    {
        if (IsBlueReservedSlot(slot))
        {
            return new Color(0.72f, 0.88f, 1f, 1f);
        }

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

    private bool IsBlueReservedSlot(ResidentInventoryLayoutSlotViewModel slot)
    {
        return slot.ReservedQuantity > 0
            && (slot.VisualKind == ResidentInventorySlotVisualKind.BuildingBox
                || (slot.StackId != null
                    && _terrainSession?.HasActiveResidentInventoryPlacement(
                        slot.StackId) == true));
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