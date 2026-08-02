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
