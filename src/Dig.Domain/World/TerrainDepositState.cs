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
        return _byCell.TryGetValue(cell, out deposit!);
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
}

}
