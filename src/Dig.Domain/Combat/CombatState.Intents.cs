using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Combat
{

public sealed partial class CombatState
{
    private readonly Dictionary<CombatIntentId, CombatIntentRecord> _intents =
        new Dictionary<CombatIntentId, CombatIntentRecord>();
    private readonly Dictionary<EntityId, CombatIntentId> _activeIntents =
        new Dictionary<EntityId, CombatIntentId>();

    public CombatIntentSnapshot IssueIntent(CombatIntentRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (_intents.TryGetValue(request.IntentId, out CombatIntentRecord? existing))
        {
            return existing.CreateSnapshot();
        }

        CombatIntentSnapshot? previous = null;
        if (_activeIntents.TryGetValue(request.ActorId, out CombatIntentId activeId))
        {
            CombatIntentRecord active = _intents[activeId];
            active.Finish(
                CombatIntentStatus.Cancelled,
                request.CreatedTick,
                "replaced_by_new_intent");
            previous = active.CreateSnapshot();
            Raise(new CombatIntentFinished(request.CreatedTick, previous));
        }

        CombatIntentRecord created = new CombatIntentRecord(request);
        _intents.Add(request.IntentId, created);
        _activeIntents[request.ActorId] = request.IntentId;
        Version = checked(Version + 1);
        CombatIntentSnapshot snapshot = created.CreateSnapshot();
        Raise(new CombatIntentChanged(
            request.CreatedTick,
            request.ActorId,
            previous,
            snapshot));
        return snapshot;
    }

    public Result CompleteIntent(CombatIntentId intentId, long tick)
    {
        return FinishIntent(
            intentId,
            CombatIntentStatus.Completed,
            tick,
            "completed");
    }

    public Result CancelIntent(CombatIntentId intentId, string reasonCode, long tick)
    {
        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            throw new ArgumentException("Cancellation reason is required.", nameof(reasonCode));
        }

        return FinishIntent(
            intentId,
            CombatIntentStatus.Cancelled,
            tick,
            reasonCode.Trim());
    }

    public IReadOnlyList<CombatIntentSnapshot> ExpireIntents(long tick)
    {
        ValidateIntentTick(tick);
        List<CombatIntentSnapshot> expired = new List<CombatIntentSnapshot>();
        CombatIntentId[] activeIds = _activeIntents.Values
            .OrderBy(id => id)
            .ToArray();
        foreach (CombatIntentId intentId in activeIds)
        {
            CombatIntentRecord intent = _intents[intentId];
            if (intent.ExpiresTick > tick)
            {
                continue;
            }

            intent.Finish(CombatIntentStatus.Expired, tick, "expired");
            _activeIntents.Remove(intent.ActorId);
            CombatIntentSnapshot snapshot = intent.CreateSnapshot();
            expired.Add(snapshot);
            Raise(new CombatIntentFinished(tick, snapshot));
        }

        if (expired.Count > 0)
        {
            Version = checked(Version + 1);
        }

        return new ReadOnlyCollection<CombatIntentSnapshot>(expired);
    }

    public CombatIntentSnapshot? GetActiveIntent(EntityId actorId)
    {
        return _activeIntents.TryGetValue(actorId, out CombatIntentId intentId)
            ? _intents[intentId].CreateSnapshot()
            : null;
    }

    public IReadOnlyList<CombatIntentSnapshot> CreateIntentSnapshot()
    {
        CombatIntentSnapshot[] intents = _intents.Values
            .OrderBy(intent => intent.IntentId)
            .Select(intent => intent.CreateSnapshot())
            .ToArray();
        return new ReadOnlyCollection<CombatIntentSnapshot>(intents);
    }

    private Result FinishIntent(
        CombatIntentId intentId,
        CombatIntentStatus status,
        long tick,
        string reasonCode)
    {
        ValidateIntentTick(tick);
        if (!_intents.TryGetValue(intentId, out CombatIntentRecord? intent))
        {
            return Result.Failure(new DomainError(
                "combat.intent.unknown",
                "The combat intent is not registered."));
        }

        if (!intent.IsActive)
        {
            return Result.Success();
        }

        intent.Finish(status, tick, reasonCode);
        _activeIntents.Remove(intent.ActorId);
        Version = checked(Version + 1);
        Raise(new CombatIntentFinished(tick, intent.CreateSnapshot()));
        return Result.Success();
    }

    private static void ValidateIntentTick(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }
    }

    private sealed class CombatIntentRecord
    {
        public CombatIntentRecord(CombatIntentRequest request)
        {
            IntentId = request.IntentId;
            ActorId = request.ActorId;
            Kind = request.Kind;
            Source = request.Source;
            CreatedTick = request.CreatedTick;
            ExpiresTick = request.ExpiresTick;
            TargetEntityId = request.TargetEntityId;
            TargetCell = request.TargetCell;
        }

        public CombatIntentId IntentId { get; }
        public EntityId ActorId { get; }
        public CombatIntentKind Kind { get; }
        public CombatIntentSource Source { get; }
        public long CreatedTick { get; }
        public long ExpiresTick { get; }
        public EntityId? TargetEntityId { get; }
        public Dig.Domain.World.CellId? TargetCell { get; }
        public CombatIntentStatus Status { get; private set; }
        public long? FinishedTick { get; private set; }
        public string? FinishReason { get; private set; }
        public bool IsActive => Status == CombatIntentStatus.Active;

        public void Finish(
            CombatIntentStatus status,
            long tick,
            string reasonCode)
        {
            if (!IsActive)
            {
                return;
            }

            Status = status;
            FinishedTick = tick;
            FinishReason = reasonCode;
        }

        public CombatIntentSnapshot CreateSnapshot()
        {
            return new CombatIntentSnapshot(
                IntentId,
                ActorId,
                Kind,
                Source,
                Status,
                CreatedTick,
                ExpiresTick,
                FinishedTick,
                TargetEntityId,
                TargetCell,
                FinishReason);
        }
    }
}
}
