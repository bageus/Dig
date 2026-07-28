using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private static Texture2D[] CreateEatCursorFrames()
        {
            Texture2D[] frames = new Texture2D[4];
            int[] openings = { 2, 5, 8, 5 };
            for (int index = 0; index < frames.Length; index++)
            {
                Color32[] pixels = new Color32[CommandCursorSize * CommandCursorSize];
                DrawEatingMouth(pixels, openings[index], index);
                frames[index] = CreateCursorTexture(
                    $"Direct food mouth cursor {index}",
                    pixels);
            }

            return frames;
        }

        private static void DrawEatingMouth(
            Color32[] pixels,
            int opening,
            int phase)
        {
            Color32 outline = new Color32(18, 64, 28, 255);
            Color32 lip = new Color32(55, 205, 87, 255);
            Color32 highlight = new Color32(184, 255, 197, 255);
            Color32 inside = new Color32(7, 35, 13, 255);
            int centreY = 15;
            int halfOpening = opening / 2;

            for (int x = 6; x <= 26; x++)
            {
                int distance = x < 16 ? 16 - x : x - 16;
                int curve = distance / 3;
                int upper = centreY + halfOpening + curve;
                int lower = centreY - halfOpening - curve;
                SetPixel(pixels, x, upper + 1, outline);
                SetPixel(pixels, x, upper, lip);
                SetPixel(pixels, x, lower - 1, outline);
                SetPixel(pixels, x, lower, lip);
                if (x > 7 && x < 25 && upper - lower > 2)
                {
                    FillRect(pixels, x, lower + 1, x, upper - 1, inside);
                }
            }

            int biteX = phase % 2 == 0 ? 24 : 27;
            int biteY = phase < 2 ? 22 : 19;
            FillRect(pixels, biteX, biteY, biteX + 2, biteY + 2, highlight);
            SetPixel(pixels, 10, centreY + halfOpening + 2, highlight);
        }
    }
}
