using System;
using System.Collections.Generic;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Unity
{
    internal sealed partial class DigWorldSession
    {
        private readonly CaveRoomPlanner _caveRoomPlanner = new CaveRoomPlanner();

        internal CaveRoomPlanResult PlanCaveRoom(
            CaveRoomPresetKind kind,
            CellId entrance)
        {
            return _caveRoomPlanner.Plan(
                LoadSnapshot(),
                _boundaryPolicy,
                kind,
                entrance);
        }

        internal Result ApplyCaveRoomPlan(CaveRoomPlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
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

            return Result.Success();
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
