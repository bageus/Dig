using System;
using Dig.Domain.Core;

namespace Dig.Domain.Buildings
{

internal sealed class BuildingPackingPlanState
{
    public BuildingPackingPlanState(EntityId jobId, EntityId outputStackId)
    {
        if (jobId.IsEmpty || outputStackId.IsEmpty)
        {
            throw new ArgumentException("Packing job and output stack ids are required.");
        }

        JobId = jobId;
        OutputStackId = outputStackId;
        CommitState = BuildingPackingCommitState.Active;
    }

    public EntityId JobId { get; }

    public EntityId OutputStackId { get; }

    public int CompletedWork { get; private set; }

    public BuildingPackingCommitState CommitState { get; private set; }

    internal static Result<BuildingPackingPlanState> Restore(
        BuildingPackingPlanSnapshot snapshot,
        BuildingSnapshot building)
    {
        if (snapshot is null || building is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        BuildingBoxPolicy? policy = building.Definition.BoxPolicy;
        if (policy is null
            || !policy.PackingEnabled
            || snapshot.CompletedWork > policy.PackingWork)
        {
            return Invalid("Saved packing plan does not match its building policy.");
        }

        bool stateMatches = snapshot.CommitState switch
        {
            BuildingPackingCommitState.Active => building.Status == BuildingStatus.Completed,
            BuildingPackingCommitState.Completed => building.Status == BuildingStatus.Removed
                && snapshot.CompletedWork == policy.PackingWork,
            BuildingPackingCommitState.Cancelled => building.Status == BuildingStatus.Completed,
            _ => false,
        };
        if (!stateMatches)
        {
            return Invalid("Saved packing commitment does not match building status.");
        }

        BuildingPackingPlanState restored = new BuildingPackingPlanState(
            snapshot.JobId,
            snapshot.OutputStackId)
        {
            CompletedWork = snapshot.CompletedWork,
            CommitState = snapshot.CommitState,
        };
        return Result<BuildingPackingPlanState>.Success(restored);
    }

    public void AddWork(int amount, int requiredWork)
    {
        if (CommitState != BuildingPackingCommitState.Active)
        {
            throw new InvalidOperationException("Only active packing can receive work.");
        }

        CompletedWork = Math.Min(requiredWork, checked(CompletedWork + amount));
    }

    public void Complete(int requiredWork)
    {
        if (CommitState != BuildingPackingCommitState.Active
            || CompletedWork != requiredWork)
        {
            throw new InvalidOperationException("Packing work is not ready to complete.");
        }

        CommitState = BuildingPackingCommitState.Completed;
    }

    public void Cancel()
    {
        if (CommitState != BuildingPackingCommitState.Active)
        {
            throw new InvalidOperationException("Only active packing can be cancelled.");
        }

        CommitState = BuildingPackingCommitState.Cancelled;
    }

    public BuildingPackingPlanSnapshot CreateSnapshot()
    {
        return new BuildingPackingPlanSnapshot(
            JobId,
            OutputStackId,
            CompletedWork,
            CommitState);
    }

    private static Result<BuildingPackingPlanState> Invalid(string message)
    {
        return Result<BuildingPackingPlanState>.Failure(new DomainError(
            "buildings.restore.invalid_packing_plan",
            message));
    }
}

public sealed partial class BuildingsState
{
    private readonly System.Collections.Generic.Dictionary<EntityId, BuildingPackingPlanState>
        _packingPlans = new System.Collections.Generic.Dictionary<EntityId, BuildingPackingPlanState>();

