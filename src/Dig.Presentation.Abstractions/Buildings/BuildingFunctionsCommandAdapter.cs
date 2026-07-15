using System;
using Dig.Application.Buildings;
using Dig.Application.Messaging;
using Dig.Domain.Buildings;
using Dig.Domain.Core;

namespace Dig.Presentation.Buildings
{

public static class BuildingFunctionsCommandErrors
{
    public static readonly DomainError ActionUnavailable = new DomainError(
        "building.functions.action_unavailable",
        "The requested building function is not available in the current snapshot.");
}

public sealed class BuildingFunctionsCommandAdapter
{
    private readonly BuildingFunctionsPresenter _presenter;
    private readonly ICommandHandler<StartBuildingBoxPackingCommand, Result> _packingHandler;

    public BuildingFunctionsCommandAdapter(
        BuildingFunctionsPresenter presenter,
        ICommandHandler<StartBuildingBoxPackingCommand, Result> packingHandler)
    {
        _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
        _packingHandler = packingHandler
            ?? throw new ArgumentNullException(nameof(packingHandler));
    }

    public Result StartPacking(
        BuildingSnapshot snapshot,
        EntityId jobId,
        EntityId outputStackId,
        int priority,
        long tick)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        BuildingFunctionsViewModel model = _presenter.Present(snapshot);
        if (!_presenter.TryCreatePackingDraft(
            model,
            jobId,
            outputStackId,
            priority,
            tick,
            out BuildingPackingCommandDraft? draft))
        {
            return Result.Failure(BuildingFunctionsCommandErrors.ActionUnavailable);
        }

        return _packingHandler.Handle(_presenter.CreateCommand(draft!));
    }
}
}
