using System;
using System.Collections.Generic;
using Dig.Application.Messaging;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Farming;
using Dig.Domain.Inventory;

namespace Dig.Application.Farming
{

public interface IFarmRepository
{
    IReadOnlyCollection<EntityId> GetFarmIds();
    FarmState? Get(EntityId buildingId);
    void Save(EntityId buildingId, FarmState state);
    void Remove(EntityId buildingId);
}

public sealed class FarmItemCatalog
{
    public FarmItemCatalog(ItemId mushroomCap, ItemId hamster, ItemId grub)
    {
        MushroomCap = mushroomCap;
        Hamster = hamster;
        Grub = grub;
    }

    public ItemId MushroomCap { get; }
    public ItemId Hamster { get; }
    public ItemId Grub { get; }

    public static FarmItemCatalog Default => new FarmItemCatalog(
        CampfireProductionContent.MushroomCapItemId,
        LivingMaterialContent.HamsterItemId,
        LivingMaterialContent.GrubItemId);

    public ItemId Resolve(FarmDeliveryKind kind)
    {
        switch (kind)
        {
            case FarmDeliveryKind.MushroomSeed:
            case FarmDeliveryKind.MushroomFeed:
                return MushroomCap;
            case FarmDeliveryKind.Hamster:
                return Hamster;
            case FarmDeliveryKind.Grub:
                return Grub;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }
}

public readonly struct FarmSupplyDemand
{
    public FarmSupplyDemand(FarmDeliveryKind kind, ItemId itemId, int quantity)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        Kind = kind;
        ItemId = itemId;
        Quantity = quantity;
    }

    public FarmDeliveryKind Kind { get; }
    public ItemId ItemId { get; }
    public int Quantity { get; }
}

public sealed class RegisterFarmCommand : ICommand<Result>
{
    public RegisterFarmCommand(EntityId buildingId, FarmMode initialMode = FarmMode.Mushrooms)
    {
        BuildingId = buildingId;
        InitialMode = initialMode;
    }

    public EntityId BuildingId { get; }
    public FarmMode InitialMode { get; }
}

public sealed class RemoveFarmCommand : ICommand<Result>
{
    public RemoveFarmCommand(EntityId buildingId)
    {
        BuildingId = buildingId;
    }

    public EntityId BuildingId { get; }
}

public sealed class SetFarmModeCommand : ICommand<Result<FarmModeTransition>>
{
    public SetFarmModeCommand(EntityId buildingId, FarmMode mode, long tick)
    {
        BuildingId = buildingId;
        Mode = mode;
        Tick = tick;
    }

    public EntityId BuildingId { get; }
    public FarmMode Mode { get; }
    public long Tick { get; }
}

public sealed class AdvanceFarmCommand : ICommand<Result<FarmAdvanceResult>>
{
    public AdvanceFarmCommand(EntityId buildingId, long tick)
    {
        BuildingId = buildingId;
        Tick = tick;
    }

    public EntityId BuildingId { get; }
    public long Tick { get; }
}

public sealed class DeliverFarmStockCommand : ICommand<Result>
{
    public DeliverFarmStockCommand(
        EntityId buildingId,
        FarmDeliveryKind kind,
        int quantity,
        long tick)
    {
        BuildingId = buildingId;
        Kind = kind;
        Quantity = quantity;
        Tick = tick;
    }

    public EntityId BuildingId { get; }
    public FarmDeliveryKind Kind { get; }
    public int Quantity { get; }
    public long Tick { get; }
}

public sealed class CollectFarmProductCommand : ICommand<Result>
{
    public CollectFarmProductCommand(EntityId buildingId, FarmDeliveryKind kind)
    {
        BuildingId = buildingId;
        Kind = kind;
    }

    public EntityId BuildingId { get; }
    public FarmDeliveryKind Kind { get; }
}

public sealed class GetFarmSupplyDemandsQuery : IQuery<IReadOnlyList<FarmSupplyDemand>>
{
    public GetFarmSupplyDemandsQuery(EntityId buildingId)
    {
        BuildingId = buildingId;
    }

    public EntityId BuildingId { get; }
}

public sealed class GetFarmSnapshotQuery : IQuery<FarmSnapshot?>
{
    public GetFarmSnapshotQuery(EntityId buildingId)
    {
        BuildingId = buildingId;
    }

    public EntityId BuildingId { get; }
}

public static class FarmApplicationErrors
{
    public static readonly DomainError MissingFarm = new DomainError(
        "farm.missing",
        "The farm is not registered for this building.");

    public static readonly DomainError ProductUnavailable = new DomainError(
        "farm.product_unavailable",
        "The farm has no collectable product above its protected reserve.");

    public static readonly DomainError InvalidDelivery = new DomainError(
        "farm.invalid_delivery",
        "The delivered stock does not match the farm's current demand.");
}

}
