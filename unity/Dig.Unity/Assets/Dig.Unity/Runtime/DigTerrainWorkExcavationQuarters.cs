using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Application.World;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Presentation.Agents;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private readonly ExcavationWorkCoordinator _excavationQuarterWork =
            new ExcavationWorkCoordinator();
        private readonly ExcavationCadenceResolver _excavationCadenceResolver =
            new ExcavationCadenceResolver(
                ExcavationCadenceProfile.CreateLegacyDeterministic());
        private readonly Dictionary<EntityId, ExcavationCadenceDecision>
            _excavationCadenceDecisions =
                new Dictionary<EntityId, ExcavationCadenceDecision>();
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

        internal bool TryLoadExcavationCadence(
            string residentId,
            out ExcavationCadenceDecision cadence)
        {
            if (string.IsNullOrWhiteSpace(residentId))
            {
                cadence = default;
                return false;
            }

            return _excavationCadenceDecisions.TryGetValue(
                EntityId.Parse(residentId),
                out cadence);
        }

        internal bool CancelManualQuarterExcavation(string residentId)
        {
            if (string.IsNullOrWhiteSpace(residentId))
            {
                return false;
            }

            EntityId workerId = EntityId.Parse(residentId);
            _excavationCadenceDecisions.Remove(workerId);
            return _excavationQuarterWork.Cancel(workerId);
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

            _ = worldSeed;
            EntityId workerId = EntityId.Parse(residentId);
            ExcavationWorkerAssignment? assignment =
                _excavationQuarterWork.GetAssignment(workerId);
            if (assignment == null)
            {
                return Array.Empty<ExcavationQuarterCompletion>();
            }

            SynchronizeExcavationQuarterState(assignment.Target);
            CellSnapshot target = RequireExcavationCell(assignment.Target.CellId);
            ExcavationCadenceDecision cadence = ResolveExcavationCadence(
                workerId,
                target,
                assignment.MiningSkill,
                TerrainWorkPosture.Standing,
                tick);
            if (!ExcavationCadenceResolver.IsDue(tick, cadence))
            {
                return Array.Empty<ExcavationQuarterCompletion>();
            }

            IReadOnlyList<ExcavationQuarterCompletion> completions =
                _excavationQuarterWork.ApplyWork(workerId);
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
            TerrainWorkPosture posture,
            long tick)
        {
            SynchronizeExcavationQuarterState(target);
            CellSnapshot before = RequireExcavationCell(target.CellId);
            ExcavationQuarter required = ResolveRequiredExcavationQuarters(
                target.CellId);
            if (!before.IsSolid
                || (before.State.CompletedExcavationQuarters & required) == required)
            {
                return true;
            }

            if (before.State.Designation != CellDesignation.Dig)
            {
                _excavationQuarterWork.Remove(target);
                _excavationCadenceDecisions.Remove(workerId);
                return false;
            }

            int skill = ResolveExcavationMiningSkill(workerId);
            ExcavationCadenceDecision cadence = ResolveExcavationCadence(
                workerId,
                before,
                skill,
                posture,
                tick);
            EnsureExcavationQuarterAssignment(
                workerId,
                target,
                residentCell,
                skill);
            if (!ExcavationCadenceResolver.IsDue(tick, cadence))
            {
                return false;
            }

            IReadOnlyList<ExcavationQuarterCompletion> completions =
                _excavationQuarterWork.ApplyWork(workerId);
            CommitExcavationQuarterCompletions(completions, tick);

            CellSnapshot current = RequireExcavationCell(target.CellId);
            _excavationQuarterWork.SynchronizeCompleted(
                target,
                current.State.CompletedExcavationQuarters);
            return !current.IsSolid
                || (current.State.CompletedExcavationQuarters & required) == required;
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
    }
}
