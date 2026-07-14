using Dig.Application.Messaging;
using Dig.Application.World;
using Dig.Domain.Buildings;
using Dig.Domain.Core;

namespace Dig.Application.Buildings;

public sealed class PlaceBuildingHandler : ICommandHandler<PlaceBuildingCommand, Result>
{
    private readonly BuildingCatalog _catalog;
    private readonly IWorldRepository _worldRepository;
    private readonly IBuildingsRepository _buildingsRepository;
    private readonly BuildingPlacementValidator _validator;
    private readonly IEventSink _eventSink;

    public PlaceBuildingHandler(
        BuildingCatalog catalog,
        IWorldRepository worldRepository,
        IBuildingsRepository buildingsRepository,
        BuildingPlacementValidator validator,
        IEventSink eventSink)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _worldRepository = worldRepository
            ?? throw new ArgumentNullException(nameof(worldRepository));
        _buildingsRepository = buildingsRepository
            ?? throw new ArgumentNullException(nameof(buildingsRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(PlaceBuildingCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        BuildingsState buildings = _buildingsRepository.Get();
        BuildingDefinition definition = _catalog.Get(command.DefinitionId);
        BuildingPlacementResult placement = _validator.Validate(
            definition,
            command.Origin,
            command.Orientation,
            _worldRepository.Get().CreateSnapshot(),
            buildings.GetOccupiedCells(),
            command.ReachableCells);
        Result result = buildings.Place(
            command.BuildingId,
            definition,
            command.Origin,
            command.Orientation,
            placement,
            command.Tick);
        if (result.IsFailure)
        {
            return result;
        }

        _buildingsRepository.Save(buildings);
        _eventSink.Append(buildings.DequeueUncommittedEvents());
        return Result.Success();
    }
}

public sealed class GetBuildingHandler : IQueryHandler<GetBuildingQuery, BuildingSnapshot?>
{
    private readonly IBuildingsRepository _repository;

    public GetBuildingHandler(IBuildingsRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public BuildingSnapshot? Handle(GetBuildingQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        return _repository.Get().Get(query.BuildingId);
    }
}
