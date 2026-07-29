using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dig.Application.Saving
{

[DataContract]
public sealed class CombatSaveData
{
    [DataMember(Order = 1)] public long Version { get; set; }
    [DataMember(Order = 2)] public List<CombatIntentSaveData> Intents { get; set; } = new List<CombatIntentSaveData>();
    [DataMember(Order = 3)] public List<CombatExecutionSaveData> Executions { get; set; } = new List<CombatExecutionSaveData>();
    [DataMember(Order = 4)] public List<CombatResolutionSaveData> Resolutions { get; set; } = new List<CombatResolutionSaveData>();
    [DataMember(Order = 5)] public List<CombatCooldownSaveData> Cooldowns { get; set; } = new List<CombatCooldownSaveData>();
    [DataMember(Order = 6)] public List<CombatStatusSaveData> Statuses { get; set; } = new List<CombatStatusSaveData>();
}

[DataContract]
public sealed class CombatIntentSaveData
{
    [DataMember(Order = 1)] public string IntentId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string ActorId { get; set; } = string.Empty;
    [DataMember(Order = 3)] public int Kind { get; set; }
    [DataMember(Order = 4)] public int Source { get; set; }
    [DataMember(Order = 5)] public int Status { get; set; }
    [DataMember(Order = 6)] public long CreatedTick { get; set; }
    [DataMember(Order = 7)] public long ExpiresTick { get; set; }
    [DataMember(Order = 8)] public long? FinishedTick { get; set; }
    [DataMember(Order = 9)] public string? TargetEntityId { get; set; }
    [DataMember(Order = 10)] public int? TargetX { get; set; }
    [DataMember(Order = 11)] public int? TargetY { get; set; }
    [DataMember(Order = 12)] public int? TargetZ { get; set; }
    [DataMember(Order = 13)] public string? FinishReason { get; set; }
}

[DataContract]
public sealed class CombatExecutionSaveData
{
    [DataMember(Order = 1)] public string ExecutionId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string IntentId { get; set; } = string.Empty;
    [DataMember(Order = 3)] public string ActorId { get; set; } = string.Empty;
    [DataMember(Order = 4)] public int Source { get; set; }
    [DataMember(Order = 5)] public int Stage { get; set; }
    [DataMember(Order = 6)] public long StartedTick { get; set; }
    [DataMember(Order = 7)] public long NextStageTick { get; set; }
    [DataMember(Order = 8)] public string? TargetEntityId { get; set; }
    [DataMember(Order = 9)] public int? LastKnownX { get; set; }
    [DataMember(Order = 10)] public int? LastKnownY { get; set; }
    [DataMember(Order = 11)] public int? LastKnownZ { get; set; }
    [DataMember(Order = 12)] public string? WeaponProfileId { get; set; }
    [DataMember(Order = 13)] public int? EngagementX { get; set; }
    [DataMember(Order = 14)] public int? EngagementY { get; set; }
    [DataMember(Order = 15)] public int? EngagementZ { get; set; }
    [DataMember(Order = 16)] public string? LastResolvedActionId { get; set; }
    [DataMember(Order = 17)] public int ResolvedActionCount { get; set; }
    [DataMember(Order = 18)] public int RetryCount { get; set; }
    [DataMember(Order = 19)] public string ReasonCode { get; set; } = string.Empty;
    [DataMember(Order = 20)] public long Version { get; set; }
}

[DataContract]
public sealed class CombatResolutionSaveData
{
    [DataMember(Order = 1)] public string ActionId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string AttackerId { get; set; } = string.Empty;
    [DataMember(Order = 3)] public string TargetId { get; set; } = string.Empty;
    [DataMember(Order = 4)] public string WeaponProfileId { get; set; } = string.Empty;
    [DataMember(Order = 5)] public int Outcome { get; set; }
    [DataMember(Order = 6)] public int Distance { get; set; }
    [DataMember(Order = 7)] public int HitChance { get; set; }
    [DataMember(Order = 8)] public int HitRoll { get; set; }
    [DataMember(Order = 9)] public int BlockRoll { get; set; }
    [DataMember(Order = 10)] public int Damage { get; set; }
    [DataMember(Order = 11)] public string? AppliedStatusId { get; set; }
}

[DataContract]
public sealed class CombatCooldownSaveData
{
    [DataMember(Order = 1)] public string ActorId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public long LastAttackTick { get; set; }
}

[DataContract]
public sealed class CombatStatusSaveData
{
    [DataMember(Order = 1)] public string TargetId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string StatusId { get; set; } = string.Empty;
    [DataMember(Order = 3)] public string SourceActionId { get; set; } = string.Empty;
    [DataMember(Order = 4)] public long NextTick { get; set; }
    [DataMember(Order = 5)] public int RemainingTicks { get; set; }
    [DataMember(Order = 6)] public int DamagePerTick { get; set; }
}
}
