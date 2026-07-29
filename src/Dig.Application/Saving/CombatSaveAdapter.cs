using System;
using System.Linq;
using Dig.Domain.Combat;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public static class CombatSaveAdapter
{
    public static CombatSaveData Encode(CombatState? combat)
    {
        CombatSaveData data = new CombatSaveData();
        if (combat is null)
        {
            return data;
        }

        CombatStateSnapshot state = combat.CreateSnapshot();
        data.Version = state.Version;
        foreach (CombatIntentSnapshot intent in combat.CreateIntentSnapshot())
        {
            data.Intents.Add(new CombatIntentSaveData
            {
                IntentId = intent.IntentId.ToString(),
                ActorId = intent.ActorId.ToString(),
                Kind = (int)intent.Kind,
                Source = (int)intent.Source,
                Status = (int)intent.Status,
                CreatedTick = intent.CreatedTick,
                ExpiresTick = intent.ExpiresTick,
                FinishedTick = intent.FinishedTick,
                TargetEntityId = intent.TargetEntityId?.ToString(),
                TargetX = intent.TargetCell?.X,
                TargetY = intent.TargetCell?.Y,
                TargetZ = intent.TargetCell?.Z,
                FinishReason = intent.FinishReason,
            });
        }

        foreach (CombatExecutionSnapshot execution in combat.CreateExecutionSnapshot())
        {
            data.Executions.Add(new CombatExecutionSaveData
            {
                ExecutionId = execution.ExecutionId.ToString(),
                IntentId = execution.IntentId.ToString(),
                ActorId = execution.ActorId.ToString(),
                Source = (int)execution.Source,
                Stage = (int)execution.Stage,
                StartedTick = execution.StartedTick,
                NextStageTick = execution.NextStageTick,
                TargetEntityId = execution.TargetEntityId?.ToString(),
                LastKnownX = execution.LastKnownTargetCell?.X,
                LastKnownY = execution.LastKnownTargetCell?.Y,
                LastKnownZ = execution.LastKnownTargetCell?.Z,
                WeaponProfileId = execution.WeaponProfileId?.ToString(),
                EngagementX = execution.EngagementCell?.X,
                EngagementY = execution.EngagementCell?.Y,
                EngagementZ = execution.EngagementCell?.Z,
                LastResolvedActionId = execution.LastResolvedActionId?.ToString(),
                ResolvedActionCount = execution.ResolvedActionCount,
                RetryCount = execution.RetryCount,
                ReasonCode = execution.ReasonCode,
                Version = execution.Version,
            });
        }

        foreach (CombatAttackResolution resolution in combat.CreateResolutionSnapshot())
        {
            data.Resolutions.Add(new CombatResolutionSaveData
            {
                ActionId = resolution.ActionId.ToString(),
                AttackerId = resolution.AttackerId.ToString(),
                TargetId = resolution.TargetId.ToString(),
                WeaponProfileId = resolution.WeaponProfileId.ToString(),
                Outcome = (int)resolution.Outcome,
                Distance = resolution.Distance,
                HitChance = resolution.HitChance,
                HitRoll = resolution.HitRoll,
                BlockRoll = resolution.BlockRoll,
                Damage = resolution.Damage,
                AppliedStatusId = resolution.AppliedStatusId?.ToString(),
            });
        }

        foreach (CombatCooldownSnapshot cooldown in combat.CreateCooldownSnapshot())
        {
            data.Cooldowns.Add(new CombatCooldownSaveData
            {
                ActorId = cooldown.ActorId.ToString(),
                LastAttackTick = cooldown.LastAttackTick,
            });
        }

        foreach (CombatStatusSnapshot status in state.Statuses)
        {
            data.Statuses.Add(new CombatStatusSaveData
            {
                TargetId = status.TargetId.ToString(),
                StatusId = status.StatusId.ToString(),
                SourceActionId = status.SourceActionId.ToString(),
                NextTick = status.NextTick,
                RemainingTicks = status.RemainingTicks,
                DamagePerTick = status.DamagePerTick,
            });
        }

        return data;
    }

    public static Result<CombatState> Decode(CombatSaveData? data, WeaponCatalog weapons)
    {
        if (weapons is null)
        {
            throw new ArgumentNullException(nameof(weapons));
        }

        data ??= new CombatSaveData();
        try
        {
            CombatState combat = new CombatState(weapons);
            CombatIntentSnapshot[] intents = data.Intents
                .OrderBy(item => item.IntentId, StringComparer.Ordinal)
                .Select(DecodeIntent)
                .ToArray();
            CombatExecutionSnapshot[] executions = data.Executions
                .OrderBy(item => item.ExecutionId, StringComparer.Ordinal)
                .Select(DecodeExecution)
                .ToArray();
            CombatAttackResolution[] resolutions = data.Resolutions
                .OrderBy(item => item.ActionId, StringComparer.Ordinal)
                .Select(DecodeResolution)
                .ToArray();
            CombatCooldownSnapshot[] cooldowns = data.Cooldowns
                .OrderBy(item => item.ActorId, StringComparer.Ordinal)
                .Select(item => new CombatCooldownSnapshot(
                    EntityId.Parse(item.ActorId),
                    item.LastAttackTick))
                .ToArray();
            CombatStatusSnapshot[] statuses = data.Statuses
                .OrderBy(item => item.TargetId, StringComparer.Ordinal)
                .ThenBy(item => item.StatusId, StringComparer.Ordinal)
                .Select(item => new CombatStatusSnapshot(
                    EntityId.Parse(item.TargetId),
                    new CombatStatusId(item.StatusId),
                    new CombatActionId(item.SourceActionId),
                    item.NextTick,
                    item.RemainingTicks,
                    item.DamagePerTick))
                .ToArray();
            Result restored = combat.RestoreRuntime(
                data.Version,
                intents,
                executions,
                resolutions,
                cooldowns,
                statuses);
            return restored.IsSuccess
                ? Result<CombatState>.Success(combat)
                : Result<CombatState>.Failure(restored.Error!);
        }
        catch (Exception)
        {
            return Result<CombatState>.Failure(SaveErrors.InvalidDocument);
        }
    }

    private static CombatIntentSnapshot DecodeIntent(CombatIntentSaveData item) =>
        new CombatIntentSnapshot(
            new CombatIntentId(item.IntentId),
            EntityId.Parse(item.ActorId),
            (CombatIntentKind)item.Kind,
            (CombatIntentSource)item.Source,
            (CombatIntentStatus)item.Status,
            item.CreatedTick,
            item.ExpiresTick,
            item.FinishedTick,
            ParseOptionalId(item.TargetEntityId),
            BuildOptionalCell(item.TargetX, item.TargetY, item.TargetZ),
            item.FinishReason);

    private static CombatExecutionSnapshot DecodeExecution(CombatExecutionSaveData item) =>
        new CombatExecutionSnapshot(
            new CombatExecutionId(item.ExecutionId),
            new CombatIntentId(item.IntentId),
            EntityId.Parse(item.ActorId),
            (CombatIntentSource)item.Source,
            (CombatExecutionStage)item.Stage,
            item.StartedTick,
            item.NextStageTick,
            ParseOptionalId(item.TargetEntityId),
            BuildOptionalCell(item.LastKnownX, item.LastKnownY, item.LastKnownZ),
            string.IsNullOrWhiteSpace(item.WeaponProfileId)
                ? (WeaponProfileId?)null
                : new WeaponProfileId(item.WeaponProfileId),
            BuildOptionalCell(item.EngagementX, item.EngagementY, item.EngagementZ),
            string.IsNullOrWhiteSpace(item.LastResolvedActionId)
                ? (CombatActionId?)null
                : new CombatActionId(item.LastResolvedActionId),
            item.ResolvedActionCount,
            item.RetryCount,
            item.ReasonCode,
            item.Version);

    private static CombatAttackResolution DecodeResolution(CombatResolutionSaveData item) =>
        new CombatAttackResolution(
            new CombatActionId(item.ActionId),
            EntityId.Parse(item.AttackerId),
            EntityId.Parse(item.TargetId),
            new WeaponProfileId(item.WeaponProfileId),
            (CombatAttackOutcome)item.Outcome,
            item.Distance,
            item.HitChance,
            item.HitRoll,
            item.BlockRoll,
            item.Damage,
            string.IsNullOrWhiteSpace(item.AppliedStatusId)
                ? (CombatStatusId?)null
                : new CombatStatusId(item.AppliedStatusId),
            wasAlreadyProcessed: false);

    private static EntityId? ParseOptionalId(string? value) =>
        string.IsNullOrWhiteSpace(value) ? (EntityId?)null : EntityId.Parse(value);

    private static CellId? BuildOptionalCell(int? x, int? y, int? z) =>
        x.HasValue && y.HasValue && z.HasValue
            ? new CellId(x.Value, y.Value, z.Value)
            : (CellId?)null;
}
}
