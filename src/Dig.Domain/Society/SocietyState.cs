using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Society
{

public sealed partial class SocietyState : AggregateRoot
{
    private readonly Dictionary<EntityId, ResidentSocialState> _residents;
    private readonly Dictionary<SocialBondKey, SocialBond> _bonds;

    public SocietyState(SocietyPolicy policy)
    {
        Policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _residents = new Dictionary<EntityId, ResidentSocialState>();
        _bonds = new Dictionary<SocialBondKey, SocialBond>();
    }

    public SocietyPolicy Policy { get; }
    public long Version { get; private set; }

    public Result RegisterFounder(ResidentRegistration registration, long tick)
    {
        if (registration is null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        if (tick < registration.BirthTick)
        {
            return Result.Failure(SocietyErrors.InvalidTick);
        }

        if (_residents.ContainsKey(registration.Id))
        {
            return Result.Failure(SocietyErrors.DuplicateResident);
        }

        ResidentLifeStage stage = Policy.ResolveStage(registration.BirthTick, tick);
        if (stage == ResidentLifeStage.Deceased)
        {
            return Result.Failure(SocietyErrors.ResidentDead);
        }

        _residents.Add(
            registration.Id,
            new ResidentSocialState(registration, stage));
        Version = checked(Version + 1);
        return Result.Success();
    }

    public Result UpdateLastKnownPosition(EntityId residentId, CellId position)
    {
        if (!TryGetResident(residentId, out ResidentSocialState? resident))
        {
            return Result.Failure(SocietyErrors.UnknownResident);
        }

        if (resident.LastKnownPosition == position)
        {
            return Result.Success();
        }

        resident.LastKnownPosition = position;
        Version = checked(Version + 1);
        return Result.Success();
    }

    public Result SetSocialBond(
        EntityId firstResidentId,
        EntityId secondResidentId,
        int sympathy,
        int trust,
        long tick)
    {
        ValidateSocialValue(sympathy, nameof(sympathy));
        ValidateSocialValue(trust, nameof(trust));
        ValidateTick(tick);
        if (!TryGetLivingResident(firstResidentId, out _)
            || !TryGetLivingResident(secondResidentId, out _))
        {
            return Result.Failure(SocietyErrors.UnknownResident);
        }

        SocialBondKey key = new SocialBondKey(firstResidentId, secondResidentId);
        if (_bonds.TryGetValue(key, out SocialBond? bond))
        {
            if (tick < bond.LastInteractionTick)
            {
                return Result.Failure(SocietyErrors.InvalidTick);
            }

            bond.Sympathy = sympathy;
            bond.Trust = trust;
            bond.LastInteractionTick = tick;
        }
        else
        {
            _bonds.Add(key, new SocialBond(key, sympathy, trust, tick));
        }

        Version = checked(Version + 1);
        return Result.Success();
    }

    public SocietySnapshot CreateSnapshot()
    {
        ResidentSocietySnapshot[] residents = _residents.Values
            .OrderBy(resident => resident.Id.ToString(), StringComparer.Ordinal)
            .Select(resident => resident.CreateSnapshot())
            .ToArray();
        SocialBondSnapshot[] bonds = _bonds.Values
            .OrderBy(bond => bond.Key)
            .Select(bond => bond.CreateSnapshot())
            .ToArray();
        return new SocietySnapshot(
            Version,
            new ReadOnlyCollection<ResidentSocietySnapshot>(residents),
            new ReadOnlyCollection<SocialBondSnapshot>(bonds));
    }

    public ResidentSocietySnapshot? GetResident(EntityId residentId)
    {
        return _residents.TryGetValue(residentId, out ResidentSocialState? resident)
            ? resident.CreateSnapshot()
            : null;
    }

    public Result ValidateFamilyGraph()
    {
        foreach (ResidentSocialState resident in _residents.Values)
        {
            HashSet<EntityId> visiting = new HashSet<EntityId>();
            if (HasAncestorCycle(resident.Id, visiting))
            {
                return Result.Failure(SocietyErrors.FamilyCycle);
            }
        }

        return Result.Success();
    }

    internal bool TryGetResident(
        EntityId residentId,
        out ResidentSocialState? resident)
    {
        return _residents.TryGetValue(residentId, out resident);
    }

    internal bool TryGetLivingResident(
        EntityId residentId,
        out ResidentSocialState? resident)
    {
        return _residents.TryGetValue(residentId, out resident)
            && resident.IsAlive;
    }

    private bool HasAncestorCycle(EntityId residentId, HashSet<EntityId> visiting)
    {
        if (!visiting.Add(residentId))
        {
            return true;
        }

        if (_residents.TryGetValue(residentId, out ResidentSocialState? resident))
        {
            if (resident.MotherId.HasValue
                && HasAncestorCycle(resident.MotherId.Value, visiting))
            {
                return true;
            }

            if (resident.FatherId.HasValue
                && HasAncestorCycle(resident.FatherId.Value, visiting))
            {
                return true;
            }
        }

        visiting.Remove(residentId);
        return false;
    }

    private static void ValidateSocialValue(int value, string parameterName)
    {
        if (value < 0 || value > 10_000)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void ValidateTick(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }
    }
}
}
