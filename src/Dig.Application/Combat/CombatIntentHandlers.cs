using System;
using Dig.Application.Messaging;
using Dig.Domain.Combat;
using Dig.Domain.Core;

namespace Dig.Application.Combat
{

public sealed class IssueCombatIntentCommand
    : ICommand<CombatIntentSnapshot>
{
    public IssueCombatIntentCommand(CombatIntentRequest request)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
    }

    public CombatIntentRequest Request { get; }
}

public sealed class FinishCombatIntentCommand : ICommand<Result>
{
    public FinishCombatIntentCommand(
        CombatIntentId intentId,
        bool completed,
        string reasonCode,
        long tick)
    {
        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            throw new ArgumentException("Finish reason is required.", nameof(reasonCode));
        }

        IntentId = intentId;
        Completed = completed;
        ReasonCode = reasonCode.Trim();
        Tick = tick;
    }

    public CombatIntentId IntentId { get; }
    public bool Completed { get; }
    public string ReasonCode { get; }
    public long Tick { get; }
}

public sealed class IssueCombatIntentHandler
    : ICommandHandler<IssueCombatIntentCommand, CombatIntentSnapshot>
{
    private readonly ICombatRepository _combat;
    private readonly IEventSink _eventSink;

    public IssueCombatIntentHandler(
        ICombatRepository combat,
        IEventSink eventSink)
    {
        _combat = combat ?? throw new ArgumentNullException(nameof(combat));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public CombatIntentSnapshot Handle(IssueCombatIntentCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        CombatState combat = _combat.Get();
        CombatIntentSnapshot intent = combat.IssueIntent(command.Request);
        _combat.Save(combat);
        _eventSink.Append(combat.DequeueUncommittedEvents());
        return intent;
    }
}

public sealed class FinishCombatIntentHandler
    : ICommandHandler<FinishCombatIntentCommand, Result>
{
    private readonly ICombatRepository _combat;
    private readonly IEventSink _eventSink;

    public FinishCombatIntentHandler(
        ICombatRepository combat,
        IEventSink eventSink)
    {
        _combat = combat ?? throw new ArgumentNullException(nameof(combat));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(FinishCombatIntentCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        CombatState combat = _combat.Get();
        Result result = command.Completed
            ? combat.CompleteIntent(command.IntentId, command.Tick)
            : combat.CancelIntent(command.IntentId, command.ReasonCode, command.Tick);
        if (result.IsFailure)
        {
            return result;
        }

        _combat.Save(combat);
        _eventSink.Append(combat.DequeueUncommittedEvents());
        return Result.Success();
    }
}
}
