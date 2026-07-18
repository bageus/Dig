using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Unity
{
    internal sealed partial class DigWorldSession
    {
        private readonly CaveRoomPlanner _caveRoomPlanner = new CaveRoomPlanner();
        private readonly List<CaveRoomPlan> _caveRoomPlans = new List<CaveRoomPlan>();

        internal CaveRoomPlanResult PlanCaveRoom(
            CaveRoomPresetKind kind,
            CellId entrance)
        {
            WorldSnapshot snapshot = LoadSnapshot();
            return _caveRoomPlanner.Plan(
                snapshot,
                _boundaryPolicy,
                kind,
                entrance,
                GetCompletedCaveRoomPlans(snapshot));
        }

        internal Result ApplyCaveRoomPlan(CaveRoomPlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            bool alreadyPlaced = _caveRoomPlans.Any(existing =>
                existing.Entrance == plan.Entrance
                && existing.Preset.Kind == plan.Preset.Kind);
            if (alreadyPlaced)
            {
                return Result.Success();
            }

            List<CellId> applied = new List<CellId>();
            for (int index = 0; index < plan.FrontExcavationCells.Count; index++)
            {
                CellId cell = plan.FrontExcavationCells[index];
                Result result = SetDesignation(cell, active: true);
                if (result.IsFailure)
                {
                    RollBackDesignations(applied);
                    return result;
                }

                applied.Add(cell);
            }

            _caveRoomPlans.Add(plan);
            return Result.Success();
        }

        internal IReadOnlyList<CaveRoomPlan> LoadCompletedCaveRoomPlans()
        {
            return GetCompletedCaveRoomPlans(LoadSnapshot());
        }

        private IReadOnlyList<CaveRoomPlan> GetCompletedCaveRoomPlans(
            WorldSnapshot snapshot)
        {
            Dictionary<CellId, CellSnapshot> cells = snapshot.Chunks
                .SelectMany(chunk => chunk.Cells)
                .ToDictionary(cell => cell.Id);
            return _caveRoomPlans
                .Where(plan => plan.FrontExcavationCells.All(cell =>
                    cells.TryGetValue(cell, out CellSnapshot value)
                    && !value.IsSolid))
                .ToArray();
        }

        private void RollBackDesignations(IReadOnlyList<CellId> cells)
        {
            for (int index = cells.Count - 1; index >= 0; index--)
            {
                SetDesignation(cells[index], active: false);
            }
        }
    }
}
