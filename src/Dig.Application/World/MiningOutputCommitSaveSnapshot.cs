using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Application.World
{

public sealed class MiningOutputCommitLineSaveEntry
{
    public MiningOutputCommitLineSaveEntry(
        string itemId,
        int quantity,
        IEnumerable<string> stackIds)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            throw new ArgumentException("Committed output item id is required.", nameof(itemId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        string[] ids = (stackIds ?? throw new ArgumentNullException(nameof(stackIds)))
            .Select(value => value?.Trim() ?? string.Empty)
            .ToArray();
        if (ids.Length == 0
            || ids.Any(string.IsNullOrWhiteSpace)
            || ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
        {
            throw new ArgumentException(
                "Committed output requires unique stack ids.",
                nameof(stackIds));
        }

        ItemId = itemId.Trim();
        Quantity = quantity;
        StackIds = new ReadOnlyCollection<string>(ids);
    }

    public string ItemId { get; }
    public int Quantity { get; }
    public IReadOnlyList<string> StackIds { get; }
}

public sealed class MiningOutputCommitSaveEntry
{
    private readonly IReadOnlyList<MiningOutputCommitLineSaveEntry> _outputs;

    public MiningOutputCommitSaveEntry(
        CellId cell,
        MiningOutputSourceKind sourceKind,
        string sourceId,
        int sourceVersion,
        IEnumerable<MiningOutputCommitLineSaveEntry> outputs)
    {
        if (!Enum.IsDefined(typeof(MiningOutputSourceKind), sourceKind))
        {
            throw new ArgumentOutOfRangeException(nameof(sourceKind));
        }

        if (string.IsNullOrWhiteSpace(sourceId))
        {
            throw new ArgumentException("Committed output source id is required.", nameof(sourceId));
        }

        if (sourceVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceVersion));
        }

        MiningOutputCommitLineSaveEntry[] values = (outputs
            ?? throw new ArgumentNullException(nameof(outputs)))
            .OrderBy(value => value.ItemId, StringComparer.Ordinal)
            .ToArray();
        if (values.Any(value => value == null)
            || values.Select(value => value.ItemId)
                .Distinct(StringComparer.Ordinal).Count() != values.Length)
        {
            throw new ArgumentException(
                "Committed outputs must be non-null and unique by item id.",
                nameof(outputs));
        }

        Cell = cell;
        SourceKind = sourceKind;
        SourceId = sourceId.Trim();
        SourceVersion = sourceVersion;
        _outputs = new ReadOnlyCollection<MiningOutputCommitLineSaveEntry>(values);
    }

    // Legacy format compatibility.
    public MiningOutputCommitSaveEntry(
        CellId cell,
        MiningOutputSourceKind sourceKind,
        string itemId,
        int quantity,
        string? stackId,
        bool hasStack)
        : this(
            cell,
            sourceKind,
            sourceKind == MiningOutputSourceKind.Deposit
                ? "legacy.deposit-output"
                : "legacy.terrain-output",
            sourceVersion: 1,
            CreateLegacyOutputs(itemId, quantity, stackId, hasStack))
    {
    }

    public CellId Cell { get; }
    public MiningOutputSourceKind SourceKind { get; }
    public string SourceId { get; }
    public int SourceVersion { get; }
    public IReadOnlyList<MiningOutputCommitLineSaveEntry> Outputs => _outputs;

    // Legacy accessors retained for old tests and v1 adapters.
    public string ItemId => _outputs.Count == 1 ? _outputs[0].ItemId : string.Empty;
    public int Quantity => _outputs.Sum(value => value.Quantity);
    public string? StackId => _outputs.Count == 1 && _outputs[0].StackIds.Count == 1
        ? _outputs[0].StackIds[0]
        : null;
    public bool HasStack => _outputs.Count > 0;

    private static IEnumerable<MiningOutputCommitLineSaveEntry> CreateLegacyOutputs(
        string itemId,
        int quantity,
        string? stackId,
        bool hasStack)
    {
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (hasStack != (quantity > 0))
        {
            throw new ArgumentException("Stack presence must match output quantity.", nameof(hasStack));
        }

        if (!hasStack)
        {
            return Array.Empty<MiningOutputCommitLineSaveEntry>();
        }

        if (string.IsNullOrWhiteSpace(itemId) || string.IsNullOrWhiteSpace(stackId))
        {
            throw new ArgumentException("Legacy committed output is incomplete.");
        }

        return new[]
        {
            new MiningOutputCommitLineSaveEntry(itemId, quantity, new[] { stackId }),
        };
    }
}

public sealed class MiningOutputCommitSaveSnapshot
{
    public const int CurrentFormatVersion = 2;

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
                commit.SourceId,
                commit.SourceVersion,
                commit.Outputs.Select(output => new MiningOutputCommitLineSaveEntry(
                    output.ItemId.ToString(),
                    output.Quantity,
                    output.StackIds.Select(value => value.ToString()))))));
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
            restored.Restore(
                entry.Cell,
                entry.SourceKind,
                entry.SourceId,
                entry.SourceVersion,
                entry.Outputs.Select(output => new MiningOutputCommitLine(
                    new ItemId(output.ItemId),
                    output.Quantity,
                    output.StackIds.Select(EntityId.Parse))));
        }

        return restored;
    }
}

}
