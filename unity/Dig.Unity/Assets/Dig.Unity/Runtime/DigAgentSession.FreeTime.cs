using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Society;
using Dig.Domain.Runtime;

namespace Dig.Unity
{

internal sealed partial class DigAgentSession
{
    private const int FreeTimeNeedFloor = 7_500;
    private const int FreeTimeMoodIntervalTicks = 25;
    private const int SoloPlayMoodGain = 30;
    private const int GroupPlayMoodGain = 50;
    private const int SocialMoodGain = 60;
    private readonly Dictionary<EntityId, EntityId> _freeTimeMeetingPartners =
        new Dictionary<EntityId, EntityId>();
    private readonly Dictionary<EntityId, long> _postpartumUntilTick =
        new Dictionary<EntityId, long>();

    internal bool TryGetFreeTimeMeetingPartner(
        EntityId residentId,
        out EntityId partnerId)
    {
        return _freeTimeMeetingPartners.TryGetValue(residentId, out partnerId);
    }

    private void AdvanceResidentFreeTime()
    {
        RequireSociety(_society.AdvanceLifecycle(SocietyTick));
        CompleteDueResidentBirths();
        AgentState[] residents = _repository.GetAll()
            .Where(agent => _residentSexes.ContainsKey(agent.Id) && agent.IsAlive)
            .OrderBy(agent => agent.Id.ToString(), StringComparer.Ordinal)
            .ToArray();
        foreach (AgentState resident in residents)
        {
            RequireSociety(_society.UpdateLastKnownPosition(
                resident.Id,
                resident.Position));
        }

        _freeTimeMeetingPartners.Clear();
        List<AgentState> available = residents
            .Where(IsAvailableForFreeTime)
            .ToList();
        while (available.Count >= 2)
        {
            AgentState first = available[0];
            available.RemoveAt(0);
            AgentState? second = SelectFreeTimePartner(first, available);
            if (second == null)
            {
                ApplyLeisureMood(first, SoloPlayMoodGain, "free_time_solo_play");
                continue;
            }

            available.Remove(second);
            _freeTimeMeetingPartners[first.Id] = second.Id;
            _freeTimeMeetingPartners[second.Id] = first.Id;
            if (!AreStandingTogether(first.Position, second.Position))
            {
                continue;
            }

            bool groupPlay = (_tick / FreeTimeMoodIntervalTicks) % 2 == 0;
            int moodGain = groupPlay ? GroupPlayMoodGain : SocialMoodGain;
            string source = groupPlay
                ? "free_time_group_play"
                : "free_time_social";
            ApplyLeisureMood(first, moodGain, source);
            ApplyLeisureMood(second, moodGain, source);
            AdvanceRelationshipAndReproduction(first, second);
        }

        foreach (AgentState resident in available)
        {
            ApplyLeisureMood(resident, SoloPlayMoodGain, "free_time_solo_play");
        }
    }

    private bool IsAvailableForFreeTime(AgentState resident)
    {
        AgentSnapshot snapshot = resident.CreateSnapshot(_tick);
        ResidentSocietySnapshot? social = _society.CreateSnapshot().Residents
            .FirstOrDefault(value => value.Id == resident.Id);
        if (snapshot.ScheduledActivity == ScheduleActivity.Work
            || social?.LifeStage != ResidentLifeStage.Adult
            || snapshot.Needs.Nutrition.Points < FreeTimeNeedFloor
            || snapshot.Needs.Alertness.Points < FreeTimeNeedFloor
            || snapshot.ActiveAction?.IntentKind is AgentIntentKind.Eat
                or AgentIntentKind.Sleep
                or AgentIntentKind.PlayerOrder
                or AgentIntentKind.Work
            || _manualTunnelMovements.ContainsKey(resident.Id)
            || (_hasActiveTerrainDirectCommand?.Invoke(resident.Id) ?? false))
        {
            return false;
        }

        return true;
    }

