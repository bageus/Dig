using System;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Presentation.Input;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private DigWorldItemVisual? _interactionHighlightedItem;
        private bool _hoveredInventoryHasItem;
        private bool _hoveredInventoryCanDrop;
        private bool _hoveredInventoryCanUse;
        private ItemInteractionFeedbackKind _hoveredInventoryUseFeedback;
        private string? _hoveredInventoryItemId;

        internal void SetInventorySlotHoverFeedback(
            ResidentInventoryLayoutSlotViewModel slot)
        {
            if (slot == null)
            {
                throw new ArgumentNullException(nameof(slot));
            }

            _hoveredInventoryHasItem = !slot.IsEmpty;
            _hoveredInventoryCanDrop = slot.CanDrop;
            _hoveredInventoryCanUse = slot.CanUse;
            _hoveredInventoryUseFeedback =
                slot.InteractionProfile.DirectUseFeedback;
            _hoveredInventoryItemId = slot.ItemId;
        }

        internal void ClearInventorySlotHoverFeedback()
        {
            _hoveredInventoryHasItem = false;
            _hoveredInventoryCanDrop = false;
            _hoveredInventoryCanUse = false;
            _hoveredInventoryUseFeedback = ItemInteractionFeedbackKind.None;
            _hoveredInventoryItemId = null;
        }

        private DirectCommandCursorKind ResolveInventoryHoverCursorKind()
        {
            if (_agentRenderer == null
                || _agentRenderer.SelectedCount == 0
                || !_hoveredInventoryHasItem)
            {
                return DirectCommandCursorKind.Default;
            }

            if (IsAltPressed() && _hoveredInventoryCanUse)
            {
                return _hoveredInventoryUseFeedback
                    == ItemInteractionFeedbackKind.Eat
                        ? DirectCommandCursorKind.Eat
                        : DirectCommandCursorKind.Use;
            }

            if (Input.GetKey(KeyCode.B)
                && (_hoveredInventoryItemId == "material.mushroom_leg"
                    || _hoveredInventoryItemId == "material.stone"))
            {
                return DirectCommandCursorKind.Hammer;
            }

            return Input.GetKey(KeyCode.C) && _hoveredInventoryCanDrop
                ? DirectCommandCursorKind.Drop
                : DirectCommandCursorKind.Default;
        }

        private bool TryResolveWorldItemPointerTarget(
            RaycastHit[] hits,
            bool altPressed,
            out ResolvedWorldItemPointerTarget target)
        {
            if (TryResolveBuildingInternalStockHit(
                    hits,
                    out DigBuildingInternalStockVisual stock))
            {
                DigWorldItemVisual item = stock.WorldItemVisual;
                ItemWorldInteractionAction action =
                    item.Model.ResolveWorldAction(altPressed);
                CellId cell = new CellId(
                    item.Model.CellX,
                    item.Model.CellY,
                    item.Model.CellZ);
                bool available = action == ItemWorldInteractionAction.Pickup
                    && _terrainSession != null
                    && _terrainSession.TryResolveBuildingInternalStockPickup(
                        stock.StackId,
                        out cell)
                    && CanSelectedResidentPickup(item);
                target = new ResolvedWorldItemPointerTarget(
                    item,
                    action,
                    ContextWorldTargetKind.GenericItem,
                    cell,
                    available);
                return action != ItemWorldInteractionAction.None;
            }

            if (!TryResolveAnyWorldItemHit(hits, out DigWorldItemVisual worldItem))
            {
                target = default;
                return false;
            }

            ItemWorldInteractionAction resolved =
                worldItem.Model.ResolveWorldAction(altPressed);
            ContextWorldTargetKind kind = ResolveWorldItemTargetKind(
                worldItem.Model,
                resolved);
            CellId sourceCell = new CellId(
                worldItem.Model.CellX,
                worldItem.Model.CellY,
                worldItem.Model.CellZ);
            bool actionAvailable = ResolveWorldItemActionAvailability(
                worldItem,
                resolved,
                sourceCell);
            target = new ResolvedWorldItemPointerTarget(
                worldItem,
                resolved,
                kind,
                sourceCell,
                actionAvailable);
            return resolved != ItemWorldInteractionAction.None;
        }

        private bool ResolveWorldItemActionAvailability(
            DigWorldItemVisual item,
            ItemWorldInteractionAction action,
            CellId sourceCell)
        {
            if (!item.Model.IsActionAvailable(action))
            {
                return false;
            }

            switch (action)
            {
                case ItemWorldInteractionAction.SelectBuildingBox:
                    return true;
                case ItemWorldInteractionAction.Pickup:
                    return CanSelectedResidentPickup(item);
                case ItemWorldInteractionAction.DirectUse:
                    return _terrainSession != null
                        && _terrainSession.ValidateWorldConsumableAction(
                            item.Model.StackId).IsSuccess
                        && CanSelectedResidentPickup(item);
                case ItemWorldInteractionAction.UseProductionPackage:
                    if (_terrainSession == null
                        || _agentRenderer?.SelectedModel == null)
                    {
                        return false;
                    }

                    var resident = _agentRenderer.SelectedModel;
                    return _terrainSession.CanDirectUseProductionPackage(
                        Dig.Domain.Core.EntityId.Parse(item.Model.StackId),
                        new CellId(
                            resident.CellX,
                            resident.CellY,
                            resident.CellZ),
                        out _);
                default:
                    return false;
            }
        }

        private static ContextWorldTargetKind ResolveWorldItemTargetKind(
            WorldItemViewModel item,
            ItemWorldInteractionAction action)
        {
            if (item.IsBuildingBox)
            {
                return ContextWorldTargetKind.BuildingBox;
            }

            return action switch
            {
                ItemWorldInteractionAction.DirectUse =>
                    ContextWorldTargetKind.FoodItem,
                ItemWorldInteractionAction.UseProductionPackage =>
                    ContextWorldTargetKind.ProductionPackage,
                _ => ContextWorldTargetKind.GenericItem,
            };
        }

        private bool CanSelectedResidentPickup(DigWorldItemVisual item)
        {
            string? residentId = _agentRenderer?.SelectedModel?.Id;
            return _terrainSession != null
                && !string.IsNullOrWhiteSpace(residentId)
                && _terrainSession.ValidateResidentCanPickupStack(
                    residentId!,
                    item.Model.StackId).IsSuccess;
        }

        private readonly struct ResolvedWorldItemPointerTarget
        {
            internal ResolvedWorldItemPointerTarget(
                DigWorldItemVisual item,
                ItemWorldInteractionAction action,
                ContextWorldTargetKind kind,
                CellId cell,
                bool actionAvailable)
            {
                Item = item;
                Action = action;
                Kind = kind;
                Cell = cell;
                ActionAvailable = actionAvailable;
            }

            internal DigWorldItemVisual Item { get; }
            internal ItemWorldInteractionAction Action { get; }
            internal ContextWorldTargetKind Kind { get; }
            internal CellId Cell { get; }
            internal bool ActionAvailable { get; }
        }

        private void SetInteractionHighlightedItem(DigWorldItemVisual? item)
        {
            if (ReferenceEquals(_interactionHighlightedItem, item))
            {
                return;
            }

            _interactionHighlightedItem?.SetInteractionHighlighted(false);
            _interactionHighlightedItem = item;
            _interactionHighlightedItem?.SetInteractionHighlighted(true);
        }

        private Texture2D[] CreateDropCursorFrames()
        {
            Texture2D[] pickup = _pickupCursorFrames ??= CreatePickupCursorFrames();
            Texture2D[] frames = new Texture2D[pickup.Length];
            for (int index = 0; index < pickup.Length; index++)
            {
                Texture2D source = pickup[index];
                Color32[] sourcePixels = source.GetPixels32();
                Color32[] rotated = new Color32[sourcePixels.Length];
                int width = source.width;
                int height = source.height;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        rotated[(y * width) + x] =
                            sourcePixels[((height - 1 - y) * width) + (width - 1 - x)];
                    }
                }

                Texture2D frame = new Texture2D(
                    width,
                    height,
                    TextureFormat.RGBA32,
                    mipChain: false);
                frame.name = $"Dig Drop Cursor {index}";
                frame.filterMode = FilterMode.Point;
                frame.wrapMode = TextureWrapMode.Clamp;
                frame.SetPixels32(rotated);
                frame.Apply(updateMipmaps: false, makeNoLongerReadable: false);
                frames[index] = frame;
            }

            return frames;
        }
    }
}
