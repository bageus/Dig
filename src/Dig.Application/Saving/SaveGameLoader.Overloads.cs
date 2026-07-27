using Dig.Domain.Buildings;
using Dig.Domain.Inventory;
using Dig.Domain.WorldObjects;

namespace Dig.Application.Saving
{

public sealed partial class SaveGameLoader
{
    public SaveGameLoader(
        ItemCatalog itemCatalog,
        IItemEffectCatalog itemEffectCatalog,
        BuildingCatalog? buildingCatalog,
        BarrelCatalog? barrelCatalog,
        params ISaveMigration[] migrations)
        : this(
            itemCatalog,
            itemEffectCatalog,
            buildingCatalog,
            mushroomCatalog: null,
            barrelCatalog,
            jobCodecs: null,
            migrations)
    {
    }

    public SaveGameLoader(
        ItemCatalog itemCatalog,
        IItemEffectCatalog itemEffectCatalog,
        BuildingCatalog? buildingCatalog,
        BarrelCatalog? barrelCatalog,
        IJobDefinitionSaveCodec[] jobCodecs,
        params ISaveMigration[] migrations)
        : this(
            itemCatalog,
            itemEffectCatalog,
            buildingCatalog,
            mushroomCatalog: null,
            barrelCatalog,
            jobCodecs,
            migrations)
    {
    }
}

}