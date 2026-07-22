using System;
using System.Collections.Generic;
using Dig.Domain.World;

namespace Dig.Presentation.World
{

public sealed class TerrainDepositPresenter
{
    public TerrainDepositVolumeViewModel Present(
        int width,
        int height,
        int depth,
        IReadOnlyCollection<TerrainDepositPresentationInput> deposits)
    {
        ValidateDimensions(width, height, depth);
        if (deposits == null)
        {
            throw new ArgumentNullException(nameof(deposits));
        }

        Dictionary<CellId, Projection> projected =
            new Dictionary<CellId, Projection>();
        foreach (TerrainDepositPresentationInput source in deposits)
        {
            if (source == null)
            {
                throw new ArgumentException(
                    "Deposit presentation input cannot contain null.",
                    nameof(deposits));
            }

            ValidateCell(source.Cell, width, height, depth);
            if (projected.ContainsKey(source.Cell))
            {
                throw new ArgumentException(
                    $"Duplicate deposit cell '{source.Cell}'.",
                    nameof(deposits));
            }

            projected.Add(source.Cell, Project(source));
        }

        List<CellId> orderedCells = new List<CellId>(projected.Keys);
        orderedCells.Sort();
        List<TerrainDepositCellViewModel> cells =
            new List<TerrainDepositCellViewModel>(orderedCells.Count);
        for (int index = 0; index < orderedCells.Count; index++)
        {
            CellId cell = orderedCells[index];
            Projection value = projected[cell];
            TerrainDepositConnection connections = ResolveConnections(
                cell,
                value,
                projected);
            cells.Add(new TerrainDepositCellViewModel(
                cell,
                value.State,
                value.VisibleDepositId,
                value.DamageBand,
                connections,
                value.SourceVersion));
        }

        long version = CalculateVersion(width, height, depth, cells);
        return new TerrainDepositVolumeViewModel(
            width,
            height,
            depth,
            version,
            cells);
    }

    private static Projection Project(TerrainDepositPresentationInput source)
    {
        TerrainDepositVisualState state;
        string visibleDepositId = string.Empty;
        byte damageBand = 0;
        if (source.RemainingYield == 0)
        {
            state = TerrainDepositVisualState.Depleted;
        }
        else if (!source.IsRevealed)
        {
            state = TerrainDepositVisualState.Hidden;
        }
        else if (source.RemainingYield == source.MaximumYield)
        {
            state = TerrainDepositVisualState.Revealed;
            visibleDepositId = source.DepositId;
        }
        else
        {
            state = TerrainDepositVisualState.Damaged;
            visibleDepositId = source.DepositId;
            damageBand = CalculateDamageBand(
                source.RemainingYield,
                source.MaximumYield);
        }

        return new Projection(
            state,
            visibleDepositId,
            damageBand,
            source.Version);
    }

    private static byte CalculateDamageBand(int remaining, int maximum)
    {
        int consumed = maximum - remaining;
        int band = ((consumed * 3) + maximum - 1) / maximum;
        return (byte)Math.Max(1, Math.Min(3, band));
    }

    private static TerrainDepositConnection ResolveConnections(
        CellId cell,
        Projection value,
        IReadOnlyDictionary<CellId, Projection> deposits)
    {
        if (!value.IsVisible)
        {
            return TerrainDepositConnection.None;
        }

        TerrainDepositConnection result = TerrainDepositConnection.None;
        AddConnection(
            ref result,
            TerrainDepositConnection.NegativeX,
            new CellId(cell.X - 1, cell.Y, cell.Z),
            value.VisibleDepositId,
            deposits);
        AddConnection(
            ref result,
            TerrainDepositConnection.PositiveX,
            new CellId(cell.X + 1, cell.Y, cell.Z),
            value.VisibleDepositId,
            deposits);
        AddConnection(
            ref result,
            TerrainDepositConnection.NegativeY,
            new CellId(cell.X, cell.Y - 1, cell.Z),
            value.VisibleDepositId,
            deposits);
        AddConnection(
            ref result,
            TerrainDepositConnection.PositiveY,
            new CellId(cell.X, cell.Y + 1, cell.Z),
            value.VisibleDepositId,
            deposits);
        AddConnection(
            ref result,
            TerrainDepositConnection.NegativeZ,
            new CellId(cell.X, cell.Y, cell.Z - 1),
            value.VisibleDepositId,
            deposits);
        AddConnection(
            ref result,
            TerrainDepositConnection.PositiveZ,
            new CellId(cell.X, cell.Y, cell.Z + 1),
            value.VisibleDepositId,
            deposits);
        return result;
    }

    private static void AddConnection(
        ref TerrainDepositConnection connections,
        TerrainDepositConnection flag,
        CellId neighbour,
        string depositId,
        IReadOnlyDictionary<CellId, Projection> deposits)
    {
        if (deposits.TryGetValue(neighbour, out Projection? value)
            && value != null
            && value.IsVisible
            && string.Equals(
                depositId,
                value.VisibleDepositId,
                StringComparison.Ordinal))
        {
            connections |= flag;
        }
    }

    private static long CalculateVersion(
        int width,
        int height,
        int depth,
        IReadOnlyList<TerrainDepositCellViewModel> cells)
    {
        const ulong offset = 1469598103934665603UL;
        const ulong prime = 1099511628211UL;
        ulong hash = offset;
        Mix(ref hash, (ulong)(uint)width, prime);
        Mix(ref hash, (ulong)(uint)height, prime);
        Mix(ref hash, (ulong)(uint)depth, prime);
        for (int index = 0; index < cells.Count; index++)
        {
            TerrainDepositCellViewModel cell = cells[index];
            Mix(ref hash, (ulong)(uint)cell.Cell.X, prime);
            Mix(ref hash, (ulong)(uint)cell.Cell.Y, prime);
            Mix(ref hash, (ulong)(uint)cell.Cell.Z, prime);
            Mix(ref hash, (ulong)cell.State, prime);
            Mix(ref hash, cell.DamageBand, prime);
            Mix(ref hash, (byte)cell.Connections, prime);
            for (int character = 0;
                 character < cell.VisibleDepositId.Length;
                 character++)
            {
                Mix(ref hash, cell.VisibleDepositId[character], prime);
            }
        }

        return unchecked((long)(hash & (ulong)long.MaxValue));
    }

    private static void Mix(ref ulong hash, ulong value, ulong prime)
    {
        hash ^= value;
        hash *= prime;
    }

    private static void ValidateDimensions(int width, int height, int depth)
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
    }

    private static void ValidateCell(
        CellId cell,
        int width,
        int height,
        int depth)
    {
        if (cell.X < 0
            || cell.Y < 0
            || cell.Z < 0
            || cell.X >= width
            || cell.Y >= height
            || cell.Z >= depth)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cell),
                $"Deposit cell '{cell}' is outside the presentation volume.");
        }
    }

    private sealed class Projection
    {
        internal Projection(
            TerrainDepositVisualState state,
            string visibleDepositId,
            byte damageBand,
            long sourceVersion)
        {
            State = state;
            VisibleDepositId = visibleDepositId;
            DamageBand = damageBand;
            SourceVersion = sourceVersion;
        }

        internal TerrainDepositVisualState State { get; }

        internal string VisibleDepositId { get; }

        internal byte DamageBand { get; }

        internal long SourceVersion { get; }

        internal bool IsVisible =>
            State == TerrainDepositVisualState.Revealed
            || State == TerrainDepositVisualState.Damaged;
    }
}

}
