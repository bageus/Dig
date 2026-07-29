using System;
using Dig.Application.Saving;
using Dig.Domain.Agents;
using Dig.Domain.Combat;
using Dig.Domain.Core;
using Dig.Domain.Factions;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class CombatSpatialSaveRoundTripTests
{
    private static readonly EntityId Attacker = EntityId.Parse("d1000000000000000000000000000001");
    private static readonly EntityId Target = EntityId.Parse("d2000000000000000000000000000002");
    private static readonly FactionId FirstFaction = new FactionId("faction.save.first");
    private static readonly FactionId SecondFaction = new FactionId("faction.save.second");
    private static readonly WeaponProfileId WeaponId = new WeaponProfileId("weapon.save.spatial");

    [Fact]
    public void Active_execution_and_resolved_action_round_trip_without_duplicate_attack()
    {
        WeaponCatalog weapons = Weapons();
        CombatState combat = new CombatState(weapons);
        CombatIntentId intentId = new CombatIntentId("intent.save.spatial");
        CombatIntentSnapshot intent = combat.IssueIntent(new CombatIntentRequest(
            intentId,
            Attacker,
            CombatIntentKind.Attack,
            CombatIntentSource.PlayerOrder,
            1,
            100,
            Target,
            new CellId(1, 0, 1)));
        CombatExecutionId executionId = new CombatExecutionId("execution.save.spatial");
        Assert.True(combat.StartExecution(new CombatExecutionRequest(
            executionId,
            intent.IntentId,
            Attacker,
            intent.Source,
            CombatExecutionStage.SelectEquipment,
            1)).IsSuccess);
        Assert.True(combat.SetExecutionEquipment(
            executionId,
            WeaponId,
            2,
            "equipment_selected").IsSuccess);
        Assert.True(combat.SetExecutionEngagement(
            executionId,
            new CellId(0, 0, 1),
            2,
            "engagement_selected").IsSuccess);

        CombatActionId actionId = new CombatActionId("action.save.spatial");
        FactionState factions = Factions();
        Result<CombatAttackResolution> resolved = combat.ResolveAttack(
            new CombatAttackRequest(actionId, Attacker, Target, WeaponId, 99UL, 3),
            Combatant(Attacker, FirstFaction, new CellId(0, 0, 1)),
            Combatant(Target, SecondFaction, new CellId(1, 0, 1)),
            factions);
        Assert.True(resolved.IsSuccess);
        Assert.True(combat.RecordExecutionAttack(executionId, actionId, 5, 3).IsSuccess);

        CombatSaveData data = CombatSaveAdapter.Encode(combat);
        Result<CombatState> restored = CombatSaveAdapter.Decode(data, weapons);

        Assert.True(restored.IsSuccess, restored.Error?.ToString());
        CombatExecutionSnapshot execution = restored.Value.GetActiveExecution(Attacker)!;
        Assert.Equal(CombatExecutionStage.Recover, execution.Stage);
        Assert.Equal(actionId, execution.LastResolvedActionId);
        Assert.True(restored.Value.HasResolvedAttack(actionId));
        Result<CombatAttackResolution> replay = restored.Value.ResolveAttack(
            new CombatAttackRequest(actionId, Attacker, Target, WeaponId, 99UL, 3),
            Combatant(Attacker, FirstFaction, new CellId(0, 0, 1)),
            Combatant(Target, SecondFaction, new CellId(1, 0, 1)),
            factions);
        Assert.True(replay.IsSuccess);
        Assert.True(replay.Value.WasAlreadyProcessed);
    }

    [Fact]
    public void Version_nine_migration_adds_empty_combat_section()
    {
        SaveGameDocument document = new SaveGameDocument
        {
            FormatVersion = 9,
            Combat = null!,
        };

        new SaveVersionNineCombatSpatialMigration().Apply(document);

        Assert.Equal(10, document.FormatVersion);
        Assert.NotNull(document.Combat);
        Assert.Empty(document.Combat.Executions);
    }

    private static WeaponCatalog Weapons() => new WeaponCatalog(new[]
    {
        new WeaponProfile(
            WeaponId,
            1,
            1,
            10_000,
            500,
            0,
            2,
            spatialMode: CombatAttackSpatialMode.Melee),
    });

    private static FactionState Factions()
    {
        FactionState factions = new FactionState(
            new FactionCatalog(new[]
            {
                new FactionDefinition(FirstFaction, "First", -10_000),
                new FactionDefinition(SecondFaction, "Second", -10_000),
            }),
            new FactionDiplomacyPolicy(-5_000, 3_000, 8_000, 1_000));
        factions.AssignMember(Attacker, FirstFaction);
        factions.AssignMember(Target, SecondFaction);
        return factions;
    }

    private static CombatantSnapshot Combatant(
        EntityId id,
        FactionId faction,
        CellId cell) => new CombatantSnapshot(
            id,
            faction,
            cell,
            true,
            new NeedValue(10_000),
            0,
            0,
            0,
            0,
            0);
}
}
