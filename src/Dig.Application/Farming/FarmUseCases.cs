using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Farming;

namespace Dig.Application.Farming
{

public sealed class RegisterFarmCommandHandler
    : ICommandHandler<RegisterFarmCommand, Result>
{
    private readonly IFarmRepository _repository;

    public RegisterFarmCommandHandler(IFarmRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Result Handle(RegisterFarmCommand command)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        if (_repository.Get(command.BuildingId) == null)
        {
            _repository.Save(command.BuildingId, new FarmState(command.InitialMode));
        }
        return Result.Success();
    }
}

public sealed class RemoveFarmCommandHandler
    : ICommandHandler<RemoveFarmCommand, Result>
{
    private readonly IFarmRepository _repository;

    public RemoveFarmCommandHandler(IFarmRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Result Handle(RemoveFarmCommand command)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        _repository.Remove(command.BuildingId);
        return Result.Success();
    }
}

public sealed class SetFarmModeCommandHandler
    : ICommandHandler<SetFarmModeCommand, Result<FarmModeTransition>>
{
    private readonly IFarmRepository _repository;

    public SetFarmModeCommandHandler(IFarmRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Result<FarmModeTransition> Handle(SetFarmModeCommand command)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        FarmState? farm = _repository.Get(command.BuildingId);
        if (farm == null) return Result<FarmModeTransition>.Failure(FarmApplicationErrors.MissingFarm);
        FarmModeTransition transition = farm.SwitchMode(command.Mode, command.Tick);
        _repository.Save(command.BuildingId, farm);
        return Result<FarmModeTransition>.Success(transition);
    }
}

public sealed class AdvanceFarmCommandHandler
    : ICommandHandler<AdvanceFarmCommand, Result<FarmAdvanceResult>>
{
    private readonly IFarmRepository _repository;

    public AdvanceFarmCommandHandler(IFarmRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Result<FarmAdvanceResult> Handle(AdvanceFarmCommand command)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        FarmState? farm = _repository.Get(command.BuildingId);
        if (farm == null) return Result<FarmAdvanceResult>.Failure(FarmApplicationErrors.MissingFarm);
        FarmAdvanceResult result = farm.Advance(command.Tick);
        _repository.Save(command.BuildingId, farm);
        return Result<FarmAdvanceResult>.Success(result);
    }
}

public sealed class DeliverFarmStockCommandHandler
    : ICommandHandler<DeliverFarmStockCommand, Result>
{
    private readonly IFarmRepository _repository;

    public DeliverFarmStockCommandHandler(IFarmRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Result Handle(DeliverFarmStockCommand command)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        FarmState? farm = _repository.Get(command.BuildingId);
        if (farm == null) return Result.Failure(FarmApplicationErrors.MissingFarm);
        FarmDeliveryDemand? demand = farm.GetDeliveryDemands()
            .Cast<FarmDeliveryDemand?>()
            .FirstOrDefault(value => value!.Value.Kind == command.Kind);
        if (!demand.HasValue || command.Quantity <= 0 || command.Quantity > demand.Value.Quantity)
        {
            return Result.Failure(FarmApplicationErrors.InvalidDelivery);
        }
        farm.Deliver(command.Kind, command.Quantity, command.Tick);
        _repository.Save(command.BuildingId, farm);
        return Result.Success();
    }
}

public sealed class CollectFarmProductCommandHandler
    : ICommandHandler<CollectFarmProductCommand, Result>
{
    private readonly IFarmRepository _repository;

    public CollectFarmProductCommandHandler(IFarmRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Result Handle(CollectFarmProductCommand command)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        FarmState? farm = _repository.Get(command.BuildingId);
        if (farm == null) return Result.Failure(FarmApplicationErrors.MissingFarm);
        bool collected;
        switch (command.Kind)
        {
            case FarmDeliveryKind.MushroomSeed:
                collected = farm.HarvestMushroom();
                break;
            case FarmDeliveryKind.Hamster:
                collected = farm.CollectHamster();
                break;
            case FarmDeliveryKind.Grub:
                collected = farm.CollectGrub();
                break;
            default:
                collected = false;
                break;
        }
        if (!collected) return Result.Failure(FarmApplicationErrors.ProductUnavailable);
        _repository.Save(command.BuildingId, farm);
        return Result.Success();
    }
}

public sealed class GetFarmSupplyDemandsQueryHandler
    : IQueryHandler<GetFarmSupplyDemandsQuery, IReadOnlyList<FarmSupplyDemand>>
{
    private readonly IFarmRepository _repository;
    private readonly FarmItemCatalog _items;

    public GetFarmSupplyDemandsQueryHandler(IFarmRepository repository, FarmItemCatalog items)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _items = items ?? throw new ArgumentNullException(nameof(items));
    }

    public IReadOnlyList<FarmSupplyDemand> Handle(GetFarmSupplyDemandsQuery query)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        FarmState? farm = _repository.Get(query.BuildingId);
        if (farm == null) return Array.Empty<FarmSupplyDemand>();
        return farm.GetDeliveryDemands()
            .Select(value => new FarmSupplyDemand(value.Kind, _items.Resolve(value.Kind), value.Quantity))
            .ToArray();
    }
}

public sealed class GetFarmSnapshotQueryHandler
    : IQueryHandler<GetFarmSnapshotQuery, FarmSnapshot?>
{
    private readonly IFarmRepository _repository;

    public GetFarmSnapshotQueryHandler(IFarmRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public FarmSnapshot? Handle(GetFarmSnapshotQuery query)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        return _repository.Get(query.BuildingId)?.CreateSnapshot();
    }
}

}
