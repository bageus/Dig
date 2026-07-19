using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainChunkRenderer
    {
        private static Color ResolveDepositFallbackColor(
            DigTerrainMaterialKey key)
        {
            unchecked
            {
                uint hash = 2166136261u;
                for (int index = 0; index < key.DepositId.Length; index++)
                {
                    hash ^= key.DepositId[index];
                    hash *= 16777619u;
                }

                float hue = (hash & 1023u) / 1023f;
                float value = key.DepositState == TerrainDepositVisualState.Damaged
                    ? 0.58f - (key.DepositDamageBand * 0.06f)
                    : 0.76f;
                int links = CountConnectionBits((byte)key.DepositConnections);
                value = Mathf.Clamp01(value + (links * 0.015f));
                return Color.HSVToRGB(hue, 0.58f, value);
            }
        }

        private static int CountConnectionBits(byte value)
        {
            int count = 0;
            while (value != 0)
            {
                count += value & 1;
                value >>= 1;
            }

            return count;
        }
    }
}
