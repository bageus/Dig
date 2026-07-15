using System;
using Dig.Application.Buildings;
using Dig.Domain.Buildings;
using Dig.Domain.Core;

namespace Dig.Presentation.Buildings
{

public sealed class BuildingFunctionsPresenter
{
    public const string PackLabelKey = "building.function.pack";
    public const string PackingActiveReason = "building_box.packing.already_active";
    public const string PackingDisabledReason = "building_box.packing.disabled";
    public const string BuildingDamagedReason = "building_box.packing.damaged";
    public const string BuildingIncompleteReason = "building_box.packing.incomplete";
    public const string BuildingInactiveReason = "building_box.packing.inactive";

    public BuildingFunctionsViewModel Present(BuildingSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        BuildingBoxPolicy? policy = snapshot.Definition.BoxPolicy;
        bool isPacking = snapshot.PackingPlan?.CommitState
            == BuildingPackingCommitState.Active;
        int packingRequiredWork = policy?.PackingWork ?? 0;
        int packingCompletedWork = isPacking
            ? snapshot.PackingPlan!.CompletedWork
            : 0;
        string? disabledReason = ResolvePackDisabledReason(snapshot, policy, isPacking);
        BuildingFunctionActionViewModel pack = new BuildingFunctionActionViewModel(
            BuildingFunctionActionKind.Pack,
            PackLabelKey,
            disabledReason is null,
            disabledReason);
        return new BuildingFunctionsViewModel(
            snapshot.Id,
            snapshot.Definition.Id,
            snapshot.Status,
            snapshot.Durability,
            snapshot.Definition.MaximumDurability,
            isPacking,
            packingCompletedWork,
            packingRequiredWork,
            new[] { pack });
    }

    public bool TryCreatePackingDraft(
        BuildingFunctionsViewModel model,
        EntityId jobId,
        EntityId outputStackId,
        int priority,
        long tick,
        out BuildingPackingCommandDraft? draft)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        BuildingFunctionActionViewModel pack = model.Actions[0];
        if (pack.Kind != BuildingFunctionActionKind.Pack || !pack.IsEnabled)
        {
            draft = null;
            return false;
        }

        draft = new BuildingPackingCommandDraft(
            model.BuildingId,
            jobId,
            outputStackId,
            priority,
            tick);
        return true;
    }

    public StartBuildingBoxPackingCommand CreateCommand(
        BuildingPackingCommandDraft draft)
    {
        if (draft is null)
        {
            throw new ArgumentNullException(nameof(draft));
        }

        return new StartBuildingBoxPackingCommand(
            draft.BuildingId,
            draft.JobId,
            draft.OutputStackId,
            draft.Priority,
            draft.Tick);
    }

    private static string? ResolvePackDisabledReason(
        BuildingSnapshot snapshot,
        BuildingBoxPolicy? policy,
        bool isPacking)
    {
        if (policy is null || !policy.PackingEnabled)
        {
            return PackingDisabledReason;
        }

        if (isPacking)
        {
            return PackingActiveReason;
        }

        return snapshot.Status switch
        {
            BuildingStatus.Completed => null,
            BuildingStatus.Damaged => BuildingDamagedReason,
            BuildingStatus.Cancelled or BuildingStatus.Removed => BuildingInactiveReason,
            _ => BuildingIncompleteReason,
        };
    }
}
}
