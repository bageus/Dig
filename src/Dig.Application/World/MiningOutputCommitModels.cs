using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Application.World
{

public sealed class MiningOutputCommitLine
{
    private readonly IReadOnlyList<EntityId> _stackIds;

    internal MiningOutputCommitLine(
        ItemId itemId,
        int quantity,
        IEnumerable<EntityId> stackIds)
    {
        if (itemId.IsEmpty)
        {
            throw new ArgumentException("Committed output item id is required.", nameof(itemId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        EntityId[] ids = (stackIds ?? throw new ArgumentNullException(nameof(stackIds)))
            .ToArray();
        if (ids.Length == 0 || ids.Any(value => value.IsEmpty)
            || ids.Distinct().Count() != ids.Length)
        {
            throw new ArgumentException(
                "Committed output requires unique non-empty stack ids.",
                nameof(stackIds));
        }

        ItemId = itemId;
        Quantity = quantity;
        _stackIds = new ReadOnlyCollection<EntityId>(ids);
    }

    public ItemId ItemId { get; }
    public int Quantity { get; }
    public IReadOnlyList<EntityId> StackIds => _stackIds;
}

public sealed class MiningOutputCommit
{
    private readonly IReadOnlyList<MiningOutputCommitLine> _outputs;

    internal MiningOutputCommit(
        CellId cell,
        MiningOutputSourceKind sourceKind,
        string sourceId,
        int sourceVersion,
        IEnumerable<MiningOutputCommitLine> outputs)
    {
        MiningOutputCommitLine[] values = (outputs
            ?? throw new ArgumentNullException(nameof(outputs)))
            .OrderBy(value => value.ItemId)
            .ToArray();
        if (values.Select(value => value.ItemId).Distinct().Count() != values.Length)
        {
            throw new ArgumentException(
                "Committed outputs must be unique by item id.",
                nameof(outputs));
        }

        if (string.IsNullOrWhiteSpace(sourceId))
        {
            throw new ArgumentException("Committed output source id is required.", nameof(sourceId));
        }

        if (sourceVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceVersion));
        }

        Cell = cell;
        SourceKind = sourceKind;
        SourceId = sourceId.Trim();
        SourceVersion = sourceVersion;
        _outputs = new ReadOnlyCollection<MiningOutputCommitLine>(values);
    }

    public CellId Cell { get; }
    public MiningOutputSourceKind SourceKind { get; }
    public string SourceId { get; }
    public int SourceVersion { get; }
    public IReadOnlyList<MiningOutputCommitLine> Outputs => _outputs;
    public int Quantity => _outputs.Sum(value => value.Quantity);
    public bool HasStack => _outputs.Count > 0;
    public IReadOnlyList<EntityId> StackIds => new ReadOnlyCollection<EntityId>(
        _outputs.SelectMany(value => value.StackIds).ToArray());

    // Compatibility for existing single-output diagnostics/tests.
    public ItemId ItemId => _outputs.Count == 1 ? _outputs[0].ItemId : default;
    public EntityId StackId => _outputs.Count == 1 && _outputs[0].StackIds.Count == 1
        ? _outputs[0].StackIds[0]
        : default;
}

}
