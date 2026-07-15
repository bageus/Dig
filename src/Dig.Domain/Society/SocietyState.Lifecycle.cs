using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Society
{

public sealed partial class SocietyState
{
    private static readonly ResidentDeathCauseId OldAgeCause =
        new ResidentDeathCauseId("old_age");

    public IReadOnlyList<ResidentSocietySnapshot> GetDuePregnancies(long tick)
    {
        ValidateTick(tick);
        ResidentSocietySnapshot[] due = _residents.Values
            .Where(resident => resident.IsAlive
                && resident.Pregnancy is not null
                && resident.Pregnancy.DueTick <= tick)
            .OrderBy(resident => resident.Id.ToString(), StringComparer.Ordinal)
            .Select(resident => resident.CreateSnapshot())
            .ToArray();
        return new ReadOnlyCollection<ResidentSocietySnapshot>(due);
    }

    public Result RegisterBirth(
        EntityId motherId,
        ResidentBirthPlan birth,
        long tick)
    {
        if (birth is null)
        {
            throw new ArgumentNullException(nameof(birth));
        }

        ValidateTick(tick);
        if (!_residents.TryGetValue(motherId, out ResidentSocialState? mother)
            || !mother.IsAlive)
        {
            return Result.Failure(SocietyErrors.UnknownResident);
        }

        if (mother.Pregnancy is null)
        {
            return Result.Failure(SocietyErrors.PregnancyMissing);
        }

        PregnancySnapshot pregnancy = mother.Pregnancy;
        if (tick < pregnancy.DueTick)
        {
            return Result.Failure(SocietyErrors.PregnancyNotDue);
        }

        if (!_residents.TryGetValue(pregnancy.FatherId, out ResidentSocialState? father)
            || father.Id == mother.Id
            || mother.Sex != ResidentSex.Female
            || father.Sex != ResidentSex.Male)
        {
            return Result.Failure(SocietyErrors.InvalidParents);
        }

        if (_residents.ContainsKey(birth.Id))
        {
            return Result.Failure(SocietyErrors.DuplicateResident);
        }

        ResidentRegistration registration = new ResidentRegistration(
            birth.Id,
            birth.Name,
            birth.Sex,
            tick,
            birth.Position,
            birth.Heritage);
        ResidentSocialState child = new ResidentSocialState(
            registration,
            ResidentLifeStage.Child,
            mother.Id,
            father.Id);
        _residents.Add(child.Id, child);
        mother.Pregnancy = null;
        Version = checked(Version + 1);

        Result graphValidation = ValidateFamilyGraph();
        if (graphValidation.IsFailure)
        {
            _residents.Remove(child.Id);
            mother.Pregnancy = pregnancy;
            Version = checked(Version + 1);
            return graphValidation;
        }

        Raise(new ResidentBorn(
            tick,
            child.Id,
            child.Name,
            child.Sex,
            mother.Id,
            father.Id,
            child.LastKnownPosition));
        return Result.Success();
    }

    public Result AdvanceLifecycle(long tick)
    {
        ValidateTick(tick);
        ResidentSocialState[] residents = _residents.Values
            .OrderBy(resident => resident.Id.ToString(), StringComparer.Ordinal)
            .ToArray();
        foreach (ResidentSocialState resident in residents)
        {
            if (!resident.IsAlive)
            {
                continue;
            }

            if (tick < resident.BirthTick)
            {
                return Result.Failure(SocietyErrors.InvalidTick);
            }

            ResidentLifeStage resolved = Policy.ResolveStage(resident.BirthTick, tick);
            if (resolved == ResidentLifeStage.Deceased)
            {
                RecordDeath(resident.Id, OldAgeCause, resident.LastKnownPosition, tick);
                continue;
            }

            if (resolved == resident.LifeStage)
            {
                continue;
            }

            ResidentLifeStage previous = resident.LifeStage;
            resident.LifeStage = resolved;
            Version = checked(Version + 1);
            Raise(new ResidentLifeStageChanged(
                tick,
                resident.Id,
                previous,
                resolved));
        }

        return Result.Success();
    }

    public Result RecordDeath(
        EntityId residentId,
        ResidentDeathCauseId cause,
        CellId lastKnownPosition,
        long tick)
    {
        ValidateTick(tick);
        if (cause.IsEmpty)
        {
            throw new ArgumentException("Death cause cannot be empty.", nameof(cause));
        }

        if (!_residents.TryGetValue(residentId, out ResidentSocialState? resident))
        {
            return Result.Failure(SocietyErrors.UnknownResident);
        }

        if (!resident.IsAlive)
        {
            return Result.Success();
        }

        ResidentLifeStage previous = resident.LifeStage;
        EntityId? partnerId = resident.PartnerId;
        resident.LifeStage = ResidentLifeStage.Deceased;
        resident.LastKnownPosition = lastKnownPosition;
        resident.DeathCause = cause;
        resident.DeathTick = tick;
        resident.Pregnancy = null;
        resident.PartnerId = null;
        if (partnerId.HasValue
            && _residents.TryGetValue(partnerId.Value, out ResidentSocialState? partner)
            && partner.PartnerId == resident.Id)
        {
            partner.PartnerId = null;
            Raise(new ResidentPartnershipChanged(
                tick,
                resident.Id,
                partner.Id,
                partnered: false));
        }

        Version = checked(Version + 1);
        Raise(new ResidentLifeStageChanged(
            tick,
            resident.Id,
            previous,
            ResidentLifeStage.Deceased));
        Raise(new ResidentDied(
            tick,
            resident.Id,
            cause,
            lastKnownPosition));
        return Result.Success();
    }
}
}
