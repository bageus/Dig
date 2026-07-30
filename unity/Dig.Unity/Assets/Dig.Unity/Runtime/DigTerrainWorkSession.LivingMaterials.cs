using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Ecology;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
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

    private InMemoryLivingMaterialEcologyRepository? _livingMaterials;
    private AdvanceLivingMaterialEcologyCommandHandler? _livingMaterialAdvance;
    private readonly LivingMaterialCreatureVisualProjector _livingMaterialVisuals =
        new LivingMaterialCreatureVisualProjector();
    private readonly LivingMaterialCampfireTetherProjector _livingMaterialTethers =
        new LivingMaterialCampfireTetherProjector();

    internal void InitializeLivingMaterials(long tick)
    {
        if (_livingMaterials != null)
        {
            return;
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
        return _livingMaterialAdvance.Handle(
            new AdvanceLivingMaterialEcologyCommand(tick, residentCells));
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
}

}
