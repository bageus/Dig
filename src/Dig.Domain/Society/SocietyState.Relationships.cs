using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Society
{

public sealed partial class SocietyState
{
    public Result FormPartnership(
        EntityId firstResidentId,
        EntityId secondResidentId,
        long tick)
    {
        ValidateTick(tick);
        if (!TryGetLivingResident(firstResidentId, out ResidentSocialState first)
            || !TryGetLivingResident(secondResidentId, out ResidentSocialState second))
        {
            return Result.Failure(SocietyErrors.UnknownResident);
        }

        if (first.Id == second.Id
            || first.LifeStage != ResidentLifeStage.Adult
            || second.LifeStage != ResidentLifeStage.Adult)
        {
            return Result.Failure(SocietyErrors.PartnershipBlocked);
        }

        if (first.PartnerId.HasValue || second.PartnerId.HasValue)
        {
            return Result.Failure(SocietyErrors.AlreadyPartnered);
        }

        if (AreCloseRelatives(first.Id, second.Id))
        {
            return Result.Failure(SocietyErrors.CloseRelatives);
        }

        SocialBondKey key = new SocialBondKey(first.Id, second.Id);
        if (!_bonds.TryGetValue(key, out SocialBond bond)
            || bond.Sympathy < Policy.MinimumPartnershipSympathy
            || bond.Trust < Policy.MinimumPartnershipTrust)
        {
            return Result.Failure(SocietyErrors.PartnershipBlocked);
        }

        first.PartnerId = second.Id;
        second.PartnerId = first.Id;
        Version = checked(Version + 1);
        Raise(new ResidentPartnershipChanged(tick, first.Id, second.Id, partnered: true));
        return Result.Success();
    }

    public Result EndPartnership(EntityId residentId, long tick)
    {
        ValidateTick(tick);
        if (!TryGetResident(residentId, out ResidentSocialState resident))
        {
            return Result.Failure(SocietyErrors.UnknownResident);
        }

        if (!resident.PartnerId.HasValue)
        {
            return Result.Success();
        }

        EntityId partnerId = resident.PartnerId.Value;
        resident.PartnerId = null;
        if (_residents.TryGetValue(partnerId, out ResidentSocialState partner)
            && partner.PartnerId == resident.Id)
        {
            partner.PartnerId = null;
        }

        Version = checked(Version + 1);
        Raise(new ResidentPartnershipChanged(tick, resident.Id, partnerId, partnered: false));
        return Result.Success();
    }

    public bool AreCloseRelatives(EntityId firstResidentId, EntityId secondResidentId)
    {
        if (firstResidentId == secondResidentId)
        {
            return true;
        }

        if (!_residents.ContainsKey(firstResidentId)
            || !_residents.ContainsKey(secondResidentId))
        {
            return false;
        }

        Dictionary<EntityId, int> firstAncestors = GetAncestors(
            firstResidentId,
            Policy.CloseKinshipDepth);
        Dictionary<EntityId, int> secondAncestors = GetAncestors(
            secondResidentId,
            Policy.CloseKinshipDepth);
        if (firstAncestors.ContainsKey(secondResidentId)
            || secondAncestors.ContainsKey(firstResidentId))
        {
            return true;
        }

        return firstAncestors.Keys.Any(secondAncestors.ContainsKey);
    }

    public ReproductionEvaluation EvaluateReproduction(
        EntityId motherId,
        EntityId fatherId,
        ResidentReproductionContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        List<ReproductionBlockReason> reasons = new List<ReproductionBlockReason>();
        if (!_residents.TryGetValue(motherId, out ResidentSocialState mother)
            || !_residents.TryGetValue(fatherId, out ResidentSocialState father))
        {
            reasons.Add(ReproductionBlockReason.UnknownResident);
            return new ReproductionEvaluation(reasons);
        }

        if (!mother.IsAlive || !father.IsAlive)
        {
            reasons.Add(ReproductionBlockReason.ResidentDead);
        }

        if (mother.Sex != ResidentSex.Female || father.Sex != ResidentSex.Male)
        {
            reasons.Add(ReproductionBlockReason.SexMismatch);
        }

        if (mother.LifeStage != ResidentLifeStage.Adult
            || father.LifeStage != ResidentLifeStage.Adult)
        {
            reasons.Add(ReproductionBlockReason.NotAdult);
        }

        if (mother.PartnerId != father.Id || father.PartnerId != mother.Id)
        {
            reasons.Add(ReproductionBlockReason.NotPartners);
        }

        if (AreCloseRelatives(mother.Id, father.Id))
        {
            reasons.Add(ReproductionBlockReason.CloseRelatives);
        }

        if (mother.Pregnancy is not null)
        {
            reasons.Add(ReproductionBlockReason.AlreadyPregnant);
        }

        if (context.MotherMood < Policy.MinimumReproductionMood
            || context.FatherMood < Policy.MinimumReproductionMood)
        {
            reasons.Add(ReproductionBlockReason.MoodTooLow);
        }

        if (context.MotherHealth < Policy.MinimumReproductionHealth
            || context.FatherHealth < Policy.MinimumReproductionHealth)
        {
            reasons.Add(ReproductionBlockReason.HealthTooLow);
        }

        if (context.FertilityModifier <= 0)
        {
            reasons.Add(ReproductionBlockReason.Infertile);
        }

        if (!context.HasBirthPlace)
        {
            reasons.Add(ReproductionBlockReason.NoBirthPlace);
        }

        return new ReproductionEvaluation(reasons);
    }

    public Result StartPregnancy(
        EntityId motherId,
        EntityId fatherId,
        ResidentReproductionContext context,
        long tick)
    {
        ValidateTick(tick);
        ReproductionEvaluation evaluation = EvaluateReproduction(
            motherId,
            fatherId,
            context);
        if (!evaluation.CanReproduce)
        {
            return Result.Failure(SocietyErrors.PregnancyBlocked);
        }

        ResidentSocialState mother = _residents[motherId];
        long dueTick = checked(tick + Policy.GestationTicks);
        mother.Pregnancy = new PregnancySnapshot(fatherId, tick, dueTick);
        Version = checked(Version + 1);
        Raise(new ResidentPregnancyStarted(tick, motherId, fatherId, dueTick));
        return Result.Success();
    }

    private Dictionary<EntityId, int> GetAncestors(EntityId residentId, int maximumDepth)
    {
        Dictionary<EntityId, int> ancestors = new Dictionary<EntityId, int>();
        Queue<KeyValuePair<EntityId, int>> pending =
            new Queue<KeyValuePair<EntityId, int>>();
        pending.Enqueue(new KeyValuePair<EntityId, int>(residentId, 0));
        while (pending.Count > 0)
        {
            KeyValuePair<EntityId, int> current = pending.Dequeue();
            if (current.Value >= maximumDepth
                || !_residents.TryGetValue(current.Key, out ResidentSocialState resident))
            {
                continue;
            }

            EnqueueParent(resident.MotherId, current.Value + 1, ancestors, pending);
            EnqueueParent(resident.FatherId, current.Value + 1, ancestors, pending);
        }

        return ancestors;
    }

    private static void EnqueueParent(
        EntityId? parentId,
        int depth,
        Dictionary<EntityId, int> ancestors,
        Queue<KeyValuePair<EntityId, int>> pending)
    {
        if (!parentId.HasValue)
        {
            return;
        }

        if (!ancestors.TryGetValue(parentId.Value, out int existingDepth)
            || depth < existingDepth)
        {
            ancestors[parentId.Value] = depth;
            pending.Enqueue(new KeyValuePair<EntityId, int>(parentId.Value, depth));
        }
    }
}
}
