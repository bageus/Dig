using System;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Society;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class SocietyStateTests
{
    private static readonly EntityId MotherId = Id("10000000000000000000000000000001");
    private static readonly EntityId FatherId = Id("20000000000000000000000000000002");
    private static readonly EntityId FirstChildId = Id("30000000000000000000000000000003");
    private static readonly EntityId SecondChildId = Id("40000000000000000000000000000004");
    private static readonly CellId Home = new CellId(4, 5);

    [Fact]
    public void Mood_75_is_neutral_and_does_not_satisfy_reproduction()
    {
        SocietyState society = CreateAdultCouple();

        ReproductionEvaluation neutral = society.EvaluateReproduction(
            MotherId,
            FatherId,
            Context(motherMood: 7_500));
        ReproductionEvaluation joy = society.EvaluateReproduction(
            MotherId,
            FatherId,
            Context(motherMood: 7_600));

        Assert.False(neutral.CanReproduce);
        Assert.Contains(ReproductionBlockReason.MoodTooLow, neutral.Reasons);
        Assert.True(joy.CanReproduce);
    }

    [Fact]
    public void Birth_requires_due_pregnancy_and_publishes_one_typed_event()
    {
        SocietyState society = CreateAdultCouple();
        Assert.True(society.StartPregnancy(MotherId, FatherId, Context(), tick: 20).IsSuccess);
        ResidentBirthPlan birth = BirthPlan(FirstChildId, "Mira", ResidentSex.Female);

        Result early = society.RegisterBirth(MotherId, birth, tick: 24);
        Result completed = society.RegisterBirth(MotherId, birth, tick: 25);
        Result duplicate = society.RegisterBirth(MotherId, birth, tick: 25);

        Assert.True(early.IsFailure);
        Assert.Equal(SocietyErrors.PregnancyNotDue, early.Error);
        Assert.True(completed.IsSuccess);
        Assert.True(duplicate.IsFailure);
        ResidentBorn born = Assert.Single(society.PeekUncommittedEvents().OfType<ResidentBorn>());
        Assert.Equal("resident-born:" + FirstChildId, born.EventId);
        Assert.Equal(MotherId, born.MotherId);
        Assert.Equal(FatherId, born.FatherId);
        Assert.Equal(Home, born.Position);
        Assert.True(society.ValidateFamilyGraph().IsSuccess);
    }

    [Fact]
    public void Siblings_are_close_relatives_and_cannot_form_partnership()
    {
        SocietyState society = CreateAdultCouple();
        RegisterChild(society, FirstChildId, "Mira", ResidentSex.Female, 20);
        RegisterChild(society, SecondChildId, "Borin", ResidentSex.Male, 26);
        Assert.True(society.AdvanceLifecycle(tick: 41).IsSuccess);
        Assert.True(society.SetSocialBond(
            FirstChildId,
            SecondChildId,
            sympathy: 9_000,
            trust: 9_000,
            tick: 41).IsSuccess);

        Result partnership = society.FormPartnership(FirstChildId, SecondChildId, tick: 41);

        Assert.True(society.AreCloseRelatives(FirstChildId, SecondChildId));
        Assert.True(partnership.IsFailure);
        Assert.Equal(SocietyErrors.CloseRelatives, partnership.Error);
        Assert.True(society.ValidateFamilyGraph().IsSuccess);
    }

    [Fact]
    public void Lifecycle_transitions_are_emitted_once_and_old_age_records_position()
    {
        SocietyPolicy policy = new SocietyPolicy(
            adultAgeTicks: 10,
            oldAgeTicks: 20,
            maximumAgeTicks: 30,
            gestationTicks: 5,
            closeKinshipDepth: 2,
            minimumPartnershipSympathy: 6_000,
            minimumPartnershipTrust: 6_000,
            minimumReproductionMood: 7_600,
            minimumReproductionHealth: 5_000);
        SocietyState society = new SocietyState(policy);
        EntityId residentId = Id("50000000000000000000000000000005");
        Assert.True(society.RegisterFounder(
            Registration(residentId, "Edda", ResidentSex.Female, birthTick: 0),
            tick: 0).IsSuccess);
        CellId lastPosition = new CellId(9, 7);
        Assert.True(society.UpdateLastKnownPosition(residentId, lastPosition).IsSuccess);

        society.AdvanceLifecycle(10);
        society.AdvanceLifecycle(10);
        society.AdvanceLifecycle(20);
        society.AdvanceLifecycle(30);
        society.AdvanceLifecycle(31);

        ResidentLifeStageChanged[] stages = society.PeekUncommittedEvents()
            .OfType<ResidentLifeStageChanged>()
            .ToArray();
        Assert.Equal(3, stages.Length);
        Assert.Equal(ResidentLifeStage.Adult, stages[0].CurrentStage);
        Assert.Equal(ResidentLifeStage.Old, stages[1].CurrentStage);
        Assert.Equal(ResidentLifeStage.Deceased, stages[2].CurrentStage);
        ResidentDied died = Assert.Single(society.PeekUncommittedEvents().OfType<ResidentDied>());
        Assert.Equal(new ResidentDeathCauseId("old_age"), died.Cause);
        Assert.Equal(lastPosition, died.LastKnownPosition);
        Assert.Equal("resident-died:" + residentId, died.EventId);
    }

    [Fact]
    public void Explicit_death_is_idempotent_and_breaks_partnership()
    {
        SocietyState society = CreateAdultCouple();
        ResidentDeathCauseId cause = new ResidentDeathCauseId("accident");
        CellId lastPosition = new CellId(12, 3);

        Result first = society.RecordDeath(MotherId, cause, lastPosition, tick: 22);
        Result second = society.RecordDeath(MotherId, cause, lastPosition, tick: 23);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Single(society.PeekUncommittedEvents().OfType<ResidentDied>());
        ResidentSocietySnapshot mother = society.GetResident(MotherId)!;
        ResidentSocietySnapshot father = society.GetResident(FatherId)!;
        Assert.False(mother.IsAlive);
        Assert.Null(mother.PartnerId);
        Assert.Null(father.PartnerId);
        Assert.Equal(lastPosition, mother.LastKnownPosition);
    }

    [Fact]
    public void Identity_and_inheritance_are_deterministic_for_seed_and_sequence()
    {
        AgentTraitId steady = new AgentTraitId("steady");
        AgentTraitId bold = new AgentTraitId("bold");
        ResidentInheritancePolicy inheritance = new ResidentInheritancePolicy(
            potentialVariance: 500,
            new[]
            {
                new TraitInheritanceDefinition(steady, 10_000, 10_000),
                new TraitInheritanceDefinition(bold, 0, 10_000),
            });
        ResidentHeritage mother = new ResidentHeritage(7_000, new[] { steady, bold });
        ResidentHeritage father = new ResidentHeritage(9_000, new[] { bold });
        ResidentNameCatalog names = new ResidentNameCatalog(
            new[] { "Mira", "Edda" },
            new[] { "Borin", "Doran" });
        ResidentIdentityGenerator generator = new ResidentIdentityGenerator();

        ResidentBirthPlan first = generator.CreateBirthPlan(
            42UL, 7, names, mother, father, inheritance, Home);
        ResidentBirthPlan replay = generator.CreateBirthPlan(
            42UL, 7, names, mother, father, inheritance, Home);
        ResidentBirthPlan next = generator.CreateBirthPlan(
            42UL, 8, names, mother, father, inheritance, Home);

        Assert.Equal(first.Id, replay.Id);
        Assert.Equal(first.Name, replay.Name);
        Assert.Equal(first.Sex, replay.Sex);
        Assert.Equal(first.Heritage.Potential, replay.Heritage.Potential);
        Assert.Equal(first.Heritage.Traits.ToArray(), replay.Heritage.Traits.ToArray());
        Assert.NotEqual(first.Id, next.Id);
        Assert.Contains(steady, first.Heritage.Traits);
        Assert.Contains(bold, first.Heritage.Traits);
    }

    [Fact]
    public void Multi_generation_family_graph_remains_acyclic()
    {
        SocietyState society = CreateAdultCouple();
        RegisterChild(society, FirstChildId, "Mira", ResidentSex.Female, 20);
        EntityId outsider = Id("60000000000000000000000000000006");
        Assert.True(society.RegisterFounder(
            Registration(outsider, "Doran", ResidentSex.Male, birthTick: 0),
            tick: 41).IsSuccess);
        Assert.True(society.AdvanceLifecycle(41).IsSuccess);
        Assert.True(society.SetSocialBond(
            FirstChildId, outsider, 9_000, 9_000, tick: 41).IsSuccess);
        Assert.True(society.FormPartnership(FirstChildId, outsider, tick: 41).IsSuccess);
        Assert.True(society.StartPregnancy(
            FirstChildId, outsider, Context(), tick: 42).IsSuccess);
        EntityId grandchild = Id("70000000000000000000000000000007");
        Assert.True(society.RegisterBirth(
            FirstChildId,
            BirthPlan(grandchild, "Kara", ResidentSex.Female),
            tick: 47).IsSuccess);

        Assert.True(society.ValidateFamilyGraph().IsSuccess);
        Assert.True(society.AreCloseRelatives(MotherId, grandchild));
        Assert.False(society.AreCloseRelatives(FatherId, outsider));
    }

    private static SocietyState CreateAdultCouple()
    {
        SocietyState society = new SocietyState(CreatePolicy());
        Assert.True(society.RegisterFounder(
            Registration(MotherId, "Edda", ResidentSex.Female, birthTick: 0),
            tick: 20).IsSuccess);
        Assert.True(society.RegisterFounder(
            Registration(FatherId, "Borin", ResidentSex.Male, birthTick: 0),
            tick: 20).IsSuccess);
        Assert.True(society.SetSocialBond(
            MotherId, FatherId, 9_000, 9_000, tick: 20).IsSuccess);
        Assert.True(society.FormPartnership(MotherId, FatherId, tick: 20).IsSuccess);
        return society;
    }

    private static void RegisterChild(
        SocietyState society,
        EntityId childId,
        string name,
        ResidentSex sex,
        long conceptionTick)
    {
        Assert.True(society.StartPregnancy(
            MotherId, FatherId, Context(), conceptionTick).IsSuccess);
        Assert.True(society.RegisterBirth(
            MotherId,
            BirthPlan(childId, name, sex),
            conceptionTick + 5).IsSuccess);
    }

    private static SocietyPolicy CreatePolicy()
    {
        return new SocietyPolicy(
            adultAgeTicks: 10,
            oldAgeTicks: 100,
            maximumAgeTicks: 200,
            gestationTicks: 5,
            closeKinshipDepth: 3,
            minimumPartnershipSympathy: 6_000,
            minimumPartnershipTrust: 6_000,
            minimumReproductionMood: 7_600,
            minimumReproductionHealth: 5_000);
    }

    private static ResidentReproductionContext Context(int motherMood = 8_000)
    {
        return new ResidentReproductionContext(
            motherMood,
            fatherMood: 8_000,
            motherHealth: 9_000,
            fatherHealth: 9_000,
            fertilityModifier: 10_000,
            hasBirthPlace: true);
    }

    private static ResidentRegistration Registration(
        EntityId id,
        string name,
        ResidentSex sex,
        long birthTick)
    {
        return new ResidentRegistration(
            id,
            name,
            sex,
            birthTick,
            Home,
            new ResidentHeritage(8_000));
    }

    private static ResidentBirthPlan BirthPlan(
        EntityId id,
        string name,
        ResidentSex sex)
    {
        return new ResidentBirthPlan(
            id,
            name,
            sex,
            new ResidentHeritage(8_000),
            Home);
    }

    private static EntityId Id(string value)
    {
        return EntityId.Parse(value);
    }
}
}
