using System;
using Dig.Domain.Core;
using Dig.Domain.Society;

namespace Dig.Unity
{

internal sealed partial class DigAgentSession
{
    private const long TicksPerYear = 24 * 365;
    private const long DemoSocietyStartTick = TicksPerYear * 25;

    internal long SocietyTick => DemoSocietyStartTick + _tick;

    internal SocietySnapshot LoadSocietySnapshot()
    {
        return _society.CreateSnapshot();
    }

    private static SocietyState CreateDemoSociety()
    {
        return new SocietyState(new SocietyPolicy(
            adultAgeTicks: TicksPerYear * 18,
            oldAgeTicks: TicksPerYear * 60,
            maximumAgeTicks: TicksPerYear * 100,
            gestationTicks: 24,
            closeKinshipDepth: 3,
            minimumPartnershipSympathy: 6_000,
            minimumPartnershipTrust: 6_000,
            minimumReproductionMood: 7_600,
            minimumReproductionHealth: 5_000));
    }

    private static void RegisterDemoResident(
        SocietyState society,
        ResidentBirthPlan identity)
    {
        Result result = society.RegisterFounder(
            new ResidentRegistration(
                identity.Id,
                identity.Name,
                identity.Sex,
                birthTick: 0,
                identity.Position,
                identity.Heritage),
            tick: DemoSocietyStartTick);
        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error!.ToString());
        }
    }

}

}
