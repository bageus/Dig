using System;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Presentation.Buildings
{

public sealed class BuildingBoxRelocationPlanViewModel
{
    public BuildingBoxRelocationPlanViewModel(
        EntityId jobId,
        EntityId stackId,
        ItemId itemId,
        CellId destination)
    {
        if (jobId.IsEmpty)
        {
            throw new ArgumentException("Job id is required.", nameof(jobId));
        }

        if (stackId.IsEmpty)
        {
            throw new ArgumentException("Stack id is required.", nameof(stackId));
        }

        if (itemId.IsEmpty)
        {
            throw new ArgumentException("Item id is required.", nameof(itemId));
        }

        JobId = jobId;
        StackId = stackId;
        ItemId = itemId;
        Destination = destination;
    }

    public EntityId JobId { get; }

    public EntityId StackId { get; }

    public ItemId ItemId { get; }

    public CellId Destination { get; }
}

}
