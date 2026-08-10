using Dig.Domain.Inventory;
using Dig.Presentation.Management;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private readonly SettlementItemSummaryPresenter _itemSummaryPresenter =
        new SettlementItemSummaryPresenter();

    internal SettlementItemSummaryViewModel LoadItemSummary()
    {
        InventoryState inventory = _inventoryRepository.Get();
        return _itemSummaryPresenter.Present(
            inventory.CreateSnapshot(),
            inventory.Catalog,
            LoadBuildings());
    }
}

}
