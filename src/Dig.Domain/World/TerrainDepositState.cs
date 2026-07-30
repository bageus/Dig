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

    public TerrainDepositState(int generatorVersion = 1)
    {
        if (generatorVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(generatorVersion));
        }

        GeneratorVersion = generatorVersion;
    }

    public int GeneratorVersion { get; private set; }

    public IReadOnlyList<TerrainDepositInstance> Snapshot()
    {
        return new ReadOnlyCollection<TerrainDepositInstance>(
            _byCell.Values.OrderBy(value => value.Cell).ToArray());
    }

    public TerrainDepositSaveSnapshot CaptureSaveSnapshot()
    {
        TerrainDepositSaveEntry[] deposits = _byCell.Values
            .OrderBy(value => value.Cell)
            .Select(value => new TerrainDepositSaveEntry(
                value.InstanceId,
                value.Definition.Id,
                value.DefinitionVersion,
                value.Cell,
                value.IsRevealed,
                value.RemainingYield,
                value.Version))
            .ToArray();
        return new TerrainDepositSaveSnapshot(
            TerrainDepositSaveSnapshot.CurrentFormatVersion,
            GeneratorVersion,
            deposits);
    }

    public TerrainDepositSaveSnapshot CaptureSaveSnapshot(int generatorVersion)
    {
        if (generatorVersion != GeneratorVersion)
        {
            throw new InvalidOperationException(
                "The requested generator version does not match authoritative deposit state.");
        }

        return CaptureSaveSnapshot();
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

            if (definition.Version != entry.DefinitionVersion)
            {
                throw new InvalidOperationException(
                    $"Terrain deposit definition '{entry.DefinitionId}' version "
                    + $"{entry.DefinitionVersion} is unavailable; current version is "
                    + $"{definition.Version}.");
            }

            restored[index] = new TerrainDepositInstance(
                entry.InstanceId,
                entry.Cell,
                definition,
                entry.IsRevealed,
                entry.RemainingYield,
                entry.Version);
        }

        ReplaceAll(restored, snapshot.GeneratorVersion);
    }

    public void ReplaceAll(
        IEnumerable<TerrainDepositInstance> deposits,
        int generatorVersion = 1)
    {
        if (deposits == null)
        {
            throw new ArgumentNullException(nameof(deposits));
        }

        if (generatorVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(generatorVersion));
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

        GeneratorVersion = generatorVersion;
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
        return TryReveal(cell, version, out _);
    }

    public bool TryReveal(
        CellId cell,
        long version,
        out TerrainDepositChange change)
    {
        if (!_byCell.TryGetValue(cell, out TerrainDepositInstance? current)
            || current.IsRevealed
            || current.IsDepleted)
        {
            change = null!;
            return false;
        }

        TerrainDepositInstance revealed = current.Reveal(
            Math.Max(current.Version + 1, version));
        _byCell[cell] = revealed;
        change = CreateChange(TerrainDepositChangeKind.Revealed, revealed);
        return true;
    }

    public int RevealAdjacentTo(CellId excavatedCell, long version)
    {
        return RevealAdjacentToChanges(excavatedCell, version).Count;
    }

    public IReadOnlyList<TerrainDepositChange> RevealAdjacentToChanges(
        CellId excavatedCell,
        long version)
    {
        List<TerrainDepositChange> changes = new List<TerrainDepositChange>();
        CellId[] neighbors = CreateNeighbors(excavatedCell);
        for (int index = 0; index < neighbors.Length; index++)
        {
            if (TryReveal(neighbors[index], version, out TerrainDepositChange change))
            {
                changes.Add(change);
            }
        }

        return new ReadOnlyCollection<TerrainDepositChange>(changes);
    }


    public bool MatchesActiveDeposit(
        CellId cell,
        string expectedInstanceId,
        int expectedYield)
    {
        if (string.IsNullOrWhiteSpace(expectedInstanceId))
        {
            throw new ArgumentException(
                "Expected deposit instance id is required.",
                nameof(expectedInstanceId));
        }

        if (expectedYield <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedYield));
        }

        return _byCell.TryGetValue(cell, out TerrainDepositInstance? current)
            && !current.IsDepleted
            && string.Equals(
                current.InstanceId,
                expectedInstanceId,
                StringComparison.Ordinal)
            && current.RemainingYield == expectedYield;
    }

    public bool Deplete(CellId cell, long version)
    {
        return TryDeplete(cell, expectedInstanceId: null, expectedYield: null, version, out _);
    }

    public bool TryDeplete(
        CellId cell,
        string? expectedInstanceId,
        int? expectedYield,
        long version,
        out TerrainDepositChange change)
    {
        if (!_byCell.TryGetValue(cell, out TerrainDepositInstance? current)
            || current.IsDepleted)
        {
            change = null!;
            return false;
        }

        if (expectedInstanceId != null
            && !string.Equals(current.InstanceId, expectedInstanceId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Deposit depletion target no longer matches the expected instance.");
        }

        if (expectedYield.HasValue && current.RemainingYield != expectedYield.Value)
        {
            throw new InvalidOperationException(
                "Deposit depletion target no longer matches the expected yield.");
        }

        TerrainDepositInstance depleted = current.Deplete(
            Math.Max(current.Version + 1, version));
        _byCell[cell] = depleted;
        change = CreateChange(TerrainDepositChangeKind.Depleted, depleted);
        return true;
    }

    private static TerrainDepositChange CreateChange(
        TerrainDepositChangeKind kind,
        TerrainDepositInstance deposit)
    {
        return new TerrainDepositChange(
            kind,
            deposit.InstanceId,
            deposit.Definition.Id,
            deposit.Cell,
            deposit.Version);
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
