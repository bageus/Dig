using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Core;

namespace Dig.Domain.Society
{

public enum ResidentSex
{
    Female = 0,
    Male = 1,
}

public enum ResidentLifeStage
{
    Child = 0,
    Adult = 1,
    Old = 2,
    Deceased = 3,
}

public enum ReproductionBlockReason
{
    UnknownResident = 0,
    ResidentDead = 1,
    SexMismatch = 2,
    NotAdult = 3,
    NotPartners = 4,
    CloseRelatives = 5,
    AlreadyPregnant = 6,
    MoodTooLow = 7,
    HealthTooLow = 8,
    Infertile = 9,
    NoBirthPlace = 10,
}

public readonly struct ResidentDeathCauseId : IEquatable<ResidentDeathCauseId>, IComparable<ResidentDeathCauseId>
{
    private readonly string? _value;

    public ResidentDeathCauseId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Death cause id is required.", nameof(value));
        }

        _value = value.Trim();
    }

    public bool IsEmpty => string.IsNullOrEmpty(_value);

    public int CompareTo(ResidentDeathCauseId other)
    {
        return string.Compare(_value, other._value, StringComparison.Ordinal);
    }

    public bool Equals(ResidentDeathCauseId other)
    {
        return string.Equals(_value, other._value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is ResidentDeathCauseId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(_value ?? string.Empty);
    }

    public override string ToString()
    {
        return _value ?? string.Empty;
    }

    public static bool operator ==(ResidentDeathCauseId left, ResidentDeathCauseId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ResidentDeathCauseId left, ResidentDeathCauseId right)
    {
        return !left.Equals(right);
    }
}

public sealed class SocietyPolicy
{
    public SocietyPolicy(
        long adultAgeTicks,
        long oldAgeTicks,
        long maximumAgeTicks,
        long gestationTicks,
        int closeKinshipDepth,
        int minimumPartnershipSympathy,
        int minimumPartnershipTrust,
        int minimumReproductionMood,
        int minimumReproductionHealth)
    {
        if (adultAgeTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(adultAgeTicks));
        }

        if (oldAgeTicks <= adultAgeTicks)
        {
            throw new ArgumentOutOfRangeException(nameof(oldAgeTicks));
        }

        if (maximumAgeTicks <= oldAgeTicks)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAgeTicks));
        }

        if (gestationTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(gestationTicks));
        }

        if (closeKinshipDepth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(closeKinshipDepth));
        }

        ValidateNeedValue(minimumPartnershipSympathy, nameof(minimumPartnershipSympathy));
        ValidateNeedValue(minimumPartnershipTrust, nameof(minimumPartnershipTrust));
        ValidateNeedValue(minimumReproductionMood, nameof(minimumReproductionMood));
        ValidateNeedValue(minimumReproductionHealth, nameof(minimumReproductionHealth));

        AdultAgeTicks = adultAgeTicks;
        OldAgeTicks = oldAgeTicks;
        MaximumAgeTicks = maximumAgeTicks;
        GestationTicks = gestationTicks;
        CloseKinshipDepth = closeKinshipDepth;
        MinimumPartnershipSympathy = minimumPartnershipSympathy;
        MinimumPartnershipTrust = minimumPartnershipTrust;
        MinimumReproductionMood = minimumReproductionMood;
        MinimumReproductionHealth = minimumReproductionHealth;
    }

    public long AdultAgeTicks { get; }
    public long OldAgeTicks { get; }
    public long MaximumAgeTicks { get; }
    public long GestationTicks { get; }
    public int CloseKinshipDepth { get; }
    public int MinimumPartnershipSympathy { get; }
    public int MinimumPartnershipTrust { get; }
    public int MinimumReproductionMood { get; }
    public int MinimumReproductionHealth { get; }

    public ResidentLifeStage ResolveStage(long birthTick, long tick)
    {
        if (birthTick < 0 || tick < birthTick)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        long age = tick - birthTick;
        if (age >= MaximumAgeTicks)
        {
            return ResidentLifeStage.Deceased;
        }

        if (age >= OldAgeTicks)
        {
            return ResidentLifeStage.Old;
        }

        return age >= AdultAgeTicks
            ? ResidentLifeStage.Adult
            : ResidentLifeStage.Child;
    }

    private static void ValidateNeedValue(int value, string parameterName)
    {
        if (value < NeedValue.Minimum || value > NeedValue.Maximum)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}

