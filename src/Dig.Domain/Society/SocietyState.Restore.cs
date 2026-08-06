using System;
using Dig.Domain.Core;

namespace Dig.Domain.Society
{

public sealed partial class SocietyState
{
    public static Result<SocietyState> Restore(
        SocietyPolicy policy,
        SocietySnapshot snapshot)
    {
        if (policy is null) throw new ArgumentNullException(nameof(policy));
        if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));
        try
        {
            SocietyState state = new SocietyState(policy);
            foreach (ResidentSocietySnapshot saved in snapshot.Residents)
            {
                ResidentRegistration registration = new ResidentRegistration(
                    saved.Id,
                    saved.Name,
                    saved.Sex,
                    saved.BirthTick,
                    saved.LastKnownPosition,
                    saved.Heritage);
                ResidentSocialState resident = new ResidentSocialState(
                    registration,
                    saved.LifeStage,
                    saved.MotherId,
                    saved.FatherId)
                {
                    PartnerId = saved.PartnerId,
                    Pregnancy = saved.Pregnancy,
                    DeathCause = saved.DeathCause,
                    DeathTick = saved.DeathTick,
                    PostpartumUntilTick = saved.PostpartumUntilTick,
                };
                state._residents.Add(resident.Id, resident);
            }

            foreach (SocialBondSnapshot saved in snapshot.Bonds)
            {
                SocialBondKey key = new SocialBondKey(
                    saved.FirstResidentId,
                    saved.SecondResidentId);
                state._bonds.Add(key, new SocialBond(
                    key,
                    saved.Sympathy,
                    saved.Trust,
                    saved.LastInteractionTick));
            }

            state.Version = snapshot.Version;
            Result graph = state.ValidateFamilyGraph();
            return graph.IsFailure
                ? Result<SocietyState>.Failure(graph.Error!)
                : Result<SocietyState>.Success(state);
        }
        catch (Exception exception) when (
            exception is ArgumentException || exception is InvalidOperationException)
        {
            return Result<SocietyState>.Failure(SocietyErrors.InvalidFamilyGraph);
        }
    }
}

}
