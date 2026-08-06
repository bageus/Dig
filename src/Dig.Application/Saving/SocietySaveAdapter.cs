using System;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Society;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public static class SocietySaveAdapter
{
    public static SocietySaveData Encode(SocietyState? state)
    {
        if (state == null) return new SocietySaveData();
        SocietyPolicy policy = state.Policy;
        SocietySnapshot snapshot = state.CreateSnapshot();
        SocietySaveData data = new SocietySaveData
        {
            Version = snapshot.Version,
            Policy = new SocietyPolicySaveData
            {
                AdultAgeTicks = policy.AdultAgeTicks,
                OldAgeTicks = policy.OldAgeTicks,
                MaximumAgeTicks = policy.MaximumAgeTicks,
                GestationTicks = policy.GestationTicks,
                CloseKinshipDepth = policy.CloseKinshipDepth,
                MinimumPartnershipSympathy = policy.MinimumPartnershipSympathy,
                MinimumPartnershipTrust = policy.MinimumPartnershipTrust,
                MinimumReproductionMood = policy.MinimumReproductionMood,
                MinimumReproductionHealth = policy.MinimumReproductionHealth,
                PostpartumCooldownTicks = policy.PostpartumCooldownTicks,
            },
        };
        foreach (ResidentSocietySnapshot resident in snapshot.Residents)
        {
            data.Residents.Add(new SocietyResidentSaveData
            {
                Id = resident.Id.ToString(),
                Name = resident.Name,
                Sex = (int)resident.Sex,
                BirthTick = resident.BirthTick,
                LifeStage = (int)resident.LifeStage,
                MotherId = resident.MotherId?.ToString(),
                FatherId = resident.FatherId?.ToString(),
                PartnerId = resident.PartnerId?.ToString(),
                PregnancyFatherId = resident.Pregnancy?.FatherId.ToString(),
                PregnancyConceptionTick = resident.Pregnancy?.ConceptionTick ?? 0,
                PregnancyDueTick = resident.Pregnancy?.DueTick ?? 0,
                X = resident.LastKnownPosition.X,
                Y = resident.LastKnownPosition.Y,
                Z = resident.LastKnownPosition.Z,
                DeathCause = resident.DeathCause?.ToString(),
                DeathTick = resident.DeathTick,
                Potential = resident.Heritage.Potential,
                Traits = resident.Heritage.Traits.Select(value => value.ToString()).ToList(),
                PostpartumUntilTick = resident.PostpartumUntilTick,
            });
        }

        foreach (SocialBondSnapshot bond in snapshot.Bonds)
        {
            data.Bonds.Add(new SocialBondSaveData
            {
                FirstResidentId = bond.FirstResidentId.ToString(),
                SecondResidentId = bond.SecondResidentId.ToString(),
                Sympathy = bond.Sympathy,
                Trust = bond.Trust,
                LastInteractionTick = bond.LastInteractionTick,
            });
        }

        return data;
    }

    public static Result<SocietyState?> Decode(SocietySaveData? data)
    {
        if (data?.Policy == null) return Result<SocietyState?>.Success(null);
        try
        {
            SocietyPolicySaveData savedPolicy = data.Policy;
            SocietyPolicy policy = new SocietyPolicy(
                savedPolicy.AdultAgeTicks,
                savedPolicy.OldAgeTicks,
                savedPolicy.MaximumAgeTicks,
                savedPolicy.GestationTicks,
                savedPolicy.CloseKinshipDepth,
                savedPolicy.MinimumPartnershipSympathy,
                savedPolicy.MinimumPartnershipTrust,
                savedPolicy.MinimumReproductionMood,
                savedPolicy.MinimumReproductionHealth,
                savedPolicy.PostpartumCooldownTicks);
            ResidentSocietySnapshot[] residents = data.Residents.Select(DecodeResident).ToArray();
            SocialBondSnapshot[] bonds = data.Bonds.Select(value => new SocialBondSnapshot(
                EntityId.Parse(value.FirstResidentId),
                EntityId.Parse(value.SecondResidentId),
                value.Sympathy,
                value.Trust,
                value.LastInteractionTick)).ToArray();
            Result<SocietyState> restored = SocietyState.Restore(
                policy,
                new SocietySnapshot(data.Version, residents, bonds));
            return restored.IsFailure
                ? Result<SocietyState?>.Failure(restored.Error!)
                : Result<SocietyState?>.Success(restored.Value);
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is FormatException
            || exception is OverflowException)
        {
            return Result<SocietyState?>.Failure(SaveErrors.InvalidDocument);
        }
    }

    private static ResidentSocietySnapshot DecodeResident(SocietyResidentSaveData value)
    {
        PregnancySnapshot? pregnancy = string.IsNullOrWhiteSpace(value.PregnancyFatherId)
            ? null
            : new PregnancySnapshot(
                EntityId.Parse(value.PregnancyFatherId),
                value.PregnancyConceptionTick,
                value.PregnancyDueTick);
        ResidentDeathCauseId? deathCause = string.IsNullOrWhiteSpace(value.DeathCause)
            ? null
            : new ResidentDeathCauseId(value.DeathCause);
        return new ResidentSocietySnapshot(
            EntityId.Parse(value.Id),
            value.Name,
            (ResidentSex)value.Sex,
            value.BirthTick,
            (ResidentLifeStage)value.LifeStage,
            (ResidentLifeStage)value.LifeStage != ResidentLifeStage.Deceased,
            ParseOptional(value.MotherId),
            ParseOptional(value.FatherId),
            ParseOptional(value.PartnerId),
            pregnancy,
            new CellId(value.X, value.Y, value.Z),
            deathCause,
            value.DeathTick,
            new ResidentHeritage(
                value.Potential,
                value.Traits.Select(item => new AgentTraitId(item))),
            value.PostpartumUntilTick);
    }

    private static EntityId? ParseOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : EntityId.Parse(value);
}

}
