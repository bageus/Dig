using System;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Messaging;
using Dig.Domain.Agents;
using Dig.Domain.Combat;
using Dig.Domain.Core;
using Dig.Domain.Navigation;

namespace Dig.Application.Combat
{

public sealed partial class CombatSpatialExecutionHandler
    : ICommandHandler<AdvanceCombatSpatialExecutionCommand, Result<CombatSpatialExecutionReport>>
{
    private readonly IAgentRepository _agents;
    private readonly ICombatRepository _combat;
    private readonly IFactionRepository _factions;
    private readonly TunnelNavigationVolume _volume;
    private readonly ICombatEquipmentProvider _equipment;
    private readonly IEventSink _events;
    private readonly CombatSpatialPolicy _policy;
    private readonly ResolveCombatAttackHandler _attackHandler;
    private readonly MoveAgentCommandHandler _moveHandler;

    public CombatSpatialExecutionHandler(
        IAgentRepository agents,
        ICombatRepository combat,
        IFactionRepository factions,
        TunnelNavigationVolume volume,
        ICombatEquipmentProvider equipment,
        IEventSink events,
        IAgentSkillGrantService skillGrants,
        CombatSpatialPolicy policy)
    {
        _agents = agents ?? throw new ArgumentNullException(nameof(agents));
        _combat = combat ?? throw new ArgumentNullException(nameof(combat));
        _factions = factions ?? throw new ArgumentNullException(nameof(factions));
        _volume = volume ?? throw new ArgumentNullException(nameof(volume));
        _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _attackHandler = new ResolveCombatAttackHandler(
            agents, combat, factions, events,
            skillGrants ?? throw new ArgumentNullException(nameof(skillGrants)));
        _moveHandler = new MoveAgentCommandHandler(agents, events);
    }

    public Result<CombatSpatialExecutionReport> Handle(AdvanceCombatSpatialExecutionCommand command)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));
        CombatState combat = _combat.Get();
        CombatExecutionSnapshot? executionBeforeExpiry =
            combat.GetActiveExecution(command.ActorId);
        bool actorIntentExpired = combat.ExpireIntents(command.Tick)
            .Any(value => value.ActorId == command.ActorId);
        if (actorIntentExpired)
        {
            SaveCombat(combat);
            if (executionBeforeExpiry is not null)
            {
                return Report(
                    combat.GetExecution(executionBeforeExpiry.ExecutionId)!,
                    false,
                    null,
                    "intent_expired");
            }

            return Result<CombatSpatialExecutionReport>.Failure(
                CombatSpatialApplicationErrors.IntentMissing);
        }

        CombatIntentSnapshot? intent = combat.GetActiveIntent(command.ActorId);
        if (intent is null)
        {
            return Result<CombatSpatialExecutionReport>.Failure(
                CombatSpatialApplicationErrors.IntentMissing);
        }

        AgentState? actor = _agents.Get(command.ActorId);
        if (actor is null || !actor.IsAlive)
        {
            return Result<CombatSpatialExecutionReport>.Failure(
                CombatSpatialApplicationErrors.ActorUnavailable);
        }

        CombatExecutionSnapshot? execution = combat.GetActiveExecution(command.ActorId);
        if (execution is null)
        {
            Result<CombatExecutionSnapshot> started = combat.StartExecution(
                new CombatExecutionRequest(
                    CreateExecutionId(intent),
                    intent.IntentId,
                    command.ActorId,
                    intent.Source,
                    CombatExecutionStage.AcquireTarget,
                    command.Tick));
            if (started.IsFailure)
            {
                return Result<CombatSpatialExecutionReport>.Failure(started.Error!);
            }

            SaveCombat(combat);
            return Report(started.Value, false, null, "execution_started");
        }

        Result<CombatSpatialExecutionReport>? sightLoss =
            FinishAutonomousSightLossBeforeStage(
                command,
                combat,
                intent,
                actor,
                execution);
        if (sightLoss != null)
        {
            return sightLoss;
        }

        if (command.Tick < execution.NextStageTick)
        {
            return Report(execution, false, null, "stage_waiting");
        }

        return execution.Stage switch
        {
            CombatExecutionStage.AcquireTarget => AcquireTarget(command, combat, intent, actor),
            CombatExecutionStage.SelectEquipment => SelectEquipment(command, combat, actor),
            CombatExecutionStage.SelectEngagementCell => SelectEngagement(command, combat, actor),
            CombatExecutionStage.Approach => Approach(command, combat, actor),
            CombatExecutionStage.FaceTarget => Advance(
                combat, execution, CombatExecutionStage.WindUp,
                checked(command.Tick + _policy.WindUpTicks), command.Tick, "target_faced"),
            CombatExecutionStage.WindUp => Advance(
                combat, execution, CombatExecutionStage.ResolveAttack,
                command.Tick, command.Tick, "wind_up_complete"),
            CombatExecutionStage.ResolveAttack => ResolveAttack(command, combat, actor),
            CombatExecutionStage.Recover => Advance(
                combat, execution, CombatExecutionStage.Reevaluate,
                command.Tick, command.Tick, "recovery_complete"),
            CombatExecutionStage.Reevaluate => Reevaluate(command, combat, intent, actor),
            CombatExecutionStage.Retreat => Retreat(command, combat, actor),
            CombatExecutionStage.Blocked => RetryBlocked(command, combat, execution),
            _ => Report(execution, false, null, "execution_terminal"),
        };
    }

    private Result<CombatSpatialExecutionReport>?
        FinishAutonomousSightLossBeforeStage(
            AdvanceCombatSpatialExecutionCommand command,
            CombatState combat,
            CombatIntentSnapshot intent,
            AgentState actor,
            CombatExecutionSnapshot execution)
    {
        if (intent.Source == CombatIntentSource.PlayerOrder
            || execution.Stage == CombatExecutionStage.AcquireTarget
            || !execution.TargetEntityId.HasValue)
        {
            return null;
        }

        AgentState? target = _agents.Get(execution.TargetEntityId.Value);
        if (target == null
            || !target.IsAlive
            || IsVisible(actor.Position, target.Position))
        {
            return null;
        }

        return FinishForTargetLoss(
            command,
            combat,
            intent,
            "enemy_target_out_of_sight");
    }

    private static CombatExecutionId CreateExecutionId(CombatIntentSnapshot intent) =>
        new CombatExecutionId("execution:" + intent.IntentId);

    private Result<CombatSpatialExecutionReport> Advance(
        CombatState combat,
        CombatExecutionSnapshot execution,
        CombatExecutionStage stage,
        long nextStageTick,
        long tick,
        string reason)
    {
        Result advanced = combat.AdvanceExecutionStage(
            execution.ExecutionId, stage, nextStageTick, tick, reason);
        if (advanced.IsFailure)
        {
            return Result<CombatSpatialExecutionReport>.Failure(advanced.Error!);
        }

        SaveCombat(combat);
        return Report(combat.GetExecution(execution.ExecutionId)!, false, null, reason);
    }

    private void SaveCombat(CombatState combat)
    {
        _combat.Save(combat);
        _events.Append(combat.DequeueUncommittedEvents());
    }

    private static Result<CombatSpatialExecutionReport> Report(
        CombatExecutionSnapshot execution,
        bool moved,
        CombatAttackResolution? attack,
        string reason) =>
        Result<CombatSpatialExecutionReport>.Success(
            new CombatSpatialExecutionReport(execution, moved, attack, reason));
}
}
