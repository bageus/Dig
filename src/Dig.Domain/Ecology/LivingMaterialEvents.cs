using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Ecology
{

public sealed class LivingMaterialRegistered : IDomainEvent
{
    public LivingMaterialRegistered(long tick, EntityId creatureId, LivingMaterialSpecies species)
    {
        Tick = tick;
        CreatureId = creatureId;
        Species = species;
    }

    public long Tick { get; }
    public EntityId CreatureId { get; }
    public LivingMaterialSpecies Species { get; }
}

public sealed class LivingMaterialContainmentChanged : IDomainEvent
{
    public LivingMaterialContainmentChanged(
        long tick,
        EntityId creatureId,
        LivingMaterialContainment containment,
        CellId? cell)
    {
        Tick = tick;
        CreatureId = creatureId;
        Containment = containment;
        Cell = cell;
    }

    public long Tick { get; }
    public EntityId CreatureId { get; }
    public LivingMaterialContainment Containment { get; }
    public CellId? Cell { get; }
}

public sealed class LivingMaterialMoved : IDomainEvent
{
    public LivingMaterialMoved(long tick, EntityId creatureId, CellId from, CellId to)
    {
        Tick = tick;
        CreatureId = creatureId;
        From = from;
        To = to;
    }

    public long Tick { get; }
    public EntityId CreatureId { get; }
    public CellId From { get; }
    public CellId To { get; }
}

public sealed class LivingMaterialActivityChanged : IDomainEvent
{
    public LivingMaterialActivityChanged(
        long tick,
        EntityId creatureId,
        LivingMaterialActivity activity,
        int remainingSteps)
    {
        Tick = tick;
        CreatureId = creatureId;
        Activity = activity;
        RemainingSteps = remainingSteps;
    }

    public long Tick { get; }
    public EntityId CreatureId { get; }
    public LivingMaterialActivity Activity { get; }
    public int RemainingSteps { get; }
}

public sealed class LivingMaterialReproduced : IDomainEvent
{
    public LivingMaterialReproduced(
        long tick,
        EntityId parentId,
        EntityId offspringId,
        LivingMaterialSpecies species,
        CellId cell)
    {
        Tick = tick;
        ParentId = parentId;
        OffspringId = offspringId;
        Species = species;
        Cell = cell;
    }

    public long Tick { get; }
    public EntityId ParentId { get; }
    public EntityId OffspringId { get; }
    public LivingMaterialSpecies Species { get; }
    public CellId Cell { get; }
}

}
