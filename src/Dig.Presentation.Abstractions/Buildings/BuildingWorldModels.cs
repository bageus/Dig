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
        : this(x, y, CellId.MinimumDepth)
    {
    }

    public BuildingFootprintCellViewModel(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public int X { get; }
    public int Y { get; }
    public int Z { get; }
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
        : this(
            id,
            definitionId,
            name,
            originX,
            originY,
            CellId.MinimumDepth,
            BuildingOrientation.North,
            originX,
            originY,
            CellId.MinimumDepth,
            status,
            status == BuildingStatus.Completed || status == BuildingStatus.Damaged ? 1 : 0,
            1,
            version,
            footprint,
            functions)
    {
    }

    public BuildingWorldViewModel(
        string id,
        string definitionId,
        string name,
        int originX,
        int originY,
        BuildingOrientation orientation,
        int workPositionX,
        int workPositionY,
        BuildingStatus status,
        int completedWork,
        int requiredWork,
        long version,
        IReadOnlyCollection<BuildingFootprintCellViewModel> footprint,
        BuildingFunctionsViewModel functions)
        : this(
            id,
            definitionId,
            name,
            originX,
            originY,
            CellId.MinimumDepth,
            orientation,
            workPositionX,
            workPositionY,
            CellId.MinimumDepth,
            status,
            completedWork,
            requiredWork,
            version,
            footprint,
            functions)
    {
    }

    public BuildingWorldViewModel(
        string id,
        string definitionId,
        string name,
        int originX,
        int originY,
        int originZ,
        BuildingOrientation orientation,
        int workPositionX,
        int workPositionY,
        int workPositionZ,
        BuildingStatus status,
        int completedWork,
        int requiredWork,
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

        if (!Enum.IsDefined(typeof(BuildingOrientation), orientation))
        {
            throw new ArgumentOutOfRangeException(nameof(orientation));
        }

        if (originZ < CellId.MinimumDepth || originZ > CellId.MaximumDepth)
        {
            throw new ArgumentOutOfRangeException(nameof(originZ));
        }

        if (workPositionZ < CellId.MinimumDepth || workPositionZ > CellId.MaximumDepth)
        {
            throw new ArgumentOutOfRangeException(nameof(workPositionZ));
        }

        if (completedWork < 0
            || requiredWork <= 0
            || completedWork > requiredWork)
        {
            throw new ArgumentOutOfRangeException(nameof(completedWork));
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
        OriginZ = originZ;
        Orientation = orientation;
        WorkPositionX = workPositionX;
        WorkPositionY = workPositionY;
        WorkPositionZ = workPositionZ;
        Status = status;
        CompletedWork = completedWork;
        RequiredWork = requiredWork;
        Version = version;
        Footprint = new ReadOnlyCollection<BuildingFootprintCellViewModel>(
            footprint.OrderBy(cell => cell.Z)
                .ThenBy(cell => cell.Y)
                .ThenBy(cell => cell.X)
                .ToArray());
        Functions = functions ?? throw new ArgumentNullException(nameof(functions));
    }

    public string Id { get; }
    public string DefinitionId { get; }
    public string Name { get; }
    public int OriginX { get; }
    public int OriginY { get; }
    public int OriginZ { get; }
    public BuildingOrientation Orientation { get; }
    public int WorkPositionX { get; }
    public int WorkPositionY { get; }
    public int WorkPositionZ { get; }
    public BuildingStatus Status { get; }
    public int CompletedWork { get; }
    public int RequiredWork { get; }
    public long Version { get; }
    public IReadOnlyList<BuildingFootprintCellViewModel> Footprint { get; }
    public BuildingFunctionsViewModel Functions { get; }
    public bool IsSelectable => Status == BuildingStatus.Completed;
    public double AssemblyProgress => Math.Min(1d, (double)CompletedWork / RequiredWork);
    public BuildingVisualState VisualState => BuildingVisualStateResolver.Resolve(
        Status,
        Functions.IsPacking);
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
            .Select(cell => new BuildingFootprintCellViewModel(cell.X, cell.Y, cell.Z))
            .ToArray();
        return new BuildingWorldViewModel(
            snapshot.Id.ToString(),
            snapshot.Definition.Id.ToString(),
            snapshot.Definition.Name,
            snapshot.Origin.X,
            snapshot.Origin.Y,
            snapshot.Origin.Z,
            snapshot.Orientation,
            snapshot.WorkPosition.X,
            snapshot.WorkPosition.Y,
            snapshot.WorkPosition.Z,
            snapshot.Status,
            snapshot.CompletedWork,
            snapshot.Definition.RequiredWork,
            snapshot.Version,
            footprint,
            _functions.Present(snapshot));
    }
}
}
