using Dig.Domain.Core;

namespace Dig.Domain.World;

public sealed class CellChanged : IDomainEvent
{
    public CellChanged(
        long tick,
        long worldVersion,
        CellId cellId,
        CellState previousState,
        CellState currentState)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (worldVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(worldVersion));
        }

        Tick = tick;
        WorldVersion = worldVersion;
        CellId = cellId;
        PreviousState = previousState;
        CurrentState = currentState;
    }

    public long Tick { get; }

    public long WorldVersion { get; }

    public CellId CellId { get; }

    public CellState PreviousState { get; }

    public CellState CurrentState { get; }
}

public sealed class ChunkInvalidated : IDomainEvent
{
    public ChunkInvalidated(
        long tick,
        long worldVersion,
        ChunkId chunkId,
        long chunkVersion)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (worldVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(worldVersion));
        }

        if (chunkVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkVersion));
        }

        Tick = tick;
        WorldVersion = worldVersion;
        ChunkId = chunkId;
        ChunkVersion = chunkVersion;
    }

    public long Tick { get; }

    public long WorldVersion { get; }

    public ChunkId ChunkId { get; }

    public long ChunkVersion { get; }
}
