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
    private readonly ItemId? _interactiveItemId;

    public InventoryWorldPresenter(
        GetInventorySnapshotQueryHandler queryHandler,
        WorldItemInteractionKind interactionKind = WorldItemInteractionKind.None,
        ItemId? interactiveItemId = null)
    {
        _queryHandler = queryHandler ?? throw new ArgumentNullException(nameof(queryHandler));
        if (!Enum.IsDefined(typeof(WorldItemInteractionKind), interactionKind))
        {
            throw new ArgumentOutOfRangeException(nameof(interactionKind));
        }

        if (interactiveItemId.HasValue && interactiveItemId.Value.IsEmpty)
        {
            throw new ArgumentException(
                "Interactive item id cannot be empty.",
                nameof(interactiveItemId));
        }

        _interactionKind = interactionKind;
        _interactiveItemId = interactiveItemId;
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
                ResolveInteraction(stack.ItemId)))
            .OrderBy(item => item.CellY)
            .ThenBy(item => item.CellX)
            .ThenBy(item => item.StackId, StringComparer.Ordinal)
            .ToArray();
        return new ReadOnlyCollection<WorldItemViewModel>(items);
    }

    private WorldItemInteractionKind ResolveInteraction(ItemId itemId)
    {
        return !_interactiveItemId.HasValue || _interactiveItemId.Value == itemId
            ? _interactionKind
            : WorldItemInteractionKind.None;
    }
}
}
