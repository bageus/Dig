using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public sealed partial class SaveGameLoader
{
    private static Result<BuildingsState> BuildBuildingsState(
        BuildingsSaveData data,
        BuildingCatalog? catalog)
    {
        if (data is null || data.Buildings is null)
        {
            return Result<BuildingsState>.Failure(SaveErrors.InvalidDocument);
        }

        if (data.Buildings.Count == 0)
        {
            return Result<BuildingsState>.Success(new BuildingsState());
        }

        if (catalog is null)
        {
            return Result<BuildingsState>.Failure(SaveErrors.UnknownBuildingDefinition);
        }

        List<BuildingSnapshot> snapshots = new List<BuildingSnapshot>();
        foreach (BuildingSaveData saved in data.Buildings
            .OrderBy(item => item.BuildingId, StringComparer.Ordinal))
        {
            if (saved is null
                || !Enum.IsDefined(typeof(BuildingOrientation), saved.Orientation)
                || !Enum.IsDefined(typeof(BuildingStatus), saved.Status))
            {
                return Result<BuildingsState>.Failure(SaveErrors.InvalidDocument);
            }

            BuildingDefinition definition;
            try
            {
                definition = catalog.Get(new BuildingDefinitionId(saved.DefinitionId));
            }
            catch (KeyNotFoundException)
            {
                return Result<BuildingsState>.Failure(SaveErrors.UnknownBuildingDefinition);
            }

            BuildingOrientation orientation = (BuildingOrientation)saved.Orientation;
            CellId origin = new CellId(saved.OriginX, saved.OriginY);
            BuildingBoxPlanSnapshot? boxPlan = ParseBoxPlan(saved.BoxPlan);
            snapshots.Add(new BuildingSnapshot(
                EntityId.Parse(saved.BuildingId),
                definition,
                origin,
                orientation,
                definition.ResolveFootprint(origin, orientation),
                new CellId(saved.WorkPositionX, saved.WorkPositionY),
                (BuildingStatus)saved.Status,
                saved.CompletedWork,
                saved.Durability,
                saved.Version,
                saved.DiagnosticReason,
                boxPlan));
        }

        return BuildingsState.Restore(snapshots);
    }

    private static BuildingBoxPlanSnapshot? ParseBoxPlan(
        BuildingBoxPlanSaveData? data)
    {
        if (data is null)
        {
            return null;
        }

        if (!Enum.IsDefined(typeof(BuildingBoxCommitState), data.CommitState))
        {
            throw new InvalidOperationException("Saved box commitment is invalid.");
        }

        return new BuildingBoxPlanSnapshot(
            EntityId.Parse(data.SourceStackId),
            EntityId.Parse(data.JobId),
            (BuildingBoxCommitState)data.CommitState);
    }

    private static Result ValidateBuildingReferences(
        InventoryState inventory,
        JobSystem jobs,
        BuildingsState buildings)
    {
        foreach (BuildingSnapshot building in buildings.GetAll())
        {
            if (building.BoxPlan is null)
            {
                continue;
            }

            BuildingBoxPlanSnapshot plan = building.BoxPlan;
            JobSnapshot? job = jobs.Get(plan.JobId);
            if (job?.Definition is not BuildingBoxAssemblyJobDefinition definition
                || definition.BuildingId != building.Id
                || definition.SourceStackId != plan.SourceStackId)
            {
                return Result.Failure(SaveErrors.InvalidDocument);
            }

            ItemStackSnapshot? stack = inventory.GetStack(plan.SourceStackId);
            if (building.Status == BuildingStatus.Cancelled)
            {
                if (job.Status != JobStatus.Cancelled
                    || !IsAvailableSingleBox(stack, building.Definition.BoxPolicy!.BoxItemId)
                    || HasReservation(stack!, plan.JobId))
                {
                    return Result.Failure(SaveErrors.InvalidDocument);
                }

                continue;
            }

            if (plan.CommitState == BuildingBoxCommitState.Reserved)
            {
                if (job.IsTerminal
                    || !IsAvailableSingleBox(stack, building.Definition.BoxPolicy!.BoxItemId)
                    || !HasReservation(stack!, plan.JobId))
                {
                    return Result.Failure(SaveErrors.InvalidDocument);
                }
            }
            else if (plan.CommitState == BuildingBoxCommitState.AtSite)
            {
                if (job.IsTerminal
                    || !IsAvailableSingleBox(stack, building.Definition.BoxPolicy!.BoxItemId)
                    || stack!.Location != ItemLocation.InBuilding(building.Id)
                    || HasReservation(stack, plan.JobId))
                {
                    return Result.Failure(SaveErrors.InvalidDocument);
                }
            }
            else if (plan.CommitState == BuildingBoxCommitState.Consumed)
            {
                if (job.Status != JobStatus.Completed || stack is not null)
                {
                    return Result.Failure(SaveErrors.InvalidDocument);
                }
            }
            else
            {
                return Result.Failure(SaveErrors.InvalidDocument);
            }
        }

        return Result.Success();
    }

    private static bool IsAvailableSingleBox(ItemStackSnapshot? stack, ItemId itemId)
    {
        return stack is not null
            && stack.ItemId == itemId
            && stack.Quantity == 1;
    }

    private static bool HasReservation(ItemStackSnapshot stack, EntityId jobId)
    {
        return stack.Reservations.Any(item => item.JobId == jobId && item.Quantity == 1);
    }
}
}