using System;
using Dig.Application.Messaging;
using Dig.Domain.Inventory;

namespace Dig.Application.Inventory
{

public sealed class GetInventorySnapshotQuery : IQuery<InventorySnapshot>
{
}

public sealed class GetInventorySnapshotQueryHandler
    : IQueryHandler<GetInventorySnapshotQuery, InventorySnapshot>
{
    private readonly IInventoryRepository _repository;

    public GetInventorySnapshotQueryHandler(IInventoryRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public InventorySnapshot Handle(GetInventorySnapshotQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        return _repository.Get().CreateSnapshot();
    }
}

}
