using System;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Ecology;

namespace Dig.Application.Ecology
{

public sealed class AdvanceMushroomGrowthCommandHandler
    : ICommandHandler<AdvanceMushroomGrowthCommand, Result>
{
    private readonly IMushroomRepository _mushrooms;
    private readonly IEventSink _events;

    public AdvanceMushroomGrowthCommandHandler(
        IMushroomRepository mushrooms,
        IEventSink events)
    {
        _mushrooms = mushrooms ?? throw new ArgumentNullException(nameof(mushrooms));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public Result Handle(AdvanceMushroomGrowthCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        MushroomState state = _mushrooms.Get();
        Result result = state.AdvanceGrowth(command.Tick);
        if (result.IsFailure)
        {
            return result;
        }

        _mushrooms.Save(state);
        _events.Append(state.DequeueUncommittedEvents());
        return Result.Success();
    }
}

}
