using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Buildings
{

internal sealed class BuildingBoxPlanState
{
    public BuildingBoxPlanState(EntityId sourceStackId, EntityId jobId)
    {
        if (sourceStackId.IsEmpty || jobId.IsEmpty)
        {
            throw new ArgumentException("Box plan ids cannot be empty.");
        }

        SourceStackId = sourceStackId;
        JobId = jobId;
        CommitState = BuildingBoxCommitState.Reserved;
    }

    public EntityId SourceStackId { get; }

    public EntityId JobId { get; }

    public BuildingBoxCommitState CommitState { get; private set; }

    internal static Result<BuildingBoxPlanState> Restore(
        BuildingBoxPlanSnapshot snapshot,
        BuildingStatus buildingStatus)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        bool statusMatches = snapshot.CommitState switch
        {
            BuildingBoxCommitState.Reserved => buildingStatus is
                BuildingStatus.AwaitingBox or BuildingStatus.Cancelled,
            BuildingBoxCommitState.AtSite => buildingStatus is
                BuildingStatus.ReadyToBuild
                or BuildingStatus.UnderConstruction
                or BuildingStatus.ReadyToComplete
                or BuildingStatus.Cancelled,
            BuildingBoxCommitState.Consumed => buildingStatus is
                BuildingStatus.Completed
                or BuildingStatus.Damaged
                or BuildingStatus.Removed,
            _ => false,
        };
        if (!statusMatches)
        {
            return Result<BuildingBoxPlanState>.Failure(new DomainError(
                "buildings.restore.invalid_box_commit",
                "Saved box commitment does not match the building status."));
        }

        BuildingBoxPlanState restored = new BuildingBoxPlanState(
            snapshot.SourceStackId,
            snapshot.JobId)
        {
            CommitState = snapshot.CommitState,
        };
        return Result<BuildingBoxPlanState>.Success(restored);
    }

    public void MarkAtSite()
    {
        if (CommitState != BuildingBoxCommitState.Reserved)
        {
            throw new InvalidOperationException("Only a reserved box can be committed to site.");
        }

        CommitState = BuildingBoxCommitState.AtSite;
    }

    public void MarkConsumed()
    {
        if (CommitState != BuildingBoxCommitState.AtSite)
        {
            throw new InvalidOperationException("Only a site box can be consumed.");
        }

        CommitState = BuildingBoxCommitState.Consumed;
    }

    public BuildingBoxPlanSnapshot CreateSnapshot()
    {
        return new BuildingBoxPlanSnapshot(SourceStackId, JobId, CommitState);
    }
}

public sealed partial class BuildingsState
{
    private readonly Dictionary<EntityId, BuildingBoxPlanState> _boxPlans =
        new Dictionary<EntityId, BuildingBoxPlanState>();

    public static Result<BuildingsState> Restore(
        IEnumerable<BuildingSnapshot> snapshots)
    {
        if (snapshots is null)
        {
            throw new ArgumentNullException(nameof(snapshots));
        }

        BuildingsState buildings = new BuildingsState();
        foreach (BuildingSnapshot snapshot in snapshots
            .OrderBy(item => item.Id.ToString(), StringComparer.Ordinal))
        {
            if (buildings._buildings.ContainsKey(snapshot.Id))
            {
                return InvalidRestore("Saved buildings contain duplicate ids.");
            }

            bool boxPolicy = snapshot.Definition.ConstructionPolicy
                == BuildingConstructionPolicyKind.BuildingBox;
            if (boxPolicy != (snapshot.BoxPlan is not null))
            {
                return InvalidRestore(
                    "Saved building box plan does not match its construction policy.");
            }

            Result<BuildingProjectState> project = BuildingProjectState.Restore(snapshot);
            if (project.IsFailure)
            {
                return Result<BuildingsState>.Failure(project.Error!);
            }

            buildings._buildings.Add(snapshot.Id, project.Value);
            if (snapshot.BoxPlan is not null)
            {
                Result<BuildingBoxPlanState> box = BuildingBoxPlanState.Restore(
                    snapshot.BoxPlan,
                    snapshot.Status);
                if (box.IsFailure)
                {
                    return Result<BuildingsState>.Failure(box.Error!);
                }

                buildings._boxPlans.Add(snapshot.Id, box.Value);
            }
        }

        return Result<BuildingsState>.Success(buildings);
    }

