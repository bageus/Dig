using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Farming;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Farming;
using Dig.Presentation.Agents;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private readonly InMemoryFarmRepository _farmRepository = new InMemoryFarmRepository();
    private readonly FarmItemCatalog _farmItems = FarmItemCatalog.Default;

    public Result AdvanceFarms(long tick, IReadOnlyList<AgentViewModel> agents)
    {
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
        SynchronizeFarmRegistrations();

        AdvanceFarmCommandHandler advance = new AdvanceFarmCommandHandler(_farmRepository);
        foreach (EntityId farmId in _farmRepository.GetFarmIds())
        {
            Result<FarmAdvanceResult> result = advance.Handle(new AdvanceFarmCommand(farmId, tick));
            if (result.IsFailure)
            {
                return Result.Failure(result.Error!);
            }

            Result escaped = MaterializeEscapedFarmAnimals(
                farmId,
                result.Value,
                tick);
            if (escaped.IsFailure) return escaped;
        }

        return SynchronizeFarmLogisticsRuntime(
            tick,
            agents,
            _worldSession.CreateTunnelNavigationVolume().Cells);
    }

    public FarmSnapshot? LoadFarmSnapshot(string buildingId)
    {
        if (string.IsNullOrWhiteSpace(buildingId))
        {
            return null;
        }

        SynchronizeFarmRegistrations();
        return new GetFarmSnapshotQueryHandler(_farmRepository).Handle(
            new GetFarmSnapshotQuery(EntityId.Parse(buildingId)));
    }

    public IReadOnlyList<FarmSupplyDemand> LoadFarmSupplyDemands(string buildingId)
    {
        if (string.IsNullOrWhiteSpace(buildingId))
        {
            return Array.Empty<FarmSupplyDemand>();
        }

        SynchronizeFarmRegistrations();
        return new GetFarmSupplyDemandsQueryHandler(_farmRepository, _farmItems).Handle(
            new GetFarmSupplyDemandsQuery(EntityId.Parse(buildingId)));
    }

    public Result SetFarmMode(string buildingId, FarmMode mode, long tick)
    {
        if (string.IsNullOrWhiteSpace(buildingId))
        {
            throw new ArgumentException("Building id is required.", nameof(buildingId));
        }

        SynchronizeFarmRegistrations();
        Result<FarmModeTransition> result = new SetFarmModeCommandHandler(_farmRepository).Handle(
            new SetFarmModeCommand(EntityId.Parse(buildingId), mode, tick));
        if (result.IsFailure) return Result.Failure(result.Error!);
        return MaterializeReleasedFarmFeed(
            EntityId.Parse(buildingId),
            result.Value.ReleasedFeed,
            tick);
    }

    public Result DeliverFarmStock(
        string buildingId,
        FarmDeliveryKind kind,
        int quantity,
        long tick)
    {
        if (string.IsNullOrWhiteSpace(buildingId))
        {
            throw new ArgumentException("Building id is required.", nameof(buildingId));
        }

        SynchronizeFarmRegistrations();
        return new DeliverFarmStockCommandHandler(_farmRepository).Handle(
            new DeliverFarmStockCommand(
                EntityId.Parse(buildingId),
                kind,
                quantity,
                tick));
    }

    public Result CollectFarmProduct(string buildingId, FarmDeliveryKind kind)
    {
        if (string.IsNullOrWhiteSpace(buildingId))
        {
            throw new ArgumentException("Building id is required.", nameof(buildingId));
        }

        SynchronizeFarmRegistrations();
        return new CollectFarmProductCommandHandler(_farmRepository).Handle(
            new CollectFarmProductCommand(EntityId.Parse(buildingId), kind));
    }

    private void SynchronizeFarmRegistrations()
    {
        if (_buildingsRepository == null)
        {
            foreach (EntityId existing in _farmRepository.GetFarmIds().ToArray())
            {
                _farmRepository.Remove(existing);
            }
            return;
        }

        EntityId[] active = _buildingsRepository.Get().GetAll()
            .Where(value => value.Status == BuildingStatus.Completed
                && value.Definition.Id == WorkshopProductionContent.FarmBuildingId)
            .Select(value => value.Id)
            .OrderBy(value => value.ToString(), StringComparer.Ordinal)
            .ToArray();
        HashSet<EntityId> activeSet = new HashSet<EntityId>(active);

        RegisterFarmCommandHandler register = new RegisterFarmCommandHandler(_farmRepository);
        foreach (EntityId farmId in active)
        {
            register.Handle(new RegisterFarmCommand(farmId));
        }

        foreach (EntityId existing in _farmRepository.GetFarmIds().ToArray())
        {
            if (!activeSet.Contains(existing))
            {
                _farmRepository.Remove(existing);
                _farmLogisticsReservations.ReleaseForFarm(existing);
            }
        }
    }
}

}
