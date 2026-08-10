using System;
using Dig.Application.Tunnels;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    internal TunnelInfrastructureRuntimeSnapshot
        CaptureTunnelInfrastructureRuntimeState()
    {
        EnsureTunnelInfrastructureRuntime();
        return new TunnelInfrastructureRuntimeSnapshot(
            _tunnelInfrastructure!.Get().CaptureSnapshot(),
            _tunnelAutomaticJobSequence,
            _tunnelManualJobSequence);
    }

    internal Result RestoreTunnelInfrastructureRuntimeState(
        TunnelInfrastructureRuntimeSnapshot runtime)
    {
        if (runtime == null)
        {
            throw new ArgumentNullException(nameof(runtime));
        }

        Result<TunnelInfrastructureState> restored =
            TunnelInfrastructureState.Restore(runtime.Infrastructure);
        if (restored.IsFailure)
        {
            return Result.Failure(restored.Error!);
        }

        _tunnelInfrastructure = new InMemoryTunnelInfrastructureRepository(
            restored.Value);
        _tunnelTopologySync = new SynchronizeTunnelTopologyHandler(
            _tunnelInfrastructure,
            _inventoryRepository,
            _jobRepository,
            _journal);
        _tunnelSupportSync = new SynchronizeTunnelAutomaticSupportHandler(
            _tunnelInfrastructure,
            _inventoryRepository,
            _jobRepository,
            _journal);
        _tunnelTrimPlacementSync = new SynchronizeTunnelJunctionTrimPlacementHandler(
            _tunnelInfrastructure,
            _inventoryRepository,
            _jobRepository,
            _journal);
        _tunnelWorkCompletion = new CompleteTunnelAutomaticWorkHandler(
            _tunnelInfrastructure,
            _inventoryRepository,
            _jobRepository,
            _journal,
            _skillGrants);
        _tunnelAutomaticJobSequence = runtime.NextAutomaticJobSequence;
        _tunnelManualJobSequence = runtime.NextManualJobSequence;
        ResetTunnelManualRuntimeHandlers();
        PublishTunnelInfrastructureVisuals();
        return Result.Success();
    }
}

}
