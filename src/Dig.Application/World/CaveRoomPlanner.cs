using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.World;

namespace Dig.Application.World
{

public sealed class CaveRoomPlanner
{
    public CaveRoomPlanResult Plan(
        WorldSnapshot world,
        ExcavationBoundaryPolicy boundary,
        CaveRoomPresetKind kind,
        CellId entrance,
        IReadOnlyCollection<CaveRoomPlan>? completedPlans = null)
    {
        return PlanCore(
            world,
            materials: null,
            boundary,
            kind,
            entrance,
            completedPlans);
    }

    public CaveRoomPlanResult Plan(
        WorldSnapshot world,
        MaterialCatalog materials,
        ExcavationBoundaryPolicy boundary,
        CaveRoomPresetKind kind,
        CellId entrance,
        IReadOnlyCollection<CaveRoomPlan>? completedPlans = null)
    {
        return PlanCore(
            world,
            materials ?? throw new ArgumentNullException(nameof(materials)),
            boundary,
            kind,
            entrance,
            completedPlans);
    }

    private CaveRoomPlanResult PlanCore(
        WorldSnapshot world,
        MaterialCatalog? materials,
        ExcavationBoundaryPolicy boundary,
        CaveRoomPresetKind kind,
        CellId entrance,
        IReadOnlyCollection<CaveRoomPlan>? completedPlans)
    {
        if (world is null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        if (boundary is null)
        {
            throw new ArgumentNullException(nameof(boundary));
        }

        if (entrance.Z != CellId.MinimumDepth)
        {
            return CaveRoomPlanResult.Failure(
                CaveRoomPlanFailureReason.EntranceOutOfBounds,
                "Cave room extrusion currently starts on the front Z=0 layer.");
        }

        if (completedPlans?.Any(plan => plan.Entrance == entrance) == true)
        {
            return CaveRoomPlanResult.Failure(
                CaveRoomPlanFailureReason.RoomObstructed,
                "A completed cave room is immutable.");
        }

        Dictionary<CellId, CellSnapshot> cells = world.Chunks
            .SelectMany(chunk => chunk.Cells)
            .ToDictionary(cell => cell.Id);
        CaveRoomPreset preset = CaveRoomPresetCatalog.Get(kind);
        List<CaveRoomInvalidCell> invalid = new List<CaveRoomInvalidCell>();
        List<CellId> baseTunnel = new List<CellId>(preset.BaseWidth);
        List<CellId> front = new List<CellId>();
        List<CellId> excavation = new List<CellId>();
        List<CellId> volume = new List<CellId>();

        for (int level = 0; level < preset.Height; level++)
        {
            int y = entrance.Y - level;
            int rowWidth = InterpolateWidth(preset, level);
            int minX = entrance.X - ((rowWidth - 1) / 2);
            for (int offset = 0; offset < rowWidth; offset++)
            {
                int x = minX + offset;
                for (int z = 0; z < preset.Depth; z++)
                {
                    CellId cell = new CellId(x, y, z);
                    volume.Add(cell);
                    bool openBaseTunnelCell = level == 0 && z == CellId.MinimumDepth;
                    ValidateVolumeCell(
                        world,
                        materials,
                        boundary,
                        cells,
                        cell,
                        openBaseTunnelCell,
                        invalid,
                        excavation,
                        front);
                    if (openBaseTunnelCell)
                    {
                        baseTunnel.Add(cell);
                    }
                }
            }
        }

        List<CellId> roof = ValidateRoof(
            world,
            cells,
            preset,
            entrance,
            invalid);
        if (invalid.Count > 0)
        {
            CaveRoomPlanFailureReason reason = SelectPrimaryFailure(invalid);
            return CaveRoomPlanResult.Failure(
                reason,
                FailureDetail(reason),
                invalid);
        }

        if (excavation.Count == 0)
        {
            return CaveRoomPlanResult.Failure(
                CaveRoomPlanFailureReason.NothingToExcavate,
                "The selected room volume contains no mineable rock.");
        }

        return CaveRoomPlanResult.Success(new CaveRoomPlan(
            preset,
            entrance,
            front.OrderBy(cell => cell).ToArray(),
            excavation.OrderBy(cell => cell).ToArray(),
            baseTunnel.OrderBy(cell => cell).ToArray(),
            volume.OrderBy(cell => cell).ToArray(),
            roof.OrderBy(cell => cell).ToArray()));
    }

    public static int InterpolateWidth(CaveRoomPreset preset, int level)
    {
        if (preset is null)
        {
            throw new ArgumentNullException(nameof(preset));
        }

        if (level < 0 || level >= preset.Height)
        {
            throw new ArgumentOutOfRangeException(nameof(level));
        }

        if (preset.Height == 1)
        {
            return preset.BaseWidth;
        }

        double progress = level / (double)(preset.Height - 1);
        double width = preset.BaseWidth
            + ((preset.TopWidth - preset.BaseWidth) * progress);
        return (int)Math.Round(width, MidpointRounding.AwayFromZero);
    }

    private static void ValidateVolumeCell(
        WorldSnapshot world,
        MaterialCatalog? materials,
        ExcavationBoundaryPolicy boundary,
        IReadOnlyDictionary<CellId, CellSnapshot> cells,
        CellId cell,
        bool openBaseTunnelCell,
        ICollection<CaveRoomInvalidCell> invalid,
        ICollection<CellId> excavation,
        ICollection<CellId> front)
    {
        if (!world.Size.Contains(cell) || !cells.TryGetValue(cell, out CellSnapshot snapshot))
        {
            invalid.Add(new CaveRoomInvalidCell(
                cell,
                CaveRoomPlanFailureReason.RoomOutOfBounds));
            return;
        }

        if (boundary.IsProtected(cell))
        {
            invalid.Add(new CaveRoomInvalidCell(
                cell,
                CaveRoomPlanFailureReason.ProtectedRock));
            return;
        }

        if (openBaseTunnelCell)
        {
            if (snapshot.IsSolid)
            {
                invalid.Add(new CaveRoomInvalidCell(
                    cell,
                    CaveRoomPlanFailureReason.BaseTunnelMissing));
            }

            return;
        }

        if (!snapshot.IsSolid)
        {
            invalid.Add(new CaveRoomInvalidCell(
                cell,
                CaveRoomPlanFailureReason.RoomObstructed));
            return;
        }

        MaterialDefinition? material = materials?.Get(snapshot.State.MaterialId);
        if (material != null && !material.IsMineable)
        {
            invalid.Add(new CaveRoomInvalidCell(
                cell,
                CaveRoomPlanFailureReason.UnmineableRock));
            return;
        }

        excavation.Add(cell);
        if (cell.Z == CellId.MinimumDepth)
        {
            front.Add(cell);
        }
    }

    private static List<CellId> ValidateRoof(
        WorldSnapshot world,
        IReadOnlyDictionary<CellId, CellSnapshot> cells,
        CaveRoomPreset preset,
        CellId entrance,
        ICollection<CaveRoomInvalidCell> invalid)
    {
        List<CellId> roof = new List<CellId>(preset.TopWidth);
        int roofY = entrance.Y - preset.Height;
        int roofMinX = entrance.X - ((preset.TopWidth - 1) / 2);
        for (int offset = 0; offset < preset.TopWidth; offset++)
        {
            CellId roofCell = new CellId(
                roofMinX + offset,
                roofY,
                CellId.MinimumDepth);
            if (!world.Size.Contains(roofCell)
                || !cells.TryGetValue(roofCell, out CellSnapshot roofSnapshot)
                || !roofSnapshot.IsSolid)
            {
                invalid.Add(new CaveRoomInvalidCell(
                    roofCell,
                    CaveRoomPlanFailureReason.MissingRoof));
                continue;
            }

            roof.Add(roofCell);
        }

        return roof;
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
                "The room outline leaves the world bounds.",
            CaveRoomPlanFailureReason.ProtectedRock =>
                "The room overlaps a protected rock cell.",
            CaveRoomPlanFailureReason.UnmineableRock =>
                "Every room excavation cell above the tunnel must be mineable rock.",
            CaveRoomPlanFailureReason.BaseTunnelMissing =>
                "The complete bottom row must already be an open through tunnel.",
            CaveRoomPlanFailureReason.RoomObstructed =>
                "Every room cell above the tunnel must still contain mineable rock.",
            CaveRoomPlanFailureReason.MissingRoof =>
                "One complete row of solid rock must remain above the room.",
            _ => "The cave room placement is invalid.",
        };
    }
}

}
