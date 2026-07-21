using System;
using Dig.Domain.Core;

namespace Dig.Domain.Buildings
{

public sealed class BuildingConstructionProgressed : IDomainEvent
{
    public BuildingConstructionProgressed(
        long tick,
        EntityId buildingId,
        int completedWork,
        int requiredWork)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (buildingId.IsEmpty)
        {
            throw new ArgumentException("Building id cannot be empty.", nameof(buildingId));
        }

        if (completedWork < 0 || requiredWork <= 0 || completedWork > requiredWork)
        {
            throw new ArgumentOutOfRangeException(nameof(completedWork));
        }

        Tick = tick;
        BuildingId = buildingId;
        CompletedWork = completedWork;
        RequiredWork = requiredWork;
    }

    public long Tick { get; }
    public EntityId BuildingId { get; }
    public int CompletedWork { get; }
    public int RequiredWork { get; }
}
}
