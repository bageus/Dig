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
        private readonly CaveRoomResumePlanner _caveRoomResumePlanner =
            new CaveRoomResumePlanner();
        private readonly List<CaveRoomPlan> _caveRoomPlans = new List<CaveRoomPlan>();
        private readonly HashSet<CaveRoomPlan> _pausedCaveRoomPlans =
            new HashSet<CaveRoomPlan>();

        internal CaveRoomPlanResult PlanCaveRoom(
            CaveRoomPresetKind kind,
            CellId entrance)
        {
            WorldSnapshot snapshot = LoadSnapshot();
            CaveRoomPlan? paused = _pausedCaveRoomPlans.FirstOrDefault(value =>
                value.Entrance == entrance && value.Preset.Kind == kind);
            if (paused != null)
            {
                CaveRoomPlanResult resumed = _caveRoomResumePlanner.Plan(
                    snapshot,
                    _repository.Get().Materials,
                    _boundaryPolicy,
                    paused);
                if (resumed.Succeeded)
                {
                    return resumed;
                }

                if (resumed.FailureReason != CaveRoomPlanFailureReason.NothingToExcavate)
                {
                    return resumed;
                }

                _pausedCaveRoomPlans.Remove(paused);
            }

            IReadOnlyList<CaveRoomPlan> completed = GetCompletedCaveRoomPlans(snapshot);
            return _caveRoomPlanner.Plan(
                snapshot,
                _repository.Get().Materials,
                _boundaryPolicy,
                kind,
                entrance,
                completed);
        }

        internal Result ApplyCaveRoomPlan(CaveRoomPlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            CaveRoomPlan? existingPlan = _caveRoomPlans.FirstOrDefault(existing =>
                existing.Entrance == plan.Entrance
                && existing.Preset.Kind == plan.Preset.Kind);
            if (existingPlan != null && !_pausedCaveRoomPlans.Contains(existingPlan))
            {
                return Result.Success();
            }

            if (GetCompletedCaveRoomPlans(LoadSnapshot()).Any(existing =>
                existing.Entrance == plan.Entrance))
            {
                return Result.Failure(ProtectedRock);
            }

            for (int index = 0; index < plan.ExcavationCells.Count; index++)
            {
                if (IsProtected(plan.ExcavationCells[index]))
                {
                    return Result.Failure(ProtectedRock);
                }
            }

            Result<WorldMutationResult> designated = _repository.Get().SetDigDesignations(
                plan.ExcavationCells,
                _simulationState.Clock.TickIndex);
            if (designated.IsFailure)
            {
                return Result.Failure(designated.Error!);
            }

            if (existingPlan == null)
            {
                _caveRoomPlans.Add(plan);
            }
            else
            {
                _pausedCaveRoomPlans.Remove(existingPlan);
            }

            return Result.Success();
        }

        internal IReadOnlyList<CaveRoomPlan> LoadCompletedCaveRoomPlans()
        {
            return GetCompletedCaveRoomPlans(LoadSnapshot());
        }

        internal bool TryGetCaveRoomExcavationTarget(
            CellId cell,
            out CaveRoomExcavationTarget target)
        {
            for (int index = 0; index < _caveRoomPlans.Count; index++)
            {
                if (_caveRoomPlans[index].TryGetExcavationTarget(cell, out target))
                {
                    return true;
                }
            }

            target = default;
            return false;
        }

        internal IReadOnlyList<CellId> ExpandExcavationEraseCells(
            IReadOnlyList<CellId> requested)
        {
            HashSet<CellId> expanded = new HashSet<CellId>(requested);
            WorldSnapshot snapshot = LoadSnapshot();
            HashSet<CaveRoomPlan> completed = new HashSet<CaveRoomPlan>(
                GetCompletedCaveRoomPlans(snapshot));
            for (int index = 0; index < _caveRoomPlans.Count; index++)
            {
                CaveRoomPlan plan = _caveRoomPlans[index];
                if (completed.Contains(plan)
                    || !plan.VolumeCells.Any(expanded.Contains))
                {
                    continue;
                }

                expanded.UnionWith(plan.VolumeCells);
            }

            return expanded.OrderBy(cell => cell).ToArray();
        }

        internal void CommitExcavationErase(IReadOnlyList<CellId> cells)
        {
            HashSet<CellId> erased = new HashSet<CellId>(cells);
            Dictionary<CellId, CellSnapshot> world = LoadSnapshot().Chunks
                .SelectMany(chunk => chunk.Cells)
                .ToDictionary(cell => cell.Id);
            for (int index = 0; index < _caveRoomPlans.Count; index++)
            {
                CaveRoomPlan plan = _caveRoomPlans[index];
                if (plan.VolumeCells.Any(erased.Contains)
                    && plan.ExcavationTargets.Any(target =>
                        !IsCaveRoomTargetComplete(target, world)))
                {
                    _pausedCaveRoomPlans.Add(plan);
                }
            }

            RemoveTunnelPlans(cells);
        }

        private IReadOnlyList<CaveRoomPlan> GetCompletedCaveRoomPlans(
            WorldSnapshot snapshot)
        {
            Dictionary<CellId, CellSnapshot> cells = snapshot.Chunks
                .SelectMany(chunk => chunk.Cells)
                .ToDictionary(cell => cell.Id);
            return _caveRoomPlans
                .Where(plan => plan.ExcavationTargets.All(target =>
                    IsCaveRoomTargetComplete(target, cells)))
                .ToArray();
        }

        private static bool IsCaveRoomTargetComplete(
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
                && (value.State.CompletedExcavationQuarters
                    & target.RequiredQuarters) == target.RequiredQuarters;
        }
    }
}
