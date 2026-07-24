using System;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private const int CommandCursorSize = 32;

        private static Texture2D[] CreateShovelCursorFrames()
        {
            Texture2D[] frames = new Texture2D[4];
            int[] offsets = { -1, 0, 1, 0 };
            for (int index = 0; index < frames.Length; index++)
            {
                Color32[] pixels = NewCursorPixels();
                DrawShovel(pixels, offsets[index], index);
                frames[index] = CreateCursorTexture(
                    $"Direct excavation shovel cursor {index}",
                    pixels);
            }

            return frames;
        }

        private static Texture2D[] CreatePickupCursorFrames()
        {
            Texture2D[] frames = new Texture2D[4];
            int[] rises = { 0, 1, 3, 1 };
            for (int index = 0; index < frames.Length; index++)
            {
                Color32[] pixels = NewCursorPixels();
                DrawPickupArrow(pixels, rises[index], index);
                frames[index] = CreateCursorTexture(
                    $"Pickup arrow cursor {index}",
                    pixels);
            }

            return frames;
        }

        private static Texture2D[] CreateMovementCursorFrames()
        {
            Texture2D[] frames = new Texture2D[4];
            for (int index = 0; index < frames.Length; index++)
            {
                Color32[] pixels = NewCursorPixels();
                DrawWalkingFeet(pixels, index);
                frames[index] = CreateCursorTexture(
                    $"Movement feet cursor {index}",
                    pixels);
            }

            return frames;
        }

        private static Color32[] NewCursorPixels()
        {
            return new Color32[CommandCursorSize * CommandCursorSize];
        }

        private static Texture2D CreateCursorTexture(string name, Color32[] pixels)
        {
            Texture2D texture = new Texture2D(
                CommandCursorSize,
                CommandCursorSize,
                TextureFormat.RGBA32,
                mipChain: false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            return texture;
        }

        private static void DrawShovel(Color32[] pixels, int offsetX, int phase)
        {
            Color32 outline = new Color32(39, 31, 25, 255);
            Color32 handle = new Color32(139, 91, 52, 255);
            Color32 metal = new Color32(184, 193, 198, 255);
            Color32 highlight = new Color32(232, 238, 240, 255);
            int x = offsetX;

            FillRect(pixels, 12 + x, 25, 20 + x, 29, outline);
            FillRect(pixels, 14 + x, 26, 18 + x, 27, handle);
            FillRect(pixels, 15 + x, 8, 17 + x, 25, outline);
            FillRect(pixels, 16 + x, 9, 16 + x, 24, handle);
            for (int y = 2; y <= 9; y++)
            {
                int inset = Math.Max(0, 5 - y);
                FillRect(pixels, 9 + inset + x, y, 23 - inset + x, y, outline);
                if (y > 2)
                {
                    FillRect(pixels, 11 + inset + x, y, 21 - inset + x, y, metal);
                }
            }

            FillRect(pixels, 13 + x, 7, 19 + x, 8, outline);
            FillRect(pixels, 14 + x, 7, 18 + x, 7, metal);
            int sparkX = phase % 2 == 0 ? 22 + x : 20 + x;
            int sparkY = phase < 2 ? 8 : 5;
            SetPixel(pixels, sparkX, sparkY, highlight);
            SetPixel(pixels, sparkX - 1, sparkY, highlight);
            SetPixel(pixels, sparkX, sparkY + 1, highlight);
        }

        private static void DrawPickupArrow(Color32[] pixels, int rise, int phase)
        {
            Color32 outline = new Color32(28, 35, 43, 255);
            Color32 fill = new Color32(128, 210, 244, 255);
            Color32 highlight = new Color32(223, 248, 255, 255);
            int baseY = 5 + rise;

            FillRect(pixels, 14, baseY, 18, baseY + 15, outline);
            FillRect(pixels, 15, baseY + 1, 17, baseY + 14, fill);
            for (int row = 0; row < 7; row++)
            {
                FillRect(
                    pixels,
                    9 + row,
                    baseY + 14 + row,
                    23 - row,
                    baseY + 14 + row,
                    outline);
                if (row > 0 && row < 6)
                {
                    FillRect(
                        pixels,
                        11 + row,
                        baseY + 14 + row,
                        21 - row,
                        baseY + 14 + row,
                        fill);
                }
            }

            SetPixel(pixels, 16, baseY + 18, highlight);
            int trailY = Math.Max(1, baseY - 2 - (phase % 2));
            FillRect(pixels, 15, trailY, 17, trailY, new Color32(128, 210, 244, 150));
        }

        private static void DrawWalkingFeet(Color32[] pixels, int phase)
        {
            Color32 outline = new Color32(43, 32, 26, 255);
            Color32 leather = new Color32(151, 92, 48, 255);
            Color32 sole = new Color32(77, 58, 45, 255);
            bool leftForward = phase % 2 == 0;
            int bounce = phase == 1 || phase == 3 ? 1 : 0;

            DrawBoot(
                pixels,
                leftForward ? 8 : 10,
                leftForward ? 12 + bounce : 7 + bounce,
                mirror: false,
                outline,
                leather,
                sole);
            DrawBoot(
                pixels,
                leftForward ? 18 : 16,
                leftForward ? 7 + bounce : 12 + bounce,
                mirror: true,
                outline,
                leather,
                sole);
        }

        private static void DrawBoot(
            Color32[] pixels,
            int x,
            int y,
            bool mirror,
            Color32 outline,
            Color32 leather,
            Color32 sole)
        {
            FillRect(pixels, x, y + 5, x + 5, y + 13, outline);
            FillRect(pixels, x + 1, y + 6, x + 4, y + 12, leather);
            int toeMin = mirror ? x - 3 : x + 2;
            int toeMax = mirror ? x + 3 : x + 8;
            FillRect(pixels, toeMin, y + 1, toeMax, y + 6, outline);
            FillRect(pixels, toeMin + 1, y + 2, toeMax - 1, y + 5, leather);
            FillRect(pixels, toeMin, y, toeMax, y + 1, sole);
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
            if (x < 0 || y < 0 || x >= CommandCursorSize || y >= CommandCursorSize)
            {
                return;
            }

            pixels[(y * CommandCursorSize) + x] = color;
        }
    }
}
