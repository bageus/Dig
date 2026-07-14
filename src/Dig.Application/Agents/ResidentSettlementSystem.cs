using System;
using System.Collections.Generic;
using Dig.Application.Inventory;
using Dig.Application.Messaging;
using Dig.Application.Runtime;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Application.Agents
{

public sealed class ResidentSettlementSystem : ISimulationSystem
{
    private readonly IAgentRepository _agents;
    private readonly IAgentDecisionContextProvider _externalContexts;
    private readonly IInventoryRepository _inventory;
    private readonly IBuildingFacilitiesRepository _facilities;
    private readonly IEventSink _events;
    private readonly AgentDecisionSystem _decisions;
    private readonly AgentBehaviorPolicy _policy;
    private readonly SettlementTargetCoordinator _targets;

    public ResidentSettlementSystem(
        IAgentRepository agents,
        IAgentDecisionContextProvider externalContexts,
        IInventoryRepository inventory,
        IBuildingFacilitiesRepository facilities,
        IEventSink events,
        AgentDecisionSystem decisions,
        AgentBehaviorPolicy policy,
        int order = 300,
        int intervalTicks = 1)
    {
        if (intervalTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(intervalTicks));
        }

        _agents = agents ?? throw new ArgumentNullException(nameof(agents));
        _externalContexts = externalContexts
            ?? throw new ArgumentNullException(nameof(externalContexts));
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        _facilities = facilities ?? throw new ArgumentNullException(nameof(facilities));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _decisions = decisions ?? throw new ArgumentNullException(nameof(decisions));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _targets = new SettlementTargetCoordinator(inventory, facilities);
        Order = order;
        IntervalTicks = intervalTicks;
    }

    public string Name => "agents.settlement";

    public int Order { get; }

    public int IntervalTicks { get; }

    public SettlementTickReport? LastReport { get; private set; }

    public void Execute(SimulationContext context)
    {
        IReadOnlyList<AgentState> agents = _agents.GetAll();
        SettlementAgentDiagnostic[] diagnostics =
            new SettlementAgentDiagnostic[agents.Count];
        int diagnosticCount = 0;
        foreach (AgentState agent in agents)
        {
            if (!agent.IsAlive)
            {
                _targets.Release(agent.Id, context.Tick);
                continue;
            }

            Require(agent.AdvanceNeeds(_policy, context.Tick));
            if (!agent.IsAlive)
            {
                _targets.Release(agent.Id, context.Tick);
                SaveAgent(agent);
                continue;
            }

            AgentSnapshot snapshot = agent.CreateSnapshot(context.Tick);
            string? blockedReason = _targets.ValidateExistingTarget(
                agent,
                snapshot.ActiveAction,
                context.Tick);
            if (blockedReason is not null)
            {
                snapshot = agent.CreateSnapshot(context.Tick);
            }

            AgentDecisionContext external = _externalContexts.GetContext(snapshot, context.Tick);
            AgentDecisionContext decisionContext = _targets.CreateContext(snapshot, external);
            AgentDecision decision = _decisions.Decide(
                snapshot,
                decisionContext,
                _policy,
                context.Tick);
            string? unavailable = _targets.GetUnavailableNeedReason(
                snapshot,
                decisionContext,
                _policy);
            if (IsCriticalUnavailable(unavailable))
            {
                blockedReason = unavailable;
                _targets.Release(agent.Id, context.Tick);
                if (snapshot.ActiveAction.HasValue)
                {
                    Require(agent.BlockCurrentAction(
                        InferBlockedIntent(unavailable!),
                        unavailable!,
                        context.Tick));
                }
                else
                {
                    RecordUnavailable(
                        agent,
                        InferBlockedIntent(unavailable!),
                        unavailable!,
                        context.Tick);
                }

                SaveAgent(agent);
                diagnostics[diagnosticCount++] = new SettlementAgentDiagnostic(
                    agent.Id,
                    decision,
                    target: null,
                    actionCompleted: false,
                    blockedReason);
                continue;
            }

            AgentActivityTarget? target = null;
            bool completed = false;
            if (IsTargeted(decision.SelectedIntent))
            {
                Result<AgentActivityTarget> acquired = ResolveTarget(
                    snapshot,
                    decision.SelectedIntent,
                    context.Tick);
                if (acquired.IsFailure)
                {
                    blockedReason = acquired.Error!.Code;
                    RecordUnavailable(agent, decision.SelectedIntent, blockedReason, context.Tick);
                }
                else
                {
                    target = acquired.Value;
                    Require(agent.ApplyDecision(decision, _policy, target, context.Tick));
                    Result<bool> progress = agent.AdvanceTargetedAction(context.Tick);
                    if (progress.IsFailure)
                    {
                        throw new InvalidOperationException(progress.Error!.ToString());
                    }

                    if (progress.Value)
                    {
                        Result externalCompletion = _targets.Complete(
                            agent.Id,
                            target.Value,
                            context.Tick);
                        if (externalCompletion.IsFailure)
                        {
                            blockedReason = externalCompletion.Error!.Code;
                            Require(agent.BlockTargetedAction(blockedReason, context.Tick));
                            _targets.Release(agent.Id, context.Tick);
                        }
                        else
                        {
                            Require(agent.CompleteTargetedAction(_policy, context.Tick));
                            completed = true;
                        }
                    }
                }
            }
            else
            {
                if (snapshot.ActiveAction?.Target.HasValue == true)
                {
                    _targets.Release(agent.Id, context.Tick);
                }

                Require(agent.ApplyDecision(decision, _policy, context.Tick));
                Require(agent.AdvanceAction(_policy, context.Tick));
            }

            if (blockedReason is null && unavailable is not null)
            {
                blockedReason = unavailable;
                RecordUnavailable(
                    agent,
                    InferBlockedIntent(unavailable),
                    unavailable,
                    context.Tick);
            }

            SaveAgent(agent);
            diagnostics[diagnosticCount++] = new SettlementAgentDiagnostic(
                agent.Id,
                decision,
                target,
                completed,
                blockedReason);
        }

        SaveSharedState();
        LastReport = SettlementTickReport.CreateFromStableOrder(
            context.Tick,
            diagnostics,
            diagnosticCount);
    }

    private Result<AgentActivityTarget> ResolveTarget(
        AgentSnapshot snapshot,
        AgentIntentKind intent,
        long tick)
    {
        AgentActivityTarget? current = snapshot.ActiveAction?.Target;
        if (snapshot.ActiveAction?.IntentKind == intent
            && current.HasValue
            && _targets.IsValidReservation(snapshot.Id, current.Value))
        {
            return Result<AgentActivityTarget>.Success(current.Value);
        }

        return _targets.Acquire(snapshot.Id, intent, tick);
    }

    private void SaveAgent(AgentState agent)
    {
        _agents.Save(agent);
        _events.Append(agent.DequeueUncommittedEvents());
    }

    private void SaveSharedState()
    {
        InventoryState inventory = _inventory.Get();
        BuildingFacilitiesState facilities = _facilities.Get();
        _inventory.Save(inventory);
        _facilities.Save(facilities);
        _events.Append(inventory.DequeueUncommittedEvents());
        _events.Append(facilities.DequeueUncommittedEvents());
    }

    private static bool IsTargeted(AgentIntentKind intent)
    {
        return intent is AgentIntentKind.Eat or AgentIntentKind.Sleep or AgentIntentKind.Rest;
    }

    private static bool IsCriticalUnavailable(string? reason)
    {
        return reason is "food_unavailable" or "bed_unavailable";
    }

    private static AgentIntentKind InferBlockedIntent(string reason)
    {
        return reason switch
        {
            "food_unavailable" => AgentIntentKind.Eat,
            "bed_unavailable" => AgentIntentKind.Sleep,
            "leisure_unavailable" => AgentIntentKind.Rest,
            _ => AgentIntentKind.Idle,
        };
    }

    private static void RecordUnavailable(
        AgentState agent,
        AgentIntentKind intent,
        string reason,
        long tick)
    {
        if (!string.Equals(agent.LastActionBlockReason, reason, StringComparison.Ordinal))
        {
            agent.RecordBlockedIntent(intent, reason, tick);
        }
    }

    private static void Require(Result result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error!.ToString());
        }
    }
}
}
