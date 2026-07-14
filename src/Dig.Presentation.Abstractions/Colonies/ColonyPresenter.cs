using System;
using Dig.Application.Colonies;
using Dig.Application.Messaging;
using Dig.Domain.Core;

namespace Dig.Presentation.Colonies
{

public sealed class ColonyPresenter
{
    private readonly IQueryHandler<GetColonySummaryQuery, Result<ColonySummary>> _queryHandler;

    public ColonyPresenter(
        IQueryHandler<GetColonySummaryQuery, Result<ColonySummary>> queryHandler)
    {
        _queryHandler = queryHandler ?? throw new ArgumentNullException(nameof(queryHandler));
    }

    public Result<ColonyViewModel> Load(EntityId colonyId)
    {
        Result<ColonySummary> result = _queryHandler.Handle(
            new GetColonySummaryQuery(colonyId));

        if (result.IsFailure)
        {
            return Result<ColonyViewModel>.Failure(result.Error!);
        }

        ColonySummary summary = result.Value;
        ColonyViewModel viewModel = new ColonyViewModel(
            summary.Id.ToString(),
            summary.Name,
            summary.Version);

        return Result<ColonyViewModel>.Success(viewModel);
    }
}

public sealed class ColonyViewModel
{
    public ColonyViewModel(string id, string displayName, long version)
    {
        Id = id;
        DisplayName = displayName;
        Version = version;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public long Version { get; }
}
}
