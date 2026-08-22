using System;
using Dig.Application.Rooms;
using Dig.Application.Tunnels;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Domain.Storage;

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
        Result<StorageState> storage = StorageSaveAdapter.Decode(document.Storage);
        if (storage.IsFailure)
        {
            return Result<RestoredInfrastructureRuntime>.Failure(storage.Error!);
        }

        Result<TunnelInfrastructureRuntimeSnapshot> tunnel =
            TunnelInfrastructureSaveAdapter.Decode(
                document.TunnelInfrastructure,
                jobs,
                inventory);
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
            new RestoredInfrastructureRuntime(tunnel.Value, room.Value, storage.Value));
    }

    private sealed class RestoredInfrastructureRuntime
    {
        public RestoredInfrastructureRuntime(
            TunnelInfrastructureRuntimeSnapshot tunnel,
            RoomInfrastructureRuntimeSnapshot room,
            StorageState storage)
        {
            Tunnel = tunnel ?? throw new ArgumentNullException(nameof(tunnel));
            Room = room ?? throw new ArgumentNullException(nameof(room));
            Storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public TunnelInfrastructureRuntimeSnapshot Tunnel { get; }
        public RoomInfrastructureRuntimeSnapshot Room { get; }
        public StorageState Storage { get; }
    }
}

}
