using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Combat;
using Dig.Application.Messaging;
using Dig.Domain.Agents;
using Dig.Domain.Combat;
using Dig.Domain.Core;
using Dig.Domain.Factions;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class CombatApplicationTests
{
    private static readonly EntityId AttackerId = Id("b1000000000000000000000000000001");
    private static readonly EntityId TargetId = Id("b2000000000000000000000000000002");
    private static readonly EntityId HealerId = Id("b3000000000000000000000000000003");
    private static readonly EntityId HealingJobId = Id("b4000000000000000000000000000004");
    private static readonly FactionId PlayerFaction = new FactionId("faction.player");
    private static readonly FactionId RaiderFaction = new FactionId("faction.raiders");
    private static readonly WeaponProfileId WeaponId = new WeaponProfileId("weapon.test");

    [Fact]
    public void Attack_handler_applies_damage_once_for_stable_action_id()
    {
        InMemoryAgentRepository agents = CreateAgents(targetHealth: 5_000);
        FactionState factions = CreateFactions(agents);
        CombatState combat = CreateCombat(status: null);
        RecordingEventSink events = new RecordingEventSink();
        ResolveCombatAttackHandler handler = new ResolveCombatAttackHandler(
            agents,
            new InMemoryCombatRepository(combat),
            new InMemoryFactionRepository(factions),
            events,
            new AgentSkillGrantService(agents, events));
        ResolveCombatAttackCommand command = AttackCommand("application-attack", tick: 10);

        Result<CombatAttackResolution> first = handler.Handle(command);
        int healthAfterFirst = agents.Get(TargetId)!.CreateSnapshot(10).Needs.Health.Points;
        int eventCount = events.Events.Count;
        Result<CombatAttackResolution> replay = handler.Handle(command);
        int healthAfterReplay = agents.Get(TargetId)!.CreateSnapshot(10).Needs.Health.Points;

        Assert.True(first.IsSuccess);
        Assert.True(replay.IsSuccess);
        Assert.False(first.Value.WasAlreadyProcessed);
        Assert.True(replay.Value.WasAlreadyProcessed);
        Assert.Equal(4_000, healthAfterFirst);
        Assert.Equal(healthAfterFirst, healthAfterReplay);
        Assert.Equal(eventCount, events.Events.Count);
        Assert.Single(events.Events.OfType<CombatAttackResolved>());
        Assert.Single(events.Events.OfType<AgentExternalEffectApplied>());
    }

    [Fact]
    public void Periodic_status_damage_is_aggregated_once_per_target_and_tick()
    {
        CombatStatusDefinition bleeding = new CombatStatusDefinition(
            new CombatStatusId("status.bleeding"),
            applicationChance: 10_000,
            durationTicks: 2,
            damagePerTick: 75);
        InMemoryAgentRepository agents = CreateAgents(targetHealth: 5_000);
        FactionState factions = CreateFactions(agents);
        CombatState combat = CreateCombat(bleeding);
        RecordingEventSink events = new RecordingEventSink();
        InMemoryCombatRepository combatRepository = new InMemoryCombatRepository(combat);
        ResolveCombatAttackHandler attack = new ResolveCombatAttackHandler(
            agents,
            combatRepository,
            new InMemoryFactionRepository(factions),
            events,
            new AgentSkillGrantService(agents, events));
        Assert.True(attack.Handle(AttackCommand("status-attack", tick: 10)).IsSuccess);
        int afterAttack = agents.Get(TargetId)!.CreateSnapshot(10).Needs.Health.Points;
        AdvanceCombatStatusesHandler statusHandler = new AdvanceCombatStatusesHandler(
            agents,
            combatRepository,
            events);

        Result<CombatStatusAdvanceReport> first = statusHandler.Handle(
            new AdvanceCombatStatusesCommand(11));
        int afterStatus = agents.Get(TargetId)!.CreateSnapshot(11).Needs.Health.Points;
        Result<CombatStatusAdvanceReport> replay = statusHandler.Handle(
            new AdvanceCombatStatusesCommand(11));

        Assert.True(first.IsSuccess);
        Assert.Equal(1, first.Value.StatusCount);
        Assert.Equal(75, first.Value.TotalDamage);
        Assert.Equal(afterAttack - 75, afterStatus);
        Assert.True(replay.IsSuccess);
        Assert.Equal(0, replay.Value.StatusCount);
        Assert.Equal(afterStatus, agents.Get(TargetId)!.CreateSnapshot(11).Needs.Health.Points);
    }

    [Fact]
    public void Confirmed_weapon_hit_and_shield_event_grant_both_skills_once()
    {
        InMemoryAgentRepository agents = CreateAgents(targetHealth: 5_000);
        FactionState factions = CreateFactions(agents);
        CombatState combat = CreateCombat(status: null);
        RecordingEventSink events = new RecordingEventSink();
        AgentSkillGrantService skills = new AgentSkillGrantService(agents, events);
        ResolveCombatAttackHandler handler = new ResolveCombatAttackHandler(
            agents,
            new InMemoryCombatRepository(combat),
            new InMemoryFactionRepository(factions),
            events,
            skills);
        CombatantModifiers attacker = new CombatantModifiers(0, 0, 0, 0, 0);
        CombatantModifiers shield = new CombatantModifiers(
            0, 0, 0, blockChance: 10_000, blockValue: 0,
            shieldSkillProfile: new ShieldSkillProfile(
                "shield.test",
                defenseGrantUnits: 50));
        ResolveCombatAttackCommand command = new ResolveCombatAttackCommand(
            new CombatActionId("skill-attack"),
            AttackerId,
            TargetId,
            WeaponId,
            worldSeed: 77UL,
            tick: 10,
            attacker,
            shield);

        Assert.True(handler.Handle(command).IsSuccess);
        Assert.True(handler.Handle(command).Value.WasAlreadyProcessed);

        Assert.Equal(75, agents.Get(AttackerId)!.CreateSnapshot(10)
            .GetSkillLevel(AgentSkillCatalog.OneHandedCombat));
        Assert.Equal(50, agents.Get(TargetId)!.CreateSnapshot(10)
            .GetSkillLevel(AgentSkillCatalog.Defense));
        Assert.Equal(2, events.Events.OfType<AgentSkillGrantApplied>().Count());
        SkillProgressionResultConfirmed[] confirmed = events.Events
            .OfType<SkillProgressionResultConfirmed>()
            .ToArray();
        Assert.Contains(confirmed, value => value.Bundle.SourceId
            == "skill-attack:weapon:weapon.test");
        Assert.Contains(confirmed, value => value.Bundle.SourceId
            == "skill-attack:shield:shield.test");
    }

    [Fact]
    public void Miss_does_not_grant_weapon_or_defense_skill()
    {
        InMemoryAgentRepository agents = CreateAgents(targetHealth: 5_000);
        RecordingEventSink events = new RecordingEventSink();
        ResolveCombatAttackHandler handler = new ResolveCombatAttackHandler(
            agents,
            new InMemoryCombatRepository(CreateCombat(status: null, accuracy: 0)),
            new InMemoryFactionRepository(CreateFactions(agents)),
            events,
            new AgentSkillGrantService(agents, events));
        CombatantModifiers none = new CombatantModifiers(0, 0, 0, 0, 0);
        CombatantModifiers shield = new CombatantModifiers(
            0, 0, 0, blockChance: 10_000, blockValue: 0,
            shieldSkillProfile: new ShieldSkillProfile("shield.test", 50));

        Result<CombatAttackResolution> result = handler.Handle(
            new ResolveCombatAttackCommand(
                new CombatActionId("missed-skill-attack"),
                AttackerId,
                TargetId,
                WeaponId,
                worldSeed: 77UL,
                tick: 10,
                none,
                shield));

        Assert.True(result.IsSuccess);
        Assert.Equal(CombatAttackOutcome.Miss, result.Value.Outcome);
        Assert.Empty(events.Events.OfType<AgentSkillGrantApplied>());
    }

    [Fact]
    public void Healing_job_restores_health_once_and_releases_reservations()
    {
        InMemoryAgentRepository agents = CreateAgents(targetHealth: 5_000);
        JobSystem jobs = new JobSystem();
        HealingJobDefinition definition = new HealingJobDefinition(
            HealingJobId,
            TargetId,
            new CellId(1, 0),
            healthRestored: 1_000,
            priority: 10,
            createdTick: 0,
            JobRetryPolicy.Default);
        Assert.True(jobs.Add(definition).IsSuccess);
        Assert.True(jobs.MakeAvailable(HealingJobId, tick: 0).IsSuccess);
        Assert.True(jobs.Claim(HealingJobId, HealerId, tick: 1).IsSuccess);
        RecordingEventSink events = new RecordingEventSink();
        CompleteHealingJobHandler handler = new CompleteHealingJobHandler(
            agents,
            new InMemoryJobRepository(jobs),
            events,
            new AgentSkillGrantService(agents, events));
        CompleteHealingJobCommand command = new CompleteHealingJobCommand(
            HealingJobId,
            TargetId,
            tick: 2);

        Result first = handler.Handle(command);
        Result replay = handler.Handle(command);

        Assert.True(first.IsSuccess);
        Assert.True(replay.IsFailure);
        Assert.Equal(CombatApplicationErrors.HealingJobInvalid, replay.Error);
        Assert.Equal(6_000, agents.Get(TargetId)!.CreateSnapshot(2).Needs.Health.Points);
        Assert.Equal(JobStatus.Completed, jobs.Get(HealingJobId)!.Status);
        Assert.Empty(jobs.GetReservations());
        Assert.Single(events.Events.OfType<AgentExternalEffectApplied>());
        Assert.Equal(100, agents.Get(HealerId)!.CreateSnapshot(2)
            .GetSkillLevel(AgentSkillCatalog.Service));
    }

    private static InMemoryAgentRepository CreateAgents(int targetHealth)
    {
        InMemoryAgentRepository agents = new InMemoryAgentRepository();
        Assert.True(agents.Add(CreateAgent(
            AttackerId,
            health: 10_000,
            new CellId(0, 0))).IsSuccess);
        Assert.True(agents.Add(CreateAgent(
            TargetId,
            targetHealth,
            new CellId(1, 0))).IsSuccess);
        Assert.True(agents.Add(CreateAgent(
            HealerId,
            health: 10_000,
            new CellId(1, 1))).IsSuccess);
        return agents;
    }

    private static AgentState CreateAgent(EntityId id, int health, CellId position)
    {
        return new AgentState(
            id,
            "Combat Test Dwarf",
            AgentTestFactory.CreateNeeds(8_000, 8_000, 8_000, health),
            AgentTestFactory.CreateWorkSchedule(),
            skills: null,
            traits: null,
            position);
    }

    private static FactionState CreateFactions(InMemoryAgentRepository agents)
    {
        FactionState factions = new FactionState(
            new FactionCatalog(new[]
            {
                new FactionDefinition(PlayerFaction, "Player", -10_000),
                new FactionDefinition(RaiderFaction, "Raiders", -10_000),
            }),
            new FactionDiplomacyPolicy(-5_000, 3_000, 8_000, 1_000));
        factions.AssignMember(AttackerId, PlayerFaction);
        factions.AssignMember(TargetId, RaiderFaction);
        factions.AssignMember(HealerId, PlayerFaction);
        return factions;
    }

    private static CombatState CreateCombat(
        CombatStatusDefinition? status,
        int accuracy = 10_000)
    {
        return new CombatState(new WeaponCatalog(new[]
        {
            new WeaponProfile(
                WeaponId,
                minimumRange: 1,
                maximumRange: 1,
                accuracy: accuracy,
                baseDamage: 1_000,
                armorPenetration: 0,
                cooldownTicks: 1,
                statusOnHit: status,
                skillProfile: new CombatSkillProfile(
                    AgentSkillCatalog.OneHandedCombat,
                    hitGrantUnits: 75)),
        }));
    }

    private static ResolveCombatAttackCommand AttackCommand(string actionId, long tick)
    {
        CombatantModifiers none = new CombatantModifiers(0, 0, 0, 0, 0);
        return new ResolveCombatAttackCommand(
            new CombatActionId(actionId),
            AttackerId,
            TargetId,
            WeaponId,
            worldSeed: 77UL,
            tick,
            none,
            none);
    }

    private static EntityId Id(string value)
    {
        return EntityId.Parse(value);
    }

    private sealed class RecordingEventSink : IEventSink
    {
        private readonly List<IDomainEvent> _events = new List<IDomainEvent>();

        public IReadOnlyList<IDomainEvent> Events => _events;

        public void Append(IReadOnlyCollection<IDomainEvent> events)
        {
            if (events is null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            _events.AddRange(events);
        }
    }
}
}
