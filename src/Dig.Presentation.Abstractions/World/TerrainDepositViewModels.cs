using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Domain.World;

namespace Dig.Presentation.World
{

public enum TerrainDepositVisualState
{
    Hidden = 0,
    Revealed = 1,
    Damaged = 2,
    Depleted = 3,
}

[Flags]
public enum TerrainDepositConnection : byte
{
    None = 0,
    NegativeX = 1 << 0,
    PositiveX = 1 << 1,
    NegativeY = 1 << 2,
    PositiveY = 1 << 3,
    NegativeZ = 1 << 4,
    PositiveZ = 1 << 5,
}

public sealed class TerrainDepositCellViewModel
{
    public TerrainDepositCellViewModel(
        CellId cell,
        TerrainDepositVisualState state,
        string visibleDepositId,
        byte damageBand,
        TerrainDepositConnection connections,
        long sourceVersion)
    {
        if (visibleDepositId == null)
        {
            throw new ArgumentNullException(nameof(visibleDepositId));
        }

        if (damageBand > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(damageBand));
        }

        if (sourceVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceVersion));
        }

        bool visible = state == TerrainDepositVisualState.Revealed
            || state == TerrainDepositVisualState.Damaged;
        if (visible != !string.IsNullOrWhiteSpace(visibleDepositId))
        {
            throw new ArgumentException(
                "Only revealed or damaged deposits may expose a visual id.",
                nameof(visibleDepositId));
        }

        if (state != TerrainDepositVisualState.Damaged && damageBand != 0)
        {
            throw new ArgumentException(
                "Only damaged deposits may expose a damage band.",
                nameof(damageBand));
        }

        if (!visible && connections != TerrainDepositConnection.None)
        {
            throw new ArgumentException(
                "Hidden or depleted deposits cannot expose visual connections.",
                nameof(connections));
        }

        Cell = cell;
        State = state;
        VisibleDepositId = visibleDepositId;
        DamageBand = damageBand;
        Connections = connections;
        SourceVersion = sourceVersion;
    }

    public CellId Cell { get; }

    public TerrainDepositVisualState State { get; }

    public string VisibleDepositId { get; }

    public byte DamageBand { get; }

    public TerrainDepositConnection Connections { get; }

    public long SourceVersion { get; }

    public bool IsVisible =>
        State == TerrainDepositVisualState.Revealed
        || State == TerrainDepositVisualState.Damaged;
}

public sealed class TerrainDepositVolumeViewModel
{
    public TerrainDepositVolumeViewModel(
        int width,
        int height,
        int depth,
        long version,
        IReadOnlyCollection<TerrainDepositCellViewModel> cells)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        if (depth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(depth));
        }

        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        if (cells == null)
        {
            throw new ArgumentNullException(nameof(cells));
        }

        Width = width;
        Height = height;
        Depth = depth;
        Version = version;
        Cells = new ReadOnlyCollection<TerrainDepositCellViewModel>(
            new List<TerrainDepositCellViewModel>(cells));
    }

    public int Width { get; }

    public int Height { get; }

    public int Depth { get; }

    public long Version { get; }

    public IReadOnlyList<TerrainDepositCellViewModel> Cells { get; }

    public static TerrainDepositVolumeViewModel Empty(
        int width,
        int height,
        int depth)
    {
        return new TerrainDepositVolumeViewModel(
            width,
            height,
            depth,
            version: 0,
            Array.Empty<TerrainDepositCellViewModel>());
    }
}

}
