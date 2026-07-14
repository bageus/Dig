using System;
using System.Collections.Generic;
using Dig.Application.Messaging;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Application.Buildings
{

public interface IBuildingsRepository
{
    BuildingsState Get();

    void Save(BuildingsState buildings);
}

public sealed class PlaceBuildingCommand : ICommand<Result>
{
    public PlaceBuildingCommand(
        EntityId buildingId,
        BuildingDefinitionId definitionId,
        CellId origin,
        BuildingOrientation orientation,
        IReadOnlyCollection<CellId> reachableCells,
        long tick)
    {
        BuildingId = buildingId;
        DefinitionId = definitionId;
        Origin = origin;
        Orientation = orientation;
        ReachableCells = reachableCells
            ?? throw new ArgumentNullException(nameof(reachableCells));
        Tick = tick;
    }

    public EntityId BuildingId { get; }

    public BuildingDefinitionId DefinitionId { get; }

    public CellId Origin { get; }

    public BuildingOrientation Orientation { get; }

    public IReadOnlyCollection<CellId> ReachableCells { get; }

    public long Tick { get; }
}

public sealed class CreateBuildingDeliveryCommand : ICommand<Result>
{
    public CreateBuildingDeliveryCommand(
        EntityId jobId,
        EntityId buildingId,
        EntityId sourceStackId,
        int quantity,
        int priority,
        long tick)
    {
        JobId = jobId;
        BuildingId = buildingId;
        SourceStackId = sourceStackId;
        Quantity = quantity;
        Priority = priority;
        Tick = tick;
    }

    public EntityId JobId { get; }

    public EntityId BuildingId { get; }

    public EntityId SourceStackId { get; }

    public int Quantity { get; }

    public int Priority { get; }

    public long Tick { get; }
}

public sealed class CompleteBuildingDeliveryCommand : ICommand<Result>
{
    public CompleteBuildingDeliveryCommand(EntityId jobId, EntityId splitStackId, long tick)
    {
        JobId = jobId;
        SplitStackId = splitStackId;
        Tick = tick;
    }

    public EntityId JobId { get; }

    public EntityId SplitStackId { get; }

    public long Tick { get; }
}

public sealed class RefreshBuildingMaterialsCommand : ICommand<Result>
{
    public RefreshBuildingMaterialsCommand(EntityId buildingId)
    {
        BuildingId = buildingId;
    }

    public EntityId BuildingId { get; }
}

public sealed class CreateConstructionJobCommand : ICommand<Result>
{
    public CreateConstructionJobCommand(
        EntityId jobId,
        EntityId buildingId,
        IReadOnlyCollection<CellId> reachableCells,
        int priority,
        long tick)
    {
        JobId = jobId;
        BuildingId = buildingId;
        ReachableCells = reachableCells
            ?? throw new ArgumentNullException(nameof(reachableCells));
        Priority = priority;
        Tick = tick;
    }

    public EntityId JobId { get; }

    public EntityId BuildingId { get; }

    public IReadOnlyCollection<CellId> ReachableCells { get; }

    public int Priority { get; }

    public long Tick { get; }
}

public sealed class AddConstructionWorkCommand : ICommand<Result>
{
    public AddConstructionWorkCommand(
        EntityId jobId,
        EntityId buildingId,
        int workAmount,
        long tick)
    {
        JobId = jobId;
        BuildingId = buildingId;
        WorkAmount = workAmount;
        Tick = tick;
    }

    public EntityId JobId { get; }

    public EntityId BuildingId { get; }

    public int WorkAmount { get; }

    public long Tick { get; }
}

public sealed class CompleteConstructionCommand : ICommand<Result>
{
    public CompleteConstructionCommand(EntityId jobId, EntityId buildingId, long tick)
    {
        JobId = jobId;
        BuildingId = buildingId;
        Tick = tick;
    }

    public EntityId JobId { get; }

    public EntityId BuildingId { get; }

    public long Tick { get; }
}

public sealed class CancelBuildingCommand : ICommand<Result>
{
    public CancelBuildingCommand(EntityId buildingId, string reason, long tick)
    {
        BuildingId = buildingId;
        Reason = reason;
        Tick = tick;
    }

    public EntityId BuildingId { get; }

    public string Reason { get; }

    public long Tick { get; }
}

public sealed class RemoveBuildingCommand : ICommand<Result>
{
    public RemoveBuildingCommand(EntityId buildingId, string reason, long tick)
    {
        BuildingId = buildingId;
        Reason = reason;
        Tick = tick;
    }

    public EntityId BuildingId { get; }

    public string Reason { get; }

    public long Tick { get; }
}

public sealed class RepairBuildingCommand : ICommand<Result>
{
    public RepairBuildingCommand(EntityId buildingId, int amount)
    {
        BuildingId = buildingId;
        Amount = amount;
    }

    public EntityId BuildingId { get; }

    public int Amount { get; }
}

public sealed class GetBuildingQuery : IQuery<BuildingSnapshot?>
{
    public GetBuildingQuery(EntityId buildingId)
    {
        BuildingId = buildingId;
    }

    public EntityId BuildingId { get; }
}
}
