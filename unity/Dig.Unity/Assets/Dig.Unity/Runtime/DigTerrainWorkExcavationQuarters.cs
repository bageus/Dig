using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Agents;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private readonly ExcavationWorkCoordinator _excavationQuarterWork =
            new ExcavationWorkCoordinator();
        private Func<EntityId, int>? _excavationMiningSkill;

        internal void BindExcavationSkillSource(Func<EntityId, int> skillSource)
        {
            _excavationMiningSkill = skillSource
                ?? throw new ArgumentNullException(nameof(skillSource));
        }

        internal ExcavationWorkerAssignment AssignManualQuarterExcavation(
            string residentId,
            CellId targetCell,
            int targetZ,
            CellId residentCell,
            int miningSkill)
        {
            if (string.IsNullOrWhiteSpace(residentId))
            {
                throw new ArgumentException("Resident id is required.", nameof(residentId));
            }

            ExcavationWorkTarget target = new ExcavationWorkTarget(targetCell, targetZ);
            SynchronizeExcavationQuarterState(target);
            return EnsureExcavationQuarterAssignment(
                EntityId.Parse(residentId),
                target,
                residentCell,
                miningSkill);
        }

        internal ExcavationWorkerAssignment? LoadManualQuarterAssignment(
            string residentId)
        {
            if (string.IsNullOrWhiteSpace(residentId))
            {
                return null;
            }

            return _excavationQuarterWork.GetAssignment(EntityId.Parse(residentId));
        }

        internal bool CancelManualQuarterExcavation(string residentId)
        {
            if (string.IsNullOrWhiteSpace(residentId))
            {
                return false;
            }

            return _excavationQuarterWork.Cancel(EntityId.Parse(residentId));
        }

        internal IReadOnlyList<ExcavationQuarterCompletion> AdvanceManualQuarterExcavation(
            string residentId,
            long tick,
            ulong worldSeed)
        {
            if (string.IsNullOrWhiteSpace(residentId))
            {
                throw new ArgumentException("Resident id is required.", nameof(residentId));
            }

            EntityId workerId = EntityId.Parse(residentId);
            ExcavationWorkerAssignment? assignment =
                _excavationQuarterWork.GetAssignment(workerId);
            if (assignment == null)
            {
                return Array.Empty<ExcavationQuarterCompletion>();
            }

            SynchronizeExcavationQuarterState(assignment.Target);
            ulong seed = BuildExcavationSeed(worldSeed, tick, workerId);
            IReadOnlyList<ExcavationQuarterCompletion> completions =
                _excavationQuarterWork.ApplySwing(workerId, seed);
            CommitExcavationQuarterCompletions(completions, tick);
            return completions;
        }

        internal IReadOnlyList<ExcavationQuarterCompletion>
            AdvanceReadyManualQuarterExcavations(
                long tick,
                IReadOnlyList<AgentViewModel> agents)
        {
            if (agents == null)
            {
                throw new ArgumentNullException(nameof(agents));
            }

            return Array.Empty<ExcavationQuarterCompletion>();
        }

        internal ExcavationQuarterState LoadExcavationQuarterState(
            CellId targetCell,
            int targetZ)
        {
            ExcavationWorkTarget target = new ExcavationWorkTarget(targetCell, targetZ);
            SynchronizeExcavationQuarterState(target);
            return _excavationQuarterWork.GetState(target);
        }

        internal IReadOnlyList<ExcavationQuarterProgressSnapshot>
            LoadExcavationQuarterProgress()
        {
            return _worldSession.LoadSnapshot().Chunks
                .SelectMany(chunk => chunk.Cells)
                .Where(cell => cell.State.CompletedExcavationQuarters
                    != ExcavationQuarter.None)
                .OrderBy(cell => cell.Id)
                .Select(cell => new ExcavationQuarterProgressSnapshot(
                    new ExcavationWorkTarget(cell.Id, cell.Id.Z),
                    cell.State.CompletedExcavationQuarters))
                .ToArray();
        }

        private bool AdvanceExcavationQuarterWork(
            EntityId workerId,
            ExcavationWorkTarget target,
            CellId residentCell,
            long tick)
        {
            SynchronizeExcavationQuarterState(target);
            CellSnapshot before = RequireExcavationCell(target.CellId);
            if (!before.IsSolid)
            {
                return true;
            }

            int skill = ResolveExcavationMiningSkill(workerId);
            EnsureExcavationQuarterAssignment(
                workerId,
                target,
                residentCell,
                skill);
            ulong seed = BuildExcavationSeed(
                unchecked((ulong)(uint)_worldSession.MiningOutputWorldSeed),
                tick,
                workerId);
            IReadOnlyList<ExcavationQuarterCompletion> completions =
                _excavationQuarterWork.ApplySwing(workerId, seed);
            CommitExcavationQuarterCompletions(completions, tick);

            CellSnapshot current = RequireExcavationCell(target.CellId);
            _excavationQuarterWork.SynchronizeCompleted(
                target,
                current.State.CompletedExcavationQuarters);
            ExcavationQuarter required = ResolveRequiredExcavationQuarters(
                target.CellId);
            return !current.IsSolid
                || (current.State.CompletedExcavationQuarters & required) == required;
        }

        private ExcavationWorkerAssignment EnsureExcavationQuarterAssignment(
            EntityId workerId,
            ExcavationWorkTarget target,
            CellId residentCell,
            int miningSkill)
        {
            _excavationQuarterWork.CancelAssignmentsExcept(target, workerId);
            ExcavationCutPattern cutPattern = ResolveExcavationCutPattern(target.CellId);
            ExcavationApproachSide approach = ExcavationApproachResolver.Resolve(
                residentCell,
                target.CellId,
                cutPattern);
            ExcavationWorkerAssignment? existing =
                _excavationQuarterWork.GetAssignment(workerId);
            if (existing != null
                && existing.Target.Equals(target)
                && existing.Approach == approach)
            {
                return existing;
            }

            if (existing != null)
            {
                _excavationQuarterWork.Cancel(workerId);
            }

            return _excavationQuarterWork.Assign(
                workerId,
                target,
                approach,
                Math.Max(0, Math.Min(100, miningSkill)));
        }

        private void CommitExcavationQuarterCompletions(
            IReadOnlyList<ExcavationQuarterCompletion> completions,
            long tick)
        {
            if (completions.Count == 0)
            {
                return;
            }

            WorldState world = _worldSession.Repository.Get();
            for (int index = 0; index < completions.Count; index++)
            {
                ExcavationQuarterCompletion completion = completions[index];
                ExcavationCutPattern pattern = ResolveExcavationCutPattern(
                    completion.Target.CellId);
                Result<WorldMutationResult> committed = world.CommitExcavationQuarter(
                    completion.Target.CellId,
                    completion.Quarter,
                    pattern,
                    _worldSession.EmptyMaterialId,
                    tick);
                if (committed.IsFailure)
                {
                    throw new InvalidOperationException(committed.Error!.ToString());
                }
            }

            _worldSession.Repository.Save(world);
            _worldSession.Journal.Append(world.DequeueUncommittedEvents());
            MarkAuthoritativeWorldChanged();
        }

        private void SynchronizeExcavationQuarterState(ExcavationWorkTarget target)
        {
            CellSnapshot cell = RequireExcavationCell(target.CellId);
            ExcavationQuarter required = ResolveRequiredExcavationQuarters(target.CellId);
            ExcavationQuarter excluded = ExcavationQuarter.All & ~required;
            _excavationQuarterWork.SynchronizeCompleted(
                target,
                cell.State.CompletedExcavationQuarters | excluded);
        }

        private ExcavationQuarter ResolveRequiredExcavationQuarters(CellId target)
        {
            return _worldSession.TryGetCaveRoomExcavationTarget(
                    target,
                    out CaveRoomExcavationTarget roomTarget)
                ? roomTarget.RequiredQuarters
                : ExcavationQuarter.All;
        }

        private CellSnapshot RequireExcavationCell(CellId cellId)
        {
            Result<CellSnapshot> cell = _worldSession.Repository.Get().GetCell(cellId);
            if (cell.IsFailure)
            {
                throw new InvalidOperationException(cell.Error!.ToString());
            }

            return cell.Value;
        }

        private ExcavationCutPattern ResolveExcavationCutPattern(CellId target)
        {
            CellSnapshot cell = RequireExcavationCell(target);
            if (cell.State.ExcavationCutPattern != ExcavationCutPattern.None)
            {
                return cell.State.ExcavationCutPattern;
            }

            if (_worldSession.PlannedVerticalTunnelCells.Contains(target))
            {
                return ExcavationCutPattern.HorizontalRows;
            }

            return target.Z > CellId.MinimumDepth
                ? ExcavationCutPattern.DepthFace
                : ExcavationCutPattern.VerticalColumns;
        }

        private int ResolveExcavationMiningSkill(EntityId workerId)
        {
            int value = _excavationMiningSkill?.Invoke(workerId) ?? 0;
            return Math.Max(0, Math.Min(100, value));
        }

        private void CompleteExcavationQuarterTarget(CellId target)
        {
            _excavationQuarterWork.Remove(
                new ExcavationWorkTarget(target, target.Z));
        }

        private static ulong BuildExcavationSeed(
            ulong worldSeed,
            long tick,
            EntityId workerId)
        {
            unchecked
            {
                ulong value = worldSeed ^ (ulong)tick;
                string text = workerId.ToString();
                for (int index = 0; index < text.Length; index++)
                {
                    value ^= text[index];
                    value *= 1099511628211UL;
                }

                return value;
            }
        }
    }
}
