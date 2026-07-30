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

public sealed class MiningOutputLine
{
    public MiningOutputLine(ItemId itemId, int quantity)
    {
        if (itemId.IsEmpty)
        {
            throw new ArgumentException("Mining output item id is required.", nameof(itemId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        ItemId = itemId;
        Quantity = quantity;
    }

    public ItemId ItemId { get; }
    public int Quantity { get; }
}

public sealed class MiningOutputPlan
{
    private readonly IReadOnlyList<MiningOutputLine> _outputs;

    internal MiningOutputPlan(
        CellId cell,
        MiningOutputSourceKind sourceKind,
        IEnumerable<MiningOutputLine> outputs,
        string sourceId,
        int sourceVersion,
        string? depositInstanceId)
    {
        if (outputs == null)
        {
            throw new ArgumentNullException(nameof(outputs));
        }

        MiningOutputLine[] values = outputs
            .OrderBy(value => value.ItemId)
            .ToArray();
        if (values.Any(value => value == null))
        {
            throw new ArgumentException("Mining output lines cannot contain null values.", nameof(outputs));
        }

        if (values.Select(value => value.ItemId).Distinct().Count() != values.Length)
        {
            throw new ArgumentException(
                "Mining output lines must be unique by item id.",
                nameof(outputs));
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
        _outputs = new ReadOnlyCollection<MiningOutputLine>(values);
        SourceId = sourceId.Trim();
        SourceVersion = sourceVersion;
        DepositInstanceId = depositInstanceId;
    }

    public CellId Cell { get; }
    public MiningOutputSourceKind SourceKind { get; }
    public IReadOnlyList<MiningOutputLine> Outputs => _outputs;
    public int TotalQuantity => _outputs.Sum(value => value.Quantity);
    public bool IsEmpty => _outputs.Count == 0;
    public string SourceId { get; }
    public int SourceVersion { get; }
    public string? DepositInstanceId { get; }

    // Compatibility for existing single-output callers. New code should use Outputs.
    public ItemId ItemId => _outputs.Count == 1 ? _outputs[0].ItemId : default;
    public int Quantity => _outputs.Count == 1 ? _outputs[0].Quantity : 0;
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
                new[]
                {
                    new MiningOutputLine(
                        deposit.Definition.OutputItemId,
                        deposit.RemainingYield),
                },
                deposit.Definition.Id,
                deposit.Definition.Version,
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
            roll.Outputs.Select(value => new MiningOutputLine(
                value.ItemId,
                value.Quantity)),
            roll.ProfileId,
            roll.ProfileVersion,
            depositInstanceId: null);
    }
}

}
