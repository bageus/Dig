using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dig.Application.World
{

public enum CaveRoomPresetKind
{
    Small = 0,
    Medium = 1,
    Large = 2,
    Tall = 3,
}

public sealed class CaveRoomPreset
{
    internal CaveRoomPreset(
        CaveRoomPresetKind kind,
        int baseWidth,
        int topWidth,
        int depth,
        int height)
    {
        Kind = kind;
        BaseWidth = baseWidth;
        TopWidth = topWidth;
        Depth = depth;
        Height = height;
    }

    public CaveRoomPresetKind Kind { get; }
    public int BaseWidth { get; }
    public int TopWidth { get; }
    public int Depth { get; }
    public int Height { get; }
}

public static class CaveRoomPresetCatalog
{
    private static readonly IReadOnlyDictionary<CaveRoomPresetKind, CaveRoomPreset> Presets =
        new ReadOnlyDictionary<CaveRoomPresetKind, CaveRoomPreset>(
            new Dictionary<CaveRoomPresetKind, CaveRoomPreset>
            {
                [CaveRoomPresetKind.Small] = new CaveRoomPreset(
                    CaveRoomPresetKind.Small, 5, 3, 3, 3),
                [CaveRoomPresetKind.Medium] = new CaveRoomPreset(
                    CaveRoomPresetKind.Medium, 7, 3, 4, 3),
                [CaveRoomPresetKind.Large] = new CaveRoomPreset(
                    CaveRoomPresetKind.Large, 9, 5, 4, 5),
                [CaveRoomPresetKind.Tall] = new CaveRoomPreset(
                    CaveRoomPresetKind.Tall, 8, 4, 4, 6),
            });

    public static CaveRoomPreset Get(CaveRoomPresetKind kind)
    {
        if (!Presets.TryGetValue(kind, out CaveRoomPreset? preset))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        return preset;
    }
}

}
