using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Application.World
{

public enum MiningOutputSourceKind
{
    Terrain = 0,
    Deposit = 1,
}

public sealed class MiningOutputPlan
{
    internal MiningOutputPlan(
        CellId cell,
        MiningOutputSourceKind sourceKind,
        ItemId itemId,
        int quantity,
        string sourceId,
        int sourceVersion,
        string? depositInstanceId)
    {
        if (quantity < 0 || (quantity == 0 && !itemId.IsEmpty))
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (quantity > 0 && itemId.IsEmpty)
        {
            throw new ArgumentException("A non-empty mining output requires an item id.", nameof(itemId));
        }

        if (string.IsNullOrWhiteSpace(sourceId))
        {
            throw new ArgumentException("Mining output source id is required.", nameof(sourceId));
        }

        if (sourceVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceVersion));
        }

        if (sourceKind == MiningOutputSourceKind.Deposit
            && string.IsNullOrWhiteSpace(depositInstanceId))
        {
            throw new ArgumentException(
                "Deposit output requires a stable deposit instance id.",
                nameof(depositInstanceId));
        }

        Cell = cell;
        SourceKind = sourceKind;
        ItemId = itemId;
        Quantity = quantity;
        SourceId = sourceId.Trim();
        SourceVersion = sourceVersion;
        DepositInstanceId = depositInstanceId;
    }

    public CellId Cell { get; }
    public MiningOutputSourceKind SourceKind { get; }
    public ItemId ItemId { get; }
    public int Quantity { get; }
    public bool IsEmpty => Quantity == 0;
    public string SourceId { get; }
    public int SourceVersion { get; }
    public string? DepositInstanceId { get; }
}

public sealed class MiningOutputResolver
{
    private readonly TerrainOutputResolver _terrainResolver;

    public MiningOutputResolver(TerrainOutputResolver? terrainResolver = null)
    {
        _terrainResolver = terrainResolver ?? new TerrainOutputResolver();
    }

    public MiningOutputPlan Resolve(
        int worldSeed,
        int generatorVersion,
        CellId cell,
        MaterialDefinition terrain,
        TerrainDepositState deposits)
    {
        if (terrain == null)
        {
            throw new ArgumentNullException(nameof(terrain));
        }

        if (deposits == null)
        {
            throw new ArgumentNullException(nameof(deposits));
        }

        if (deposits.TryGet(cell, out TerrainDepositInstance deposit))
        {
            if (deposit.IsDepleted)
            {
                throw new InvalidOperationException(
                    $"Deposit '{deposit.InstanceId}' at {cell} is already depleted.");
            }

            return new MiningOutputPlan(
                cell,
                MiningOutputSourceKind.Deposit,
                deposit.Definition.OutputItemId,
                deposit.RemainingYield,
                deposit.Definition.Id,
                checked((int)Math.Max(1L, deposit.Version)),
                deposit.InstanceId);
        }

        if (!terrain.IsMineable || terrain.OutputProfile == null)
        {
            throw new InvalidOperationException(
                $"Terrain material '{terrain.Id}' cannot produce mining output.");
        }

        TerrainOutputRoll roll = _terrainResolver.Resolve(
            worldSeed,
            generatorVersion,
            cell,
            terrain.OutputProfile);
        return new MiningOutputPlan(
            cell,
            MiningOutputSourceKind.Terrain,
            roll.ItemId,
            roll.Quantity,
            roll.ProfileId,
            roll.ProfileVersion,
            depositInstanceId: null);
    }
}

public sealed class MiningOutputCommit
{
    internal MiningOutputCommit(
        CellId cell,
        MiningOutputSourceKind sourceKind,
        ItemId itemId,
        int quantity,
        EntityId stackId,
        bool hasStack)
    {
        Cell = cell;
        SourceKind = sourceKind;
        ItemId = itemId;
        Quantity = quantity;
        StackId = stackId;
        HasStack = hasStack;
    }

    public CellId Cell { get; }
    public MiningOutputSourceKind SourceKind { get; }
    public ItemId ItemId { get; }
    public int Quantity { get; }
    public EntityId StackId { get; }
    public bool HasStack { get; }
}

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

    public MiningOutputCommit Commit(
        MiningOutputPlan plan,
        EntityId stackId,
        InventoryState inventory,
        TerrainDepositState deposits,
        long tick)
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

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (_commits.ContainsKey(plan.Cell))
        {
            throw new InvalidOperationException(
                $"Mining output for cell {plan.Cell} was already committed.");
        }

        TerrainDepositInstance? deposit = ValidateDepositPlan(plan, deposits);
        ValidateWorldStack(plan, stackId, inventory);

        if (!plan.IsEmpty)
        {
            Dig.Domain.Core.Result added = inventory.AddStack(
                stackId,
                plan.ItemId,
                plan.Quantity,
                ItemLocation.InWorld(plan.Cell),
                tick);
            if (added.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Mining output world stack preflight diverged: {added.Error}");
            }
        }

        if (deposit != null && !deposits.Deplete(plan.Cell, tick))
        {
            throw new InvalidOperationException(
                "Deposit changed after mining output preflight.");
        }

        MiningOutputCommit committed = new MiningOutputCommit(
            plan.Cell,
            plan.SourceKind,
            plan.ItemId,
            plan.Quantity,
            stackId,
            hasStack: !plan.IsEmpty);
        _commits.Add(plan.Cell, committed);
        return committed;
    }

    private static TerrainDepositInstance? ValidateDepositPlan(
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

            return null;
        }

        if (!hasDeposit
            || current.IsDepleted
            || !string.Equals(
                current.InstanceId,
                plan.DepositInstanceId,
                StringComparison.Ordinal)
            || current.Definition.Id != plan.SourceId
            || current.Definition.OutputItemId != plan.ItemId
            || current.RemainingYield != plan.Quantity)
        {
            throw new InvalidOperationException(
                "Deposit output plan no longer matches authoritative deposit state.");
        }

        return current;
    }

    private static void ValidateWorldStack(
        MiningOutputPlan plan,
        EntityId stackId,
        InventoryState inventory)
    {
        if (plan.IsEmpty)
        {
            return;
        }

        if (stackId.IsEmpty)
        {
            throw new ArgumentException("Mining output stack id is required.", nameof(stackId));
        }

        if (inventory.GetStack(stackId) != null)
        {
            throw new InvalidOperationException(
                $"Mining output stack '{stackId}' already exists.");
        }

        if (!inventory.Catalog.Contains(plan.ItemId))
        {
            throw new InvalidOperationException(
                $"Mining output item '{plan.ItemId}' is missing from the inventory catalog.");
        }

        ItemDefinition definition = inventory.Catalog.Get(plan.ItemId);
        if (plan.Quantity > definition.MaximumStackSize)
        {
            throw new InvalidOperationException(
                $"Mining output quantity {plan.Quantity} exceeds stack size "
                + $"{definition.MaximumStackSize} for '{plan.ItemId}'.");
        }
    }
}

}
