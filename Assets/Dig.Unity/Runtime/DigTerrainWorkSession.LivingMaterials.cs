using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Ecology;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Inventory;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;
using Dig.Presentation.Creatures;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private static readonly DomainError LivingMaterialsNotInitialized = new DomainError(
        "unity.living_materials.not_initialized",
        "The hamster and grub ecology runtime is not initialized.");
    private static readonly DomainError LivingMaterialSeedConflict = new DomainError(
        "unity.living_materials.seed_conflict",
        "The deterministic hamster and grub demo identities are already occupied.");
    private static readonly EntityId DemoHamsterOneId = EntityId.Parse(
        "71000000000000000000000000000001");
    private static readonly EntityId DemoHamsterTwoId = EntityId.Parse(
        "71000000000000000000000000000002");
    private static readonly EntityId DemoGrubId = EntityId.Parse(
        "72000000000000000000000000000001");

    private InMemoryLivingMaterialEcologyRepository? _livingMaterials;
    private AdvanceLivingMaterialEcologyCommandHandler? _livingMaterialAdvance;
    private readonly LivingMaterialCreatureVisualProjector _livingMaterialVisuals =
        new LivingMaterialCreatureVisualProjector();
    private readonly LivingMaterialCampfireTetherProjector _livingMaterialTethers =
        new LivingMaterialCampfireTetherProjector();

    internal void InitializeLivingMaterials(
        long tick,
        IReadOnlyList<AgentViewModel> agents)
    {
        if (_livingMaterials != null)
        {
            return;
        }

        if (agents == null)
        {
            throw new ArgumentNullException(nameof(agents));
        }

        LivingMaterialEcologyState state = new LivingMaterialEcologyState(
            unchecked((ulong)(uint)_worldSession.MiningOutputWorldSeed));
        _livingMaterials = new InMemoryLivingMaterialEcologyRepository(state);
        _livingMaterialAdvance = new AdvanceLivingMaterialEcologyCommandHandler(
            _livingMaterials,
            _inventoryRepository,
            _navigationRepository,
            _profile.Id,
            _journal);

        Result seeded = SeedDemoLivingMaterials(tick, agents);
        if (seeded.IsFailure)
        {
            throw new InvalidOperationException(seeded.Error!.ToString());
        }

        Result synchronized = _livingMaterialAdvance.Synchronize(tick);
        if (synchronized.IsFailure)
        {
            throw new InvalidOperationException(synchronized.Error!.ToString());
        }
    }

    internal Result SynchronizeLivingMaterials(long tick)
    {
        return _livingMaterialAdvance == null
            ? Result.Failure(LivingMaterialsNotInitialized)
            : _livingMaterialAdvance.Synchronize(tick);
    }

    internal Result AdvanceLivingMaterials(
        long tick,
        IReadOnlyList<AgentViewModel> agents)
    {
        if (_livingMaterialAdvance == null)
        {
            return Result.Failure(LivingMaterialsNotInitialized);
        }

        if (agents == null)
        {
            throw new ArgumentNullException(nameof(agents));
        }

        CellId[] residentCells = agents
            .Where(value => value.IsAlive)
            .Select(value => new CellId(value.CellX, value.CellY, value.CellZ))
            .OrderBy(value => value)
            .ToArray();
        Result ecology = _livingMaterialAdvance.Handle(
            new AdvanceLivingMaterialEcologyCommand(tick, residentCells));
        return ecology.IsFailure ? ecology : AdvanceFarms(tick);
    }

    internal IReadOnlyList<CreatureVisualSnapshot> LoadLivingMaterialCreatures()
    {
        return _livingMaterials == null
            ? Array.Empty<CreatureVisualSnapshot>()
            : _livingMaterialVisuals.Project(_livingMaterials.Get().GetAll());
    }

    internal IReadOnlyList<LivingMaterialCampfireTetherViewModel>
        LoadLivingMaterialCampfireTethers()
    {
        return _livingMaterials == null
            ? Array.Empty<LivingMaterialCampfireTetherViewModel>()
            : _livingMaterialTethers.Project(
                _inventoryRepository.Get().CreateSnapshot(),
                LoadBuildings());
    }

    private Result SeedDemoLivingMaterials(
        long tick,
        IReadOnlyList<AgentViewModel> agents)
    {
        InventoryState inventory = _inventoryRepository.Get();
        InventorySnapshot snapshot = inventory.CreateSnapshot();
        if (snapshot.Stacks.Any(stack =>
            LivingMaterialEcologyProfiles.TryResolve(stack.ItemId, out _)))
        {
            return Result.Success();
        }

        NavigationMap? map = _navigationRepository.Get(_profile.Id);
        if (map == null)
        {
            return Result.Failure(LivingMaterialApplicationErrors.NavigationUnavailable);
        }

        Result<NavigationSnapshot> navigation = map.GetSnapshot();
        if (navigation.IsFailure)
        {
            return Result.Failure(LivingMaterialApplicationErrors.NavigationUnavailable);
        }

        CellId[] occupiedInitialCells = snapshot.Stacks
            .Where(stack => stack.Location.Kind == ItemLocationKind.World)
            .Select(stack => stack.Location.CellId)
            .Concat(agents
                .Where(value => value.IsAlive)
                .Select(value => new CellId(
                    value.CellX,
                    value.CellY,
                    value.CellZ)))
            .Distinct()
            .OrderBy(value => value)
            .ToArray();
        Result<LivingMaterialInitialPopulationPlan> planned =
            new LivingMaterialInitialPopulationPlanner().Plan(
                navigation.Value,
                occupiedInitialCells);
        if (planned.IsFailure)
        {
            return Result.Failure(planned.Error!);
        }

        EntityId[] stableIds =
        {
            DemoHamsterOneId,
            DemoHamsterTwoId,
            DemoGrubId,
        };
        if (planned.Value.Placements.Count != stableIds.Length)
        {
            return Result.Failure(LivingMaterialSeedConflict);
        }

        ItemId[] itemIds = planned.Value.Placements
            .Select(placement => LivingMaterialEcologyProfiles.Get(
                placement.Species).ItemId)
            .ToArray();
        for (int index = 0; index < stableIds.Length; index++)
        {
            if (inventory.GetStack(stableIds[index]) != null)
            {
                return Result.Failure(LivingMaterialSeedConflict);
            }

            if (!inventory.Catalog.Contains(itemIds[index]))
            {
                return Result.Failure(LivingMaterialApplicationErrors.UnknownItem);
            }
        }

        for (int index = 0; index < stableIds.Length; index++)
        {
            Result added = inventory.AddUnit(
                stableIds[index],
                itemIds[index],
                ItemLocation.InWorld(planned.Value.Placements[index].Cell),
                tick);
            if (added.IsFailure)
            {
                throw new InvalidOperationException(
                    "Validated living material demo seed failed during Inventory commit: "
                    + added.Error);
            }
        }

        _inventoryRepository.Save(inventory);
        _journal.Append(inventory.DequeueUncommittedEvents());
        return Result.Success();
    }
}

}
