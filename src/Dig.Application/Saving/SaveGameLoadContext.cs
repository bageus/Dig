using System;
using Dig.Application.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Ecology;
using Dig.Domain.Inventory;
using Dig.Domain.Production;
using Dig.Domain.World;
using Dig.Domain.WorldObjects;

namespace Dig.Application.Saving
{

public sealed class SaveGameLoadContext
{
    public SaveGameLoadContext(
        MaterialCatalog materials,
        ItemCatalog items,
        BuildingCatalog buildings,
        TerrainDepositCatalog terrainDeposits,
        MushroomCatalog mushrooms,
        ProductionContentCatalog production,
        BarrelCatalog barrels,
        IAgentRepository agents)
    {
        Materials = materials ?? throw new ArgumentNullException(nameof(materials));
        Items = items ?? throw new ArgumentNullException(nameof(items));
        Buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
        TerrainDeposits = terrainDeposits
            ?? throw new ArgumentNullException(nameof(terrainDeposits));
        Mushrooms = mushrooms ?? throw new ArgumentNullException(nameof(mushrooms));
        Production = production ?? throw new ArgumentNullException(nameof(production));
        Barrels = barrels ?? throw new ArgumentNullException(nameof(barrels));
        Agents = agents ?? throw new ArgumentNullException(nameof(agents));
    }

    public MaterialCatalog Materials { get; }
    public ItemCatalog Items { get; }
    public BuildingCatalog Buildings { get; }
    public TerrainDepositCatalog TerrainDeposits { get; }
    public MushroomCatalog Mushrooms { get; }
    public ProductionContentCatalog Production { get; }
    public BarrelCatalog Barrels { get; }
    public IAgentRepository Agents { get; }
}

}