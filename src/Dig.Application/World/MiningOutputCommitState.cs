using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Application.World
{

public sealed class MiningOutputCommitState
{
    private readonly Dictionary<CellId, MiningOutputCommit> _commits =
        new Dictionary<CellId, MiningOutputCommit>();

    public IReadOnlyList<MiningOutputCommit> Snapshot()
    {
        return new ReadOnlyCollection<MiningOutputCommit>(
            _commits.Values.OrderBy(value => value.Cell).ToArray());
    }

    public bool IsCommitted(CellId cell)
    {
        return _commits.ContainsKey(cell);
    }

    public void Validate(
        MiningOutputPlan plan,
        IReadOnlyList<EntityId> stackIds,
        InventoryState inventory,
        TerrainDepositState deposits)
    {
        if (plan == null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        if (stackIds == null)
        {
            throw new ArgumentNullException(nameof(stackIds));
        }

        if (inventory == null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        if (deposits == null)
        {
            throw new ArgumentNullException(nameof(deposits));
        }

        if (_commits.ContainsKey(plan.Cell))
        {
            throw new InvalidOperationException(
                $"Mining output for cell {plan.Cell} was already committed.");
        }

        ValidateDepositPlan(plan, deposits);
        ValidateWorldUnits(plan, stackIds, inventory);
    }

    public void Validate(
        MiningOutputPlan plan,
        EntityId stackId,
        InventoryState inventory,
        TerrainDepositState deposits)
    {
        if (plan == null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        if (inventory == null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        if (deposits == null)
        {
            throw new ArgumentNullException(nameof(deposits));
        }

        if (_commits.ContainsKey(plan.Cell))
        {
            throw new InvalidOperationException(
                $"Mining output for cell {plan.Cell} was already committed.");
        }

        ValidateDepositPlan(plan, deposits);
        ValidateWorldUnits(
            plan,
            plan.IsEmpty ? Array.Empty<EntityId>() : new[] { stackId },
            inventory,
            allowAggregateStack: true);
    }

    public MiningOutputCommit Record(
        MiningOutputPlan plan,
        IReadOnlyList<EntityId> stackIds)
    {
        if (plan == null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        if (stackIds == null)
        {
            throw new ArgumentNullException(nameof(stackIds));
        }

        if (_commits.ContainsKey(plan.Cell))
        {
            throw new InvalidOperationException(
                $"Mining output for cell {plan.Cell} was already committed.");
        }

        if (plan.IsEmpty && stackIds.Count != 0)
        {
            throw new ArgumentException("Empty output cannot reference stacks.", nameof(stackIds));
        }

        if (!plan.IsEmpty && stackIds.Count != plan.TotalQuantity)
        {
            throw new ArgumentException(
                "Unit output stack count must equal total output quantity.",
                nameof(stackIds));
        }

        List<MiningOutputCommitLine> lines = new List<MiningOutputCommitLine>();
        int offset = 0;
        foreach (MiningOutputLine output in plan.Outputs)
        {
            lines.Add(new MiningOutputCommitLine(
                output.ItemId,
                output.Quantity,
                stackIds.Skip(offset).Take(output.Quantity).ToArray()));
            offset += output.Quantity;
        }

        return AddCommit(plan, lines);
    }

    public MiningOutputCommit Record(MiningOutputPlan plan, EntityId stackId)
    {
        if (plan == null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        if (plan.IsEmpty)
        {
            return AddCommit(plan, Array.Empty<MiningOutputCommitLine>());
        }

        if (plan.Outputs.Count != 1 || stackId.IsEmpty)
        {
            throw new ArgumentException(
                "Legacy aggregate record requires one output and one stack id.",
                nameof(stackId));
        }

        MiningOutputLine output = plan.Outputs[0];
        return AddCommit(
            plan,
            new[]
            {
                new MiningOutputCommitLine(
                    output.ItemId,
                    output.Quantity,
                    new[] { stackId }),
            });
    }

    internal MiningOutputCommit Restore(
        CellId cell,
        MiningOutputSourceKind sourceKind,
        string sourceId,
        int sourceVersion,
        IEnumerable<MiningOutputCommitLine> outputs)
    {
        if (_commits.ContainsKey(cell))
        {
            throw new InvalidOperationException(
                $"Mining output for cell {cell} was already restored.");
        }

        MiningOutputCommit commit = new MiningOutputCommit(
            cell,
            sourceKind,
            sourceId,
            sourceVersion,
            outputs);
        _commits.Add(cell, commit);
        return commit;
    }

    private MiningOutputCommit AddCommit(
        MiningOutputPlan plan,
        IEnumerable<MiningOutputCommitLine> outputs)
    {
        if (_commits.ContainsKey(plan.Cell))
        {
            throw new InvalidOperationException(
                $"Mining output for cell {plan.Cell} was already committed.");
        }

        MiningOutputCommit committed = new MiningOutputCommit(
            plan.Cell,
            plan.SourceKind,
            plan.SourceId,
            plan.SourceVersion,
            outputs);
        _commits.Add(plan.Cell, committed);
        return committed;
    }

    private static void ValidateDepositPlan(
        MiningOutputPlan plan,
        TerrainDepositState deposits)
    {
        bool hasDeposit = deposits.TryGet(plan.Cell, out TerrainDepositInstance current);
        if (plan.SourceKind == MiningOutputSourceKind.Terrain)
        {
            if (hasDeposit && !current.IsDepleted)
            {
                throw new InvalidOperationException(
                    "Terrain output cannot be committed while a deposit occupies the cell.");
            }

            return;
        }

        MiningOutputLine? output = plan.Outputs.Count == 1 ? plan.Outputs[0] : null;
        if (!hasDeposit
            || current.IsDepleted
            || output == null
            || !string.Equals(
                current.InstanceId,
                plan.DepositInstanceId,
                StringComparison.Ordinal)
            || current.Definition.Id != plan.SourceId
            || current.Definition.Version != plan.SourceVersion
            || current.Definition.OutputItemId != output.ItemId
            || current.RemainingYield != output.Quantity)
        {
            throw new InvalidOperationException(
                "Deposit output plan no longer matches authoritative deposit state.");
        }
    }

    private static void ValidateWorldUnits(
        MiningOutputPlan plan,
        IReadOnlyList<EntityId> stackIds,
        InventoryState inventory,
        bool allowAggregateStack = false)
    {
        if (plan.IsEmpty)
        {
            if (stackIds.Count != 0)
            {
                throw new ArgumentException("Empty output cannot reserve stack ids.", nameof(stackIds));
            }

            return;
        }

        int expectedCount = allowAggregateStack && plan.Outputs.Count == 1
            ? 1
            : plan.TotalQuantity;
        if (stackIds.Count != expectedCount
            || stackIds.Any(value => value.IsEmpty)
            || stackIds.Distinct().Count() != stackIds.Count)
        {
            throw new ArgumentException(
                "Mining output requires the deterministic non-empty stack id set.",
                nameof(stackIds));
        }

        foreach (EntityId stackId in stackIds)
        {
            if (inventory.GetStack(stackId) != null)
            {
                throw new InvalidOperationException(
                    $"Mining output stack '{stackId}' already exists.");
            }
        }

        foreach (MiningOutputLine output in plan.Outputs)
        {
            if (!inventory.Catalog.Contains(output.ItemId))
            {
                throw new InvalidOperationException(
                    $"Mining output item '{output.ItemId}' is missing from the inventory catalog.");
            }

            ItemDefinition definition = inventory.Catalog.Get(output.ItemId);
            if (output.Quantity > definition.MaximumStackSize)
            {
                throw new InvalidOperationException(
                    $"Mining output quantity {output.Quantity} exceeds stack size "
                    + $"{definition.MaximumStackSize} for '{output.ItemId}'.");
            }
        }
    }
}

}
