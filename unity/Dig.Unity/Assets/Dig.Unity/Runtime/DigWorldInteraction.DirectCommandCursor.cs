using System;
using Dig.Domain.World;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private const float CommandCursorFrameSeconds = 0.11f;
        private const float MovementCursorDurationSeconds = 0.85f;
        private static readonly Vector2 ShovelCursorHotspot = new Vector2(16f, 27f);
        private static readonly Vector2 PickupCursorHotspot = new Vector2(16f, 27f);
        private static readonly Vector2 DropCursorHotspot = new Vector2(16f, 5f);
        private static readonly Vector2 MovementCursorHotspot = new Vector2(16f, 27f);
        private static readonly Vector2 AxeCursorHotspot = new Vector2(11f, 27f);
        private static readonly Vector2 EatCursorHotspot = new Vector2(16f, 16f);
        private static readonly Vector2 SwordCursorHotspot = new Vector2(12f, 28f);

        private Texture2D[]? _shovelCursorFrames;
        private Texture2D[]? _pickupCursorFrames;
        private Texture2D[]? _dropCursorFrames;
        private Texture2D[]? _movementCursorFrames;
        private Texture2D[]? _axeCursorFrames;
        private Texture2D[]? _eatCursorFrames;
        private Texture2D[]? _swordCursorFrames;
        private DirectCommandCursorKind _commandCursorKind;
        private int _commandCursorFrame = -1;
        private float _commandCursorAnimationStartedAt;
        private float _movementCursorExpiresAt;
        private DigWorldItemVisual? _interactionHighlightedItem;
        private string? _hoveredInventoryItemId;
        private bool _hoveredInventoryCanDrop;
        private bool _hoveredInventoryCanUse;
        private bool _hoveredInventoryIsBuildingBox;

        private enum DirectCommandCursorKind
        {
            Default = 0,
            Shovel = 1,
            Pickup = 2,
            Movement = 3,
            Axe = 4,
            Sword = 5,
            Eat = 6,
            Drop = 7,
        }

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

        private void UpdateSelectedResidentCommandCursor()
        {
            _barrelRenderer?.SetHighlighted(null);
            SetInteractionHighlightedItem(null);
            DirectCommandCursorKind kind = ResolveCommandCursorKind();
            ApplyCommandCursor(kind);
        }

        private DirectCommandCursorKind ResolveCommandCursorKind()
        {
            if (Time.unscaledTime < _movementCursorExpiresAt)
            {
                return DirectCommandCursorKind.Movement;
            }

            if (!IsInitialized() || _hud == null || _buildingPlacementMode.HasValue)
            {
                return DirectCommandCursorKind.Default;
            }

            if (_hud.ContainsScreenPoint(Input.mousePosition))
            {
                return ResolveInventoryHoverCursorKind();
            }

            RaycastHit[] hits = GetPointerHits();
            if (_agentRenderer != null && _agentRenderer.SelectedCount > 0)
            {
                if (_excavationMode == DigExcavationDrawingMode.None
                    && !_caveRoomPreset.HasValue
                    && TryResolveMushroomHoverTarget(hits))
                {
                    return DirectCommandCursorKind.Axe;
                }

                if (_excavationMode == DigExcavationDrawingMode.None
                    && !_caveRoomPreset.HasValue
                    && TryResolveBarrelHoverTarget(hits))
                {
                    return DirectCommandCursorKind.Sword;
                }

                if (_excavationMode == DigExcavationDrawingMode.None
                    && !_caveRoomPreset.HasValue
                    && TryResolveFoodItemHoverTarget(hits, out DigWorldItemVisual food))
                {
                    SetInteractionHighlightedItem(food);
                    return IsAltPressed()
                        ? DirectCommandCursorKind.Eat
                        : DirectCommandCursorKind.Pickup;
                }

                if (_excavationMode == DigExcavationDrawingMode.None
                    && !_caveRoomPreset.HasValue
                    && IsAltPressed()
                    && TryResolveBuildingBoxHoverTarget(
                        hits,
                        out DigWorldItemVisual buildingBox))
                {
                    SetInteractionHighlightedItem(buildingBox);
                    return DirectCommandCursorKind.Pickup;
                }

                if (_excavationMode == DigExcavationDrawingMode.None
                    && !_caveRoomPreset.HasValue
                    && TryResolvePickableItemHoverTarget(
                        hits,
                        out DigWorldItemVisual item))
                {
                    SetInteractionHighlightedItem(item);
                    return DirectCommandCursorKind.Pickup;
                }

                if (_excavationMode == DigExcavationDrawingMode.None
                    && !_caveRoomPreset.HasValue
                    && TryResolveExplicitExcavationHoverTarget(hits))
                {
                    return DirectCommandCursorKind.Shovel;
                }
            }

            if (_excavationMode == DigExcavationDrawingMode.Depth
                && ResolveTunnelDepthSource().HasValue)
            {
                return DirectCommandCursorKind.Shovel;
            }

            return DirectCommandCursorKind.Default;
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

            return Input.GetKey(KeyCode.D)
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
                && item.Model.AvailableQuantity == 1;
        }

        private bool TryResolvePickableItemHoverTarget(
            RaycastHit[] hits,
            out DigWorldItemVisual item)
        {
            return TryResolveWorldItemHit(hits, out item)
                && item.Model.CanPickup
                && !item.Model.IsBuildingBox;
        }

        private bool TryResolveFoodItemHoverTarget(
            RaycastHit[] hits,
            out DigWorldItemVisual item)
        {
            return TryResolveWorldItemHit(hits, out item)
                && item.Model.CanPickup
                && IsDirectFoodItem(item.Model);
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

        private bool TryResolveBarrelHoverTarget(RaycastHit[] hits)
        {
            if (!TryResolveBarrelHit(hits, out DigBarrelVisual barrel))
            {
                return false;
            }

            Dig.Presentation.Agents.AgentViewModel? selected =
                _agentRenderer!.SelectedModel;
            bool reachable = selected != null
                && _terrainSession!.CanDirectAttackBarrel(
                    barrel.Model.BarrelId,
                    new CellId(selected.CellX, selected.CellY, selected.CellZ),
                    out _);
            if (reachable)
            {
                _barrelRenderer!.SetHighlighted(barrel.Model.BarrelId);
            }

            return reachable;
        }

        private bool TryResolveMushroomHoverTarget(RaycastHit[] hits)
        {
            return TryResolveReachableMushroomHit(hits, out _);
        }

        private bool TryResolveExplicitExcavationHoverTarget(RaycastHit[] hits)
        {
            if (hits == null)
            {
                throw new ArgumentNullException(nameof(hits));
            }

            for (int index = 0; index < hits.Length; index++)
            {
                RaycastHit hit = hits[index];
                if (_renderer!.TryGetDepthDesignation(hit, out _))
                {
                    return true;
                }

                if (_agentRenderer!.TryGetAgent(hit, out _)
                    || (_buildingRenderer != null
                        && _buildingRenderer.TryGetBuilding(hit, out _))
                    || (_itemRenderer != null
                        && _itemRenderer.TryGetItem(hit, out _)))
                {
                    continue;
                }

                if (ResolveExcavationTarget(hit).HasValue)
                {
                    return true;
                }
            }

            return false;
        }

        private void PlayMovementCursorFeedback()
        {
            _movementCursorExpiresAt = Time.unscaledTime + MovementCursorDurationSeconds;
            BeginCommandCursorAnimation(DirectCommandCursorKind.Movement);
        }

        private void ApplyCommandCursor(DirectCommandCursorKind kind)
        {
            if (kind == DirectCommandCursorKind.Default)
            {
                if (_commandCursorKind != DirectCommandCursorKind.Default
                    || _commandCursorFrame != -1)
                {
                    Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                }

                _commandCursorKind = DirectCommandCursorKind.Default;
                _commandCursorFrame = -1;
                return;
            }

            if (_commandCursorKind != kind)
            {
                BeginCommandCursorAnimation(kind);
            }

            Texture2D[] frames = ResolveCommandCursorFrames(kind);
            int frame = Mathf.FloorToInt(
                (Time.unscaledTime - _commandCursorAnimationStartedAt)
                / CommandCursorFrameSeconds) % frames.Length;
            if (_commandCursorFrame == frame)
            {
                return;
            }

            _commandCursorFrame = frame;
            Cursor.SetCursor(
                frames[frame],
                ResolveCommandCursorHotspot(kind),
                CursorMode.Auto);
        }

        private void BeginCommandCursorAnimation(DirectCommandCursorKind kind)
        {
            _commandCursorKind = kind;
            _commandCursorAnimationStartedAt = Time.unscaledTime;
            _commandCursorFrame = -1;
        }

        private Texture2D[] ResolveCommandCursorFrames(DirectCommandCursorKind kind)
        {
            switch (kind)
            {
                case DirectCommandCursorKind.Shovel:
                    return _shovelCursorFrames ??= CreateShovelCursorFrames();
                case DirectCommandCursorKind.Pickup:
                    return _pickupCursorFrames ??= CreatePickupCursorFrames();
                case DirectCommandCursorKind.Drop:
                    return _dropCursorFrames ??= CreateDropCursorFrames();
                case DirectCommandCursorKind.Movement:
                    return _movementCursorFrames ??= CreateMovementCursorFrames();
                case DirectCommandCursorKind.Axe:
                    return _axeCursorFrames ??= CreateAxeCursorFrames();
                case DirectCommandCursorKind.Sword:
                    return _swordCursorFrames ??= CreateSwordCursorFrames();
                case DirectCommandCursorKind.Eat:
                    return _eatCursorFrames ??= CreateEatCursorFrames();
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
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

        private static Vector2 ResolveCommandCursorHotspot(DirectCommandCursorKind kind)
        {
            return kind switch
            {
                DirectCommandCursorKind.Shovel => ShovelCursorHotspot,
                DirectCommandCursorKind.Pickup => PickupCursorHotspot,
                DirectCommandCursorKind.Drop => DropCursorHotspot,
                DirectCommandCursorKind.Movement => MovementCursorHotspot,
                DirectCommandCursorKind.Axe => AxeCursorHotspot,
                DirectCommandCursorKind.Sword => SwordCursorHotspot,
                DirectCommandCursorKind.Eat => EatCursorHotspot,
                _ => Vector2.zero,
            };
        }

        private void OnDisable()
        {
            ClearPointerHover();
            SetInteractionHighlightedItem(null);
            ClearInventorySlotHoverFeedback();
            ResetCommandCursor();
        }

        private void OnDestroy()
        {
            SetInteractionHighlightedItem(null);
            ClearInventorySlotHoverFeedback();
            ResetCommandCursor();
            DestroyCommandCursorFrames(_shovelCursorFrames);
            DestroyCommandCursorFrames(_pickupCursorFrames);
            DestroyCommandCursorFrames(_dropCursorFrames);
            DestroyCommandCursorFrames(_movementCursorFrames);
            DestroyCommandCursorFrames(_axeCursorFrames);
            DestroyCommandCursorFrames(_swordCursorFrames);
            DestroyCommandCursorFrames(_eatCursorFrames);
            _shovelCursorFrames = null;
            _pickupCursorFrames = null;
            _dropCursorFrames = null;
            _movementCursorFrames = null;
            _axeCursorFrames = null;
            _swordCursorFrames = null;
            _eatCursorFrames = null;
        }

        private void ResetCommandCursor()
        {
            _movementCursorExpiresAt = 0f;
            _commandCursorKind = DirectCommandCursorKind.Default;
            _commandCursorFrame = -1;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        private void DestroyCommandCursorFrames(Texture2D[]? frames)
        {
            if (frames == null)
            {
                return;
            }

            for (int index = 0; index < frames.Length; index++)
            {
                if (frames[index] != null)
                {
                    Destroy(frames[index]);
                }
            }
        }
    }
}
