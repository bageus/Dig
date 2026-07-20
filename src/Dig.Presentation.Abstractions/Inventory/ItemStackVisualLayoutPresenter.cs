using System;
using System.Collections.Generic;

namespace Dig.Presentation.Inventory
{

public sealed class ItemStackVisualLayoutPresenter
{
    public const int MaximumVisibleInstances = 4;

    private const ulong OffsetBasis = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    public ItemStackVisualLayoutViewModel Present(WorldItemViewModel model)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        if (string.IsNullOrWhiteSpace(model.StackId)
            || string.IsNullOrWhiteSpace(model.ItemId)
            || model.Quantity <= 0
            || model.ReservedQuantity < 0
            || model.ReservedQuantity > model.Quantity)
        {
            throw new ArgumentException("World item visual facts are invalid.", nameof(model));
        }

        ItemStackQuantityBand band = ResolveBand(model.Quantity);
        ItemReservationVisualState reservation = ResolveReservation(
            model.Quantity,
            model.ReservedQuantity);
        ulong seed = Hash(model.StackId, model.ItemId, (int)band);
        ItemStackLayoutInstanceViewModel[] instances = CreateInstances(band, seed);
        if (instances.Length > MaximumVisibleInstances)
        {
            throw new InvalidOperationException("Item stack layout exceeded its hard bound.");
        }

        long layoutVersion = ToVersion(HashLayout(model.StackId, model.ItemId, band, instances));
        long version = ToVersion(HashVisualFacts(
            layoutVersion,
            model.Quantity,
            model.ReservedQuantity,
            model.InteractionKind));
        return new ItemStackVisualLayoutViewModel(
            model.StackId,
            model.ItemId,
            model.Quantity,
            model.ReservedQuantity,
            band,
            reservation,
            instances,
            layoutVersion,
            version);
    }

    public static ItemStackQuantityBand ResolveBand(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (quantity == 1)
        {
            return ItemStackQuantityBand.Single;
        }

        if (quantity <= 4)
        {
            return ItemStackQuantityBand.Small;
        }

        return quantity <= 9
            ? ItemStackQuantityBand.Medium
            : ItemStackQuantityBand.Large;
    }

    private static ItemReservationVisualState ResolveReservation(
        int quantity,
        int reservedQuantity)
    {
        if (reservedQuantity == 0)
        {
            return ItemReservationVisualState.None;
        }

        return reservedQuantity == quantity
            ? ItemReservationVisualState.Full
            : ItemReservationVisualState.Partial;
    }

    private static ItemStackLayoutInstanceViewModel[] CreateInstances(
        ItemStackQuantityBand band,
        ulong seed)
    {
        (int X, int Y)[] offsets = band switch
        {
            ItemStackQuantityBand.Single => new[] { (0, 0) },
            ItemStackQuantityBand.Small => new[] { (-18, 0), (18, 0) },
            ItemStackQuantityBand.Medium => new[] { (-20, -12), (20, -12), (0, 19) },
            ItemStackQuantityBand.Large => new[]
            {
                (-20, -18),
                (20, -18),
                (-20, 18),
                (20, 18),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(band)),
        };
        int baseScale = band switch
        {
            ItemStackQuantityBand.Single => 100,
            ItemStackQuantityBand.Small => 88,
            ItemStackQuantityBand.Medium => 78,
            ItemStackQuantityBand.Large => 70,
            _ => 100,
        };
        int variant = (int)(seed % 4UL);
        ItemStackLayoutInstanceViewModel[] result =
            new ItemStackLayoutInstanceViewModel[offsets.Length];
        for (int index = 0; index < offsets.Length; index++)
        {
            (int x, int y) = Rotate(offsets[index].X, offsets[index].Y, variant);
            int scaleVariation = (int)((seed >> (index * 3)) & 3UL) - 1;
            result[index] = new ItemStackLayoutInstanceViewModel(
                index,
                x,
                y,
                baseScale + scaleVariation,
                (variant + index) % 4);
        }

        return result;
    }

    private static (int X, int Y) Rotate(int x, int y, int quarterTurns)
    {
        return quarterTurns switch
        {
            0 => (x, y),
            1 => (-y, x),
            2 => (-x, -y),
            3 => (y, -x),
            _ => throw new ArgumentOutOfRangeException(nameof(quarterTurns)),
        };
    }

    private static ulong Hash(string stackId, string itemId, int band)
    {
        ulong hash = OffsetBasis;
        hash = Append(hash, stackId);
        hash = Append(hash, itemId);
        return Append(hash, band);
    }

    private static ulong HashLayout(
        string stackId,
        string itemId,
        ItemStackQuantityBand band,
        IReadOnlyList<ItemStackLayoutInstanceViewModel> instances)
    {
        ulong hash = Hash(stackId, itemId, (int)band);
        for (int index = 0; index < instances.Count; index++)
        {
            ItemStackLayoutInstanceViewModel instance = instances[index];
            hash = Append(hash, instance.Index);
            hash = Append(hash, instance.OffsetXPercent);
            hash = Append(hash, instance.OffsetYPercent);
            hash = Append(hash, instance.ScalePercent);
            hash = Append(hash, instance.QuarterTurns);
        }

        return hash;
    }

    private static ulong HashVisualFacts(
        long layoutVersion,
        int quantity,
        int reservedQuantity,
        WorldItemInteractionKind interactionKind)
    {
        ulong hash = Append(OffsetBasis, layoutVersion);
        hash = Append(hash, quantity);
        hash = Append(hash, reservedQuantity);
        return Append(hash, (int)interactionKind);
    }

    private static ulong Append(ulong hash, string value)
    {
        for (int index = 0; index < value.Length; index++)
        {
            hash ^= value[index];
            hash *= Prime;
        }

        return hash;
    }

    private static ulong Append(ulong hash, long value)
    {
        unchecked
        {
            hash ^= (ulong)value;
            return hash * Prime;
        }
    }

    private static long ToVersion(ulong value)
    {
        return (long)(value & (ulong)long.MaxValue);
    }
}
}
