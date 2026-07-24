using System;
using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private const int ShovelCursorSize = 32;
        private static readonly Vector2 ShovelCursorHotspot = new Vector2(16f, 27f);
        private Texture2D? _shovelCursorTexture;
        private bool _shovelCursorApplied;

        private void UpdateSelectedResidentCommandCursor()
        {
            bool showShovel = IsInitialized()
                && _agentRenderer != null
                && _agentRenderer.SelectedCount > 0
                && _excavationMode == DigExcavationDrawingMode.None
                && !_buildingPlacementMode.HasValue
                && !_caveRoomPreset.HasValue
                && _hud != null
                && !_hud.ContainsScreenPoint(Input.mousePosition)
                && TryResolveExplicitExcavationHoverTarget(GetPointerHits());
            SetShovelCursor(showShovel);
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

                if (_jobRenderer!.TryGetJob(hit, out DigJobVisual job)
                    && !IsTerminalJobStatus(job.Model.Status)
                    && job.Model.TargetX.HasValue
                    && job.Model.TargetY.HasValue
                    && job.Model.TargetZ.HasValue
                    && job.Model.TargetZ.Value > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetShovelCursor(bool active)
        {
            if (_shovelCursorApplied == active)
            {
                return;
            }

            _shovelCursorApplied = active;
            if (!active)
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                return;
            }

            _shovelCursorTexture ??= CreateShovelCursorTexture();
            Cursor.SetCursor(
                _shovelCursorTexture,
                ShovelCursorHotspot,
                CursorMode.Auto);
        }

        private static Texture2D CreateShovelCursorTexture()
        {
            Texture2D texture = new Texture2D(
                ShovelCursorSize,
                ShovelCursorSize,
                TextureFormat.RGBA32,
                mipChain: false)
            {
                name = "Direct excavation shovel cursor",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            Color32[] pixels = new Color32[ShovelCursorSize * ShovelCursorSize];
            Color32 outline = new Color32(39, 31, 25, 255);
            Color32 handle = new Color32(139, 91, 52, 255);
            Color32 metal = new Color32(184, 193, 198, 255);
            Color32 highlight = new Color32(232, 238, 240, 255);

            FillRect(pixels, 12, 25, 20, 29, outline);
            FillRect(pixels, 14, 26, 18, 27, handle);
            FillRect(pixels, 15, 8, 17, 25, outline);
            FillRect(pixels, 16, 9, 16, 24, handle);
            for (int y = 2; y <= 9; y++)
            {
                int inset = Math.Max(0, 5 - y);
                FillRect(pixels, 9 + inset, y, 23 - inset, y, outline);
                if (y > 2)
                {
                    FillRect(pixels, 11 + inset, y, 21 - inset, y, metal);
                }
            }

            FillRect(pixels, 13, 7, 19, 8, outline);
            FillRect(pixels, 14, 7, 18, 7, metal);
            SetPixel(pixels, 18, 7, highlight);
            SetPixel(pixels, 19, 6, highlight);
            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            return texture;
        }

        private static void FillRect(
            Color32[] pixels,
            int minX,
            int minY,
            int maxX,
            int maxY,
            Color32 color)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    SetPixel(pixels, x, y, color);
                }
            }
        }

        private static void SetPixel(Color32[] pixels, int x, int y, Color32 color)
        {
            if (x < 0 || y < 0 || x >= ShovelCursorSize || y >= ShovelCursorSize)
            {
                return;
            }

            pixels[(y * ShovelCursorSize) + x] = color;
        }

        private void OnDisable()
        {
            SetShovelCursor(active: false);
        }

        private void OnDestroy()
        {
            SetShovelCursor(active: false);
            if (_shovelCursorTexture != null)
            {
                Destroy(_shovelCursorTexture);
                _shovelCursorTexture = null;
            }
        }
    }
}
