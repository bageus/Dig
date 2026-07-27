using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Ecology
{

public sealed class MushroomState : AggregateRoot
{
    private readonly MushroomCatalog _catalog;
    private readonly Dictionary<EntityId, MushroomSiteState> _sites = new Dictionary<EntityId, MushroomSiteState>();
    private readonly Dictionary<CellId, EntityId> _siteByCell = new Dictionary<CellId, EntityId>();

    public MushroomState(MushroomCatalog catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public MushroomCatalog Catalog => _catalog;

    public Result AddSite(
        EntityId siteId,
        MushroomDefinitionId definitionId,
        CellId cell,
        MushroomStage initialStage,
        long tick)
    {
        ValidateTick(tick);
        if (siteId.IsEmpty)
        {
            throw new ArgumentException("Mushroom site id cannot be empty.", nameof(siteId));
        }

        if (!_catalog.Contains(definitionId))
        {
            throw new KeyNotFoundException($"Mushroom definition '{definitionId}' was not found.");
        }

        if (_sites.ContainsKey(siteId))
        {
            return Result.Failure(MushroomErrors.AlreadyExists);
        }

        if (_siteByCell.ContainsKey(cell))
        {
            return Result.Failure(MushroomErrors.CellAlreadyOccupied);
        }

        MushroomSiteState site = MushroomSiteState.Create(
            siteId,
            definitionId,
            cell,
            initialStage,
            tick,
            _catalog.Get(definitionId).StageDurationTicks);
        _sites.Add(siteId, site);
        _siteByCell.Add(cell, siteId);
        return Result.Success();
    }

    public Result AdvanceGrowth(long tick)
    {
        ValidateTick(tick);
        foreach (MushroomSiteState site in _sites.Values.OrderBy(value => value.SiteId.ToString(), StringComparer.Ordinal))
        {
            AdvanceSiteGrowth(site, tick);
        }

        return Result.Success();
    }

    public Result BeginChop(
        EntityId siteId,
        EntityId jobId,
        EntityId workerId,
        int requiredSwings,
        long tick)
    {
        ValidateTick(tick);
        if (jobId.IsEmpty || workerId.IsEmpty || requiredSwings <= 0)
        {
            throw new ArgumentException("A valid job, worker and required swing count are required.");
        }

        MushroomSiteState? site = Find(siteId);
        if (site is null)
        {
            return Result.Failure(MushroomErrors.NotFound);
        }

        if (site.Stage == MushroomStage.AbsentRegrowing)
        {
            return Result.Failure(MushroomErrors.NotVisible);
        }

        if (site.ActiveChopJobId.HasValue)
        {
            return Result.Failure(MushroomErrors.ChopAlreadyActive);
        }

        site.BeginChop(jobId, workerId, requiredSwings, tick);
        Raise(new MushroomChopStarted(tick, siteId, jobId, workerId, requiredSwings));
        return Result.Success();
    }

    public Result ReleaseChop(EntityId siteId, EntityId jobId, EntityId workerId, long tick)
    {
        ValidateTick(tick);
        MushroomSiteState? site = Find(siteId);
        if (site is null)
        {
            return Result.Failure(MushroomErrors.NotFound);
        }

        Result ownership = ValidateOwnership(site, jobId, workerId);
        if (ownership.IsFailure)
        {
            return ownership;
        }

        site.ReleaseChop(tick);
        Raise(new MushroomChopReleased(tick, siteId, jobId, workerId));
        return Result.Success();
    }

    public Result<bool> CompleteSwing(EntityId siteId, EntityId jobId, EntityId workerId, long tick)
    {
        ValidateTick(tick);
        MushroomSiteState? site = Find(siteId);
        if (site is null)
        {
            return Result<bool>.Failure(MushroomErrors.NotFound);
        }

        Result ownership = ValidateOwnership(site, jobId, workerId);
        if (ownership.IsFailure)
        {
            return Result<bool>.Failure(ownership.Error!);
        }

        bool completed = site.CompleteSwing();
        Raise(new MushroomChopSwingCompleted(
            tick,
            siteId,
            jobId,
            workerId,
            site.CompletedSwings,
            site.RequiredSwings));
        return Result<bool>.Success(completed);
    }

    public Result<MushroomChopCommit> CommitChop(
        EntityId siteId,
        EntityId jobId,
        EntityId workerId,
        long tick)
    {
        ValidateTick(tick);
        MushroomSiteState? site = Find(siteId);
        if (site is null)
        {
            return Result<MushroomChopCommit>.Failure(MushroomErrors.NotFound);
        }

        Result ownership = ValidateOwnership(site, jobId, workerId);
        if (ownership.IsFailure)
        {
            return Result<MushroomChopCommit>.Failure(ownership.Error!);
        }

        if (site.CompletedSwings < site.RequiredSwings)
        {
            return Result<MushroomChopCommit>.Failure(MushroomErrors.ChopIncomplete);
        }

        MushroomStage choppedStage = site.Stage;
        MushroomDefinition definition = _catalog.Get(site.DefinitionId);
        MushroomDropProfile drops = definition.GetDrops(choppedStage);
        site.CommitChop(tick, definition.StageDurationTicks);
        string skillSourceId = $"mushroom:{site.SiteId}:{site.GrowthGeneration}";
        Raise(new MushroomChopped(
            tick,
            siteId,
            jobId,
            workerId,
            site.Cell,
            choppedStage,
            site.GrowthGeneration));
        Raise(new MushroomStageChanged(
            tick,
            siteId,
            choppedStage,
            MushroomStage.AbsentRegrowing,
            site.GrowthGeneration));

        return Result<MushroomChopCommit>.Success(new MushroomChopCommit(
            siteId,
            jobId,
            workerId,
            site.Cell,
            choppedStage,
            site.GrowthGeneration,
            definition.CapItemId,
            definition.LegItemId,
            drops,
            skillSourceId));
    }

    public MushroomSiteSnapshot? Get(EntityId siteId) => Find(siteId)?.Snapshot();

    public IReadOnlyList<MushroomSiteSnapshot> GetAll()
    {
        return new ReadOnlyCollection<MushroomSiteSnapshot>(_sites.Values
            .OrderBy(value => value.SiteId.ToString(), StringComparer.Ordinal)
            .Select(value => value.Snapshot())
            .ToArray());
    }

    public IReadOnlyList<CellId> GetBuildingBlockedCells()
    {
        return new ReadOnlyCollection<CellId>(_siteByCell.Keys.OrderBy(value => value).ToArray());
    }

    public static Result<MushroomState> Restore(
        MushroomCatalog catalog,
        IEnumerable<MushroomSiteSnapshot> snapshots)
    {
        if (catalog is null || snapshots is null)
        {
            throw new ArgumentNullException(catalog is null ? nameof(catalog) : nameof(snapshots));
        }

        MushroomState state = new MushroomState(catalog);
        foreach (MushroomSiteSnapshot snapshot in snapshots.OrderBy(value => value.SiteId.ToString(), StringComparer.Ordinal))
        {
            if (!catalog.Contains(snapshot.DefinitionId)
                || snapshot.SiteId.IsEmpty
                || state._sites.ContainsKey(snapshot.SiteId)
                || state._siteByCell.ContainsKey(snapshot.Cell)
                || !MushroomSiteState.IsValidSnapshot(snapshot))
            {
                return Result<MushroomState>.Failure(MushroomErrors.InvalidRestore);
            }

            MushroomSiteState site = MushroomSiteState.Restore(snapshot);
            state._sites.Add(site.SiteId, site);
            state._siteByCell.Add(site.Cell, site.SiteId);
        }

        return Result<MushroomState>.Success(state);
    }

    private void AdvanceSiteGrowth(MushroomSiteState site, long tick)
    {
        if (site.ActiveChopJobId.HasValue || !site.NextStageTick.HasValue)
        {
            return;
        }

        MushroomDefinition definition = _catalog.Get(site.DefinitionId);
        while (site.NextStageTick.HasValue && tick >= site.NextStageTick.Value)
        {
            long transitionTick = site.NextStageTick.Value;
            MushroomStage previous = site.Stage;
            site.AdvanceStage(transitionTick, definition.StageDurationTicks);
            Raise(new MushroomStageChanged(
                transitionTick,
                site.SiteId,
                previous,
                site.Stage,
                site.GrowthGeneration));
        }
    }

    private MushroomSiteState? Find(EntityId siteId)
    {
        if (siteId.IsEmpty)
        {
            throw new ArgumentException("Mushroom site id cannot be empty.", nameof(siteId));
        }

        return _sites.TryGetValue(siteId, out MushroomSiteState? site) ? site : null;
    }

    private static Result ValidateOwnership(MushroomSiteState site, EntityId jobId, EntityId workerId)
    {
        if (!site.ActiveChopJobId.HasValue)
        {
            return Result.Failure(MushroomErrors.ChopNotActive);
        }

        return site.ActiveChopJobId.Value != jobId || site.ActiveWorkerId != workerId
            ? Result.Failure(MushroomErrors.ChopOwnerMismatch)
            : Result.Success();
    }

    private static void ValidateTick(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }
    }

}

}
