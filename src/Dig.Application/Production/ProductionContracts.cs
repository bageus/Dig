using Dig.Application.Messaging;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Production;
using Dig.Domain.World;

namespace Dig.Application.Production;

public interface IProductionRepository
{
    ProductionState Get();

    void Save(ProductionState production);
}

public interface ITechnologyRepository
{
    Dig.Domain.Technology.TechnologyState Get();

    void Save(Dig.Domain.Technology.TechnologyState technology);
}

public interface IEnergyAvailability
{
    bool CanSupply(EntityId buildingId, int energyPerWorkTick);
}

public sealed class EnqueueProductionOrderCommand : ICommand<Result>
{
    public EnqueueProductionOrderCommand(
        EntityId orderId,
        RecipeId recipeId,
        EntityId buildingId,
        long tick)
    {
        OrderId = orderId;
        RecipeId = recipeId;
        BuildingId = buildingId;
        Tick = tick;
    }

    public EntityId OrderId { get; }

    public RecipeId RecipeId { get; }

    public EntityId BuildingId { get; }

    public long Tick { get; }
}

public sealed class PrepareProductionOrderCommand : ICommand<Result>
{
    public PrepareProductionOrderCommand(
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

public sealed class BeginProductionWorkCommand : ICommand<Result>
{
    public BeginProductionWorkCommand(EntityId orderId, EntityId jobId, long tick)
    {
        OrderId = orderId;
        JobId = jobId;
        Tick = tick;
    }

    public EntityId OrderId { get; }

    public EntityId JobId { get; }

    public long Tick { get; }
}

public sealed class ApplyProductionWorkCommand : ICommand<Result>
{
    public ApplyProductionWorkCommand(
        EntityId orderId,
        EntityId jobId,
        int baseWork,
        ProductionWorkContext context,
        long tick)
    {
        OrderId = orderId;
        JobId = jobId;
        BaseWork = baseWork;
        Context = context;
        Tick = tick;
    }

    public EntityId OrderId { get; }

    public EntityId JobId { get; }

    public int BaseWork { get; }

    public ProductionWorkContext Context { get; }

    public long Tick { get; }
}

public sealed class CompleteProductionOrderCommand : ICommand<Result>
{
    public CompleteProductionOrderCommand(
        EntityId orderId,
        EntityId jobId,
        IReadOnlyCollection<EntityId> outputStackIds,
        long tick)
    {
        OrderId = orderId;
        JobId = jobId;
        OutputStackIds = outputStackIds
            ?? throw new ArgumentNullException(nameof(outputStackIds));
        Tick = tick;
    }

    public EntityId OrderId { get; }

    public EntityId JobId { get; }

    public IReadOnlyCollection<EntityId> OutputStackIds { get; }

    public long Tick { get; }
}

public sealed class CancelProductionOrderCommand : ICommand<Result>
{
    public CancelProductionOrderCommand(
        EntityId orderId,
        EntityId jobId,
        string reason,
        long tick)
    {
        OrderId = orderId;
        JobId = jobId;
        Reason = reason;
        Tick = tick;
    }

    public EntityId OrderId { get; }

    public EntityId JobId { get; }

    public string Reason { get; }

    public long Tick { get; }
}

public sealed class UnlockTechnologyCommand : ICommand<Result>
{
    public UnlockTechnologyCommand(
        TechnologyId technologyId,
        ItemLocation researchLocation,
        long tick)
    {
        TechnologyId = technologyId;
        ResearchLocation = researchLocation;
        Tick = tick;
    }

    public TechnologyId TechnologyId { get; }

    public ItemLocation ResearchLocation { get; }

    public long Tick { get; }
}

public sealed class GetProductionOrdersQuery
    : IQuery<IReadOnlyList<ProductionOrderSnapshot>>
{
}
