using System;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Society
{

public interface IResidentLifecycleEvent : IDomainEvent
{
    string EventId { get; }
    EntityId ResidentId { get; }
}

public sealed class ResidentBorn : IResidentLifecycleEvent
{
    public ResidentBorn(
        long tick,
        EntityId residentId,
        string name,
        ResidentSex sex,
        EntityId motherId,
        EntityId fatherId,
        CellId position)
    {
        ValidateTick(tick);
        if (residentId.IsEmpty || motherId.IsEmpty || fatherId.IsEmpty)
        {
            throw new ArgumentException("Resident and parent ids cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Resident name is required.", nameof(name));
        }

        Tick = tick;
        ResidentId = residentId;
        Name = name.Trim();
        Sex = sex;
        MotherId = motherId;
        FatherId = fatherId;
        Position = position;
        EventId = $"resident-born:{residentId}";
    }

    public string EventId { get; }
    public long Tick { get; }
    public EntityId ResidentId { get; }
    public string Name { get; }
    public ResidentSex Sex { get; }
    public EntityId MotherId { get; }
    public EntityId FatherId { get; }
    public CellId Position { get; }

    private static void ValidateTick(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }
    }
}

public sealed class ResidentLifeStageChanged : IResidentLifecycleEvent
{
    public ResidentLifeStageChanged(
        long tick,
        EntityId residentId,
        ResidentLifeStage previousStage,
        ResidentLifeStage currentStage)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (residentId.IsEmpty)
        {
            throw new ArgumentException("Resident id cannot be empty.", nameof(residentId));
        }

        if (previousStage == currentStage)
        {
            throw new ArgumentException("Life stage must change.", nameof(currentStage));
        }

        Tick = tick;
        ResidentId = residentId;
        PreviousStage = previousStage;
        CurrentStage = currentStage;
        EventId = $"resident-stage:{residentId}:{currentStage}";
    }

    public string EventId { get; }
    public long Tick { get; }
    public EntityId ResidentId { get; }
    public ResidentLifeStage PreviousStage { get; }
    public ResidentLifeStage CurrentStage { get; }
}

public sealed class ResidentDied : IResidentLifecycleEvent
{
    public ResidentDied(
        long tick,
        EntityId residentId,
        ResidentDeathCauseId cause,
        CellId lastKnownPosition)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (residentId.IsEmpty)
        {
            throw new ArgumentException("Resident id cannot be empty.", nameof(residentId));
        }

        if (cause.IsEmpty)
        {
            throw new ArgumentException("Death cause cannot be empty.", nameof(cause));
        }

        Tick = tick;
        ResidentId = residentId;
        Cause = cause;
        LastKnownPosition = lastKnownPosition;
        EventId = $"resident-died:{residentId}";
    }

    public string EventId { get; }
    public long Tick { get; }
    public EntityId ResidentId { get; }
    public ResidentDeathCauseId Cause { get; }
    public CellId LastKnownPosition { get; }
}

public sealed class ResidentPartnershipChanged : IDomainEvent
{
    public ResidentPartnershipChanged(
        long tick,
        EntityId firstResidentId,
        EntityId secondResidentId,
        bool partnered)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (firstResidentId.IsEmpty || secondResidentId.IsEmpty || firstResidentId == secondResidentId)
        {
            throw new ArgumentException("Partnership requires two distinct residents.");
        }

        Tick = tick;
        FirstResidentId = firstResidentId;
        SecondResidentId = secondResidentId;
        Partnered = partnered;
    }

    public long Tick { get; }
    public EntityId FirstResidentId { get; }
    public EntityId SecondResidentId { get; }
    public bool Partnered { get; }
}

public sealed class ResidentPregnancyStarted : IDomainEvent
{
    public ResidentPregnancyStarted(
        long tick,
        EntityId motherId,
        EntityId fatherId,
        long dueTick)
    {
        if (tick < 0 || dueTick <= tick)
        {
            throw new ArgumentOutOfRangeException(nameof(dueTick));
        }

        Tick = tick;
        MotherId = motherId;
        FatherId = fatherId;
        DueTick = dueTick;
    }

    public long Tick { get; }
    public EntityId MotherId { get; }
    public EntityId FatherId { get; }
    public long DueTick { get; }
}
}
