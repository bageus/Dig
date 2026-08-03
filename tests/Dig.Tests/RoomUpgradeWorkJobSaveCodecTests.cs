using System.Linq;
using Dig.Application.Saving;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class RoomUpgradeWorkJobSaveCodecTests
{
    [Fact]
    public void Codec_round_trip_preserves_room_work_identity_and_xyz()
    {
        RoomUpgradeWorkJobDefinition definition = new RoomUpgradeWorkJobDefinition(
            Id(1),
            Id(2),
            new CellId(7, 9, 3),
            priority: 450,
            createdTick: 12,
            new JobRetryPolicy(maximumRetries: 5, retryDelayTicks: 8),
            new[] { Id(3) });
        RoomUpgradeWorkJobSaveCodec codec = new RoomUpgradeWorkJobSaveCodec();

        RoomUpgradeWorkJobDefinition restored =
            Assert.IsType<RoomUpgradeWorkJobDefinition>(
                codec.Decode(codec.Encode(definition)));

        Assert.Equal(definition.Id, restored.Id);
        Assert.Equal(definition.RoomInfrastructureId, restored.RoomInfrastructureId);
        Assert.Equal(definition.WorkCell, restored.WorkCell);
        Assert.Equal(definition.Priority, restored.Priority);
        Assert.Equal(definition.CreatedTick, restored.CreatedTick);
        Assert.Equal(
            definition.RetryPolicy.MaximumRetries,
            restored.RetryPolicy.MaximumRetries);
        Assert.Equal(
            definition.RetryPolicy.RetryDelayTicks,
            restored.RetryPolicy.RetryDelayTicks);
        Assert.Equal(definition.Dependencies.ToArray(), restored.Dependencies.ToArray());
        Assert.Equal(
            definition.CreateReservationKeys(),
            restored.CreateReservationKeys());
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}
