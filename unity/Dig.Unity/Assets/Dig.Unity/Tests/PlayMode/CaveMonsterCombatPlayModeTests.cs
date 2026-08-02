using System;
using System.Collections;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Presentation.Combat;
using Dig.Presentation.Creatures;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dig.Unity.Tests
{

public sealed class CaveMonsterCombatPlayModeTests
{
    private GameObject? _root;

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
        {
            UnityEngine.Object.DestroyImmediate(_root);
        }
    }

    [UnityTest]
    public IEnumerator Monsters_patrol_slowly_highlight_on_hover_and_keep_aggro_after_resident_direct_order()
    {
        DigWorldSession world = DigWorldSession.CreateDemo(20, 14, 5);
        DigAgentSession agents = DigAgentSession.CreateDemo(
            world.LoadView(),
            world.CreateTunnelNavigationVolume(),
            world.Journal);
        var residents = agents.LoadView();
        DigTerrainWorkSession terrain = DigTerrainWorkSession.CreateDemo(
            world,
            residents,
            world.Journal,
            agents.SkillGrants);
        agents.BindCombatInventory(terrain.InventoryRepository);

        CreatureVisualSnapshot[] initial = agents.LoadEnemyCreatures().ToArray();
        Assert.That(initial, Has.Length.EqualTo(2));
        for (int tick = 1; tick < CaveEncounterCombatContent.CaveMonster.PatrolIntervalTicks; tick++)
        {
            Assert.That(agents.Advance().IsSuccess, Is.True);
            CreatureVisualSnapshot[] beforeDue = agents.LoadEnemyCreatures().ToArray();
            Assert.That(
                beforeDue.Select(value => (value.CellX, value.CellY, value.CellZ)),
                Is.EqualTo(initial.Select(value => (value.CellX, value.CellY, value.CellZ))));
        }

        Assert.That(agents.Advance().IsSuccess, Is.True);
        CreatureVisualSnapshot[] patrolled = agents.LoadEnemyCreatures().ToArray();
        Assert.That(
            patrolled.Zip(initial, (current, start) =>
                current.CellX != start.CellX || current.CellZ != start.CellZ).Any(value => value),
            Is.True);
        Assert.That(
            patrolled.Zip(initial, (current, start) => current.CellY == start.CellY)
                .All(value => value),
            Is.True);

        _root = new GameObject("Cave monster patrol presentation");
        DigCreatureRenderer renderer = _root.AddComponent<DigCreatureRenderer>();
        renderer.Render(
            patrolled,
            camera: null,
            movementDuration: 0f);
        string highlightedId = patrolled[0].CreatureId;
        renderer.SetHighlighted(highlightedId);
        Assert.That(renderer.HighlightedCreatureId, Is.EqualTo(highlightedId));
        renderer.ClearHighlight();
        Assert.That(renderer.HighlightedCreatureId, Is.Null);
        yield return null;

        EntityId enemyId = EntityId.Parse(patrolled[0].CreatureId);
        EntityId residentId = EntityId.Parse(residents[0].Id);
        AgentState resident = agents.Repository.Get(residentId)!;
        AgentState enemy = agents.Repository.Get(enemyId)!;
        Assert.That(resident.MoveTo(enemy.Position, agents.Tick).IsSuccess, Is.True);
        agents.Repository.Save(resident);

        Assert.That(agents.Advance().IsSuccess, Is.True);
        Dig.Domain.Combat.CombatIntentSnapshot enemyIntent =
            agents.GetCombatIntent(enemyId)!;
        Assert.That(enemyIntent, Is.Not.Null);
        Assert.That(enemyIntent.IsPersistent, Is.True);
        Assert.That(agents.GetCombatIntent(residentId), Is.Not.Null);

        terrain.BindDirectCommandCombatDisengage(
            agents.DisengageResidentForDirectOrder);
        Assert.That(terrain.PrepareResidentsForDirectCommand(
            new[] { residentId.ToString() },
            agents.Tick).IsSuccess, Is.True);

        Assert.That(agents.GetCombatIntent(residentId), Is.Null);
        Assert.That(agents.GetCombatIntent(enemyId), Is.Not.Null);
        Assert.That(agents.GetCombatIntent(enemyId)!.IsPersistent, Is.True);
        Assert.That(agents.Advance().IsSuccess, Is.True);
        Assert.That(agents.GetCombatIntent(enemyId), Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator Fresh_demo_pair_uses_health_bars_inventory_weapon_and_skill_combat()
    {
        DigWorldSession world = DigWorldSession.CreateDemo(20, 14, 5);
        DigAgentSession agents = DigAgentSession.CreateDemo(
            world.LoadView(),
            world.CreateTunnelNavigationVolume(),
            world.Journal);
        var residents = agents.LoadView();
        DigTerrainWorkSession terrain = DigTerrainWorkSession.CreateDemo(
            world,
            residents,
            world.Journal,
            agents.SkillGrants);
        agents.BindCombatInventory(terrain.InventoryRepository);

        CreatureVisualSnapshot[] enemies = agents.LoadEnemyCreatures().ToArray();
        Assert.That(enemies, Has.Length.EqualTo(2));
        Assert.That(enemies, Has.All.Matches<CreatureVisualSnapshot>(value =>
            value.SpeciesId == CaveEncounterCombatContent.CaveMonsterSpeciesId));
        Assert.That(enemies.Select(value => value.CreatureId).Distinct().Count(),
            Is.EqualTo(2));
        Assert.That(enemies.Select(value => value.CellY).Distinct().Count(), Is.EqualTo(1));

        EntityId residentId = EntityId.Parse(residents[0].Id);
        EntityId enemyId = EntityId.Parse(enemies[0].CreatureId);
        InventoryState inventory = terrain.InventoryRepository.Get();
        ItemStackSnapshot club = inventory.CreateSnapshot().Stacks.Single(value =>
            value.ItemId == CombatEquipmentContent.ClubItemId);
        Result moved = inventory.MoveAvailable(
            club.StackId,
            quantity: 1,
            ItemLocation.InResidentSlot(
                residentId,
                ResidentInventoryCompartment.Main,
                slotIndex: 2),
            splitStackId: default,
            tick: 0);
        Assert.That(moved.IsSuccess, Is.True, moved.Error?.ToString());
        terrain.InventoryRepository.Save(inventory);

        Result<Dig.Domain.Combat.CombatIntentSnapshot> issued =
            agents.IssuePlayerAttackOrder(residentId, enemyId);
        Assert.That(issued.IsSuccess, Is.True, issued.Error?.ToString());

        CombatantHealthBarViewModel residentBar = agents
            .LoadResidentCombatHealthBars()
            .Single(value => value.EntityId == residentId.ToString());
        CreatureVisualSnapshot enemyBar = agents.LoadEnemyCreatures()
            .Single(value => value.CreatureId == enemyId.ToString());
        Assert.That(residentBar.IsVisible, Is.True);
        Assert.That(enemyBar.ShowHealthBar, Is.True);

        _root = new GameObject("Cave monster combat presentation");
        DigAgentRenderer residentRenderer = _root.AddComponent<DigAgentRenderer>();
        DigCreatureRenderer creatureRenderer = _root.AddComponent<DigCreatureRenderer>();
        residentRenderer.Render(residents, movementDuration: 0f);
        residentRenderer.RenderCombatHealthBars(
            agents.LoadResidentCombatHealthBars(),
            camera: null);
        creatureRenderer.Render(
            agents.LoadCreatures(Array.Empty<CreatureVisualSnapshot>()),
            camera: null,
            movementDuration: 0f);
        yield return null;

        DigCombatHealthBar[] visibleBars = _root
            .GetComponentsInChildren<DigCombatHealthBar>(includeInactive: true)
            .Where(value => value.gameObject.activeSelf)
            .ToArray();
        Assert.That(visibleBars, Has.Length.EqualTo(2));

        int startingEnemyHealth = enemyBar.CurrentHealth;
        bool heldClub = false;
        bool damagedEnemy = false;
        bool learnedWeapon = false;
        bool learnedDefense = false;
        for (int index = 0; index < 220; index++)
        {
            Assert.That(agents.Advance().IsSuccess, Is.True);
            InventoryState currentInventory = terrain.InventoryRepository.Get();
            HeldItemReferenceSnapshot? held = currentInventory.GetHeldItem(residentId);
            heldClub |= held.HasValue
                && held.Value.Purpose == HeldItemPurpose.WeaponUse
                && currentInventory.GetStack(held.Value.StackId)?.ItemId
                    == CombatEquipmentContent.ClubItemId;
            CreatureVisualSnapshot currentEnemy = agents.LoadEnemyCreatures()
                .Single(value => value.CreatureId == enemyId.ToString());
            damagedEnemy |= currentEnemy.CurrentHealth < startingEnemyHealth;
            learnedWeapon |= agents.GetSkillLevel(
                residentId,
                AgentSkillCatalog.OneHandedCombat) > 0;
            learnedDefense |= agents.GetSkillLevel(
                residentId,
                AgentSkillCatalog.Defense) > 0;
            if (heldClub && damagedEnemy && learnedWeapon && learnedDefense)
            {
                break;
            }
        }

        Assert.That(heldClub, Is.True);
        Assert.That(damagedEnemy, Is.True);
        Assert.That(learnedWeapon, Is.True);
        Assert.That(learnedDefense, Is.True);
    }
}

}
