using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Society
{

public sealed class ResidentRegistration
{
    public ResidentRegistration(
        EntityId id,
        string name,
        ResidentSex sex,
        long birthTick,
        CellId position,
        ResidentHeritage heritage)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("Resident id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Resident name is required.", nameof(name));
        }

        if (birthTick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(birthTick));
        }

        Id = id;
        Name = name.Trim();
        Sex = sex;
        BirthTick = birthTick;
        Position = position;
        Heritage = heritage ?? throw new ArgumentNullException(nameof(heritage));
    }

    public EntityId Id { get; }
    public string Name { get; }
    public ResidentSex Sex { get; }
    public long BirthTick { get; }
    public CellId Position { get; }
    public ResidentHeritage Heritage { get; }
}

public sealed class ResidentBirthPlan
{
    public ResidentBirthPlan(
        EntityId id,
        string name,
        ResidentSex sex,
        ResidentHeritage heritage,
        CellId position)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("Resident id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Resident name is required.", nameof(name));
        }

        Id = id;
        Name = name.Trim();
        Sex = sex;
        Heritage = heritage ?? throw new ArgumentNullException(nameof(heritage));
        Position = position;
    }

    public EntityId Id { get; }
    public string Name { get; }
    public ResidentSex Sex { get; }
    public ResidentHeritage Heritage { get; }
    public CellId Position { get; }
}

public sealed class PregnancySnapshot
{
    public PregnancySnapshot(EntityId fatherId, long conceptionTick, long dueTick)
    {
        FatherId = fatherId;
        ConceptionTick = conceptionTick;
        DueTick = dueTick;
    }

    public EntityId FatherId { get; }
    public long ConceptionTick { get; }
    public long DueTick { get; }
}

public sealed class ResidentSocietySnapshot
{
    public ResidentSocietySnapshot(
        EntityId id,
        string name,
        ResidentSex sex,
        long birthTick,
        ResidentLifeStage lifeStage,
        bool isAlive,
        EntityId? motherId,
        EntityId? fatherId,
        EntityId? partnerId,
        PregnancySnapshot? pregnancy,
        CellId lastKnownPosition,
        ResidentDeathCauseId? deathCause,
        long? deathTick,
        ResidentHeritage heritage)
    {
        Id = id;
        Name = name;
        Sex = sex;
        BirthTick = birthTick;
        LifeStage = lifeStage;
        IsAlive = isAlive;
        MotherId = motherId;
        FatherId = fatherId;
        PartnerId = partnerId;
        Pregnancy = pregnancy;
        LastKnownPosition = lastKnownPosition;
        DeathCause = deathCause;
        DeathTick = deathTick;
        Heritage = heritage;
    }

    public EntityId Id { get; }
    public string Name { get; }
    public ResidentSex Sex { get; }
    public long BirthTick { get; }
    public ResidentLifeStage LifeStage { get; }
    public bool IsAlive { get; }
    public EntityId? MotherId { get; }
    public EntityId? FatherId { get; }
    public EntityId? PartnerId { get; }
    public PregnancySnapshot? Pregnancy { get; }
    public CellId LastKnownPosition { get; }
    public ResidentDeathCauseId? DeathCause { get; }
    public long? DeathTick { get; }
    public ResidentHeritage Heritage { get; }
}

public sealed class SocialBondSnapshot
{
    public SocialBondSnapshot(
        EntityId firstResidentId,
        EntityId secondResidentId,
        int sympathy,
        int trust,
        long lastInteractionTick)
    {
        FirstResidentId = firstResidentId;
        SecondResidentId = secondResidentId;
        Sympathy = sympathy;
        Trust = trust;
        LastInteractionTick = lastInteractionTick;
    }

    public EntityId FirstResidentId { get; }
    public EntityId SecondResidentId { get; }
    public int Sympathy { get; }
    public int Trust { get; }
    public long LastInteractionTick { get; }
}

