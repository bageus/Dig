using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Application.World
{

public sealed class MiningOutputDiagnosticLine
{
    public MiningOutputDiagnosticLine(ItemId itemId, int quantity)
    {
        ItemId = itemId;
        Quantity = quantity;
    }

    public ItemId ItemId { get; }
    public int Quantity { get; }
}

public sealed class MiningOutputDiagnosticSnapshot
{
    internal MiningOutputDiagnosticSnapshot(
        CellId cell,
        MiningOutputSourceKind sourceKind,
        string sourceId,
        int sourceVersion,
        bool isCommitted,
        IEnumerable<MiningOutputDiagnosticLine> outputs)
    {
        Cell = cell;
        SourceKind = sourceKind;
        SourceId = sourceId;
        SourceVersion = sourceVersion;
        IsCommitted = isCommitted;
        Outputs = new ReadOnlyCollection<MiningOutputDiagnosticLine>(
            outputs.OrderBy(value => value.ItemId).ToArray());
    }

    public CellId Cell { get; }
    public MiningOutputSourceKind SourceKind { get; }
    public string SourceId { get; }
    public int SourceVersion { get; }
    public bool IsCommitted { get; }
    public IReadOnlyList<MiningOutputDiagnosticLine> Outputs { get; }
}

public sealed class MiningOutputDiagnostics
{
    public MiningOutputDiagnosticSnapshot Inspect(
        MiningOutputPlan plan,
        MiningOutputCommitState commits)
    {
        if (plan == null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        if (commits == null)
        {
            throw new ArgumentNullException(nameof(commits));
        }

        return new MiningOutputDiagnosticSnapshot(
            plan.Cell,
            plan.SourceKind,
            plan.SourceId,
            plan.SourceVersion,
            commits.IsCommitted(plan.Cell),
            plan.Outputs.Select(value => new MiningOutputDiagnosticLine(
                value.ItemId,
                value.Quantity)));
    }
}

}
