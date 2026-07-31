using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Application.Messaging;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Application.Jobs
{

public static class TerrainWorkCompletionErrors
{
    public static readonly DomainError JobTypeUnsupported = new DomainError(
        "terrain_work.job_type_unsupported",
        "The requested job is not a terrain work job.");

    public static readonly DomainError JobNotReady = new DomainError(
        "terrain_work.job_not_ready",
        "The terrain work job is not waiting at its finalization stage.");

    public static readonly DomainError TargetNotSolid = new DomainError(
        "terrain_work.target_not_solid",
        "The target cell is no longer solid.");

    public static readonly DomainError TargetNotDesignated = new DomainError(
        "terrain_work.target_not_designated",
        "The target cell is no longer designated.");

    public static readonly DomainError UnknownOutputItem = new DomainError(
        "terrain_work.output_item_unknown",
        "The output item is not registered in Inventory.");

    public static readonly DomainError OutputPlanCellMismatch = new DomainError(
        "terrain_work.output_plan_cell_mismatch",
        "The resolved mining output plan does not target the terrain job cell.");

    public static readonly DomainError OutputAlreadyCommitted = new DomainError(
        "terrain_work.output_already_committed",
        "Mining output for the terrain cell was already committed.");
}

public sealed class TerrainWorkOutputSpec
{
    public TerrainWorkOutputSpec(ItemId itemId, int quantity)
    {
        if (itemId.IsEmpty)
        {
            throw new ArgumentException("Output item id is required.", nameof(itemId));
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

public sealed class CompleteTerrainWorkCommand
    : ICommand<Result<TerrainWorkCompletionResult>>
{
    private readonly IReadOnlyList<TerrainWorkOutputSpec> _outputs;

    public CompleteTerrainWorkCommand(
        EntityId jobId,
        EntityId outputStackId,
        ItemId outputItemId,
        int outputQuantity,
        MaterialId emptyMaterialId,
        long tick,
        string? depositInstanceId = null,
        int? depositExpectedYield = null)
        : this(
            jobId,
            outputStackId,
            new[] { new TerrainWorkOutputSpec(outputItemId, outputQuantity) },
            emptyMaterialId,
            tick,
            depositInstanceId == null
                ? MiningOutputSourceKind.Terrain
                : MiningOutputSourceKind.Deposit,
            depositInstanceId == null ? "legacy.terrain-output" : "legacy.deposit-output",
            sourceVersion: 1,
            depositInstanceId,
            depositExpectedYield,
            resolvedPlanCell: null)
    {
    }

    private CompleteTerrainWorkCommand(
        EntityId jobId,
        EntityId outputStackId,
        IEnumerable<TerrainWorkOutputSpec> outputs,
        MaterialId emptyMaterialId,
        long tick,
        MiningOutputSourceKind sourceKind,
        string sourceId,
        int sourceVersion,
        string? depositInstanceId,
        int? depositExpectedYield,
        CellId? resolvedPlanCell)
    {
        if (outputs == null)
        {
            throw new ArgumentNullException(nameof(outputs));
        }

        TerrainWorkOutputSpec[] values = outputs
            .OrderBy(value => value.ItemId)
            .ToArray();
        if (values.Any(value => value == null)
            || values.Select(value => value.ItemId).Distinct().Count() != values.Length)
        {
            throw new ArgumentException(
                "Terrain output specs must be non-null and unique by item id.",
                nameof(outputs));
        }

        if ((depositInstanceId is null) != (!depositExpectedYield.HasValue)
            || depositExpectedYield <= 0)
        {
            throw new ArgumentException(
                "Deposit instance and positive expected yield must be supplied together.");
        }

        if (values.Length > 0 && outputStackId.IsEmpty)
        {
            throw new ArgumentException(
                "Non-empty terrain output requires a base stack id.",
                nameof(outputStackId));
        }

        if (string.IsNullOrWhiteSpace(sourceId))
        {
            throw new ArgumentException("Output source id is required.", nameof(sourceId));
        }

        if (sourceVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceVersion));
        }

        JobId = jobId;
        OutputStackId = outputStackId;
        _outputs = new ReadOnlyCollection<TerrainWorkOutputSpec>(values);
        EmptyMaterialId = emptyMaterialId;
        Tick = tick;
        SourceKind = sourceKind;
        SourceId = sourceId.Trim();
        SourceVersion = sourceVersion;
        DepositInstanceId = depositInstanceId;
        DepositExpectedYield = depositExpectedYield;
        ResolvedPlanCell = resolvedPlanCell;
    }

    public EntityId JobId { get; }
    public EntityId OutputStackId { get; }
    public IReadOnlyList<TerrainWorkOutputSpec> Outputs => _outputs;
    public int TotalOutputQuantity => _outputs.Sum(value => value.Quantity);
    public MaterialId EmptyMaterialId { get; }
    public long Tick { get; }
    public bool ProducesOutput => _outputs.Count > 0;
    public MiningOutputSourceKind SourceKind { get; }
    public string SourceId { get; }
    public int SourceVersion { get; }
    public string? DepositInstanceId { get; }
    public int? DepositExpectedYield { get; }
    public CellId? ResolvedPlanCell { get; }
    public bool HasResolvedOutputPlan => ResolvedPlanCell.HasValue;

    // Compatibility for existing single-output callers.
    public ItemId OutputItemId => _outputs.Count == 1 ? _outputs[0].ItemId : default;
    public int OutputQuantity => _outputs.Count == 1 ? _outputs[0].Quantity : TotalOutputQuantity;

    public static CompleteTerrainWorkCommand FromPlan(
        EntityId jobId,
        EntityId outputStackId,
        MiningOutputPlan plan,
        MaterialId emptyMaterialId,
        long tick)
    {
        if (plan == null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        return new CompleteTerrainWorkCommand(
            jobId,
            plan.IsEmpty ? default : outputStackId,
            plan.Outputs.Select(value => new TerrainWorkOutputSpec(
                value.ItemId,
                value.Quantity)),
            emptyMaterialId,
            tick,
            plan.SourceKind,
            plan.SourceId,
            plan.SourceVersion,
            plan.DepositInstanceId,
            plan.SourceKind == MiningOutputSourceKind.Deposit
                ? plan.TotalQuantity
                : (int?)null,
            plan.Cell);
    }

    public static CompleteTerrainWorkCommand WithoutOutput(
        EntityId jobId,
        MaterialId emptyMaterialId,
        long tick)
    {
        return new CompleteTerrainWorkCommand(
            jobId,
            default,
            Array.Empty<TerrainWorkOutputSpec>(),
            emptyMaterialId,
            tick,
            MiningOutputSourceKind.Terrain,
            "legacy.terrain-output.empty",
            sourceVersion: 1,
            depositInstanceId: null,
            depositExpectedYield: null,
            resolvedPlanCell: null);
    }

    internal MiningOutputPlan CreatePlan(CellId targetCell)
    {
        return new MiningOutputPlan(
            targetCell,
            SourceKind,
            _outputs.Select(value => new MiningOutputLine(
                value.ItemId,
                value.Quantity)),
            SourceId,
            SourceVersion,
            DepositInstanceId);
    }
}

public sealed class TerrainWorkProducedOutput
{
    private readonly IReadOnlyList<EntityId> _stackIds;

    public TerrainWorkProducedOutput(
        ItemId itemId,
        int quantity,
        IEnumerable<EntityId> stackIds)
    {
        ItemId = itemId;
        Quantity = quantity;
        _stackIds = new ReadOnlyCollection<EntityId>(
            (stackIds ?? throw new ArgumentNullException(nameof(stackIds))).ToArray());
    }

    public ItemId ItemId { get; }
    public int Quantity { get; }
    public IReadOnlyList<EntityId> StackIds => _stackIds;
}

public sealed class TerrainWorkCompletionResult
{
    private readonly IReadOnlyList<TerrainWorkProducedOutput> _outputs;

    public TerrainWorkCompletionResult(
        EntityId jobId,
        CellId targetCell,
        IEnumerable<TerrainWorkProducedOutput> outputs,
        long worldVersion,
        long inventoryVersion)
    {
        JobId = jobId;
        TargetCell = targetCell;
        _outputs = new ReadOnlyCollection<TerrainWorkProducedOutput>(
            (outputs ?? throw new ArgumentNullException(nameof(outputs)))
            .OrderBy(value => value.ItemId)
            .ToArray());
        WorldVersion = worldVersion;
        InventoryVersion = inventoryVersion;
    }

    public EntityId JobId { get; }
    public CellId TargetCell { get; }
    public IReadOnlyList<TerrainWorkProducedOutput> Outputs => _outputs;
    public bool ProducedOutput => _outputs.Count > 0;
    public int TotalOutputQuantity => _outputs.Sum(value => value.Quantity);
    public long WorldVersion { get; }
    public long InventoryVersion { get; }

    // Compatibility for existing single-output callers.
    public EntityId OutputStackId => _outputs.Count == 1 && _outputs[0].StackIds.Count > 0
        ? _outputs[0].StackIds[0]
        : default;
    public ItemId OutputItemId => _outputs.Count == 1 ? _outputs[0].ItemId : default;
    public int OutputQuantity => _outputs.Count == 1
        ? _outputs[0].Quantity
        : TotalOutputQuantity;
}

}
