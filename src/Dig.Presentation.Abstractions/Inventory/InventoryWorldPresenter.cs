using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Dig.Application.Inventory;
using Dig.Domain.Inventory;

namespace Dig.Presentation.Inventory
{

public sealed class InventoryWorldPresenter
{
    private readonly GetInventorySnapshotQueryHandler _queryHandler;

    public InventoryWorldPresenter(GetInventorySnapshotQueryHandler queryHandler)
    {
        _queryHandler = queryHandler ?? throw new ArgumentNullException(nameof(queryHandler));
    }

    public IReadOnlyList<WorldItemViewModel> Load()
    {
        InventorySnapshot snapshot = _queryHandler.Handle(new GetInventorySnapshotQuery());
        WorldItemViewModel[] items = snapshot.Stacks
            .Where(stack => stack.Location.Kind == ItemLocationKind.World)
            .Select(stack => new WorldItemViewModel(
                stack.StackId.ToString(),
                stack.ItemId.ToString(),
                stack.Quantity,
                stack.ReservedQuantity,
                stack.Location.CellId.X,
                stack.Location.CellId.Y))
            .OrderBy(item => item.CellY)
            .ThenBy(item => item.CellX)
            .ThenBy(item => item.StackId, StringComparer.Ordinal)
            .ToArray();
        return new ReadOnlyCollection<WorldItemViewModel>(items);
    }
}

}