public sealed class ResidentNameCatalog
{
    private readonly IReadOnlyList<string> _femaleNames;
    private readonly IReadOnlyList<string> _maleNames;

    public ResidentNameCatalog(
        IEnumerable<string> femaleNames,
        IEnumerable<string> maleNames)
    {
        _femaleNames = Normalize(femaleNames, nameof(femaleNames));
        _maleNames = Normalize(maleNames, nameof(maleNames));
    }

    public IReadOnlyList<string> GetNames(ResidentSex sex)
    {
        return sex == ResidentSex.Female ? _femaleNames : _maleNames;
    }

    public string Select(ResidentSex sex, int index)
    {
        IReadOnlyList<string> names = GetNames(sex);
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return names[index % names.Count];
    }

    private static IReadOnlyList<string> Normalize(
        IEnumerable<string> names,
        string parameterName)
    {
        if (names is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        string[] normalized = names.Select(name =>
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Resident names cannot be empty.", parameterName);
            }

            return name.Trim();
        }).ToArray();

        if (normalized.Length == 0)
        {
            throw new ArgumentException("At least one resident name is required.", parameterName);
        }

        if (normalized.Distinct(StringComparer.Ordinal).Count() != normalized.Length)
        {
            throw new ArgumentException("Resident names must be unique.", parameterName);
        }

        return new ReadOnlyCollection<string>(normalized);
    }
}

public sealed class ResidentReproductionContext
{
    public ResidentReproductionContext(
        int motherMood,
        int fatherMood,
        int motherHealth,
        int fatherHealth,
        int fertilityModifier,
        bool hasBirthPlace)
    {
        ValidateNeedValue(motherMood, nameof(motherMood));
        ValidateNeedValue(fatherMood, nameof(fatherMood));
        ValidateNeedValue(motherHealth, nameof(motherHealth));
        ValidateNeedValue(fatherHealth, nameof(fatherHealth));
        ValidateNeedValue(fertilityModifier, nameof(fertilityModifier));

        MotherMood = motherMood;
        FatherMood = fatherMood;
        MotherHealth = motherHealth;
        FatherHealth = fatherHealth;
        FertilityModifier = fertilityModifier;
        HasBirthPlace = hasBirthPlace;
    }

    public int MotherMood { get; }
    public int FatherMood { get; }
    public int MotherHealth { get; }
    public int FatherHealth { get; }
    public int FertilityModifier { get; }
    public bool HasBirthPlace { get; }

    private static void ValidateNeedValue(int value, string parameterName)
    {
        if (value < NeedValue.Minimum || value > NeedValue.Maximum)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}

public sealed class ReproductionEvaluation
{
    public ReproductionEvaluation(IEnumerable<ReproductionBlockReason> reasons)
    {
        if (reasons is null)
        {
            throw new ArgumentNullException(nameof(reasons));
        }

        ReproductionBlockReason[] ordered = reasons.Distinct().OrderBy(reason => reason).ToArray();
        Reasons = new ReadOnlyCollection<ReproductionBlockReason>(ordered);
    }

    public bool CanReproduce => Reasons.Count == 0;
    public IReadOnlyList<ReproductionBlockReason> Reasons { get; }
    public ReproductionBlockReason? PrimaryReason => Reasons.Count == 0 ? null : Reasons[0];
}

public sealed class ResidentHeritage
{
    public ResidentHeritage(int potential, IEnumerable<AgentTraitId>? traits = null)
    {
        if (potential < NeedValue.Minimum || potential > NeedValue.Maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(potential));
        }

        AgentTraitId[] orderedTraits = (traits ?? Array.Empty<AgentTraitId>())
            .OrderBy(trait => trait)
            .ToArray();
        if (orderedTraits.Any(trait => trait.IsEmpty))
        {
            throw new ArgumentException("Trait ids cannot be empty.", nameof(traits));
        }

        if (orderedTraits.Distinct().Count() != orderedTraits.Length)
        {
            throw new ArgumentException("Trait ids must be unique.", nameof(traits));
        }

        Potential = potential;
        Traits = new ReadOnlyCollection<AgentTraitId>(orderedTraits);
    }

    public int Potential { get; }
    public IReadOnlyList<AgentTraitId> Traits { get; }
}
}
