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
    private readonly WorldItemInteractionKind _interactionKind;

    public InventoryWorldPresenter(
        GetInventorySnapshotQueryHandler queryHandler,
        WorldItemInteractionKind interactionKind = WorldItemInteractionKind.None)
    {
        _queryHandler = queryHandler ?? throw new ArgumentNullException(nameof(queryHandler));
        if (!Enum.IsDefined(typeof(WorldItemInteractionKind), interactionKind))
        {
            throw new ArgumentOutOfRangeException(nameof(interactionKind));
        }

        _interactionKind = interactionKind;
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
                stack.Location.CellId.Y,
                _interactionKind))
            .OrderBy(item => item.CellY)
            .ThenBy(item => item.CellX)
            .ThenBy(item => item.StackId, StringComparer.Ordinal)
            .ToArray();
        return new ReadOnlyCollection<WorldItemViewModel>(items);
    }
}

}
