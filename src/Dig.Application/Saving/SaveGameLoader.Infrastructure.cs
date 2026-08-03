using System;
using Dig.Application.Rooms;
using Dig.Application.Tunnels;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public sealed partial class SaveGameLoader
{
    private static Result<RestoredInfrastructureRuntime> RestoreInfrastructure(
        SaveGameDocument document,
        InventoryState inventory,
        JobSystem jobs,
        WorldSize worldSize)
    {
        Result<TunnelInfrastructureRuntimeSnapshot> tunnel =
            TunnelInfrastructureSaveAdapter.Decode(
                document.TunnelInfrastructure,
                jobs);
        if (tunnel.IsFailure)
        {
            return Result<RestoredInfrastructureRuntime>.Failure(tunnel.Error!);
        }

        Result<RoomInfrastructureRuntimeSnapshot> room =
            RoomInfrastructureSaveAdapter.Decode(
                document.RoomInfrastructure,
                inventory,
                jobs,
                worldSize);
        if (room.IsFailure)
        {
            return Result<RestoredInfrastructureRuntime>.Failure(room.Error!);
        }

        return Result<RestoredInfrastructureRuntime>.Success(
            new RestoredInfrastructureRuntime(tunnel.Value, room.Value));
    }

    private sealed class RestoredInfrastructureRuntime
    {
        public RestoredInfrastructureRuntime(
            TunnelInfrastructureRuntimeSnapshot tunnel,
            RoomInfrastructureRuntimeSnapshot room)
        {
            Tunnel = tunnel ?? throw new ArgumentNullException(nameof(tunnel));
            Room = room ?? throw new ArgumentNullException(nameof(room));
        }

        public TunnelInfrastructureRuntimeSnapshot Tunnel { get; }
        public RoomInfrastructureRuntimeSnapshot Room { get; }
    }
}

}
