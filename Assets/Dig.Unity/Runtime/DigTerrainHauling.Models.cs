using Dig.Application.Inventory;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Unity
{

internal readonly struct DigStorageStatus
{
    public DigStorageStatus(
        CellId cell,
        int storedQuantity,
        int reservedIncomingQuantity,
        int capacity)
    {
        Cell = cell;
        StoredQuantity = storedQuantity;
        ReservedIncomingQuantity = reservedIncomingQuantity;
        Capacity = capacity;
    }

    public CellId Cell { get; }
    public int StoredQuantity { get; }
    public int ReservedIncomingQuantity { get; }
    public int Capacity { get; }
}

internal sealed partial class DigTerrainWorkSession
{
    private sealed class DemoHaulingJobIdSource : IHaulingJobIdSource
    {
        private ulong _jobSequence = 1;
        private ulong _splitSequence = 1;

        public EntityId Next()
        {
            return EntityId.Parse("9" + (_jobSequence++).ToString("x31"));
        }

        public EntityId NextSplitStackId()
        {
            return EntityId.Parse("a" + (_splitSequence++).ToString("x31"));
        }
    }

    private sealed class HaulingRoutePlan
    {
        public HaulingRoutePlan(CellId target, PathResult path)
        {
            Target = target;
            Path = path;
        }

        public CellId Target { get; }
        public PathResult Path { get; }
    }
}

}
