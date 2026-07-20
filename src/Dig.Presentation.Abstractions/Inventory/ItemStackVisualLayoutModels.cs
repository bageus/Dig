using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace Dig.Presentation.Inventory
{

public enum ItemStackQuantityBand
{
    Single = 0,
    Small = 1,
    Medium = 2,
    Large = 3,
}

public enum ItemReservationVisualState
{
    None = 0,
    Partial = 1,
    Full = 2,
}

public readonly struct ItemStackLayoutInstanceViewModel
    : IEquatable<ItemStackLayoutInstanceViewModel>
{
    public ItemStackLayoutInstanceViewModel(
        int index,
        int offsetXPercent,
        int offsetYPercent,
        int scalePercent,
        int quarterTurns)
    {
        if (index < 0
            || offsetXPercent < -50
            || offsetXPercent > 50
            || offsetYPercent < -50
            || offsetYPercent > 50
            || scalePercent < 40
            || scalePercent > 120
            || quarterTurns < 0
            || quarterTurns > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        Index = index;
        OffsetXPercent = offsetXPercent;
        OffsetYPercent = offsetYPercent;
        ScalePercent = scalePercent;
        QuarterTurns = quarterTurns;
    }

    public int Index { get; }
    public int OffsetXPercent { get; }
    public int OffsetYPercent { get; }
    public int ScalePercent { get; }
    public int QuarterTurns { get; }

    public bool Equals(ItemStackLayoutInstanceViewModel other)
    {
        return Index == other.Index
            && OffsetXPercent == other.OffsetXPercent
            && OffsetYPercent == other.OffsetYPercent
            && ScalePercent == other.ScalePercent
            && QuarterTurns == other.QuarterTurns;
    }

    public override bool Equals(object? obj)
    {
        return obj is ItemStackLayoutInstanceViewModel other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Index,
            OffsetXPercent,
            OffsetYPercent,
            ScalePercent,
            QuarterTurns);
    }
}

public sealed class ItemStackVisualLayoutViewModel
{
    public ItemStackVisualLayoutViewModel(
        string stackId,
        string itemId,
        int quantity,
        int reservedQuantity,
        ItemStackQuantityBand quantityBand,
        ItemReservationVisualState reservationState,
        IReadOnlyCollection<ItemStackLayoutInstanceViewModel> instances,
        long layoutVersion,
        long version)
    {
        if (string.IsNullOrWhiteSpace(stackId) || string.IsNullOrWhiteSpace(itemId))
        {
            throw new ArgumentException("Stack and item ids are required.");
        }

        if (quantity <= 0
            || reservedQuantity < 0
            || reservedQuantity > quantity
            || !Enum.IsDefined(typeof(ItemStackQuantityBand), quantityBand)
            || !Enum.IsDefined(typeof(ItemReservationVisualState), reservationState)
            || layoutVersion < 0
            || version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (instances is null || instances.Count == 0 || instances.Count > 4)
        {
            throw new ArgumentException(
                "A stack layout requires one to four visible instances.",
                nameof(instances));
        }

        ItemStackLayoutInstanceViewModel[] ordered = instances
            .OrderBy(instance => instance.Index)
            .ToArray();
        if (ordered.Select(instance => instance.Index).Distinct().Count()
            != ordered.Length)
        {
            throw new ArgumentException("Stack layout instance indices must be unique.");
        }

        StackId = stackId.Trim();
        ItemId = itemId.Trim();
        Quantity = quantity;
        ReservedQuantity = reservedQuantity;
        QuantityBand = quantityBand;
        ReservationState = reservationState;
        Instances = new ReadOnlyCollection<ItemStackLayoutInstanceViewModel>(ordered);
        LayoutVersion = layoutVersion;
        Version = version;
    }

    public string StackId { get; }
    public string ItemId { get; }
    public int Quantity { get; }
    public int ReservedQuantity { get; }
    public int AvailableQuantity => Quantity - ReservedQuantity;
    public ItemStackQuantityBand QuantityBand { get; }
    public ItemReservationVisualState ReservationState { get; }
    public IReadOnlyList<ItemStackLayoutInstanceViewModel> Instances { get; }
    public int VisibleInstanceCount => Instances.Count;
    public string QuantityBadge => Quantity.ToString(CultureInfo.InvariantCulture);
    public long LayoutVersion { get; }
    public long Version { get; }
}
}
