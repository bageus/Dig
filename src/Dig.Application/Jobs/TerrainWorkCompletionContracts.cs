using Dig.Application.Messaging;
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
}

public sealed class CompleteTerrainWorkCommand
    : ICommand<Result<TerrainWorkCompletionResult>>
{
    public CompleteTerrainWorkCommand(
        EntityId jobId,
        EntityId outputStackId,
        ItemId outputItemId,
        int outputQuantity,
        MaterialId emptyMaterialId,
        long tick)
        : this(
            jobId,
            outputStackId,
            outputItemId,
            outputQuantity,
            emptyMaterialId,
            tick,
            producesOutput: true)
    {
    }

    private CompleteTerrainWorkCommand(
        EntityId jobId,
        EntityId outputStackId,
        ItemId outputItemId,
        int outputQuantity,
        MaterialId emptyMaterialId,
        long tick,
        bool producesOutput)
    {
        JobId = jobId;
        OutputStackId = outputStackId;
        OutputItemId = outputItemId;
        OutputQuantity = outputQuantity;
        EmptyMaterialId = emptyMaterialId;
        Tick = tick;
        ProducesOutput = producesOutput;
    }

    public EntityId JobId { get; }
    public EntityId OutputStackId { get; }
    public ItemId OutputItemId { get; }
    public int OutputQuantity { get; }
    public MaterialId EmptyMaterialId { get; }
    public long Tick { get; }
    public bool ProducesOutput { get; }

    public static CompleteTerrainWorkCommand WithoutOutput(
        EntityId jobId,
        MaterialId emptyMaterialId,
        long tick)
    {
        return new CompleteTerrainWorkCommand(
            jobId,
            default,
            default,
            outputQuantity: 0,
            emptyMaterialId,
            tick,
            producesOutput: false);
    }
}

public sealed class TerrainWorkCompletionResult
{
    public TerrainWorkCompletionResult(
        EntityId jobId,
        CellId targetCell,
        EntityId outputStackId,
        ItemId outputItemId,
        int outputQuantity,
        bool producedOutput,
        long worldVersion,
        long inventoryVersion)
    {
        JobId = jobId;
        TargetCell = targetCell;
        OutputStackId = outputStackId;
        OutputItemId = outputItemId;
        OutputQuantity = outputQuantity;
        ProducedOutput = producedOutput;
        WorldVersion = worldVersion;
        InventoryVersion = inventoryVersion;
    }

    public EntityId JobId { get; }
    public CellId TargetCell { get; }
    public EntityId OutputStackId { get; }
    public ItemId OutputItemId { get; }
    public int OutputQuantity { get; }
    public bool ProducedOutput { get; }
    public long WorldVersion { get; }
    public long InventoryVersion { get; }
}

}
