using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dig.Domain.World
{

public sealed class TerrainDepositState
{
    private readonly Dictionary<CellId, TerrainDepositInstance> _byCell =
        new Dictionary<CellId, TerrainDepositInstance>();

    public IReadOnlyList<TerrainDepositInstance> Snapshot()
    {
        return new ReadOnlyCollection<TerrainDepositInstance>(
            _byCell.Values.OrderBy(value => value.Cell).ToArray());
    }

    public TerrainDepositSaveSnapshot CaptureSaveSnapshot(int generatorVersion)
    {
        TerrainDepositSaveEntry[] deposits = _byCell.Values
            .OrderBy(value => value.Cell)
            .Select(value => new TerrainDepositSaveEntry(
                value.InstanceId,
                value.Definition.Id,
                value.Cell,
                value.IsRevealed,
                value.RemainingYield,
                value.Version))
            .ToArray();
        return new TerrainDepositSaveSnapshot(
            TerrainDepositSaveSnapshot.CurrentFormatVersion,
            generatorVersion,
            deposits);
    }

    public void RestoreSaveSnapshot(
        TerrainDepositSaveSnapshot snapshot,
        TerrainDepositCatalog catalog)
    {
        if (snapshot == null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (catalog == null)
        {
            throw new ArgumentNullException(nameof(catalog));
        }

        if (snapshot.FormatVersion != TerrainDepositSaveSnapshot.CurrentFormatVersion)
        {
            throw new InvalidOperationException(
                $"Unsupported terrain deposit save format version {snapshot.FormatVersion}.");
        }

        TerrainDepositInstance[] restored = new TerrainDepositInstance[snapshot.Deposits.Count];
        for (int index = 0; index < snapshot.Deposits.Count; index++)
        {
            TerrainDepositSaveEntry entry = snapshot.Deposits[index];
            TerrainDepositDefinition? definition = catalog.Get(entry.DefinitionId);
            if (definition == null)
            {
                throw new InvalidOperationException(
                    $"Unknown terrain deposit definition '{entry.DefinitionId}' "
                    + $"for instance '{entry.InstanceId}'.");
            }

            restored[index] = new TerrainDepositInstance(
                entry.InstanceId,
                entry.Cell,
                definition,
                entry.IsRevealed,
                entry.RemainingYield,
                entry.Version);
        }

        ReplaceAll(restored);
    }

    public void ReplaceAll(IEnumerable<TerrainDepositInstance> deposits)
    {
        if (deposits == null)
        {
            throw new ArgumentNullException(nameof(deposits));
        }

        TerrainDepositInstance[] values = deposits.ToArray();
        if (values.Any(value => value == null))
        {
            throw new ArgumentException(
                "Deposit collection cannot contain null values.",
                nameof(deposits));
        }

        if (values.Select(value => value.Cell).Distinct().Count() != values.Length)
        {
            throw new ArgumentException(
                "Only one authoritative deposit may occupy a world cell.",
                nameof(deposits));
        }

        if (values.Select(value => value.InstanceId)
            .Distinct(StringComparer.Ordinal)
            .Count() != values.Length)
        {
            throw new ArgumentException(
                "Deposit instance ids must be unique.",
                nameof(deposits));
        }

        _byCell.Clear();
        for (int index = 0; index < values.Length; index++)
        {
            _byCell.Add(values[index].Cell, values[index]);
        }
    }

    public bool TryGet(CellId cell, out TerrainDepositInstance deposit)
    {
        if (_byCell.TryGetValue(cell, out TerrainDepositInstance? value))
        {
            deposit = value;
            return true;
        }

        deposit = null!;
        return false;
    }

    public bool Reveal(CellId cell, long version)
    {
        if (!_byCell.TryGetValue(cell, out TerrainDepositInstance? current)
            || current.IsRevealed)
        {
            return false;
        }

        _byCell[cell] = current.Reveal(Math.Max(current.Version + 1, version));
        return true;
    }

    public int RevealAdjacentTo(CellId excavatedCell, long version)
    {
        int revealed = 0;
        CellId[] neighbors = CreateNeighbors(excavatedCell);
        for (int index = 0; index < neighbors.Length; index++)
        {
            if (Reveal(neighbors[index], version))
            {
                revealed++;
            }
        }

        return revealed;
    }

    public bool Deplete(CellId cell, long version)
    {
        if (!_byCell.TryGetValue(cell, out TerrainDepositInstance? current)
            || current.IsDepleted)
        {
            return false;
        }

        _byCell[cell] = current.Deplete(Math.Max(current.Version + 1, version));
        return true;
    }

    private static CellId[] CreateNeighbors(CellId cell)
    {
        List<CellId> neighbors = new List<CellId>(6)
        {
            new CellId(cell.X - 1, cell.Y, cell.Z),
            new CellId(cell.X + 1, cell.Y, cell.Z),
            new CellId(cell.X, cell.Y - 1, cell.Z),
            new CellId(cell.X, cell.Y + 1, cell.Z),
        };
        if (cell.Z > CellId.MinimumDepth)
        {
            neighbors.Add(new CellId(cell.X, cell.Y, cell.Z - 1));
        }

        if (cell.Z < CellId.MaximumDepth)
        {
            neighbors.Add(new CellId(cell.X, cell.Y, cell.Z + 1));
        }

        return neighbors.OrderBy(value => value).ToArray();
    }
}

}