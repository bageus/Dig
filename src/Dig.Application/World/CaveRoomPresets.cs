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

public enum CaveRoomEntranceSide
{
    Left = 0,
    Right = 1,
}

public sealed class CaveRoomPreset
{
    internal CaveRoomPreset(
        CaveRoomPresetKind kind,
        string id,
        int version,
        string displayName,
        int baseWidth,
        int topWidth,
        int height,
        int depth,
        int requiredStoneworkUnits)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Template id is required.", nameof(id));
        }

        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Template display name is required.", nameof(displayName));
        }

        if (baseWidth <= 0 || topWidth <= 0 || topWidth > baseWidth)
        {
            throw new ArgumentOutOfRangeException(nameof(baseWidth));
        }

        if (height <= 0 || depth <= 0 || depth > 4)
        {
            throw new ArgumentOutOfRangeException(nameof(depth));
        }

        if (requiredStoneworkUnits < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredStoneworkUnits));
        }

        Kind = kind;
        Id = id.Trim();
        Version = version;
        DisplayName = displayName.Trim();
        BaseWidth = baseWidth;
        TopWidth = topWidth;
        Height = height;
        Depth = depth;
        RequiredStoneworkUnits = requiredStoneworkUnits;
    }

    public CaveRoomPresetKind Kind { get; }
    public string Id { get; }
    public int Version { get; }
    public string DisplayName { get; }
    public int BaseWidth { get; }
    public int TopWidth { get; }
    public int Height { get; }
    public int Depth { get; }
    public int RequiredStoneworkUnits { get; }
    public bool AllowsMirror => false;
    public string PassageAxis => "X";

    public int GetEntranceOffsetX(CaveRoomEntranceSide side)
    {
        return side == CaveRoomEntranceSide.Left
            ? 0
            : BaseWidth - 1;
    }
}

public static class CaveRoomPresetCatalog
{
    private static readonly IReadOnlyDictionary<CaveRoomPresetKind, CaveRoomPreset> Presets =
        new ReadOnlyDictionary<CaveRoomPresetKind, CaveRoomPreset>(
            new Dictionary<CaveRoomPresetKind, CaveRoomPreset>
            {
                [CaveRoomPresetKind.Small] = new CaveRoomPreset(
                    CaveRoomPresetKind.Small,
                    "excavation.template.cave.small",
                    1,
                    "Малая",
                    5,
                    3,
                    3,
                    2,
                    0),
                [CaveRoomPresetKind.Medium] = new CaveRoomPreset(
                    CaveRoomPresetKind.Medium,
                    "excavation.template.cave.medium",
                    1,
                    "Средняя",
                    8,
                    6,
                    3,
                    3,
                    2_000),
                [CaveRoomPresetKind.Large] = new CaveRoomPreset(
                    CaveRoomPresetKind.Large,
                    "excavation.template.cave.large",
                    1,
                    "Большая",
                    12,
                    8,
                    5,
                    4,
                    4_000),
                [CaveRoomPresetKind.Tall] = new CaveRoomPreset(
                    CaveRoomPresetKind.Tall,
                    "excavation.template.cave.tall",
                    1,
                    "Высокая",
                    10,
                    6,
                    7,
                    4,
                    6_000),
            });

    public static IReadOnlyCollection<CaveRoomPreset> Definitions =>
        new ReadOnlyCollection<CaveRoomPreset>(
            new List<CaveRoomPreset>
            {
                Presets[CaveRoomPresetKind.Small],
                Presets[CaveRoomPresetKind.Medium],
                Presets[CaveRoomPresetKind.Large],
                Presets[CaveRoomPresetKind.Tall],
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
