using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Jobs
{

public sealed class ProductionWorkJobDefinition : JobDefinition
{
    private static readonly JobStageKind[] WorkStages =
    {
        JobStageKind.TravelToTarget,
        JobStageKind.PerformWork,
        JobStageKind.Finalize,
    };

    public ProductionWorkJobDefinition(
        EntityId id,
        EntityId orderId,
        EntityId buildingId,
        RecipeId recipeId,
        CellId workPosition,
        int priority,
        long createdTick,
        JobRetryPolicy retryPolicy)
        : base(id, priority, createdTick, retryPolicy, WorkStages, dependencies: null)
    {
        if (orderId.IsEmpty || buildingId.IsEmpty || recipeId.IsEmpty)
        {
            throw new ArgumentException("Production job references cannot be empty.");
        }

        OrderId = orderId;
        BuildingId = buildingId;
        RecipeId = recipeId;
        WorkPosition = workPosition;
    }

    public EntityId OrderId { get; }

    public EntityId BuildingId { get; }

    public RecipeId RecipeId { get; }

    public CellId WorkPosition { get; }

    public override string Description =>
        $"Produce:{RecipeId}:{OrderId}@{WorkPosition}";

    public override IReadOnlyList<ReservationKey> CreateReservationKeys()
    {
        return new ReadOnlyCollection<ReservationKey>(new[]
        {
            ReservationKey.ForPosition(WorkPosition),
        });
    }
}
}
