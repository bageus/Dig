using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Domain.Agents;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Domain.Jobs
{

public sealed class HaulJobDefinition : JobDefinition
{
    private static readonly JobStageKind[] HaulStages =
    {
        JobStageKind.AcquireItem,
        JobStageKind.TravelToDestination,
        JobStageKind.DepositItem,
    };

    public HaulJobDefinition(
        EntityId id,
        EntityId sourceStackId,
        ItemId itemId,
        int quantity,
        EntityId destinationStorageId,
        int priority,
        long createdTick,
        JobRetryPolicy retryPolicy,
        IEnumerable<EntityId>? dependencies = null,
        SkillGrantProfile? skillGrantProfile = null)
        : this(
            id,
            sourceStackId,
            itemId,
            quantity,
            ItemLocation.InStorage(destinationStorageId),
            priority,
            createdTick,
            retryPolicy,
            dependencies,
            skillGrantProfile)
    {
    }

    public HaulJobDefinition(
        EntityId id,
        EntityId sourceStackId,
        ItemId itemId,
        int quantity,
        ItemLocation destination,
        int priority,
        long createdTick,
        JobRetryPolicy retryPolicy,
        IEnumerable<EntityId>? dependencies = null,
        SkillGrantProfile? skillGrantProfile = null)
        : base(id, priority, createdTick, retryPolicy, HaulStages, dependencies)
    {
        if (sourceStackId.IsEmpty)
        {
            throw new ArgumentException("Source stack id cannot be empty.", nameof(sourceStackId));
        }

        if (itemId.IsEmpty)
        {
            throw new ArgumentException("Item id cannot be empty.", nameof(itemId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (!destination.HasOwner && !destination.HasCell)
        {
            throw new ArgumentException("Hauling destination is required.", nameof(destination));
        }

        SourceStackId = sourceStackId;
        ItemId = itemId;
        Quantity = quantity;
        Destination = destination;
        SkillGrantProfile = skillGrantProfile
            ?? DefaultSkillProgressionContent.Catalog.GetProfile(
                DefaultSkillGrantProfileIds.Logistics);
    }

    public EntityId SourceStackId { get; }

    public ItemId ItemId { get; }

    public int Quantity { get; }

    public ItemLocation Destination { get; }

    public SkillGrantProfile SkillGrantProfile { get; }

    public EntityId DestinationStorageId =>
        Destination.Kind == ItemLocationKind.Storage && Destination.HasOwner
            ? Destination.OwnerId
            : default;

    public override string Description =>
        $"Haul:{Quantity} {ItemId} {SourceStackId}->{Destination}";

    public override IReadOnlyList<ReservationKey> CreateReservationKeys()
    {
        return new ReadOnlyCollection<ReservationKey>(Array.Empty<ReservationKey>());
    }
}
}
