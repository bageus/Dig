using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.World;

namespace Dig.Presentation.Buildings
{

public readonly struct BuildingFootprintCellViewModel
{
    public BuildingFootprintCellViewModel(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }

    public int Y { get; }
}

public sealed class BuildingWorldViewModel
{
    public BuildingWorldViewModel(
        string id,
        string definitionId,
        string name,
        int originX,
        int originY,
        BuildingStatus status,
        long version,
        IReadOnlyCollection<BuildingFootprintCellViewModel> footprint,
        BuildingFunctionsViewModel functions)
    {
        if (string.IsNullOrWhiteSpace(id)
            || string.IsNullOrWhiteSpace(definitionId)
            || string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Building view identifiers and name are required.");
        }

        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        if (footprint is null || footprint.Count == 0)
        {
            throw new ArgumentException("Building footprint is required.", nameof(footprint));
        }

        Id = id.Trim();
        DefinitionId = definitionId.Trim();
        Name = name.Trim();
        OriginX = originX;
        OriginY = originY;
        Status = status;
        Version = version;
        Footprint = new ReadOnlyCollection<BuildingFootprintCellViewModel>(
            footprint.OrderBy(cell => cell.Y).ThenBy(cell => cell.X).ToArray());
        Functions = functions ?? throw new ArgumentNullException(nameof(functions));
    }

    public string Id { get; }

    public string DefinitionId { get; }

    public string Name { get; }

    public int OriginX { get; }

    public int OriginY { get; }

    public BuildingStatus Status { get; }

    public long Version { get; }

    public IReadOnlyList<BuildingFootprintCellViewModel> Footprint { get; }

    public BuildingFunctionsViewModel Functions { get; }

    public bool IsSelectable => Status == BuildingStatus.Completed;
}

public sealed class BuildingWorldPresenter
{
    private readonly BuildingFunctionsPresenter _functions;

    public BuildingWorldPresenter(BuildingFunctionsPresenter functions)
    {
        _functions = functions ?? throw new ArgumentNullException(nameof(functions));
    }

    public IReadOnlyList<BuildingWorldViewModel> Load(
        IReadOnlyCollection<BuildingSnapshot> snapshots)
    {
        if (snapshots is null)
        {
            throw new ArgumentNullException(nameof(snapshots));
        }

        BuildingWorldViewModel[] models = snapshots
            .Where(snapshot => snapshot.IsActive)
            .OrderBy(snapshot => snapshot.Id.ToString(), StringComparer.Ordinal)
            .Select(Present)
            .ToArray();
        return new ReadOnlyCollection<BuildingWorldViewModel>(models);
    }

    private BuildingWorldViewModel Present(BuildingSnapshot snapshot)
    {
        BuildingFootprintCellViewModel[] footprint = snapshot.Footprint
            .Select(cell => new BuildingFootprintCellViewModel(cell.X, cell.Y))
            .ToArray();
        return new BuildingWorldViewModel(
            snapshot.Id.ToString(),
            snapshot.Definition.Id.ToString(),
            snapshot.Definition.Name,
            snapshot.Origin.X,
            snapshot.Origin.Y,
            snapshot.Status,
            snapshot.Version,
            footprint,
            _functions.Present(snapshot));
    }
}
}