    private AgentState? SelectFreeTimePartner(
        AgentState resident,
        IReadOnlyList<AgentState> candidates)
    {
        SocietySnapshot society = _society.CreateSnapshot();
        ResidentSocietySnapshot? social = society.Residents
            .FirstOrDefault(value => value.Id == resident.Id);
        if (social?.PartnerId is EntityId partnerId)
        {
            AgentState? partner = candidates.FirstOrDefault(value => value.Id == partnerId);
            if (partner != null)
            {
                return partner;
            }
        }

        return candidates
            .OrderBy(value => ManhattanDistance(resident.Position, value.Position))
            .ThenBy(value => value.Id.ToString(), StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private void AdvanceRelationshipAndReproduction(
        AgentState first,
        AgentState second)
    {
        SocietySnapshot snapshot = _society.CreateSnapshot();
        SocialBondSnapshot? existing = snapshot.Bonds.FirstOrDefault(bond =>
            (bond.FirstResidentId == first.Id && bond.SecondResidentId == second.Id)
            || (bond.FirstResidentId == second.Id && bond.SecondResidentId == first.Id));
        int sympathy = Math.Min(10_000, (existing?.Sympathy ?? 5_500) + 250);
        int trust = Math.Min(10_000, (existing?.Trust ?? 5_500) + 250);
        RequireSociety(_society.SetSocialBond(
            first.Id,
            second.Id,
            sympathy,
            trust,
            SocietyTick));

        snapshot = _society.CreateSnapshot();
        ResidentSocietySnapshot firstSocial = snapshot.Residents
            .First(value => value.Id == first.Id);
        ResidentSocietySnapshot secondSocial = snapshot.Residents
            .First(value => value.Id == second.Id);
        if (!firstSocial.PartnerId.HasValue
            && !secondSocial.PartnerId.HasValue
            && _residentSexes[first.Id] != _residentSexes[second.Id])
        {
            _society.FormPartnership(first.Id, second.Id, SocietyTick);
            snapshot = _society.CreateSnapshot();
            firstSocial = snapshot.Residents.First(value => value.Id == first.Id);
            secondSocial = snapshot.Residents.First(value => value.Id == second.Id);
        }

        if (firstSocial.PartnerId != second.Id || secondSocial.PartnerId != first.Id)
        {
            return;
        }

        AgentState mother = _residentSexes[first.Id] == ResidentSex.Female ? first : second;
        AgentState father = mother == first ? second : first;
        if (_postpartumUntilTick.TryGetValue(mother.Id, out long cooldown)
            && SocietyTick < cooldown)
        {
            return;
        }

        AgentSnapshot motherState = mother.CreateSnapshot(_tick);
        AgentSnapshot fatherState = father.CreateSnapshot(_tick);
        if (motherState.Needs.Mood.Points <= 7_500
            || fatherState.Needs.Mood.Points <= 7_500
            || motherState.Needs.Nutrition.Points <= 8_000
            || fatherState.Needs.Nutrition.Points <= 8_000
            || motherState.Needs.Alertness.Points <= 8_000
            || fatherState.Needs.Alertness.Points <= 8_000
            || motherState.Needs.Health.Points != NeedValue.Maximum
            || fatherState.Needs.Health.Points != NeedValue.Maximum)
        {
            return;
        }

        ResidentReproductionContext context = new ResidentReproductionContext(
            motherState.Needs.Mood.Points,
            fatherState.Needs.Mood.Points,
            motherState.Needs.Health.Points,
            fatherState.Needs.Health.Points,
            fertilityModifier: 1,
            hasBirthPlace: true);
        _society.StartPregnancy(mother.Id, father.Id, context, SocietyTick);
    }

    private void CompleteDueResidentBirths()
    {
        ResidentNameCatalog names = new ResidentNameCatalog(
            new[] { "Dora", "Fara", "Hela", "Iria", "Kara", "Mira", "Nora", "Runa" },
            new[] { "Borin", "Doran", "Einar", "Gimli", "Haldor", "Korin", "Orin", "Torin" });
        ResidentIdentityGenerator generator = new ResidentIdentityGenerator();
        ResidentInheritancePolicy inheritance = new ResidentInheritancePolicy(
            potentialVariance: 500);
        foreach (ResidentSocietySnapshot mother in _society
            .GetDuePregnancies(SocietyTick))
        {
            PregnancySnapshot pregnancy = mother.Pregnancy!;
            ResidentSocietySnapshot father = _society.CreateSnapshot().Residents
                .First(value => value.Id == pregnancy.FatherId);
            AgentState? motherAgent = _repository.Get(mother.Id);
            if (motherAgent == null)
            {
                continue;
            }

            ResidentBirthPlan birth = generator.CreateBirthPlan(
                _simulationState.RandomStreams.WorldSeed,
                _society.CreateSnapshot().Residents.Count,
                names,
                mother.Heritage,
                father.Heritage,
                inheritance,
                motherAgent.Position);
            RequireSociety(_society.RegisterBirth(mother.Id, birth, SocietyTick));
            AgentState child = new AgentState(
                birth.Id,
                birth.Name,
                new AgentNeedsSnapshot(
                    new NeedValue(NeedValue.Maximum),
                    new NeedValue(NeedValue.Maximum),
                    new NeedValue(8_000),
                    new NeedValue(NeedValue.Maximum)),
                DailySchedule.CreateBalanced(GameTimeCadence.TicksPerDay),
                skills: null,
                traits: birth.Heritage.Traits,
                initialPosition: birth.Position);
            RequireSociety(_repository.Add(child));
            _residentSexes.Add(birth.Id, birth.Sex);
            _routeIndices.Add(birth.Id, 0);
            _postpartumUntilTick[mother.Id] = checked(
                SocietyTick + (GameTimeCadence.TicksPerDay * 2L));
        }
    }

    private void ApplyLeisureMood(AgentState resident, int mood, string source)
    {
        if (_tick % FreeTimeMoodIntervalTicks != 0)
        {
            return;
        }

        RequireSociety(resident.ApplyExternalNeedDelta(
            new NeedDelta(0, 0, mood, 0),
            source,
            _tick));
        _repository.Save(resident);
    }

    private static bool AreStandingTogether(Dig.Domain.World.CellId first, Dig.Domain.World.CellId second)
    {
        return first.Y == second.Y && ManhattanDistance(first, second) <= 1;
    }

    private static int ManhattanDistance(Dig.Domain.World.CellId first, Dig.Domain.World.CellId second)
    {
        return Math.Abs(first.X - second.X)
            + Math.Abs(first.Y - second.Y)
            + Math.Abs(first.Z - second.Z);
    }

    private static void RequireSociety(Result result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error!.ToString());
        }
    }
}

}
