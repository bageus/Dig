using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Combat
{

public sealed partial class CombatState
{
    private sealed class CombatExecutionRecord
    {
        public CombatExecutionRecord(
            CombatExecutionRequest request,
            EntityId? targetEntityId,
            CellId? lastKnownTargetCell)
        {
            ExecutionId = request.ExecutionId;
            IntentId = request.IntentId;
            ActorId = request.ActorId;
            Source = request.Source;
            Stage = request.InitialStage;
            StartedTick = request.Tick;
            NextStageTick = request.Tick;
            TargetEntityId = targetEntityId;
            LastKnownTargetCell = lastKnownTargetCell;
            ReasonCode = "execution_started";
        }

        public CombatExecutionId ExecutionId { get; }
        public CombatIntentId IntentId { get; }
        public EntityId ActorId { get; }
        public CombatIntentSource Source { get; }
        public CombatExecutionStage Stage { get; private set; }
        public long StartedTick { get; }
        public long NextStageTick { get; private set; }
        public EntityId? TargetEntityId { get; private set; }
        public CellId? LastKnownTargetCell { get; private set; }
        public WeaponProfileId? WeaponProfileId { get; private set; }
        public CellId? EngagementCell { get; private set; }
        public CombatActionId? LastResolvedActionId { get; private set; }
        public int ResolvedActionCount { get; private set; }
        public int RetryCount { get; private set; }
        public string ReasonCode { get; private set; }
        public long Version { get; private set; }
        public bool IsTerminal => Stage == CombatExecutionStage.Completed
            || Stage == CombatExecutionStage.Cancelled;

        public void SetTarget(EntityId targetEntityId, CellId lastKnownTargetCell, string reason)
        {
            TargetEntityId = targetEntityId;
            LastKnownTargetCell = lastKnownTargetCell;
            EngagementCell = null;
            RetryCount = 0;
            SetReason(reason);
        }

        public void SetEquipment(WeaponProfileId weaponProfileId, string reason)
        {
            WeaponProfileId = weaponProfileId;
            SetReason(reason);
        }

        public void SetEngagement(CellId? engagementCell, string reason)
        {
            EngagementCell = engagementCell;
            RetryCount = 0;
            SetReason(reason);
        }

        public void Advance(CombatExecutionStage stage, long nextStageTick, string reason)
        {
            Stage = stage;
            NextStageTick = nextStageTick;
            SetReason(reason);
        }

        public void RecordAttack(CombatActionId actionId, long nextStageTick)
        {
            LastResolvedActionId = actionId;
            ResolvedActionCount = checked(ResolvedActionCount + 1);
            Stage = CombatExecutionStage.Recover;
            NextStageTick = nextStageTick;
            RetryCount = 0;
            SetReason("attack_resolved");
        }

        public void IncrementRetry(long nextStageTick, string reason)
        {
            RetryCount = checked(RetryCount + 1);
            Stage = CombatExecutionStage.Blocked;
            NextStageTick = nextStageTick;
            SetReason(reason);
        }

        public CombatExecutionSnapshot CreateSnapshot()
        {
            return new CombatExecutionSnapshot(
                ExecutionId,
                IntentId,
                ActorId,
                Source,
                Stage,
                StartedTick,
                NextStageTick,
                TargetEntityId,
                LastKnownTargetCell,
                WeaponProfileId,
                EngagementCell,
                LastResolvedActionId,
                ResolvedActionCount,
                RetryCount,
                ReasonCode,
                Version);
        }

        private void SetReason(string reason)
        {
            ReasonCode = string.IsNullOrWhiteSpace(reason)
                ? "unspecified"
                : reason.Trim();
            Version = checked(Version + 1);
        }
    }
}
}
