using System;
using Dig.Domain.World;

namespace Dig.Application.Tunnels
{

public sealed class TunnelInfrastructureRuntimeSnapshot
{
    public TunnelInfrastructureRuntimeSnapshot(
        TunnelInfrastructureSnapshot infrastructure,
        ulong nextAutomaticJobSequence)
    {
        Infrastructure = infrastructure
            ?? throw new ArgumentNullException(nameof(infrastructure));
        if (nextAutomaticJobSequence == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nextAutomaticJobSequence));
        }

        NextAutomaticJobSequence = nextAutomaticJobSequence;
    }

    public TunnelInfrastructureSnapshot Infrastructure { get; }

    public ulong NextAutomaticJobSequence { get; }

    public static TunnelInfrastructureRuntimeSnapshot Empty()
    {
        return new TunnelInfrastructureRuntimeSnapshot(
            new TunnelInfrastructureState().CaptureSnapshot(),
            nextAutomaticJobSequence: 1UL);
    }
}

}
