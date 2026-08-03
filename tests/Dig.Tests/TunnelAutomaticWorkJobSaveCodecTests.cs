using System.Linq;
using Dig.Application.Saving;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class TunnelAutomaticWorkJobSaveCodecTests
{
    [Theory]
    [InlineData(false, TunnelAutomaticWorkKind.WoodenSupport)]
    [InlineData(true, TunnelAutomaticWorkKind.WoodenSupport)]
    [InlineData(false, TunnelAutomaticWorkKind.JunctionStoneTrim)]
    [InlineData(true, TunnelAutomaticWorkKind.JunctionStoneTrim)]
    public void Codec_round_trip_preserves_kind_and_source(
        bool resolved,
        TunnelAutomaticWorkKind kind)
    {
        EntityId jobId = Id(1);
        EntityId segmentId = Id(2);
        EntityId sourceId = Id(3);
        TunnelAutomaticWorkJobDefinition definition =
            new TunnelAutomaticWorkJobDefinition(
                jobId,
                segmentId,
                kind,
                new CellId(15, 4, 2),
                createdTick: 9,
                new JobRetryPolicy(maximumRetries: 4, retryDelayTicks: 7),
                resolved ? sourceId : (EntityId?)null,
                resolved ? new CellId(3, 4, 2) : (CellId?)null,
                new[] { Id(4) });
        TunnelAutomaticWorkJobSaveCodec codec =
            new TunnelAutomaticWorkJobSaveCodec();

        JobDefinitionSaveData encoded = codec.Encode(definition);
        TunnelAutomaticWorkJobDefinition decoded =
            Assert.IsType<TunnelAutomaticWorkJobDefinition>(codec.Decode(encoded));

        Assert.Equal(jobId, decoded.Id);
        Assert.Equal(segmentId, decoded.SegmentId);
        Assert.Equal(definition.Kind, decoded.Kind);
        Assert.Equal(definition.RequiredItemId, decoded.RequiredItemId);
        Assert.Equal(definition.TargetCell, decoded.TargetCell);
        Assert.Equal(definition.SourceStackId, decoded.SourceStackId);
        Assert.Equal(definition.SourceCell, decoded.SourceCell);
        Assert.Equal(definition.CreatedTick, decoded.CreatedTick);
        Assert.Equal(
            definition.RetryPolicy.MaximumRetries,
            decoded.RetryPolicy.MaximumRetries);
        Assert.Equal(
            definition.RetryPolicy.RetryDelayTicks,
            decoded.RetryPolicy.RetryDelayTicks);
        Assert.Equal(definition.Dependencies.ToArray(), decoded.Dependencies.ToArray());
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}
}