    public Result PlaceBoxPlan(
        EntityId id,
        EntityId sourceStackId,
        EntityId jobId,
        BuildingDefinition definition,
        CellId origin,
        BuildingOrientation orientation,
        BuildingPlacementResult placement,
        long tick)
    {
        ValidateTick(tick);
        if (id.IsEmpty || sourceStackId.IsEmpty || jobId.IsEmpty)
        {
            throw new ArgumentException("Building, source stack and job ids are required.");
        }

        if (definition is null || placement is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (definition.ConstructionPolicy != BuildingConstructionPolicyKind.BuildingBox)
        {
            return Result.Failure(BuildingErrors.WrongConstructionPolicy);
        }

        if (!placement.Succeeded)
        {
            return Result.Failure(placement.Error!);
        }

        if (_buildings.ContainsKey(id) || _boxPlans.ContainsKey(id))
        {
            return Result.Failure(BuildingErrors.AlreadyExists);
        }

        BuildingProjectState project = new BuildingProjectState(
            id,
            definition,
            origin,
            orientation,
            placement.Footprint,
            placement.WorkPosition,
            BuildingStatus.AwaitingBox);
        _buildings.Add(id, project);
        _boxPlans.Add(id, new BuildingBoxPlanState(sourceStackId, jobId));
        Raise(new BuildingPlaced(tick, id, definition.Id));
        Raise(new BuildingBoxPlanCreated(
            tick,
            id,
            sourceStackId,
            jobId,
            definition.Id));
        return Result.Success();
    }

    public Result MarkBoxAtSite(EntityId id, long tick)
    {
        ValidateTick(tick);
        if (!TryGetBoxProject(id, out BuildingProjectState project, out BuildingBoxPlanState box))
        {
            return Result.Failure(BuildingErrors.BoxPlanNotFound);
        }

        if (project.Status != BuildingStatus.AwaitingBox
            || box.CommitState != BuildingBoxCommitState.Reserved)
        {
            return Result.Failure(BuildingErrors.InvalidStatus);
        }

        BuildingBoxCommitState previous = box.CommitState;
        box.MarkAtSite();
        project.MarkMaterialsReady();
        Raise(new BuildingBoxCommitChanged(tick, id, previous, box.CommitState));
        return Result.Success();
    }

    public Result CompleteBoxConstruction(EntityId id, long tick)
    {
        ValidateTick(tick);
        if (!TryGetBoxProject(id, out BuildingProjectState project, out BuildingBoxPlanState box))
        {
            return Result.Failure(BuildingErrors.BoxPlanNotFound);
        }

        if (project.Status != BuildingStatus.ReadyToComplete
            || box.CommitState != BuildingBoxCommitState.AtSite)
        {
            return Result.Failure(
                project.CompletedWork < project.Definition.RequiredWork
                    ? BuildingErrors.WorkIncomplete
                    : BuildingErrors.InvalidStatus);
        }

        BuildingBoxCommitState previous = box.CommitState;
        box.MarkConsumed();
        project.Complete();
        Raise(new BuildingBoxCommitChanged(tick, id, previous, box.CommitState));
        Raise(new BuildingCompleted(tick, id));
        return Result.Success();
    }

    public BuildingBoxPlanSnapshot? GetBoxPlan(EntityId id)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("Building id cannot be empty.", nameof(id));
        }

        return _boxPlans.TryGetValue(id, out BuildingBoxPlanState? value)
            ? value.CreateSnapshot()
            : null;
    }

    private BuildingSnapshot CreateSnapshot(BuildingProjectState project)
    {
        BuildingSnapshot snapshot = project.CreateSnapshot();
        BuildingBoxPlanSnapshot? box = _boxPlans.TryGetValue(
            project.Id,
            out BuildingBoxPlanState? state)
            ? state.CreateSnapshot()
            : null;
        return new BuildingSnapshot(
            snapshot.Id,
            snapshot.Definition,
            snapshot.Origin,
            snapshot.Orientation,
            snapshot.Footprint,
            snapshot.WorkPosition,
            snapshot.Status,
            snapshot.CompletedWork,
            snapshot.Durability,
            snapshot.Version,
            snapshot.DiagnosticReason,
            box);
    }

    private bool TryGetBoxProject(
        EntityId id,
        out BuildingProjectState project,
        out BuildingBoxPlanState box)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("Building id cannot be empty.", nameof(id));
        }

        if (_buildings.TryGetValue(id, out BuildingProjectState? foundProject)
            && _boxPlans.TryGetValue(id, out BuildingBoxPlanState? foundBox))
        {
            project = foundProject;
            box = foundBox;
            return true;
        }

        project = null!;
        box = null!;
        return false;
    }

    private static Result<BuildingsState> InvalidRestore(string message)
    {
        return Result<BuildingsState>.Failure(new DomainError(
            "buildings.restore.invalid_state",
            message));
    }
}
}