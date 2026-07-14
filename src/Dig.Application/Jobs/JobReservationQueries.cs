using System;
using System.Collections.Generic;
using Dig.Application.Messaging;
using Dig.Domain.Jobs;

namespace Dig.Application.Jobs
{

public sealed class GetJobReservationsQuery : IQuery<IReadOnlyList<ReservationSnapshot>>
{
}

public sealed class GetJobReservationsHandler
    : IQueryHandler<GetJobReservationsQuery, IReadOnlyList<ReservationSnapshot>>
{
    private readonly IJobRepository _repository;

    public GetJobReservationsHandler(IJobRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public IReadOnlyList<ReservationSnapshot> Handle(GetJobReservationsQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        return _repository.Get().GetReservations();
    }
}
}