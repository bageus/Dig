using System;
using System.Linq;
using Dig.Application.Combat;
using Dig.Domain.Agents;
using Dig.Domain.Combat;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Infrastructure.InMemory;

namespace Dig.Unity
{

internal sealed partial class DigAgentSession
{
    internal void BindCombatInventory(InMemoryInventoryRepository inventory)
    {
        if (inventory == null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        if (_combatEquipmentProvider == null)
        {
            throw new InvalidOperationException("Combat equipment is not initialized.");
        }

        _combatEquipmentProvider.BindInventory(inventory);
    }

    private sealed class DemoCombatEquipmentProvider : ICombatEquipmentProvider
    {
        private readonly DigAgentSession _owner;
        private readonly WeaponCatalog _weapons;
        private readonly CombatSkillScalingPolicy _scaling =
            CombatSkillScalingPolicy.CreateCaveEncounter();
        private InMemoryInventoryRepository? _inventory;

        public DemoCombatEquipmentProvider(
            DigAgentSession owner,
            WeaponCatalog weapons)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _weapons = weapons ?? throw new ArgumentNullException(nameof(weapons));
        }

        public void BindInventory(InMemoryInventoryRepository inventory)
        {
            _inventory = inventory
                ?? throw new ArgumentNullException(nameof(inventory));
        }

        public Result<CombatEquipmentSelection> Select(
            EntityId actorId,
            EntityId targetId)
        {
            AgentState? actor = _owner._repository.Get(actorId);
            AgentState? target = _owner._repository.Get(targetId);
            if (actor == null || target == null)
            {
                return Result<CombatEquipmentSelection>.Failure(
                    CombatApplicationErrors.AgentNotFound);
            }

            Result<WeaponProfileId> selected = _owner._enemyDefinitions.TryGetValue(
                actorId,
                out EnemyCombatDefinition? enemy)
                    ? Result<WeaponProfileId>.Success(enemy.AttackProfileId)
                    : SelectResidentWeapon(actorId);
            if (selected.IsFailure)
            {
                return Result<CombatEquipmentSelection>.Failure(selected.Error!);
            }

            WeaponProfileId profileId = selected.Value;
            WeaponProfile weapon = _weapons.Get(profileId);
            CombatantModifiers attacker = BuildAttackerModifiers(actor, weapon);
            CombatantModifiers targetModifiers = BuildTargetModifiers(target);
            return Result<CombatEquipmentSelection>.Success(
                new CombatEquipmentSelection(
                    profileId,
                    attacker,
                    targetModifiers));
        }

        private Result<WeaponProfileId> SelectResidentWeapon(EntityId residentId)
        {
            if (_inventory == null)
            {
                return Result<WeaponProfileId>.Success(
                    CaveEncounterCombatContent.UnarmedProfileId);
            }

            InventoryState inventory = _inventory.Get();
            HeldItemReferenceSnapshot? held = inventory.GetHeldItem(residentId);
            if (held.HasValue)
            {
                ItemStackSnapshot? heldStack = inventory.GetStack(held.Value.StackId);
                ResidentCombatWeaponDefinition? heldWeapon = heldStack == null
                    ? null
                    : CaveEncounterCombatContent.FindResidentWeapon(heldStack.ItemId);
                if (heldStack != null && heldWeapon != null)
                {
                    Result switched = inventory.SwitchHeldItem(
                        residentId,
                        heldStack.StackId,
                        HeldItemPurpose.WeaponUse,
                        _owner.Tick);
                    if (switched.IsFailure)
                    {
                        return Result<WeaponProfileId>.Failure(switched.Error!);
                    }

                    CommitInventory(inventory, switched);
                    return Result<WeaponProfileId>.Success(heldWeapon.ProfileId);
                }
            }

            var definitions = CaveEncounterCombatContent.ResidentWeaponDefinitions;
            var candidates = inventory.CreateSnapshot().Stacks
                .Where(stack => stack.AvailableQuantity > 0)
                .Where(stack => stack.Location.Kind == ItemLocationKind.AgentInventory
                    && stack.Location.HasOwner
                    && stack.Location.OwnerId == residentId)
                .Select(stack => new
                {
                    Stack = stack,
                    Definition = definitions.FirstOrDefault(
                        value => value.ItemId == stack.ItemId),
                })
                .Where(value => value.Definition != null)
                .OrderByDescending(value => value.Definition!.SelectionPriority)
                .ThenBy(value => value.Stack.Location.HasResidentSlot
                    ? value.Stack.Location.ResidentSlot
                    : default)
                .ThenBy(value => value.Stack.StackId.ToString(), StringComparer.Ordinal)
                .FirstOrDefault();
            if (candidates != null)
            {
                Result switched = inventory.SwitchHeldItem(
                    residentId,
                    candidates.Stack.StackId,
                    HeldItemPurpose.WeaponUse,
                    _owner.Tick);
                if (switched.IsFailure)
                {
                    return Result<WeaponProfileId>.Failure(switched.Error!);
                }

                CommitInventory(inventory, switched);
                return Result<WeaponProfileId>.Success(
                    candidates.Definition!.ProfileId);
            }

            if (held.HasValue)
            {
                Result released = inventory.ReleaseHeldItem(residentId, _owner.Tick);
                if (released.IsFailure)
                {
                    return Result<WeaponProfileId>.Failure(released.Error!);
                }
                CommitInventory(inventory, released);
            }

            return Result<WeaponProfileId>.Success(
                CaveEncounterCombatContent.UnarmedProfileId);
        }

        private CombatantModifiers BuildAttackerModifiers(
            AgentState actor,
            WeaponProfile weapon)
        {
            if (_owner._combatOnlyActors.Contains(actor.Id)
                || weapon.SkillProfile == null)
            {
                return new CombatantModifiers(0, 0, 0, 0, 0);
            }

            int skillUnits = actor.CreateSnapshot(_owner.Tick)
                .GetSkillLevel(weapon.SkillProfile.SkillId);
            return new CombatantModifiers(
                _scaling.ResolveAccuracyModifier(skillUnits),
                evasion: 0,
                armor: 0,
                blockChance: 0,
                blockValue: 0,
                shieldSkillProfile: null,
                damageMultiplier: _scaling.ResolveDamageMultiplier(skillUnits));
        }

        private CombatantModifiers BuildTargetModifiers(AgentState target)
        {
            if (_owner._combatOnlyActors.Contains(target.Id))
            {
                return new CombatantModifiers(0, 0, 0, 0, 0);
            }

            int defenseUnits = target.CreateSnapshot(_owner.Tick)
                .GetSkillLevel(AgentSkillCatalog.Defense);
            return new CombatantModifiers(
                accuracyModifier: 0,
                evasion: 0,
                armor: 0,
                blockChance: 0,
                blockValue: 0,
                shieldSkillProfile: null,
                damageMultiplier: CombatSkillScalingPolicy.BasisPoints,
                damageReduction: _scaling.ResolveDefenseReduction(defenseUnits),
                receivedHitSkillProfile:
                    CaveEncounterCombatContent.ResidentReceivedHitProfile);
        }

        private void CommitInventory(InventoryState inventory, Result result)
        {
            if (result.IsFailure)
            {
                return;
            }

            _inventory!.Save(inventory);
            _owner._combatJournal!.Append(inventory.DequeueUncommittedEvents());
        }
    }
}

}
