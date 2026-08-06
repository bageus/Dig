using System;
using Dig.Domain.World;

namespace Dig.Application.Tunnels
{

public sealed class TunnelInfrastructureRuntimeSnapshot
{
    public TunnelInfrastructureRuntimeSnapshot(
        TunnelInfrastructureSnapshot infrastructure,
        ulong nextAutomaticJobSequence,
        ulong nextManualJobSequence = 1UL)
    {
        Infrastructure = infrastructure
            ?? throw new ArgumentNullException(nameof(infrastructure));
        if (nextAutomaticJobSequence == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nextAutomaticJobSequence));
        }

        if (nextManualJobSequence == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(nextManualJobSequence));
        }

        NextAutomaticJobSequence = nextAutomaticJobSequence;
        NextManualJobSequence = nextManualJobSequence;
    }

    public TunnelInfrastructureSnapshot Infrastructure { get; }

    public ulong NextAutomaticJobSequence { get; }

    public ulong NextManualJobSequence { get; }

    public static TunnelInfrastructureRuntimeSnapshot Empty()
    {
        return new TunnelInfrastructureRuntimeSnapshot(
            new TunnelInfrastructureState().CaptureSnapshot(),
            nextAutomaticJobSequence: 1UL);
    }
}

}
