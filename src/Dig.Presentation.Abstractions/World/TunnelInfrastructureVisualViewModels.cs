using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.World;

namespace Dig.Presentation.World
{

public enum TunnelInfrastructureVisualKind
{
    WoodenSupport = 0,
    JunctionStoneTrim = 1,
}

public sealed class TunnelInfrastructureVisualViewModel
{
    public TunnelInfrastructureVisualViewModel(
        string instanceId,
        TunnelInfrastructureVisualKind kind,
        CellId cell)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException(
                "Tunnel infrastructure visual requires a stable instance id.",
                nameof(instanceId));
        }

        if (!Enum.IsDefined(typeof(TunnelInfrastructureVisualKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        InstanceId = instanceId;
        Kind = kind;
        Cell = cell;
    }

    public string InstanceId { get; }

    public TunnelInfrastructureVisualKind Kind { get; }

    public CellId Cell { get; }
}

public sealed class TunnelInfrastructureVisualVolumeViewModel
{
    public TunnelInfrastructureVisualVolumeViewModel(
        long version,
        IReadOnlyCollection<TunnelInfrastructureVisualViewModel> instances)
    {
        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        if (instances == null)
        {
            throw new ArgumentNullException(nameof(instances));
        }

        Version = version;
        Instances = new ReadOnlyCollection<TunnelInfrastructureVisualViewModel>(
            instances
                .OrderBy(value => value.Kind)
                .ThenBy(value => value.Cell)
                .ThenBy(value => value.InstanceId, StringComparer.Ordinal)
                .ToArray());
    }

    public long Version { get; }

    public IReadOnlyList<TunnelInfrastructureVisualViewModel> Instances { get; }

    public static TunnelInfrastructureVisualVolumeViewModel Empty()
    {
        return new TunnelInfrastructureVisualVolumeViewModel(
            version: 0,
            Array.Empty<TunnelInfrastructureVisualViewModel>());
    }
}

}