    public Result StartBoxPacking(
        EntityId id,
        EntityId jobId,
        EntityId outputStackId,
        long tick)
    {
        ValidateTick(tick);
        if (jobId.IsEmpty || outputStackId.IsEmpty)
        {
            throw new ArgumentException("Packing job and output stack ids are required.");
        }

        BuildingProjectState? project = Find(id);
        if (project is null)
        {
            return Result.Failure(BuildingErrors.NotFound);
        }

        BuildingBoxPolicy? policy = project.Definition.BoxPolicy;
        if (policy is null || !policy.PackingEnabled)
        {
            return Result.Failure(BuildingErrors.WrongConstructionPolicy);
        }

        if (project.Status != BuildingStatus.Completed || HasActivePackingPlan(id))
        {
            return Result.Failure(BuildingErrors.InvalidStatus);
        }

        project.StartPacking();
        _packingPlans[id] = new BuildingPackingPlanState(jobId, outputStackId);
        Raise(new BuildingPackingStarted(tick, id, jobId, outputStackId));
        return Result.Success();
    }

    public Result AddBoxPackingWork(EntityId id, int amount, long tick)
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
            || !_packingPlans.TryGetValue(id, out BuildingPackingPlanState? packing)
            || packing.CommitState != BuildingPackingCommitState.Active)
        {
            return Result.Failure(BuildingErrors.InvalidStatus);
        }

        int requiredWork = project.Definition.BoxPolicy!.PackingWork;
        packing.AddWork(amount, requiredWork);
        project.RecordPackingWork();
        Raise(new BuildingPackingProgressed(
            tick,
            id,
            packing.CompletedWork,
            requiredWork));
        return Result.Success();
    }

    public Result CompleteBoxPacking(EntityId id, long tick)
    {
        ValidateTick(tick);
        BuildingProjectState? project = Find(id);
        if (project is null)
        {
            return Result.Failure(BuildingErrors.NotFound);
        }

        if (project.Status != BuildingStatus.Completed
            || !_packingPlans.TryGetValue(id, out BuildingPackingPlanState? packing)
            || packing.CommitState != BuildingPackingCommitState.Active)
        {
            return Result.Failure(BuildingErrors.InvalidStatus);
        }

        int requiredWork = project.Definition.BoxPolicy!.PackingWork;
        if (packing.CompletedWork != requiredWork)
        {
            return Result.Failure(BuildingErrors.WorkIncomplete);
        }

        packing.Complete(requiredWork);
        project.Remove("packed");
        Raise(new BuildingPacked(tick, id, packing.OutputStackId));
        Raise(new BuildingRemoved(tick, id, "packed"));
        return Result.Success();
    }

    public Result CancelBoxPacking(EntityId id, string reason, long tick)
    {
        ValidateTick(tick);
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Packing cancellation reason is required.", nameof(reason));
        }

        BuildingProjectState? project = Find(id);
        if (project is null)
        {
            return Result.Failure(BuildingErrors.NotFound);
        }

        if (project.Status != BuildingStatus.Completed
            || !_packingPlans.TryGetValue(id, out BuildingPackingPlanState? packing)
            || packing.CommitState != BuildingPackingCommitState.Active)
        {
            return Result.Failure(BuildingErrors.InvalidStatus);
        }

        packing.Cancel();
        project.CancelPacking(reason.Trim());
        Raise(new BuildingPackingCancelled(tick, id, reason.Trim()));
        return Result.Success();
    }

    public BuildingPackingPlanSnapshot? GetPackingPlan(EntityId id)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("Building id cannot be empty.", nameof(id));
        }

        return _packingPlans.TryGetValue(id, out BuildingPackingPlanState? value)
            ? value.CreateSnapshot()
            : null;
    }

    private BuildingSnapshot CreateCombinedSnapshot(BuildingProjectState project)
    {
        BuildingSnapshot snapshot = project.CreateSnapshot();
        BuildingBoxPlanSnapshot? box = GetBoxPlan(project.Id);
        BuildingPackingPlanSnapshot? packing = _packingPlans.TryGetValue(
            project.Id,
            out BuildingPackingPlanState? state)
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
            box,
            packing);
    }

    private bool HasActivePackingPlan(EntityId id)
    {
        return _packingPlans.TryGetValue(id, out BuildingPackingPlanState? packing)
            && packing.CommitState == BuildingPackingCommitState.Active;
    }
}
}
