using System;
using System.Collections.Generic;
using Dig.Application.Inventory;
using Dig.Application.Messaging;
using Dig.Application.Navigation;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Inventory;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Application.Ecology
{

public sealed partial class AdvanceLivingMaterialEcologyCommandHandler
    : ICommandHandler<AdvanceLivingMaterialEcologyCommand, Result>
{
    private readonly ILivingMaterialEcologyRepository _ecology;
    private readonly IInventoryRepository _inventory;
    private readonly INavigationRepository _navigation;
    private readonly TraversalProfileId _profileId;
    private readonly IEventSink _events;
    private readonly LivingMaterialMovementPlanner _movement =
        new LivingMaterialMovementPlanner();

    public AdvanceLivingMaterialEcologyCommandHandler(
        ILivingMaterialEcologyRepository ecology,
        IInventoryRepository inventory,
        INavigationRepository navigation,
        TraversalProfileId profileId,
        IEventSink events)
    {
        _ecology = ecology ?? throw new ArgumentNullException(nameof(ecology));
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _profileId = profileId;
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public Result Synchronize(long simulationTick)
    {
        if (simulationTick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(simulationTick));
        }

        NavigationMap? map = _navigation.Get(_profileId);
        if (map == null)
        {
            return Result.Failure(LivingMaterialApplicationErrors.NavigationUnavailable);
        }

        Result<NavigationSnapshot> navigation = map.GetSnapshot();
        if (navigation.IsFailure)
        {
            return Result.Failure(LivingMaterialApplicationErrors.NavigationUnavailable);
        }

        LivingMaterialEcologyState ecology = _ecology.Get();
        InventoryState inventory = _inventory.Get();
        Result reconciled = ReconcileInventory(
            ecology,
            inventory,
            new LivingMaterialPlaneResolver(navigation.Value),
            simulationTick);
        if (reconciled.IsFailure)
        {
            return reconciled;
        }

        _ecology.Save(ecology);
        _events.Append(ecology.DequeueUncommittedEvents());
        return Result.Success();
    }

    public Result Handle(AdvanceLivingMaterialEcologyCommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        NavigationMap? map = _navigation.Get(_profileId);
        if (map == null)
        {
            return Result.Failure(LivingMaterialApplicationErrors.NavigationUnavailable);
        }

        Result<NavigationSnapshot> navigation = map.GetSnapshot();
        if (navigation.IsFailure)
        {
            return Result.Failure(LivingMaterialApplicationErrors.NavigationUnavailable);
        }

        LivingMaterialEcologyState ecology = _ecology.Get();
        InventoryState inventory = _inventory.Get();
        LivingMaterialPlaneResolver planes = new LivingMaterialPlaneResolver(
            navigation.Value);
        Result reconciled = ReconcileInventory(
            ecology,
            inventory,
            planes,
            command.SimulationTick);
        if (reconciled.IsFailure)
        {
            return reconciled;
        }

        for (int index = 0;
            index < LivingMaterialEcologyProfiles.EcologyStepsPerSimulationTick;
            index++)
        {
            Result advanced = ecology.AdvanceOneEcologyStep(command.SimulationTick);
            if (advanced.IsFailure)
            {
                return advanced;
            }

            Result reproduction = AdvanceReproduction(
                ecology,
                inventory,
                planes,
                command.SimulationTick);
            if (reproduction.IsFailure)
            {
                return reproduction;
            }

            Result movement = AdvanceMovement(
                ecology,
                inventory,
                planes,
                command.ResidentCells,
                command.SimulationTick);
            if (movement.IsFailure)
            {
                return movement;
            }
        }

        _ecology.Save(ecology);
        _inventory.Save(inventory);
        _events.Append(ecology.DequeueUncommittedEvents());
        _events.Append(inventory.DequeueUncommittedEvents());
        return Result.Success();
    }

    private static Result ReconcileInventory(
        LivingMaterialEcologyState ecology,
        InventoryState inventory,
        LivingMaterialPlaneResolver planes,
        long tick)
    {
        foreach (ItemStackSnapshot stack in inventory.CreateSnapshot().Stacks)
        {
            if (stack.Quantity != 1
                || !LivingMaterialEcologyProfiles.TryResolve(stack.ItemId, out LivingMaterialSpecies species))
            {
                continue;
            }

            LivingMaterialSnapshot? existing = ecology.GetByItem(stack.StackId);
            if (existing == null)
            {
                Result registered = RegisterFromInventory(
                    ecology,
                    stack,
                    species,
                    planes,
                    tick);
                if (registered.IsFailure)
                {
                    return registered;
                }
            }
        }

        foreach (LivingMaterialSnapshot creature in ecology.GetAll())
        {
            ItemStackSnapshot? stack = inventory.GetStack(creature.ItemEntityId);
            if (stack == null)
            {
                return Result.Failure(LivingMaterialApplicationErrors.MissingLinkedItem);
            }

            if (stack.Location.Kind != ItemLocationKind.World)
            {
                Result stored = ecology.Store(creature.CreatureId, tick);
                if (stored.IsFailure)
                {
                    return stored;
                }

                continue;
            }

            if (!planes.TryResolve(stack.Location.CellId, out LivingMaterialPlane plane))
            {
                return Result.Failure(LivingMaterialApplicationErrors.InvalidWorldCell);
            }

            if (!creature.IsFree || creature.Cell != stack.Location.CellId)
            {
                Result released = ecology.Release(
                    creature.CreatureId,
                    stack.Location.CellId,
                    plane.Key,
                    tick);
                if (released.IsFailure)
                {
                    return released;
                }

                continue;
            }

            if (creature.PlaneKey != plane.Key)
            {
                CellId anchor = stack.Location.CellId;
                if (planes.TryResolve(
                        creature.AnchorCell,
                        out LivingMaterialPlane anchorPlane)
                    && anchorPlane.Key == plane.Key)
                {
                    anchor = creature.AnchorCell;
                }

                Result rebound = ecology.RebindMovementRegion(
                    creature.CreatureId,
                    stack.Location.CellId,
                    anchor,
                    plane.Key,
                    tick);
                if (rebound.IsFailure)
                {
                    return rebound;
                }
            }
        }

        return Result.Success();
    }

    private static Result RegisterFromInventory(
        LivingMaterialEcologyState ecology,
        ItemStackSnapshot stack,
        LivingMaterialSpecies species,
        LivingMaterialPlaneResolver planes,
        long tick)
    {
        CellId? cell = null;
        LivingMaterialPlaneKey key = new LivingMaterialPlaneKey(default);
        if (stack.Location.Kind == ItemLocationKind.World)
        {
            if (!planes.TryResolve(stack.Location.CellId, out LivingMaterialPlane plane))
            {
                return Result.Failure(LivingMaterialApplicationErrors.InvalidWorldCell);
            }

            cell = stack.Location.CellId;
            key = plane.Key;
        }

        return ecology.Register(
            stack.StackId,
            stack.StackId,
            species,
            cell,
            key,
            tick);
    }
}

}
