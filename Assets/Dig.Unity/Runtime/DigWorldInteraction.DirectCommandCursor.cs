using System;
using Dig.Domain.World;
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
        private static readonly Vector2 UseCursorHotspot = new Vector2(16f, 16f);

        private Texture2D[]? _shovelCursorFrames;
        private Texture2D[]? _pickupCursorFrames;
        private Texture2D[]? _dropCursorFrames;
        private Texture2D[]? _movementCursorFrames;
        private Texture2D[]? _axeCursorFrames;
        private Texture2D[]? _eatCursorFrames;
        private Texture2D[]? _swordCursorFrames;
        private Texture2D[]? _useCursorFrames;
        private DirectCommandCursorKind _commandCursorKind;
        private int _commandCursorFrame = -1;
        private float _commandCursorAnimationStartedAt;
        private float _movementCursorExpiresAt;

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
            Use = 8,
        }

        private void UpdateSelectedResidentCommandCursor()
        {
            _hud?.ClearWorldTargetHoverInfo();
            _barrelRenderer?.SetHighlighted(null);
            _creatureRenderer?.ClearHighlight();
            SetInteractionHighlightedItem(null);
            DirectCommandCursorKind kind = ResolveCommandCursorKind();
            ApplyCommandCursor(kind);
        }

        private DirectCommandCursorKind ResolveCommandCursorKind()
        {
            if (!IsInitialized() || _hud == null || _buildingPlacementMode.HasValue)
            {
                return DirectCommandCursorKind.Default;
            }

            if (_hud.ContainsScreenPoint(Input.mousePosition))
            {
                return ResolveInventoryHoverCursorKind();
            }

            RaycastHit[] hits = GetPointerHits();
            if (TryResolveCompletedBuildingHit(
                hits,
                out DigBuildingVisual hoveredBuilding))
            {
                SetBuildingTargetHoverInfo(hoveredBuilding);
            }

            TryHighlightHostileCreature(hits);
            if (TryResolveBarrelHit(hits, out DigBarrelVisual hoveredBarrel))
            {
                _barrelRenderer!.SetHighlighted(hoveredBarrel.Model.BarrelId);
            }
            if (TryResolveVukerKidnapHoverTarget(hits))
            {
                return DirectCommandCursorKind.Pickup;
            }
            if (Time.unscaledTime < _movementCursorExpiresAt)
            {
                return DirectCommandCursorKind.Movement;
            }
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
                    && TryResolveHostileCombatHoverTarget(hits))
                {
                    return DirectCommandCursorKind.Sword;
                }

                if (_excavationMode == DigExcavationDrawingMode.None
                    && !_caveRoomPreset.HasValue
                    && TryResolveBarrelHoverTarget(hits))
                {
                    return DirectCommandCursorKind.Sword;
                }

                if (TryResolveWorldItemPointerTarget(
                        hits,
                        IsAltPressed(),
                        out ResolvedWorldItemPointerTarget itemTarget)
                    && itemTarget.ActionAvailable)
                {
                    _hud.SetWorldTargetHoverInfo(itemTarget.Item.Model.DisplayName);
                    SetInteractionHighlightedItem(itemTarget.Item);
                    return itemTarget.Action switch
                    {
                        Dig.Domain.Inventory.ItemWorldInteractionAction.Pickup =>
                            DirectCommandCursorKind.Pickup,
                        Dig.Domain.Inventory.ItemWorldInteractionAction.DirectUse =>
                            itemTarget.Item.Model.InteractionProfile.DirectUseFeedback
                                == Dig.Domain.Inventory.ItemInteractionFeedbackKind.Eat
                                    ? DirectCommandCursorKind.Eat
                                    : DirectCommandCursorKind.Use,
                        Dig.Domain.Inventory.ItemWorldInteractionAction.UseProductionPackage =>
                            DirectCommandCursorKind.Use,
                        _ => DirectCommandCursorKind.Default,
                    };
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
                case DirectCommandCursorKind.Use:
                    return _useCursorFrames ??= CreateUseCursorFrames();
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
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
                DirectCommandCursorKind.Use => UseCursorHotspot,
                _ => Vector2.zero,
            };
        }

        private void OnDisable()
        {
            ClearPointerHover();
            _creatureRenderer?.ClearHighlight();
            SetInteractionHighlightedItem(null);
            ClearInventorySlotHoverFeedback();
            _hud?.ClearWorldTargetHoverInfo();
            ResetCommandCursor();
        }

        private void OnDestroy()
        {
            _creatureRenderer?.ClearHighlight();
            SetInteractionHighlightedItem(null);
            ClearInventorySlotHoverFeedback();
            _hud?.ClearWorldTargetHoverInfo();
            ResetCommandCursor();
            DestroyCommandCursorFrames(_shovelCursorFrames);
            DestroyCommandCursorFrames(_pickupCursorFrames);
            DestroyCommandCursorFrames(_dropCursorFrames);
            DestroyCommandCursorFrames(_movementCursorFrames);
            DestroyCommandCursorFrames(_axeCursorFrames);
            DestroyCommandCursorFrames(_eatCursorFrames);
            DestroyCommandCursorFrames(_swordCursorFrames);
            DestroyCommandCursorFrames(_useCursorFrames);
            _shovelCursorFrames = null;
            _pickupCursorFrames = null;
            _dropCursorFrames = null;
            _movementCursorFrames = null;
            _axeCursorFrames = null;
            _eatCursorFrames = null;
            _swordCursorFrames = null;
            _useCursorFrames = null;
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
