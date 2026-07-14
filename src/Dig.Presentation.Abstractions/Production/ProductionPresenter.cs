using System.Collections.ObjectModel;
using Dig.Domain.Production;
using Dig.Domain.Technology;

namespace Dig.Presentation.Production;

public sealed class ProductionOrderView
{
    public ProductionOrderView(
        string id,
        string recipe,
        string buildingId,
        string status,
        int completedWork,
        int requiredWork,
        IReadOnlyCollection<string> reservedInputs,
        string? reason)
    {
        Id = id;
        Recipe = recipe;
        BuildingId = buildingId;
        Status = status;
        CompletedWork = completedWork;
        RequiredWork = requiredWork;
        ReservedInputs = new ReadOnlyCollection<string>(reservedInputs.ToArray());
        Reason = reason;
    }

    public string Id { get; }

    public string Recipe { get; }

    public string BuildingId { get; }

    public string Status { get; }

    public int CompletedWork { get; }

    public int RequiredWork { get; }

    public IReadOnlyList<string> ReservedInputs { get; }

    public string? Reason { get; }
}

public sealed class TechnologyView
{
    public TechnologyView(long version, IReadOnlyCollection<string> unlocked)
    {
        Version = version;
        Unlocked = new ReadOnlyCollection<string>(unlocked.ToArray());
    }

    public long Version { get; }

    public IReadOnlyList<string> Unlocked { get; }
}

public sealed class ProductionPresenter
{
    public ProductionOrderView Present(ProductionOrderSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        return new ProductionOrderView(
            snapshot.Id.ToString(),
            snapshot.Recipe.Id.ToString(),
            snapshot.BuildingId.ToString(),
            snapshot.Status.ToString(),
            snapshot.CompletedWork,
            snapshot.Recipe.RequiredWork,
            snapshot.InputAllocations
                .Select(value => $"{value.Quantity} {value.ItemId} @ {value.StackId}")
                .ToArray(),
            snapshot.Reason);
    }

    public TechnologyView Present(TechnologySnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        return new TechnologyView(
            snapshot.Version,
            snapshot.UnlockedTechnologies.Select(value => value.ToString()).ToArray());
    }
}
