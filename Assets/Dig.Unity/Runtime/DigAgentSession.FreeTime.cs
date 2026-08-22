using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Society;
using Dig.Domain.Runtime;
using Dig.Application.Agents;

namespace Dig.Unity
{

internal sealed partial class DigAgentSession
{
    private const int FreeTimeNeedFloor = 7_500;
    private const int FreeTimeMoodIntervalTicks = 25;
    private static readonly LeisureActivityDefinition SoloPlay =
        new LeisureActivityDefinition(new LeisureVarietyId("solo_play"), 30, 25, false);
    private static readonly LeisureActivityDefinition GroupPlay =
        new LeisureActivityDefinition(new LeisureVarietyId("group_play"), 50, 25, true);
    private static readonly LeisureActivityDefinition Socializing =
        new LeisureActivityDefinition(new LeisureVarietyId("social"), 60, 25, true);
    private readonly Dictionary<EntityId, EntityId> _freeTimeMeetingPartners =
        new Dictionary<EntityId, EntityId>();
    private readonly LeisureReservationLedger _freeTimeReservations =
        new LeisureReservationLedger();

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
            if (!IsAvailableForFreeTime(resident))
            {
                resident.CancelLeisure();
                _repository.Save(resident);
            }
        }

        _freeTimeMeetingPartners.Clear();
        _freeTimeReservations.Clear();
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
                AdvanceLeisure(first, SoloPlay, partnerId: null);
                continue;
            }

            available.Remove(second);
            if (!_freeTimeReservations.TryReservePair(
                first.Id,
                second.Id,
                first.Position))
            {
                AdvanceLeisure(first, SoloPlay, partnerId: null);
                available.Add(second);
                continue;
            }
            _freeTimeMeetingPartners[first.Id] = second.Id;
            _freeTimeMeetingPartners[second.Id] = first.Id;
            if (!AreStandingTogether(first.Position, second.Position))
            {
                continue;
            }

            LeisureActivityDefinition activity = new LeisureActivitySelector().SelectOrContinue(
                new[] { GroupPlay, Socializing },
                first.CreateLeisureRuntimeSnapshot(),
                second.Id,
                _simulationState.RandomStreams.WorldSeed,
                Tick + first.Id.GetHashCode());
            AdvanceLeisure(first, activity, second.Id);
            AdvanceLeisure(second, activity, first.Id);
            if (Tick % FreeTimeMoodIntervalTicks == 0)
            {
                AdvanceRelationshipAndReproduction(first, second);
            }
        }

        foreach (AgentState resident in available)
        {
            AdvanceLeisure(resident, SoloPlay, partnerId: null);
        }
    }

    private bool IsAvailableForFreeTime(AgentState resident)
    {
        AgentSnapshot snapshot = resident.CreateSnapshot(Tick);
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
        AgentSnapshot motherState = mother.CreateSnapshot(Tick);
        AgentSnapshot fatherState = father.CreateSnapshot(Tick);
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
        }
    }

    private void AdvanceLeisure(
        AgentState resident,
        LeisureActivityDefinition definition,
        EntityId? partnerId)
    {
        LeisureRuntimeSnapshot current = resident.CreateLeisureRuntimeSnapshot();
        if (!current.ActiveVariety.HasValue
            || !current.ActiveVariety.Value.Equals(definition.Id)
            || current.PartnerId != partnerId)
        {
            RequireSociety(resident.BeginLeisure(definition, partnerId, Tick));
        }

        RequireSociety(resident.AdvanceLeisure(definition, Tick));
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
