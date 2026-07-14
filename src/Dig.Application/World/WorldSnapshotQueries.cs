using System;
using Dig.Application.Messaging;
using Dig.Domain.World;

namespace Dig.Application.World
{

public sealed class GetWorldSnapshotQuery : IQuery<WorldSnapshot>
{
}

public sealed class GetWorldSnapshotQueryHandler
    : IQueryHandler<GetWorldSnapshotQuery, WorldSnapshot>
{
    private readonly IWorldRepository _repository;

    public GetWorldSnapshotQueryHandler(IWorldRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public WorldSnapshot Handle(GetWorldSnapshotQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        return _repository.Get().CreateSnapshot();
    }
}
}
