using System;
using Dig.Application.Rooms;
using Dig.Domain.Core;
using Dig.Domain.Rooms;
using Dig.Infrastructure.InMemory;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    internal RoomInfrastructureRuntimeSnapshot CaptureRoomInfrastructureRuntimeState()
    {
        EnsureRoomInfrastructureRuntime();
        return new RoomInfrastructureRuntimeSnapshot(
            _roomInfrastructure!.Get().CaptureSnapshot(),
            _roomProvenance.Values,
            _roomRuntimeSequence);
    }

    internal Result RestoreRoomInfrastructureRuntimeState(
        RoomInfrastructureRuntimeSnapshot runtime)
    {
        if (runtime == null)
        {
            throw new ArgumentNullException(nameof(runtime));
        }

        Result<RoomInfrastructureState> restored =
            RoomInfrastructureState.Restore(runtime.Infrastructure);
        if (restored.IsFailure)
        {
            return Result.Failure(restored.Error!);
        }

        _roomInfrastructure = new InMemoryRoomInfrastructureRepository(
            restored.Value);
        _roomProvenance.Clear();
        for (int index = 0; index < runtime.Provenance.Count; index++)
        {
            CompletedRoomInfrastructureProvenance source =
                runtime.Provenance[index];
            _roomProvenance.Add(source.TemplateInstanceId, source);
        }

        _roomRuntimeSequence = runtime.NextRuntimeSequence;
        ComposeRoomInfrastructureHandlers();
        return Result.Success();
    }
}

}
