using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Application.World
{

public sealed class MiningOutputCommitSaveEntry
{
    public MiningOutputCommitSaveEntry(
        CellId cell,
        MiningOutputSourceKind sourceKind,
        string itemId,
        int quantity,
        string? stackId,
        bool hasStack)
    {
        if (!Enum.IsDefined(typeof(MiningOutputSourceKind), sourceKind))
        {
            throw new ArgumentOutOfRangeException(nameof(sourceKind));
        }

        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (quantity > 0 && string.IsNullOrWhiteSpace(itemId))
        {
            throw new ArgumentException("A non-empty output requires an item id.", nameof(itemId));
        }

        if (hasStack != (quantity > 0))
        {
            throw new ArgumentException("Stack presence must match output quantity.", nameof(hasStack));
        }

        if (hasStack && string.IsNullOrWhiteSpace(stackId))
        {
            throw new ArgumentException("A committed output stack requires a stable id.", nameof(stackId));
        }

        Cell = cell;
        SourceKind = sourceKind;
        ItemId = itemId ?? string.Empty;
        Quantity = quantity;
        StackId = stackId;
        HasStack = hasStack;
    }

    public CellId Cell { get; }
    public MiningOutputSourceKind SourceKind { get; }
    public string ItemId { get; }
    public int Quantity { get; }
    public string? StackId { get; }
    public bool HasStack { get; }
}

public sealed class MiningOutputCommitSaveSnapshot
{
    public const int CurrentFormatVersion = 1;

    public MiningOutputCommitSaveSnapshot(
        int formatVersion,
        IEnumerable<MiningOutputCommitSaveEntry> commits)
    {
        if (formatVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(formatVersion));
        }

        if (commits == null)
        {
            throw new ArgumentNullException(nameof(commits));
        }

        MiningOutputCommitSaveEntry[] values = commits.ToArray();
        if (values.Any(value => value == null))
        {
            throw new ArgumentException("Commit save entries cannot contain null values.", nameof(commits));
        }

        if (values.GroupBy(value => value.Cell).Any(group => group.Count() > 1))
        {
            throw new ArgumentException("Mining output commits must be unique by cell.", nameof(commits));
        }

        FormatVersion = formatVersion;
        Commits = new ReadOnlyCollection<MiningOutputCommitSaveEntry>(
            values.OrderBy(value => value.Cell).ToArray());
    }

    public int FormatVersion { get; }
    public IReadOnlyList<MiningOutputCommitSaveEntry> Commits { get; }

    public static MiningOutputCommitSaveSnapshot Capture(MiningOutputCommitState state)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        return new MiningOutputCommitSaveSnapshot(
            CurrentFormatVersion,
            state.Snapshot().Select(commit => new MiningOutputCommitSaveEntry(
                commit.Cell,
                commit.SourceKind,
                commit.ItemId.ToString(),
                commit.Quantity,
                commit.HasStack ? commit.StackId.ToString() : null,
                commit.HasStack)));
    }

    public MiningOutputCommitState Restore()
    {
        if (FormatVersion != CurrentFormatVersion)
        {
            throw new InvalidOperationException(
                $"Unsupported mining output commit snapshot version {FormatVersion}.");
        }

        MiningOutputCommitState restored = new MiningOutputCommitState();
        foreach (MiningOutputCommitSaveEntry entry in Commits)
        {
            ItemId itemId = entry.Quantity == 0
                ? default
                : new ItemId(entry.ItemId);
            EntityId stackId = entry.HasStack
                ? EntityId.Parse(entry.StackId!)
                : default;
            string sourceId = entry.SourceKind == MiningOutputSourceKind.Deposit
                ? "restored.deposit"
                : "restored.terrain";
            MiningOutputPlan plan = new MiningOutputPlan(
                entry.Cell,
                entry.SourceKind,
                itemId,
                entry.Quantity,
                sourceId,
                sourceVersion: 1,
                depositInstanceId: entry.SourceKind == MiningOutputSourceKind.Deposit
                    ? "restored.deposit.instance"
                    : null);
            restored.Record(plan, stackId);
        }

        return restored;
    }
}

}
