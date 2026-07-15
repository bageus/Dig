using System;
using Dig.Domain.Inventory;

namespace Dig.Domain.Buildings
{

public enum BuildingConstructionPolicyKind
{
    LegacyMaterials = 0,
    BuildingBox = 1,
}

public sealed class BuildingBoxPolicy
{
    public BuildingBoxPolicy(
        ItemId boxItemId,
        int packingWork,
        bool packingEnabled = true)
    {
        if (boxItemId.IsEmpty)
        {
            throw new ArgumentException("Building box item id cannot be empty.", nameof(boxItemId));
        }

        if (packingWork <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(packingWork));
        }

        BoxItemId = boxItemId;
        PackingWork = packingWork;
        PackingEnabled = packingEnabled;
    }

    public ItemId BoxItemId { get; }

    public int PackingWork { get; }

    public bool PackingEnabled { get; }
}

public enum BuildingBoxCommitState
{
    None = 0,
    Reserved = 1,
    AtSite = 2,
    Consumed = 3,
}
}