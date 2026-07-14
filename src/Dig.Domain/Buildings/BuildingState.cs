using System.Collections.ObjectModel;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Buildings;

public sealed class BuildingsState : AggregateRoot
{
    private readonly Dictionary<EntityId, BuildingProjectState> _buildings =
        new Dictionary<EntityId, BuildingProjectState>();

    public Result Place(
        EntityId id,
        BuildingDefinition definition,
        CellId origin,
        BuildingOrientation orientation,
        BuildingPlacementResult placement,
        long tick)
    {
        ValidateTick(tick);
        if (id.IsEmpty)
        {
            throw new ArgumentException("Building id cannot be empty.", nameof(id));
        }

        if (definition is null || placement is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (!placement.Succeeded)
        {
            return Result.Failure(placement.Error!);
        }

        if (_buildings.ContainsKey(id))
        {
            return Result.Failure(BuildingErrors.AlreadyExists);
        }

        BuildingProjectState project = new BuildingProjectState(
            id,
            definition,
            origin,
            orientation,
            placement.Footprint,
            placement.WorkPosition);
        _buildings.Add(id, project);
        Raise(new BuildingPlaced(tick, id, definition.Id));
        return Result.Success();
    }

    public Result MarkMaterialsReady(EntityId id)
    {
        BuildingProjectState? project = Find(id);
        if (project is null)
        {
            return Result.Failure(BuildingErrors.NotFound);
        }

        if (project.Status != BuildingStatus.AwaitingMaterials)
        {
            return Result.Failure(BuildingErrors.InvalidStatus);
        }

        project.MarkMaterialsReady();
        return Result.Success();
    }

    public Result StartConstruction(EntityId id)
    {
        BuildingProjectState? project = Find(id);
        if (project is null)
        {
            return Result.Failure(BuildingErrors.NotFound);
        }

        if (project.Status != BuildingStatus.ReadyToBuild)
        {
            return Result.Failure(BuildingErrors.InvalidStatus);
        }

        project.StartConstruction();
        return Result.Success();
    }

    public Result AddConstructionWork(EntityId id, int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        BuildingProjectState? project = Find(id);
        if (project is null)
        {
            return Result.Failure(BuildingErrors.NotFound);
        }

        if (project.Status != BuildingStatus.UnderConstruction)
        {
            return Result.Failure(BuildingErrors.InvalidStatus);
        }

        project.AddWork(amount);
        return Result.Success();
    }

    public Result Complete(EntityId id, long tick)
    {
        ValidateTick(tick);
        BuildingProjectState? project = Find(id);
        if (project is null)
        {
            return Result.Failure(BuildingErrors.NotFound);
        }

        if (project.Status != BuildingStatus.ReadyToComplete)
        {
            return Result.Failure(
                project.CompletedWork < project.Definition.RequiredWork
                    ? BuildingErrors.WorkIncomplete
                    : BuildingErrors.InvalidStatus);
        }

        project.Complete();
        Raise(new BuildingCompleted(tick, id));
        return Result.Success();
    }

    public Result Block(EntityId id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Block reason is required.", nameof(reason));
        }

        BuildingProjectState? project = Find(id);
        if (project is null)
        {
            return Result.Failure(BuildingErrors.NotFound);
        }

        project.Block(reason.Trim());
        return Result.Success();
    }

    public Result Cancel(EntityId id, string reason, long tick)
    {
        return RemoveCore(id, reason, tick, cancel: true);
    }

    public Result Remove(EntityId id, string reason, long tick)
    {
        return RemoveCore(id, reason, tick, cancel: false);
    }

    public Result Damage(EntityId id, int amount, long tick)
    {
        ValidateTick(tick);
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        BuildingProjectState? project = Find(id);
        if (project is null)
        {
            return Result.Failure(BuildingErrors.NotFound);
        }

        if (project.Status != BuildingStatus.Completed
            && project.Status != BuildingStatus.Damaged)
        {
            return Result.Failure(BuildingErrors.InvalidStatus);
        }

        project.Damage(amount);
        Raise(new BuildingDamaged(tick, id, project.Durability));
        return Result.Success();
    }

    public Result Repair(EntityId id, int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        BuildingProjectState? project = Find(id);
        if (project is null)
        {
            return Result.Failure(BuildingErrors.NotFound);
        }

        if (project.Status != BuildingStatus.Damaged)
        {
            return Result.Failure(BuildingErrors.InvalidStatus);
        }

        project.Repair(amount);
        return Result.Success();
    }

    public BuildingSnapshot? Get(EntityId id)
    {
        return Find(id)?.CreateSnapshot();
    }

    public IReadOnlyList<BuildingSnapshot> GetAll()
    {
        return new ReadOnlyCollection<BuildingSnapshot>(_buildings.Values
            .Select(value => value.CreateSnapshot())
            .OrderBy(value => value.Id.ToString(), StringComparer.Ordinal)
            .ToArray());
    }

    public IReadOnlyList<CellId> GetOccupiedCells()
    {
        return new ReadOnlyCollection<CellId>(_buildings.Values
            .Select(value => value.CreateSnapshot())
            .Where(value => value.IsActive)
            .SelectMany(value => value.Footprint)
            .Distinct()
            .OrderBy(value => value)
            .ToArray());
    }

    private Result RemoveCore(EntityId id, string reason, long tick, bool cancel)
    {
        ValidateTick(tick);
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Removal reason is required.", nameof(reason));
        }

        BuildingProjectState? project = Find(id);
        if (project is null)
        {
            return Result.Failure(BuildingErrors.NotFound);
        }

        bool canCancel = project.Status is BuildingStatus.AwaitingMaterials
            or BuildingStatus.ReadyToBuild
            or BuildingStatus.UnderConstruction
            or BuildingStatus.ReadyToComplete;
        bool canRemove = project.Status is BuildingStatus.Completed or BuildingStatus.Damaged;
        if ((cancel && !canCancel) || (!cancel && !canRemove))
        {
            return Result.Failure(BuildingErrors.InvalidStatus);
        }

        if (cancel)
        {
            project.Cancel(reason.Trim());
        }
        else
        {
            project.Remove(reason.Trim());
        }

        Raise(new BuildingRemoved(tick, id, reason.Trim()));
        return Result.Success();
    }

    private BuildingProjectState? Find(EntityId id)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("Building id cannot be empty.", nameof(id));
        }

        return _buildings.TryGetValue(id, out BuildingProjectState? value) ? value : null;
    }

    private static void ValidateTick(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }
    }
}
