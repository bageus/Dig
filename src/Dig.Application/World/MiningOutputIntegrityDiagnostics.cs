using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Application.World
{

public static class MiningOutputIntegrityCodes
{
    public const string DuplicateStack = "mining_output.integrity.duplicate_stack";
    public const string MissingStack = "mining_output.integrity.missing_stack";
    public const string ItemMismatch = "mining_output.integrity.item_mismatch";
    public const string QuantityMismatch = "mining_output.integrity.quantity_mismatch";
    public const string LocationMismatch = "mining_output.integrity.location_mismatch";
}

public sealed class MiningOutputIntegrityIssue
{
    public MiningOutputIntegrityIssue(CellId cell, string code, string message)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("An integrity issue code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("An integrity issue message is required.", nameof(message));
        }

        Cell = cell;
        Code = code;
        Message = message;
    }

    public CellId Cell { get; }
    public string Code { get; }
    public string Message { get; }
}

public sealed class MiningOutputIntegrityReport
{
    internal MiningOutputIntegrityReport(
        IEnumerable<MiningOutputIntegrityIssue> issues,
        int committedQuantity,
        int trackedWorldQuantity,
        int reservedQuantity)
    {
        MiningOutputIntegrityIssue[] ordered = (issues
            ?? throw new ArgumentNullException(nameof(issues)))
            .OrderBy(value => value.Cell)
            .ThenBy(value => value.Code, StringComparer.Ordinal)
            .ToArray();
        Issues = new ReadOnlyCollection<MiningOutputIntegrityIssue>(ordered);
        CommittedQuantity = committedQuantity;
        TrackedWorldQuantity = trackedWorldQuantity;
        ReservedQuantity = reservedQuantity;
    }

    public IReadOnlyList<MiningOutputIntegrityIssue> Issues { get; }
    public bool IsValid => Issues.Count == 0;
    public int CommittedQuantity { get; }
    public int TrackedWorldQuantity { get; }
    public int ReservedQuantity { get; }
}

public sealed class MiningOutputIntegrityDiagnostics
{
    public MiningOutputIntegrityReport Inspect(
        MiningOutputCommitState commits,
        InventoryState inventory)
    {
        if (commits == null)
        {
            throw new ArgumentNullException(nameof(commits));
        }

        if (inventory == null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        List<MiningOutputIntegrityIssue> issues = new List<MiningOutputIntegrityIssue>();
        HashSet<EntityId> stackIds = new HashSet<EntityId>();
        int committedQuantity = 0;
        int trackedWorldQuantity = 0;
        int reservedQuantity = 0;

        foreach (MiningOutputCommit commit in commits.Snapshot())
        {
            committedQuantity = checked(committedQuantity + commit.Quantity);
            foreach (MiningOutputCommitLine output in commit.Outputs)
            {
                bool unitIdentityContract = output.StackIds.Count == output.Quantity;
                foreach (EntityId stackId in output.StackIds)
                {
                    if (!stackIds.Add(stackId))
                    {
                        issues.Add(new MiningOutputIntegrityIssue(
                            commit.Cell,
                            MiningOutputIntegrityCodes.DuplicateStack,
                            $"Mining output stack '{stackId}' is referenced more than once."));
                        continue;
                    }

                    ItemStackSnapshot? stack = inventory.GetStack(stackId);
                    if (stack == null)
                    {
                        issues.Add(new MiningOutputIntegrityIssue(
                            commit.Cell,
                            MiningOutputIntegrityCodes.MissingStack,
                            $"Mining output stack '{stackId}' is missing from Inventory."));
                        continue;
                    }

                    trackedWorldQuantity = checked(
                        trackedWorldQuantity + stack.Quantity);
                    reservedQuantity = checked(
                        reservedQuantity + stack.Reservations.Sum(value => value.Quantity));

                    if (stack.ItemId != output.ItemId)
                    {
                        issues.Add(new MiningOutputIntegrityIssue(
                            commit.Cell,
                            MiningOutputIntegrityCodes.ItemMismatch,
                            $"Mining output stack '{stackId}' contains '{stack.ItemId}' instead of '{output.ItemId}'."));
                    }

                    if (unitIdentityContract && stack.Quantity != 1)
                    {
                        issues.Add(new MiningOutputIntegrityIssue(
                            commit.Cell,
                            MiningOutputIntegrityCodes.QuantityMismatch,
                            $"Mining output unit '{stackId}' has quantity {stack.Quantity} instead of 1."));
                    }
                }
            }
        }

        return new MiningOutputIntegrityReport(
            issues,
            committedQuantity,
            trackedWorldQuantity,
            reservedQuantity);
    }
}

}
