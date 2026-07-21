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
            context.Agents));
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
        IAgentRepository agents)
    {
        return RestoreAgentSkills(Load(slotId, materials, items), agents);
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
        IAgentRepository agents)
    {
        return RestoreAgentSkills(
            Load(slotId, materials, items, buildingCatalog),
            agents);
    }

    public IReadOnlyList<SaveSlotInfo> ListSlots()
    {
        return _store.List();
    }

    private static Result<LoadedGameState> RestoreAgentSkills(
        Result<LoadedGameState> loaded,
        IAgentRepository agents)
    {
        if (loaded.IsFailure)
        {
            return loaded;
        }

        Result restored = new LoadedAgentSkillProgressionRestorer(
            agents ?? throw new ArgumentNullException(nameof(agents)))
            .Restore(loaded.Value);
        return restored.IsFailure
            ? Result<LoadedGameState>.Failure(restored.Error!)
            : loaded;
    }
}
}
