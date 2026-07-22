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
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class CombatSkillProfileIntegrationTests
{
    private static readonly EntityId AttackerId = EntityId.Parse(
        "cb000000-0000-0000-0000-000000000001");
    private static readonly EntityId TargetId = EntityId.Parse(
        "cb000000-0000-0000-0000-000000000002");
    private static readonly FactionId PlayerFaction = new FactionId("faction.player");
    private static readonly FactionId EnemyFaction = new FactionId("faction.enemy");

    [Fact]
    public void Switching_profiles_and_multiple_hits_grant_each_confirmed_combat_skill()
    {
        (string Id, AgentSkillId Skill)[] profiles =
        {
            ("weapon.one-handed", AgentSkillCatalog.OneHandedCombat),
            ("weapon.two-handed", AgentSkillCatalog.TwoHandedCombat),
            ("weapon.ranged", AgentSkillCatalog.RangedCombat),
            ("weapon.unarmed", AgentSkillCatalog.UnarmedCombat),
        };
        InMemoryAgentRepository agents = CreateAgents();
        RecordingEventSink events = new RecordingEventSink();
        ResolveCombatAttackHandler handler = new ResolveCombatAttackHandler(
            agents,
            new InMemoryCombatRepository(CreateCombat(profiles)),
            new InMemoryFactionRepository(CreateFactions()),
            events,
            new AgentSkillGrantService(agents, events));

        for (int index = 0; index < profiles.Length; index++)
        {
            WeaponProfileId weaponId = new WeaponProfileId(profiles[index].Id);
            Result<CombatAttackResolution> result = handler.Handle(
                new ResolveCombatAttackCommand(
                    new CombatActionId("profile-hit-" + index),
                    AttackerId,
                    TargetId,
                    weaponId,
                    worldSeed: 17UL,
                    tick: 10 + index,
                    new CombatantModifiers(0, 0, 0, 0, 0),
                    new CombatantModifiers(0, 0, 0, 0, 0)));

            Assert.True(result.IsSuccess);
            Assert.Equal(CombatAttackOutcome.Hit, result.Value.Outcome);
        }

        AgentSnapshot attacker = agents.Get(AttackerId)!.CreateSnapshot(tick: 20);
        Assert.All(
            profiles,
            profile => Assert.Equal(25, attacker.GetSkillLevel(profile.Skill)));
        Assert.Equal(
            profiles.Length,
            events.Events.OfType<SkillProgressionResultConfirmed>().Count());
    }

    [Fact]
    public void Missing_skill_recipient_is_rejected_before_combat_or_health_mutates()
    {
        (string Id, AgentSkillId Skill)[] profiles =
        {
            ("weapon.preflight", AgentSkillCatalog.OneHandedCombat),
        };
        InMemoryAgentRepository agents = CreateAgents();
        InMemoryAgentRepository missingSkillAgents = new InMemoryAgentRepository();
        CombatState combat = CreateCombat(profiles);
        RecordingEventSink events = new RecordingEventSink();
        ResolveCombatAttackHandler handler = new ResolveCombatAttackHandler(
            agents,
            new InMemoryCombatRepository(combat),
            new InMemoryFactionRepository(CreateFactions()),
            events,
            new AgentSkillGrantService(missingSkillAgents, events));
        int health = agents.Get(TargetId)!.CreateSnapshot(10).Needs.Health.Points;

        Result<CombatAttackResolution> result = handler.Handle(
            new ResolveCombatAttackCommand(
                new CombatActionId("preflight-rejected"),
                AttackerId,
                TargetId,
                new WeaponProfileId("weapon.preflight"),
                worldSeed: 17UL,
                tick: 10,
                new CombatantModifiers(0, 0, 0, 0, 0),
                new CombatantModifiers(0, 0, 0, 0, 0)));

        Assert.True(result.IsFailure);
        Assert.Equal(0, combat.Version);
        Assert.Equal(
            health,
            agents.Get(TargetId)!.CreateSnapshot(10).Needs.Health.Points);
        Assert.Empty(events.Events);
    }

    [Fact]
    public void Cancelled_attack_intent_cannot_resolve_or_grant_experience()
    {
        (string Id, AgentSkillId Skill)[] profiles =
        {
            ("weapon.interrupted", AgentSkillCatalog.OneHandedCombat),
        };
        InMemoryAgentRepository agents = CreateAgents();
        CombatState combat = CreateCombat(profiles);
        CombatIntentId intentId = new CombatIntentId("intent.interrupted");
        combat.IssueIntent(new CombatIntentRequest(
            intentId,
            AttackerId,
            CombatIntentKind.Attack,
            CombatIntentSource.PlayerOrder,
            createdTick: 1,
            expiresTick: 20,
            targetEntityId: TargetId));
        Assert.True(combat.CancelIntent(intentId, "manual-interrupt", tick: 2).IsSuccess);
        long versionBefore = combat.Version;
        RecordingEventSink events = new RecordingEventSink();
        ResolveCombatAttackHandler handler = new ResolveCombatAttackHandler(
            agents,
            new InMemoryCombatRepository(combat),
            new InMemoryFactionRepository(CreateFactions()),
            events,
            new AgentSkillGrantService(agents, events));

        Result<CombatAttackResolution> result = handler.Handle(
            new ResolveCombatAttackCommand(
                new CombatActionId("interrupted-hit"),
                AttackerId,
                TargetId,
                new WeaponProfileId("weapon.interrupted"),
                worldSeed: 17UL,
                tick: 3,
                new CombatantModifiers(0, 0, 0, 0, 0),
                new CombatantModifiers(0, 0, 0, 0, 0),
                sourceIntentId: intentId));

        Assert.True(result.IsFailure);
        Assert.Equal(CombatApplicationErrors.CombatIntentInactive, result.Error);
        Assert.Equal(versionBefore, combat.Version);
        Assert.Equal(0, agents.Get(AttackerId)!.CreateSnapshot(3)
            .GetSkillLevel(AgentSkillCatalog.OneHandedCombat));
        Assert.Empty(events.Events);
    }

    [Fact]
    public void Replay_does_not_revalidate_or_reapply_skill_recipient()
    {
        (string Id, AgentSkillId Skill)[] profiles =
        {
            ("weapon.replay", AgentSkillCatalog.OneHandedCombat),
        };
        InMemoryAgentRepository agents = CreateAgents();
        CombatState combat = CreateCombat(profiles);
        RecordingEventSink events = new RecordingEventSink();
        ResolveCombatAttackCommand command = new ResolveCombatAttackCommand(
            new CombatActionId("stable-replay"),
            AttackerId,
            TargetId,
            new WeaponProfileId("weapon.replay"),
            worldSeed: 17UL,
            tick: 10,
            new CombatantModifiers(0, 0, 0, 0, 0),
            new CombatantModifiers(0, 0, 0, 0, 0));
        ResolveCombatAttackHandler first = new ResolveCombatAttackHandler(
            agents,
            new InMemoryCombatRepository(combat),
            new InMemoryFactionRepository(CreateFactions()),
            events,
            new AgentSkillGrantService(agents, events));
        Assert.True(first.Handle(command).IsSuccess);
        InMemoryAgentRepository missingSkillAgents = new InMemoryAgentRepository();
        ResolveCombatAttackHandler replay = new ResolveCombatAttackHandler(
            agents,
            new InMemoryCombatRepository(combat),
            new InMemoryFactionRepository(CreateFactions()),
            events,
            new AgentSkillGrantService(missingSkillAgents, events));

        Result<CombatAttackResolution> result = replay.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.WasAlreadyProcessed);
        Assert.Equal(25, agents.Get(AttackerId)!.CreateSnapshot(10)
            .GetSkillLevel(AgentSkillCatalog.OneHandedCombat));
    }

    private static CombatState CreateCombat(
        IEnumerable<(string Id, AgentSkillId Skill)> profiles)
    {
        return new CombatState(new WeaponCatalog(profiles.Select(profile =>
            new WeaponProfile(
                new WeaponProfileId(profile.Id),
                minimumRange: 1,
                maximumRange: 2,
                accuracy: 10_000,
                baseDamage: 100,
                armorPenetration: 0,
                cooldownTicks: 1,
                skillProfile: new CombatSkillProfile(
                    profile.Skill,
                    hitGrantUnits: 25)))));
    }

    private static InMemoryAgentRepository CreateAgents()
    {
        InMemoryAgentRepository agents = new InMemoryAgentRepository();
        Assert.True(agents.Add(CreateAgent(AttackerId, new CellId(0, 0))).IsSuccess);
        Assert.True(agents.Add(CreateAgent(TargetId, new CellId(1, 0))).IsSuccess);
        return agents;
    }

    private static AgentState CreateAgent(EntityId id, CellId position)
    {
        return new AgentState(
            id,
            "Combat profile dwarf",
            AgentTestFactory.CreateNeeds(8_000, 8_000, 8_000, 10_000),
            AgentTestFactory.CreateWorkSchedule(),
            skills: null,
            traits: null,
            position);
    }

    private static FactionState CreateFactions()
    {
        FactionState factions = new FactionState(
            new FactionCatalog(new[]
            {
                new FactionDefinition(PlayerFaction, "Player", -10_000),
                new FactionDefinition(EnemyFaction, "Enemy", -10_000),
            }),
            new FactionDiplomacyPolicy(-5_000, 3_000, 8_000, 1_000));
        factions.AssignMember(AttackerId, PlayerFaction);
        factions.AssignMember(TargetId, EnemyFaction);
        return factions;
    }

    private sealed class RecordingEventSink : IEventSink
    {
        private readonly List<IDomainEvent> _events = new List<IDomainEvent>();

        public IReadOnlyList<IDomainEvent> Events => _events;

        public void Append(IReadOnlyCollection<IDomainEvent> events)
        {
            _events.AddRange(events ?? throw new ArgumentNullException(nameof(events)));
        }
    }
}

}
