using System;
using System.Collections.Generic;
using Dig.Application.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Content;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public sealed class SaveGameService
{
    private readonly SaveGameBuilder _builder;
    private readonly SaveGameLoader _loader;
    private readonly ISaveSlotStore _store;

    public SaveGameService(
        SaveGameBuilder builder,
        SaveGameLoader loader,
        ISaveSlotStore store)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public SaveGameDocument Save(SaveGameContext context)
    {
        SaveGameDocument document = _builder.Build(context);
        _store.Save(document.Metadata.SlotId, document);
        return document;
    }

    public SaveGameDocument Autosave(SaveGameContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        SaveMetadataData metadata = new SaveMetadataData
        {
            SlotId = SaveSlotNames.Autosave,
            DisplayName = "Autosave",
            SavedAtUtc = context.Metadata.SavedAtUtc,
            SimulationTick = context.Metadata.SimulationTick,
            WorldSeed = context.Metadata.WorldSeed,
            GeneratorVersion = context.Metadata.GeneratorVersion,
        };
        return Save(new SaveGameContext(
            metadata,
            context.World,
            context.Inventory,
            context.Jobs,
            context.Buildings,
            context.Agents,
            context.TerrainDeposits,
            context.PackableBuildingExecutions,
            context.MiningOutputCommits,
            context.Mushrooms,
            context.Production,
            context.BuildingSupply,
            context.Barrels,
            context.Combat));
    }

    public Result<LoadedGameState> Load(
        string slotId,
        MaterialCatalog materials,
        ItemCatalog items)
    {
        SaveGameDocument document = _store.Load(slotId);
        return _loader.Load(document, materials, items);
    }

    public Result<LoadedGameState> Load(
        string slotId,
        MaterialCatalog materials,
        ItemCatalog items,
        TerrainDepositCatalog terrainDeposits)
    {
        SaveGameDocument document = _store.Load(slotId);
        return _loader.Load(
            document,
            materials,
            items,
            buildingCatalog: null,
            terrainDeposits);
    }

    public Result<LoadedGameState> Load(
        string slotId,
        MaterialCatalog materials,
        ItemCatalog items,
        IAgentRepository agents)
    {
        return RestoreAgents(Load(slotId, materials, items), agents);
    }

    public Result<LoadedGameState> Load(
        string slotId,
        MaterialCatalog materials,
        ItemCatalog items,
        BuildingCatalog buildingCatalog)
    {
        if (buildingCatalog is null)
        {
            throw new ArgumentNullException(nameof(buildingCatalog));
        }

        SaveGameDocument document = _store.Load(slotId);
        return _loader.Load(document, materials, items, buildingCatalog);
    }

    public Result<LoadedGameState> Load(
        string slotId,
        MaterialCatalog materials,
        ItemCatalog items,
        MushroomCatalog mushrooms)
    {
        if (mushrooms is null)
        {
            throw new ArgumentNullException(nameof(mushrooms));
        }

        SaveGameDocument document = _store.Load(slotId);
        return _loader.Load(
            document,
            materials,
            items,
            buildingCatalog: null,
            terrainDepositCatalog: null,
            mushroomCatalog: mushrooms);
    }

    public Result<LoadedGameState> Load(
        string slotId,
        MaterialCatalog materials,
        ItemCatalog items,
        BuildingCatalog buildingCatalog,
        TerrainDepositCatalog terrainDeposits)
    {
        if (buildingCatalog is null)
        {
            throw new ArgumentNullException(nameof(buildingCatalog));
        }

        SaveGameDocument document = _store.Load(slotId);
        return _loader.Load(
            document,
            materials,
            items,
            buildingCatalog,
            terrainDeposits);
    }

    public Result<LoadedGameState> Load(
        string slotId,
        MaterialCatalog materials,
        ItemCatalog items,
        BuildingCatalog buildingCatalog,
        TerrainDepositCatalog terrainDeposits,
        MushroomCatalog mushrooms)
    {
        if (buildingCatalog is null
            || terrainDeposits is null
            || mushrooms is null)
        {
            throw new ArgumentNullException(
                buildingCatalog is null
                    ? nameof(buildingCatalog)
                    : terrainDeposits is null
                        ? nameof(terrainDeposits)
                        : nameof(mushrooms));
        }

        SaveGameDocument document = _store.Load(slotId);
        return _loader.Load(
            document,
            materials,
            items,
            buildingCatalog,
            terrainDeposits,
            mushrooms);
    }

    public Result<LoadedGameState> Load(
        string slotId,
        MaterialCatalog materials,
        ItemCatalog items,
        BuildingCatalog buildingCatalog,
        IAgentRepository agents)
    {
        return RestoreAgents(
            Load(slotId, materials, items, buildingCatalog),
            agents);
    }

    public Result<LoadedGameState> Load(
        string slotId,
        MaterialCatalog materials,
        ItemCatalog items,
        BuildingCatalog buildingCatalog,
        TerrainDepositCatalog terrainDeposits,
        MushroomCatalog mushrooms,
        ProductionContentCatalog productionContent)
    {
        if (buildingCatalog is null || terrainDeposits is null
            || mushrooms is null || productionContent is null)
        {
            throw new ArgumentNullException(nameof(productionContent));
        }

        SaveGameDocument document = _store.Load(slotId);
        return _loader.Load(
            document,
            materials,
            items,
            buildingCatalog,
            terrainDeposits,
            mushrooms,
            productionContent);
    }

    public Result<LoadedGameState> Load(
        string slotId,
        SaveGameLoadContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        SaveGameDocument document = _store.Load(slotId);
        return RestoreAgents(
            _loader.Load(
                document,
                context.Materials,
                context.Items,
                context.Buildings,
                context.TerrainDeposits,
                context.Mushrooms,
                context.Production,
                context.Barrels,
                context.CombatWeapons),
            context.Agents);
    }

    public IReadOnlyList<SaveSlotInfo> ListSlots()
    {
        return _store.List();
    }

    private static Result<LoadedGameState> RestoreAgents(
        Result<LoadedGameState> loaded,
        IAgentRepository agents)
    {
        if (loaded.IsFailure)
        {
            return loaded;
        }

        IAgentRepository repository = agents
            ?? throw new ArgumentNullException(nameof(agents));
        HashSet<EntityId> referencedAgents = new HashSet<EntityId>(
            loaded.Value.AgentPositions.Keys);
        referencedAgents.UnionWith(loaded.Value.AgentRuntime.Keys);
        foreach (EntityId agentId in referencedAgents)
        {
            if (repository.Get(agentId) is null)
            {
                return Result<LoadedGameState>.Failure(
                    AgentApplicationErrors.NotFound);
            }
        }

        Result skills = new LoadedAgentSkillProgressionRestorer(repository)
            .Restore(loaded.Value);
        if (skills.IsFailure)
        {
            return Result<LoadedGameState>.Failure(skills.Error!);
        }

        foreach (KeyValuePair<EntityId, Dig.Domain.Agents.AgentRuntimeSnapshot> entry
            in loaded.Value.AgentRuntime)
        {
            Dig.Domain.Agents.AgentState agent = repository.Get(entry.Key)!;
            Result restored = agent.RestoreRuntime(entry.Value);
            if (restored.IsFailure)
            {
                return Result<LoadedGameState>.Failure(restored.Error!);
            }

            repository.Save(agent);
        }

        foreach (KeyValuePair<EntityId, Dig.Domain.Navigation.SurfacePose> entry
            in loaded.Value.AgentSurfacePoses)
        {
            Dig.Domain.Agents.AgentState agent = repository.Get(entry.Key)!;
            Result restored = agent.RestoreSurfacePose(entry.Value);
            if (restored.IsFailure)
            {
                return Result<LoadedGameState>.Failure(restored.Error!);
            }

            repository.Save(agent);
        }

        return loaded;
    }
}

}
