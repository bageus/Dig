using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Domain.WorldObjects
{

internal sealed class BarrelRecord
{
    public BarrelRecord(
        EntityId barrelId,
        BarrelDefinitionId definitionId,
        CellId cell,
        BarrelLifecycle lifecycle,
        ItemId contentsItemId,
        long contentsGeneration,
        bool contentsMaterialized,
        CellId? fallSourceCell,
        CellId? fallLandingCell,
        long version)
    {
        BarrelId = barrelId;
        DefinitionId = definitionId;
        Cell = cell;
        Lifecycle = lifecycle;
        ContentsItemId = contentsItemId;
        ContentsGeneration = contentsGeneration;
        ContentsMaterialized = contentsMaterialized;
        FallSourceCell = fallSourceCell;
        FallLandingCell = fallLandingCell;
        Version = version;
    }

    public EntityId BarrelId { get; }
    public BarrelDefinitionId DefinitionId { get; }
    public CellId Cell { get; private set; }
    public BarrelLifecycle Lifecycle { get; private set; }
    public ItemId ContentsItemId { get; }
    public long ContentsGeneration { get; }
    public bool ContentsMaterialized { get; private set; }
    public CellId? FallSourceCell { get; private set; }
    public CellId? FallLandingCell { get; private set; }
    public long Version { get; private set; }

    public void BeginFall(CellId source, CellId landing)
    {
        Lifecycle = BarrelLifecycle.Falling;
        FallSourceCell = source;
        FallLandingCell = landing;
        Version = checked(Version + 1);
    }

    public void Land(CellId landing)
    {
        Cell = landing;
        Lifecycle = BarrelLifecycle.Supported;
        FallSourceCell = null;
        FallLandingCell = null;
        Version = checked(Version + 1);
    }

    public void Destroy()
    {
        Lifecycle = BarrelLifecycle.Destroyed;
        ContentsMaterialized = true;
        FallSourceCell = null;
        FallLandingCell = null;
        Version = checked(Version + 1);
    }

    public BarrelSnapshot Snapshot() => new BarrelSnapshot(
        BarrelId,
        DefinitionId,
        Cell,
        Lifecycle,
        ContentsItemId,
        ContentsGeneration,
        ContentsMaterialized,
        FallSourceCell,
        FallLandingCell,
        Version);
}

}