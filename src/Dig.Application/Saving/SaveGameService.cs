using System;
using System.Collections.Generic;
using Dig.Application.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
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
            context.TerrainDeposits));
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
        IAgentRepository agents)
    {
        return RestoreAgents(
            Load(slotId, materials, items, buildingCatalog),
            agents);
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
        foreach (KeyValuePair<EntityId, Dig.Domain.World.CellId> entry
            in loaded.Value.AgentPositions)
        {
            if (repository.Get(entry.Key) is null)
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

        foreach (KeyValuePair<EntityId, Dig.Domain.World.CellId> entry
            in loaded.Value.AgentPositions)
        {
            Dig.Domain.Agents.AgentState agent = repository.Get(entry.Key)!;
            Result restored = agent.RestorePosition(entry.Value);
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
