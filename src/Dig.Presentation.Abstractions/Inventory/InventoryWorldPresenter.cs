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
    private readonly WorldItemInteractionKind _defaultInteractionKind;
    private readonly IReadOnlyDictionary<ItemId, WorldItemInteractionKind>
        _interactionOverrides;

    public InventoryWorldPresenter(
        GetInventorySnapshotQueryHandler queryHandler,
        WorldItemInteractionKind interactionKind = WorldItemInteractionKind.None,
        ItemId? interactiveItemId = null,
        WorldItemInteractionKind fallbackInteractionKind = WorldItemInteractionKind.None)
    {
        _queryHandler = queryHandler ?? throw new ArgumentNullException(nameof(queryHandler));
        ValidateInteractionKind(interactionKind, nameof(interactionKind));
        ValidateInteractionKind(
            fallbackInteractionKind,
            nameof(fallbackInteractionKind));

        Dictionary<ItemId, WorldItemInteractionKind> overrides =
            new Dictionary<ItemId, WorldItemInteractionKind>();
        if (interactiveItemId.HasValue)
        {
            if (interactiveItemId.Value.IsEmpty)
            {
                throw new ArgumentException(
                    "Interactive item id cannot be empty.",
                    nameof(interactiveItemId));
            }

            overrides.Add(interactiveItemId.Value, interactionKind);
            _defaultInteractionKind = fallbackInteractionKind;
        }
        else
        {
            _defaultInteractionKind = interactionKind;
        }

        _interactionOverrides = overrides;
    }

    public InventoryWorldPresenter(
        GetInventorySnapshotQueryHandler queryHandler,
        IReadOnlyDictionary<ItemId, WorldItemInteractionKind> interactionOverrides,
        WorldItemInteractionKind fallbackInteractionKind)
    {
        _queryHandler = queryHandler ?? throw new ArgumentNullException(nameof(queryHandler));
        if (interactionOverrides == null)
        {
            throw new ArgumentNullException(nameof(interactionOverrides));
        }

        ValidateInteractionKind(
            fallbackInteractionKind,
            nameof(fallbackInteractionKind));
        Dictionary<ItemId, WorldItemInteractionKind> overrides =
            new Dictionary<ItemId, WorldItemInteractionKind>();
        foreach (KeyValuePair<ItemId, WorldItemInteractionKind> pair
            in interactionOverrides)
        {
            if (pair.Key.IsEmpty)
            {
                throw new ArgumentException(
                    "Interaction override item ids cannot be empty.",
                    nameof(interactionOverrides));
            }

            ValidateInteractionKind(pair.Value, nameof(interactionOverrides));
            overrides.Add(pair.Key, pair.Value);
        }

        _defaultInteractionKind = fallbackInteractionKind;
        _interactionOverrides = overrides;
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
                stack.Location.CellId.Z,
                ResolveInteraction(stack.ItemId)))
            .OrderBy(item => item.CellZ)
            .ThenBy(item => item.CellY)
            .ThenBy(item => item.CellX)
            .ThenBy(item => item.StackId, StringComparer.Ordinal)
            .ToArray();
        return new ReadOnlyCollection<WorldItemViewModel>(items);
    }

    private WorldItemInteractionKind ResolveInteraction(ItemId itemId)
    {
        return _interactionOverrides.TryGetValue(
            itemId,
            out WorldItemInteractionKind interaction)
            ? interaction
            : _defaultInteractionKind;
    }

    private static void ValidateInteractionKind(
        WorldItemInteractionKind interactionKind,
        string parameterName)
    {
        if (!Enum.IsDefined(typeof(WorldItemInteractionKind), interactionKind))
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}

}
