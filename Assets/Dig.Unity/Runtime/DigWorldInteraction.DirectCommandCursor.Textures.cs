using System;
using UnityEngine;

namespace Dig.Unity
{

public sealed partial class DigWorldInteraction
{
    private const int CommandCursorSize = 32;

    private static Texture2D[] CreateShovelCursorFrames() =>
        CreateToolFrames("Shovel", DrawShovel);

    private static Texture2D[] CreatePickupCursorFrames() =>
        CreateToolFrames("Pickup", DrawPickupArrow);

    private static Texture2D[] CreateMovementCursorFrames() =>
        CreateToolFrames("Movement", DrawWalkingFeet);

    private static Texture2D[] CreateAxeCursorFrames() =>
        CreateToolFrames("Axe", DrawAxe);

    private static Texture2D[] CreateSwordCursorFrames() =>
        CreateToolFrames("Sword", DrawSword);

    private static Texture2D[] CreateUseCursorFrames() =>
        CreateToolFrames("Use", DrawUseHand);

    private static Texture2D[] CreateToolFrames(
        string name,
        Action<Color32[], int> draw)
    {
        Texture2D[] frames = new Texture2D[4];
        for (int index = 0; index < frames.Length; index++)
        {
            Color32[] pixels = new Color32[CommandCursorSize * CommandCursorSize];
            draw(pixels, index);
            frames[index] = CreateCursorTexture($"{name} cursor {index}", pixels);
        }

        return frames;
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

    private static void DrawShovel(Color32[] pixels, int phase)
    {
        int offset = phase == 0 ? -1 : phase == 2 ? 1 : 0;
        Color32 outline = new Color32(39, 31, 25, 255);
        Color32 handle = new Color32(139, 91, 52, 255);
        Color32 metal = new Color32(184, 193, 198, 255);
        FillRect(pixels, 15 + offset, 8, 17 + offset, 25, outline);
        FillRect(pixels, 16 + offset, 9, 16 + offset, 24, handle);
        FillRect(pixels, 10 + offset, 2, 22 + offset, 8, outline);
        FillRect(pixels, 12 + offset, 3, 20 + offset, 7, metal);
    }

    private static void DrawPickupArrow(Color32[] pixels, int phase)
    {
        int rise = phase == 1 ? 2 : phase == 2 ? 3 : phase == 3 ? 1 : 0;
        Color32 outline = new Color32(28, 35, 43, 255);
        Color32 fill = new Color32(128, 210, 244, 255);
        FillRect(pixels, 14, 5 + rise, 18, 21 + rise, outline);
        FillRect(pixels, 15, 6 + rise, 17, 20 + rise, fill);
        for (int row = 0; row < 7; row++)
        {
            FillRect(pixels, 9 + row, 20 + rise + row, 23 - row, 20 + rise + row, outline);
            if (row > 0 && row < 6)
            {
                FillRect(pixels, 11 + row, 20 + rise + row, 21 - row, 20 + rise + row, fill);
            }
        }
    }

    private static void DrawWalkingFeet(Color32[] pixels, int phase)
    {
        Color32 outline = new Color32(43, 32, 26, 255);
        Color32 leather = new Color32(151, 92, 48, 255);
        bool leftForward = phase % 2 == 0;
        int bounce = phase == 1 || phase == 3 ? 1 : 0;
        DrawBoot(pixels, leftForward ? 7 : 10, leftForward ? 13 + bounce : 8 + bounce, false, outline, leather);
        DrawBoot(pixels, leftForward ? 19 : 16, leftForward ? 8 + bounce : 13 + bounce, true, outline, leather);
    }

    private static void DrawAxe(Color32[] pixels, int phase)
    {
        int offset = phase == 0 ? -2 : phase == 2 ? 2 : 0;
        Color32 outline = new Color32(38, 29, 23, 255);
        Color32 handle = new Color32(143, 87, 43, 255);
        Color32 metal = new Color32(184, 194, 201, 255);
        DrawDiagonal(pixels, 8 + offset, 5, 19, outline, handle);
        FillRect(pixels, 14 + offset, 20, 27 + offset, 27, outline);
        FillRect(pixels, 16 + offset, 21, 25 + offset, 26, metal);
    }

    private static void DrawUseHand(Color32[] pixels, int phase)
    {
        int pulse = phase == 1 ? 1 : phase == 2 ? 2 : 0;
        Color32 outline = new Color32(37, 30, 24, 255);
        Color32 skin = new Color32(224, 177, 125, 255);
        Color32 spark = new Color32(255, 230, 116, 255);
        FillRect(pixels, 10, 6 + pulse, 21, 16 + pulse, outline);
        FillRect(pixels, 11, 7 + pulse, 20, 15 + pulse, skin);
        FillRect(pixels, 8, 13 + pulse, 22, 22 + pulse, outline);
        FillRect(pixels, 9, 14 + pulse, 21, 21 + pulse, skin);
        FillRect(pixels, 15, 22 + pulse, 18, 28 + pulse, outline);
        FillRect(pixels, 16, 22 + pulse, 17, 27 + pulse, skin);
        int sparkOffset = phase % 2;
        SetPixel(pixels, 25, 22 + sparkOffset, spark);
        SetPixel(pixels, 24, 22 + sparkOffset, spark);
        SetPixel(pixels, 26, 22 + sparkOffset, spark);
        SetPixel(pixels, 25, 21 + sparkOffset, spark);
        SetPixel(pixels, 25, 23 + sparkOffset, spark);
    }

    private static void DrawSword(Color32[] pixels, int phase)
    {
        int offset = phase == 0 ? -2 : phase == 2 ? 2 : 0;
        Color32 outline = new Color32(28, 30, 35, 255);
        Color32 blade = new Color32(204, 214, 222, 255);
        Color32 grip = new Color32(124, 72, 38, 255);
        DrawDiagonal(pixels, 7 + offset, 5, 20, outline, blade);
        FillRect(pixels, 5 + offset, 3, 13 + offset, 6, outline);
        FillRect(pixels, 7 + offset, 4, 12 + offset, 5, grip);
        FillRect(pixels, 3 + offset, 1, 7 + offset, 4, outline);
        FillRect(pixels, 4 + offset, 2, 6 + offset, 3, grip);
    }

    private static void DrawDiagonal(
        Color32[] pixels,
        int startX,
        int startY,
        int length,
        Color32 outline,
        Color32 fill)
    {
        for (int step = 0; step < length; step++)
        {
            int x = startX + step;
            int y = startY + step;
            FillRect(pixels, x - 1, y - 1, x + 1, y + 1, outline);
            SetPixel(pixels, x, y, fill);
        }
    }

    private static void DrawBoot(
        Color32[] pixels,
        int x,
        int y,
        bool mirror,
        Color32 outline,
        Color32 leather)
    {
        FillRect(pixels, x, y + 4, x + 5, y + 12, outline);
        FillRect(pixels, x + 1, y + 5, x + 4, y + 11, leather);
        int toeMin = mirror ? x - 3 : x + 2;
        int toeMax = mirror ? x + 3 : x + 8;
        FillRect(pixels, toeMin, y, toeMax, y + 5, outline);
        FillRect(pixels, toeMin + 1, y + 1, toeMax - 1, y + 4, leather);
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