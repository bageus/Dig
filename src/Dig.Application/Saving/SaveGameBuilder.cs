using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public sealed class SaveGameBuilder
{
    private readonly JobDefinitionSaveRegistry _jobDefinitions;

    public SaveGameBuilder(JobDefinitionSaveRegistry jobDefinitions)
    {
        _jobDefinitions = jobDefinitions
            ?? throw new ArgumentNullException(nameof(jobDefinitions));
    }

    public SaveGameDocument Build(SaveGameContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        ValidateMetadata(context.Metadata);
        return new SaveGameDocument
        {
            FormatVersion = SaveFormat.CurrentVersion,
            Metadata = CopyMetadata(context.Metadata),
            World = BuildWorld(context.World.CreateSnapshot()),
            Inventory = BuildInventory(context.Inventory.CreateSnapshot()),
            Jobs = BuildJobs(context.Jobs),
        };
    }

    private static WorldSaveData BuildWorld(WorldSnapshot snapshot)
    {
        WorldSaveData data = new WorldSaveData
        {
            Width = snapshot.Size.Width,
            Height = snapshot.Size.Height,
            ChunkSize = snapshot.ChunkSize,
            Version = snapshot.Version,
        };
        foreach (ChunkSnapshot chunk in snapshot.Chunks.OrderBy(item => item.Id))
        {
            WorldChunkSaveData savedChunk = new WorldChunkSaveData
            {
                X = chunk.Id.X,
                Y = chunk.Id.Y,
                Version = chunk.ChunkVersion,
            };
            foreach (CellSnapshot cell in chunk.Cells.OrderBy(item => item.Id))
            {
                savedChunk.Cells.Add(new WorldCellSaveData
                {
                    X = cell.Id.X,
                    Y = cell.Id.Y,
                    MaterialId = cell.State.MaterialId.ToString(),
                    Designation = (int)cell.State.Designation,
                    IsExplored = cell.State.IsExplored,
                    Damage = cell.State.Damage,
                    Temperature = cell.State.Temperature,
                });
            }

            data.Chunks.Add(savedChunk);
        }

        return data;
    }

    private static InventorySaveData BuildInventory(InventorySnapshot snapshot)
    {
        InventorySaveData data = new InventorySaveData
        {
            Version = snapshot.Version,
        };
        foreach (ItemStackSnapshot stack in snapshot.Stacks
            .OrderBy(item => item.StackId.ToString(), StringComparer.Ordinal))
        {
            ItemStackSaveData saved = new ItemStackSaveData
            {
                StackId = stack.StackId.ToString(),
                ItemId = stack.ItemId.ToString(),
                Quantity = stack.Quantity,
                Location = BuildLocation(stack.Location),
            };
            foreach (ItemReservationSnapshot reservation in stack.Reservations
                .OrderBy(item => item.JobId.ToString(), StringComparer.Ordinal))
            {
                saved.Reservations.Add(new ItemReservationSaveData
                {
                    JobId = reservation.JobId.ToString(),
                    Quantity = reservation.Quantity,
                });
            }

            data.Stacks.Add(saved);
        }

        return data;
    }

    private JobsSaveData BuildJobs(JobSystem jobs)
    {
        JobsSaveData data = new JobsSaveData();
        foreach (JobSnapshot job in jobs.GetAll()
            .OrderBy(item => item.Id.ToString(), StringComparer.Ordinal))
        {
            data.Jobs.Add(new JobSaveData
            {
                Definition = _jobDefinitions.Encode(job.Definition),
                Status = (int)job.Status,
                Stage = (int)job.Stage,
                AssignedAgentId = job.AssignedAgentId?.ToString(),
                RetryCount = job.RetryCount,
                NextRetryTick = job.NextRetryTick,
                Version = job.Version,
                ReasonCode = job.Reason?.Code,
                ReasonMessage = job.Reason?.Message,
            });
        }

        foreach (ReservationSnapshot reservation in jobs.GetReservations()
            .OrderBy(item => item.JobId.ToString(), StringComparer.Ordinal)
            .ThenBy(item => item.Key))
        {
            data.Reservations.Add(new JobReservationSaveData
            {
                JobId = reservation.JobId.ToString(),
                AgentId = reservation.AgentId.ToString(),
                Kind = (int)reservation.Key.Kind,
                Value = reservation.Key.Value,
                CreatedTick = reservation.CreatedTick,
            });
        }

        return data;
    }

    private static ItemLocationSaveData BuildLocation(ItemLocation location)
    {
        return new ItemLocationSaveData
        {
            Kind = (int)location.Kind,
            OwnerId = location.OwnerId?.ToString(),
            CellX = location.CellId?.X,
            CellY = location.CellId?.Y,
        };
    }

    private static SaveMetadataData CopyMetadata(SaveMetadataData metadata)
    {
        return new SaveMetadataData
        {
            SlotId = metadata.SlotId,
            DisplayName = metadata.DisplayName,
            SavedAtUtc = metadata.SavedAtUtc,
            SimulationTick = metadata.SimulationTick,
            WorldSeed = metadata.WorldSeed,
            GeneratorVersion = metadata.GeneratorVersion,
        };
    }

    private static void ValidateMetadata(SaveMetadataData metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata.SlotId)
            || string.IsNullOrWhiteSpace(metadata.DisplayName)
            || string.IsNullOrWhiteSpace(metadata.SavedAtUtc)
            || metadata.SimulationTick < 0
            || metadata.GeneratorVersion <= 0)
        {
            throw new InvalidOperationException("Save metadata is incomplete or invalid.");
        }
    }
}
}
