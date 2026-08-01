using System;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private DigWorldItemVisual? _interactionHighlightedItem;
        private string? _hoveredInventoryItemId;
        private bool _hoveredInventoryCanDrop;
        private bool _hoveredInventoryCanUse;
        private bool _hoveredInventoryIsBuildingBox;

        internal void SetInventorySlotHoverFeedback(
            ResidentInventoryLayoutSlotViewModel slot)
        {
            if (slot == null)
            {
                throw new ArgumentNullException(nameof(slot));
            }

            _hoveredInventoryItemId = slot.ItemId;
            _hoveredInventoryCanDrop = slot.CanDrop;
            _hoveredInventoryCanUse = slot.CanUse;
            _hoveredInventoryIsBuildingBox = slot.IsBuildingBox;
        }

        internal void ClearInventorySlotHoverFeedback()
        {
            _hoveredInventoryItemId = null;
            _hoveredInventoryCanDrop = false;
            _hoveredInventoryCanUse = false;
            _hoveredInventoryIsBuildingBox = false;
        }

        private DirectCommandCursorKind ResolveInventoryHoverCursorKind()
        {
            if (_agentRenderer == null
                || _agentRenderer.SelectedCount == 0
                || string.IsNullOrWhiteSpace(_hoveredInventoryItemId))
            {
                return DirectCommandCursorKind.Default;
            }

            if (IsAltPressed()
                && _hoveredInventoryCanUse
                && IsDirectConsumableItemId(_hoveredInventoryItemId!))
            {
                return DirectCommandCursorKind.Eat;
            }

            return Input.GetKey(KeyCode.C)
                && _hoveredInventoryCanDrop
                && !_hoveredInventoryIsBuildingBox
                    ? DirectCommandCursorKind.Drop
                    : DirectCommandCursorKind.Default;
        }

        private bool TryResolveBuildingBoxHoverTarget(
            RaycastHit[] hits,
            out DigWorldItemVisual item)
        {
            return TryResolveBuildingBoxHit(hits, out item)
                && item.Model.AvailableQuantity == 1
                && CanSelectedResidentPickup(item);
        }

        private bool TryResolvePickableItemHoverTarget(
            RaycastHit[] hits,
            out DigWorldItemVisual item)
        {
            if (TryResolveBuildingInternalStockHit(
                    hits,
                    out DigBuildingInternalStockVisual stock)
                && _terrainSession!.TryResolveBuildingInternalStockPickup(
                    stock.StackId,
                    out _)
                && CanSelectedResidentPickup(stock.WorldItemVisual))
            {
                item = stock.WorldItemVisual;
                return true;
            }

            return TryResolveWorldItemHit(hits, out item)
                && item.Model.CanPickup
                && !item.Model.IsBuildingBox
                && CanSelectedResidentPickup(item);
        }

        private bool TryResolveFoodItemHoverTarget(
            RaycastHit[] hits,
            out DigWorldItemVisual item)
        {
            return TryResolveWorldItemHit(hits, out item)
                && item.Model.CanPickup
                && IsDirectFoodItem(item.Model)
                && CanSelectedResidentPickup(item);
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
