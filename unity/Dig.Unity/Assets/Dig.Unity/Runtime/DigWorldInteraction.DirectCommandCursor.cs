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
        private static readonly Vector2 MovementCursorHotspot = new Vector2(16f, 27f);

        private Texture2D[]? _shovelCursorFrames;
        private Texture2D[]? _pickupCursorFrames;
        private Texture2D[]? _movementCursorFrames;
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
        }

        private void UpdateSelectedResidentCommandCursor()
        {
            DirectCommandCursorKind kind = ResolveCommandCursorKind();
            ApplyCommandCursor(kind);
        }

        private DirectCommandCursorKind ResolveCommandCursorKind()
        {
            if (Time.unscaledTime < _movementCursorExpiresAt)
            {
                return DirectCommandCursorKind.Movement;
            }

            if (!IsInitialized()
                || _hud == null
                || _hud.ContainsScreenPoint(Input.mousePosition)
                || _buildingPlacementMode.HasValue)
            {
                return DirectCommandCursorKind.Default;
            }

            RaycastHit[] hits = GetPointerHits();
            if (_agentRenderer != null && _agentRenderer.SelectedCount > 0)
            {
                if (_excavationMode == DigExcavationDrawingMode.None
                    && !_caveRoomPreset.HasValue
                    && IsAltPressed()
                    && TryResolveBuildingBoxHoverTarget(hits))
                {
                    return DirectCommandCursorKind.Pickup;
                }

                if (_excavationMode == DigExcavationDrawingMode.None
                    && !_caveRoomPreset.HasValue
                    && TryResolvePickableItemHoverTarget(hits))
                {
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

        private bool TryResolveBuildingBoxHoverTarget(RaycastHit[] hits)
        {
            return TryResolveBuildingBoxHit(hits, out DigWorldItemVisual item)
                && item.Model.AvailableQuantity == 1;
        }

        private bool TryResolvePickableItemHoverTarget(RaycastHit[] hits)
        {
            return TryResolveWorldItemHit(hits, out DigWorldItemVisual item)
                && item.Model.CanPickup;
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
            if (_commandCursorKind != kind)
            {
                BeginCommandCursorAnimation(kind);
            }

            if (kind == DirectCommandCursorKind.Default)
            {
                if (_commandCursorFrame != -1)
                {
                    Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                    _commandCursorFrame = -1;
                }

                return;
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
                case DirectCommandCursorKind.Movement:
                    return _movementCursorFrames ??= CreateMovementCursorFrames();
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
                DirectCommandCursorKind.Movement => MovementCursorHotspot,
                _ => Vector2.zero,
            };
        }

        private void OnDisable()
        {
            ResetCommandCursor();
        }

        private void OnDestroy()
        {
            ResetCommandCursor();
            DestroyCommandCursorFrames(_shovelCursorFrames);
            DestroyCommandCursorFrames(_pickupCursorFrames);
            DestroyCommandCursorFrames(_movementCursorFrames);
            _shovelCursorFrames = null;
            _pickupCursorFrames = null;
            _movementCursorFrames = null;
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
