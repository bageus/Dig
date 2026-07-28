using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Domain.WorldObjects
{

public sealed class BarrelState : AggregateRoot
{
    private readonly BarrelCatalog _catalog;
    private readonly Dictionary<EntityId, BarrelRecord> _barrels =
        new Dictionary<EntityId, BarrelRecord>();
    private readonly Dictionary<CellId, EntityId> _barrelByCell =
        new Dictionary<CellId, EntityId>();

    public BarrelState(BarrelCatalog catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public BarrelCatalog Catalog => _catalog;

    public Result Add(
        EntityId barrelId,
        BarrelDefinitionId definitionId,
        CellId cell,
        ItemId contentsItemId,
        long tick)
    {
        ValidateTick(tick);
        if (barrelId.IsEmpty || definitionId.IsEmpty || contentsItemId.IsEmpty)
        {
            throw new ArgumentException(
                "Barrel id, definition id and contents item id are required.");
        }

        if (!_catalog.Contains(definitionId))
        {
            throw new KeyNotFoundException(
                $"Barrel definition '{definitionId}' was not found.");
        }

        if (!_catalog.Get(definitionId).Supports(contentsItemId))
        {
            throw new ArgumentException(
                "The contents item is not allowed by the barrel definition.",
                nameof(contentsItemId));
        }

        if (_barrels.ContainsKey(barrelId))
        {
            return Result.Failure(BarrelErrors.AlreadyExists);
        }

        if (_barrelByCell.ContainsKey(cell))
        {
            return Result.Failure(BarrelErrors.CellAlreadyOccupied);
        }

        BarrelRecord record = new BarrelRecord(
            barrelId,
            definitionId,
            cell,
            BarrelLifecycle.Supported,
            contentsItemId,
            contentsGeneration: 0,
            contentsMaterialized: false,
            fallSourceCell: null,
            fallLandingCell: null,
            version: 0);
        _barrels.Add(barrelId, record);
        _barrelByCell.Add(cell, barrelId);
        Raise(new BarrelCreated(tick, barrelId, cell, contentsItemId));
        return Result.Success();
    }

    public BarrelSnapshot? Get(EntityId barrelId)
    {
        if (barrelId.IsEmpty)
        {
            throw new ArgumentException("Barrel id cannot be empty.", nameof(barrelId));
        }

        return _barrels.TryGetValue(barrelId, out BarrelRecord? barrel)
            ? barrel.Snapshot()
            : null;
    }

    public IReadOnlyList<BarrelSnapshot> GetAll()
    {
        return new ReadOnlyCollection<BarrelSnapshot>(_barrels.Values
            .OrderBy(value => value.BarrelId.ToString(), StringComparer.Ordinal)
            .Select(value => value.Snapshot())
            .ToArray());
    }

    public IReadOnlyList<CellId> GetBuildingBlockedCells()
    {
        return new ReadOnlyCollection<CellId>(_barrels.Values
            .Where(value => value.Lifecycle == BarrelLifecycle.Supported)
            .Select(value => value.Cell)
            .Distinct()
            .OrderBy(value => value)
            .ToArray());
    }

    public Result BeginFall(EntityId barrelId, CellId landingCell, long tick)
    {
        ValidateTick(tick);
        BarrelRecord? barrel = Find(barrelId);
        if (barrel is null)
        {
            return Result.Failure(BarrelErrors.NotFound);
        }

        if (barrel.Lifecycle != BarrelLifecycle.Supported)
        {
            return Result.Failure(BarrelErrors.FallNotAllowed);
        }

        CellId sourceCell = barrel.Cell;
        _barrelByCell.Remove(sourceCell);
        barrel.BeginFall(sourceCell, landingCell);
        Raise(new BarrelSupportLost(tick, barrelId, sourceCell, landingCell));
        return Result.Success();
    }

    public Result Land(EntityId barrelId, long tick)
    {
        ValidateTick(tick);
        BarrelRecord? barrel = Find(barrelId);
        if (barrel is null)
        {
            return Result.Failure(BarrelErrors.NotFound);
        }

        if (barrel.Lifecycle != BarrelLifecycle.Falling
            || !barrel.FallSourceCell.HasValue
            || !barrel.FallLandingCell.HasValue)
        {
            return Result.Failure(BarrelErrors.LandingNotAllowed);
        }

        CellId source = barrel.FallSourceCell.Value;
        CellId landing = barrel.FallLandingCell.Value;
        if (_barrelByCell.ContainsKey(landing))
        {
            return Result.Failure(BarrelErrors.CellAlreadyOccupied);
        }

        barrel.Land(landing);
        _barrelByCell.Add(landing, barrelId);
        Raise(new BarrelLanded(tick, barrelId, source, landing));
        return Result.Success();
    }

    public Result<BarrelDestructionCommit> Destroy(
        EntityId barrelId,
        long expectedVersion,
        EntityId jobId,
        EntityId workerId,
        long tick)
    {
        ValidateTick(tick);
        if (jobId.IsEmpty || workerId.IsEmpty || expectedVersion < 0)
        {
            throw new ArgumentException(
                "A valid expected version, job and worker are required.");
        }

        BarrelRecord? barrel = Find(barrelId);
        if (barrel is null)
        {
            return Result<BarrelDestructionCommit>.Failure(BarrelErrors.NotFound);
        }

        if (barrel.Lifecycle != BarrelLifecycle.Supported)
        {
            return Result<BarrelDestructionCommit>.Failure(BarrelErrors.NotAttackable);
        }

        if (barrel.Version != expectedVersion)
        {
            return Result<BarrelDestructionCommit>.Failure(BarrelErrors.VersionConflict);
        }

        if (barrel.ContentsMaterialized)
        {
            return Result<BarrelDestructionCommit>.Failure(
                BarrelErrors.ContentsAlreadyMaterialized);
        }

        CellId cell = barrel.Cell;
        barrel.Destroy();
        _barrelByCell.Remove(cell);
        Raise(new BarrelDestroyed(
            tick,
            barrelId,
            jobId,
            workerId,
            cell,
            barrel.ContentsItemId,
            barrel.ContentsGeneration));
        return Result<BarrelDestructionCommit>.Success(new BarrelDestructionCommit(
            barrelId,
            jobId,
            workerId,
            cell,
            barrel.ContentsItemId,
            barrel.ContentsGeneration,
            barrel.Version));
    }

    public void RecordContentsMaterialized(
        BarrelDestructionCommit commit,
        EntityId outputUnitId,
        long tick)
    {
        if (commit is null)
        {
            throw new ArgumentNullException(nameof(commit));
        }

        if (outputUnitId.IsEmpty)
        {
            throw new ArgumentException(
                "Output unit id cannot be empty.",
                nameof(outputUnitId));
        }

        Raise(new BarrelContentsMaterialized(
            tick,
            commit.BarrelId,
            outputUnitId,
            commit.ContentsItemId,
            commit.Cell,
            commit.ContentsGeneration));
    }

    public static Result<BarrelState> Restore(
        BarrelCatalog catalog,
        IEnumerable<BarrelSnapshot> snapshots)
    {
        if (catalog is null || snapshots is null)
        {
            throw new ArgumentNullException(
                catalog is null ? nameof(catalog) : nameof(snapshots));
        }

        BarrelState state = new BarrelState(catalog);
        foreach (BarrelSnapshot snapshot in snapshots
            .OrderBy(value => value.BarrelId.ToString(), StringComparer.Ordinal))
        {
            if (!IsValidRestoreSnapshot(state, catalog, snapshot))
            {
                return Result<BarrelState>.Failure(BarrelErrors.InvalidRestore);
            }

            BarrelRecord record = new BarrelRecord(
                snapshot.BarrelId,
                snapshot.DefinitionId,
                snapshot.Cell,
                snapshot.Lifecycle,
                snapshot.ContentsItemId,
                snapshot.ContentsGeneration,
                snapshot.ContentsMaterialized,
                snapshot.FallSourceCell,
                snapshot.FallLandingCell,
                snapshot.Version);
            state._barrels.Add(record.BarrelId, record);
            if (record.Lifecycle == BarrelLifecycle.Supported)
            {
                state._barrelByCell.Add(record.Cell, record.BarrelId);
            }
        }

        return Result<BarrelState>.Success(state);
    }

    private static bool IsValidRestoreSnapshot(
        BarrelState state,
        BarrelCatalog catalog,
        BarrelSnapshot snapshot)
    {
        return !snapshot.BarrelId.IsEmpty
            && catalog.Contains(snapshot.DefinitionId)
            && catalog.Get(snapshot.DefinitionId).Supports(snapshot.ContentsItemId)
            && snapshot.ContentsGeneration >= 0
            && snapshot.Version >= 0
            && Enum.IsDefined(typeof(BarrelLifecycle), snapshot.Lifecycle)
            && !state._barrels.ContainsKey(snapshot.BarrelId)
            && (snapshot.Lifecycle != BarrelLifecycle.Supported
                || !state._barrelByCell.ContainsKey(snapshot.Cell))
            && (snapshot.Lifecycle != BarrelLifecycle.Destroyed
                || snapshot.ContentsMaterialized)
            && (snapshot.Lifecycle != BarrelLifecycle.Falling
                || (snapshot.FallSourceCell.HasValue
                    && snapshot.FallLandingCell.HasValue));
    }

    private BarrelRecord? Find(EntityId barrelId) =>
        _barrels.TryGetValue(barrelId, out BarrelRecord? barrel) ? barrel : null;

    private static void ValidateTick(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }
    }
}

}