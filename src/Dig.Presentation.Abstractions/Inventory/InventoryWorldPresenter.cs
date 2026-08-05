using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Application.Inventory;
using Dig.Domain.Inventory;

namespace Dig.Presentation.Inventory
{

public sealed class InventoryWorldPresenter
{
    private readonly GetInventorySnapshotQueryHandler _queryHandler;
    private readonly ItemCatalog _catalog;

    public InventoryWorldPresenter(
        GetInventorySnapshotQueryHandler queryHandler,
        ItemCatalog catalog)
    {
        _queryHandler = queryHandler ?? throw new ArgumentNullException(nameof(queryHandler));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public IReadOnlyList<WorldItemViewModel> Load()
    {
        InventorySnapshot snapshot = _queryHandler.Handle(new GetInventorySnapshotQuery());
        WorldItemViewModel[] items = snapshot.Stacks
            .Where(stack => stack.Location.Kind == ItemLocationKind.World)
            .Select(stack =>
            {
                ItemDefinition definition = _catalog.Get(stack.ItemId);
                return new WorldItemViewModel(
                    stack.StackId.ToString(),
                    stack.ItemId.ToString(),
                    stack.Quantity,
                    stack.ReservedQuantity,
                    stack.Location.CellId.X,
                    stack.Location.CellId.Y,
                    stack.Location.CellId.Z,
                    definition.Interactions,
                    definition.DisplayName);
            })
            .OrderBy(item => item.CellZ)
            .ThenBy(item => item.CellY)
            .ThenBy(item => item.CellX)
            .ThenBy(item => item.StackId, StringComparer.Ordinal)
            .ToArray();
        return new ReadOnlyCollection<WorldItemViewModel>(items);
    }
}

}
