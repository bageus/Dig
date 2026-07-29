using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.World;

namespace Dig.Application.World
{

public sealed class CaveRoomResumePlanner
{
    public CaveRoomPlanResult Plan(
        WorldSnapshot world,
        MaterialCatalog? materials,
        ExcavationBoundaryPolicy boundary,
        CaveRoomPlan pausedPlan)
    {
        if (world == null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        if (boundary == null)
        {
            throw new ArgumentNullException(nameof(boundary));
        }

        if (pausedPlan == null)
        {
            throw new ArgumentNullException(nameof(pausedPlan));
        }

        Dictionary<CellId, CellSnapshot> cells = world.Chunks
            .SelectMany(chunk => chunk.Cells)
            .ToDictionary(cell => cell.Id);
        List<CaveRoomInvalidCell> invalid = new List<CaveRoomInvalidCell>();
        for (int index = 0; index < pausedPlan.BaseTunnelCells.Count; index++)
        {
            CellId cell = pausedPlan.BaseTunnelCells[index];
            if (!cells.TryGetValue(cell, out CellSnapshot snapshot) || snapshot.IsSolid)
            {
                invalid.Add(new CaveRoomInvalidCell(
                    cell,
                    CaveRoomPlanFailureReason.BaseTunnelMissing));
            }
        }

        for (int index = 0; index < pausedPlan.RoofCells.Count; index++)
        {
            CellId cell = pausedPlan.RoofCells[index];
            if (!cells.TryGetValue(cell, out CellSnapshot snapshot) || !snapshot.IsSolid)
            {
                invalid.Add(new CaveRoomInvalidCell(
                    cell,
                    CaveRoomPlanFailureReason.MissingRoof));
            }
        }

        List<CaveRoomExcavationTarget> remaining =
            new List<CaveRoomExcavationTarget>();
        for (int index = 0; index < pausedPlan.ExcavationTargets.Count; index++)
        {
            CaveRoomExcavationTarget target = pausedPlan.ExcavationTargets[index];
            if (IsComplete(target, cells))
            {
                continue;
            }

            if (!world.Size.Contains(target.Cell)
                || !cells.TryGetValue(target.Cell, out CellSnapshot snapshot))
            {
                invalid.Add(new CaveRoomInvalidCell(
                    target.Cell,
                    CaveRoomPlanFailureReason.RoomOutOfBounds));
                continue;
            }

            if (boundary.IsProtected(target.Cell))
            {
                invalid.Add(new CaveRoomInvalidCell(
                    target.Cell,
                    CaveRoomPlanFailureReason.ProtectedRock));
                continue;
            }

            if (!snapshot.IsSolid)
            {
                invalid.Add(new CaveRoomInvalidCell(
                    target.Cell,
                    CaveRoomPlanFailureReason.RoomObstructed));
                continue;
            }

            MaterialDefinition? material = materials?.Get(snapshot.State.MaterialId);
            if (material != null && !material.IsMineable)
            {
                invalid.Add(new CaveRoomInvalidCell(
                    target.Cell,
                    CaveRoomPlanFailureReason.UnmineableRock));
                continue;
            }

            remaining.Add(target);
        }

        if (invalid.Count > 0)
        {
            CaveRoomPlanFailureReason reason = SelectPrimaryFailure(invalid);
            return CaveRoomPlanResult.Failure(
                reason,
                FailureDetail(reason),
                invalid);
        }

        if (remaining.Count == 0)
        {
            return CaveRoomPlanResult.Failure(
                CaveRoomPlanFailureReason.NothingToExcavate,
                "The paused cave room has no unfinished excavation targets.");
        }

        HashSet<CellId> remainingCells = remaining
            .Select(value => value.Cell)
            .ToHashSet();
        CaveRoomPlan resumed = CaveRoomPlan.CreateSnapshot(
            pausedPlan.Preset,
            pausedPlan.Entrance,
            pausedPlan.FrontExcavationCells.Where(remainingCells.Contains),
            remaining,
            pausedPlan.BaseTunnelCells,
            pausedPlan.VolumeCells,
            pausedPlan.RoofCells);
        return CaveRoomPlanResult.Success(resumed);
    }

    private static bool IsComplete(
        CaveRoomExcavationTarget target,
        IReadOnlyDictionary<CellId, CellSnapshot> cells)
    {
        if (!cells.TryGetValue(target.Cell, out CellSnapshot value))
        {
            return false;
        }

        if (target.IsFullCell)
        {
            return !value.IsSolid || value.State.IsExcavationOpen;
        }

        return value.IsSolid
            && value.State.Designation != CellDesignation.Dig
            && (value.State.CompletedExcavationQuarters & target.RequiredQuarters)
                == target.RequiredQuarters;
    }

    private static CaveRoomPlanFailureReason SelectPrimaryFailure(
        IReadOnlyCollection<CaveRoomInvalidCell> invalid)
    {
        CaveRoomPlanFailureReason[] priority =
        {
            CaveRoomPlanFailureReason.RoomOutOfBounds,
            CaveRoomPlanFailureReason.ProtectedRock,
            CaveRoomPlanFailureReason.UnmineableRock,
            CaveRoomPlanFailureReason.BaseTunnelMissing,
            CaveRoomPlanFailureReason.RoomObstructed,
            CaveRoomPlanFailureReason.MissingRoof,
        };
        for (int index = 0; index < priority.Length; index++)
        {
            if (invalid.Any(value => value.Reason == priority[index]))
            {
                return priority[index];
            }
        }

        return invalid.First().Reason;
    }

    private static string FailureDetail(CaveRoomPlanFailureReason reason)
    {
        return reason switch
        {
            CaveRoomPlanFailureReason.RoomOutOfBounds =>
                "The paused room outline leaves the world bounds.",
            CaveRoomPlanFailureReason.ProtectedRock =>
                "The paused room now overlaps protected rock.",
            CaveRoomPlanFailureReason.UnmineableRock =>
                "An unfinished paused-room target is no longer mineable.",
            CaveRoomPlanFailureReason.BaseTunnelMissing =>
                "The paused room base tunnel is no longer open.",
            CaveRoomPlanFailureReason.RoomObstructed =>
                "An unfinished paused-room target is obstructed.",
            CaveRoomPlanFailureReason.MissingRoof =>
                "The paused room no longer has its required roof.",
            _ => "The paused cave room cannot be resumed.",
        };
    }
}

}
