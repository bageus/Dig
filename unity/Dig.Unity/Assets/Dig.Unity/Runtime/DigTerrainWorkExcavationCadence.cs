using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Application.World;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private readonly CommitExcavationQuarterCommandHandler _quarterCommitHandler;
        private ExcavationCadenceDecision ResolveExcavationCadence(
            EntityId workerId,
            CellSnapshot target,
            int skill,
            TerrainWorkPosture posture,
            long tick)
        {
            int equipmentInterval = ResolveMiningWorkInterval(
                workerId.ToString(),
                ResidentMiningBaseIntervalTicks);
            int effortPermille = _worldSession.ResolveDepositWorkEffortPermille(
                target.Id);
            int effectiveHardness = checked((int)Math.Max(
                1L,
                ((long)target.Hardness * effortPermille + 999L) / 1_000L));
            ExcavationCadenceDecision cadence = _excavationCadenceResolver.Resolve(
                effectiveHardness,
                skill,
                equipmentInterval,
                posture,
                tick);
            _excavationCadenceDecisions[workerId] = cadence;
            return cadence;
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
                && existing.Approach == approach
                && existing.MiningSkill == miningSkill)
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
            for (int index = 0; index < completions.Count; index++)
            {
                ExcavationQuarterCompletion completion = completions[index];
                ExcavationCutPattern pattern = ResolveExcavationCutPattern(
                    completion.Target.CellId);
                Result<WorldMutationResult> committed = _quarterCommitHandler.Handle(
                    new CommitExcavationQuarterCommand(
                        completion.Target.CellId,
                        completion.Quarter,
                        pattern,
                        _worldSession.EmptyMaterialId,
                        completion.WorkerId,
                        ResolveExcavationSkillGrantProfile(
                            completion.WorkerId,
                            completion.Target.CellId),
                        tick));
                if (committed.IsFailure)
                {
                    throw new InvalidOperationException(committed.Error!.ToString());
                }
            }

            if (completions.Count > 0)
            {
                MarkAuthoritativeWorldChanged();
            }
        }

        private SkillGrantProfile ResolveExcavationSkillGrantProfile(
            EntityId workerId,
            CellId target)
        {
            foreach (JobSnapshot job in _jobRepository.Get().GetAll()
                .Where(value => value.AssignedAgentId == workerId))
            {
                if (job.Definition is DigJobDefinition dig
                    && dig.Target.CellId == target)
                {
                    return dig.SkillGrantProfile;
                }

                if (job.Definition is SpatialDigJobDefinition spatial
                    && spatial.Target.TargetCell == target)
                {
                    break;
                }
            }

            return _worldSession.ResolveExcavationSkillGrantProfile(target);
        }

    }
}