public sealed class SocietySnapshot
{
    public SocietySnapshot(
        long version,
        IReadOnlyList<ResidentSocietySnapshot> residents,
        IReadOnlyList<SocialBondSnapshot> bonds)
    {
        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        Version = version;
        Residents = residents ?? throw new ArgumentNullException(nameof(residents));
        Bonds = bonds ?? throw new ArgumentNullException(nameof(bonds));
    }

    public long Version { get; }
    public IReadOnlyList<ResidentSocietySnapshot> Residents { get; }
    public IReadOnlyList<SocialBondSnapshot> Bonds { get; }
}

internal sealed class ResidentSocialState
{
    public ResidentSocialState(
        ResidentRegistration registration,
        ResidentLifeStage lifeStage,
        EntityId? motherId = null,
        EntityId? fatherId = null)
    {
        Id = registration.Id;
        Name = registration.Name;
        Sex = registration.Sex;
        BirthTick = registration.BirthTick;
        LifeStage = lifeStage;
        LastKnownPosition = registration.Position;
        Heritage = registration.Heritage;
        MotherId = motherId;
        FatherId = fatherId;
    }

    public EntityId Id { get; }
    public string Name { get; }
    public ResidentSex Sex { get; }
    public long BirthTick { get; }
    public ResidentLifeStage LifeStage { get; set; }
    public bool IsAlive => LifeStage != ResidentLifeStage.Deceased;
    public EntityId? MotherId { get; }
    public EntityId? FatherId { get; }
    public EntityId? PartnerId { get; set; }
    public PregnancySnapshot? Pregnancy { get; set; }
    public CellId LastKnownPosition { get; set; }
    public ResidentDeathCauseId? DeathCause { get; set; }
    public long? DeathTick { get; set; }
    public ResidentHeritage Heritage { get; }

    public ResidentSocietySnapshot CreateSnapshot()
    {
        return new ResidentSocietySnapshot(
            Id,
            Name,
            Sex,
            BirthTick,
            LifeStage,
            IsAlive,
            MotherId,
            FatherId,
            PartnerId,
            Pregnancy,
            LastKnownPosition,
            DeathCause,
            DeathTick,
            Heritage);
    }
}

internal readonly struct SocialBondKey : IEquatable<SocialBondKey>, IComparable<SocialBondKey>
{
    public SocialBondKey(EntityId left, EntityId right)
    {
        if (left.IsEmpty || right.IsEmpty || left == right)
        {
            throw new ArgumentException("Social bond requires two distinct residents.");
        }

        bool leftFirst = string.Compare(left.ToString(), right.ToString(), StringComparison.Ordinal) < 0;
        First = leftFirst ? left : right;
        Second = leftFirst ? right : left;
    }

    public EntityId First { get; }
    public EntityId Second { get; }

    public int CompareTo(SocialBondKey other)
    {
        int first = string.Compare(First.ToString(), other.First.ToString(), StringComparison.Ordinal);
        return first != 0
            ? first
            : string.Compare(Second.ToString(), other.Second.ToString(), StringComparison.Ordinal);
    }

    public bool Equals(SocialBondKey other)
    {
        return First == other.First && Second == other.Second;
    }

    public override bool Equals(object? obj)
    {
        return obj is SocialBondKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(First, Second);
    }
}

internal sealed class SocialBond
{
    public SocialBond(SocialBondKey key, int sympathy, int trust, long tick)
    {
        Key = key;
        Sympathy = sympathy;
        Trust = trust;
        LastInteractionTick = tick;
    }

    public SocialBondKey Key { get; }
    public int Sympathy { get; set; }
    public int Trust { get; set; }
    public long LastInteractionTick { get; set; }

    public SocialBondSnapshot CreateSnapshot()
    {
        return new SocialBondSnapshot(
            Key.First,
            Key.Second,
            Sympathy,
            Trust,
            LastInteractionTick);
    }
}
}
