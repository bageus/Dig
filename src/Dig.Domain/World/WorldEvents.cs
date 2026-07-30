using System;
using Dig.Domain.Core;

namespace Dig.Domain.World
{

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
public sealed class TerrainDepositRevealed : IDomainEvent
{
    public TerrainDepositRevealed(
        long tick,
        long worldVersion,
        string instanceId,
        string definitionId,
        CellId cell,
        long depositVersion)
    {
        ValidateDepositEvent(
            tick,
            worldVersion,
            instanceId,
            definitionId,
            depositVersion);
        Tick = tick;
        WorldVersion = worldVersion;
        InstanceId = instanceId;
        DefinitionId = definitionId;
        Cell = cell;
        DepositVersion = depositVersion;
    }

    public long Tick { get; }
    public long WorldVersion { get; }
    public string InstanceId { get; }
    public string DefinitionId { get; }
    public CellId Cell { get; }
    public long DepositVersion { get; }

    internal static void ValidateDepositEvent(
        long tick,
        long worldVersion,
        string instanceId,
        string definitionId,
        long depositVersion)
    {
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
        if (worldVersion <= 0) throw new ArgumentOutOfRangeException(nameof(worldVersion));
        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("Deposit instance id is required.", nameof(instanceId));
        if (string.IsNullOrWhiteSpace(definitionId))
            throw new ArgumentException("Deposit definition id is required.", nameof(definitionId));
        if (depositVersion < 0)
            throw new ArgumentOutOfRangeException(nameof(depositVersion));
    }
}

public sealed class TerrainDepositDepleted : IDomainEvent
{
    public TerrainDepositDepleted(
        long tick,
        long worldVersion,
        string instanceId,
        string definitionId,
        CellId cell,
        long depositVersion)
    {
        TerrainDepositRevealed.ValidateDepositEvent(
            tick,
            worldVersion,
            instanceId,
            definitionId,
            depositVersion);
        Tick = tick;
        WorldVersion = worldVersion;
        InstanceId = instanceId;
        DefinitionId = definitionId;
        Cell = cell;
        DepositVersion = depositVersion;
    }

    public long Tick { get; }
    public long WorldVersion { get; }
    public string InstanceId { get; }
    public string DefinitionId { get; }
    public CellId Cell { get; }
    public long DepositVersion { get; }
}

}
