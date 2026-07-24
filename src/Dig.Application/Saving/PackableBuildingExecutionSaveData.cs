using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Dig.Application.Buildings;
using Dig.Domain.Buildings;
using Dig.Domain.Core;

namespace Dig.Application.Saving
{

[DataContract]
public sealed class PackableBuildingIterationClockSaveData
{
    [DataMember(Order = 1)]
    public string WorkerId { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public long StartTick { get; set; }

    [DataMember(Order = 3)]
    public int DurationSeconds { get; set; }
}

[DataContract]
public sealed class PackableBuildingExecutionSaveData
{
    [DataMember(Order = 1)]
    public string OperationId { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string PackageId { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public string DefinitionId { get; set; } = string.Empty;

    [DataMember(Order = 4)]
    public int Operation { get; set; }

    [DataMember(Order = 5)]
    public int Status { get; set; }

    [DataMember(Order = 6)]
    public int TotalIterations { get; set; }

    [DataMember(Order = 7)]
    public int CompletedIterations { get; set; }

    [DataMember(Order = 8)]
    public string? ActiveWorkerId { get; set; }

    [DataMember(Order = 9)]
    public List<string> CompletedByWorkers { get; set; } = new List<string>();

    [DataMember(Order = 10)]
    public PackableBuildingIterationClockSaveData? IterationClock { get; set; }
}

[DataContract]
public sealed class PackableBuildingExecutionsSaveData
{
    public const int CurrentFormatVersion = 1;

    [DataMember(Order = 1)]
    public int FormatVersion { get; set; } = CurrentFormatVersion;

    [DataMember(Order = 2)]
    public List<PackableBuildingExecutionSaveData> Executions { get; set; } =
        new List<PackableBuildingExecutionSaveData>();
}

public static class PackableBuildingExecutionSaveDataAdapter
{
    public static PackableBuildingExecutionsSaveData Encode(
        PackableBuildingExecutionRegistry registry)
    {
        if (registry == null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        PackableBuildingExecutionsSaveData data = new PackableBuildingExecutionsSaveData();
        foreach (PackableBuildingExecutionSnapshot snapshot in registry.CreateSnapshot())
        {
            PackableBuildingExecutionSaveData saved = new PackableBuildingExecutionSaveData
            {
                OperationId = snapshot.OperationId.ToString(),
                PackageId = snapshot.PackageId.ToString(),
                DefinitionId = snapshot.DefinitionId.ToString(),
                Operation = (int)snapshot.Operation,
                Status = (int)snapshot.Status,
                TotalIterations = snapshot.TotalIterations,
                CompletedIterations = snapshot.CompletedIterations,
                ActiveWorkerId = snapshot.ActiveWorkerId?.ToString(),
            };
            saved.CompletedByWorkers.AddRange(
                snapshot.CompletedByWorkers.Select(value => value.ToString()));
            if (snapshot.IterationClock is not null)
            {
                saved.IterationClock = new PackableBuildingIterationClockSaveData
                {
                    WorkerId = snapshot.IterationClock.WorkerId.ToString(),
                    StartTick = snapshot.IterationClock.StartTick,
                    DurationSeconds = snapshot.IterationClock.DurationSeconds,
                };
            }

            data.Executions.Add(saved);
        }

        return data;
    }

    public static Result<PackableBuildingExecutionRegistry> Decode(
        PackableBuildingExecutionsSaveData data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        try
        {
            if (data.FormatVersion != PackableBuildingExecutionsSaveData.CurrentFormatVersion
                || data.Executions == null)
            {
                return Result<PackableBuildingExecutionRegistry>.Failure(
                    SaveErrors.InvalidDocument);
            }

            List<PackableBuildingExecutionSnapshot> snapshots =
                new List<PackableBuildingExecutionSnapshot>();
            foreach (PackableBuildingExecutionSaveData saved in data.Executions.OrderBy(
                value => value.OperationId,
                StringComparer.Ordinal))
            {
                if (saved == null
                    || saved.CompletedByWorkers == null
                    || !Enum.IsDefined(typeof(PackableBuildingOperationKind), saved.Operation)
                    || !Enum.IsDefined(typeof(PackableBuildingExecutionStatus), saved.Status))
                {
                    return Result<PackableBuildingExecutionRegistry>.Failure(
                        SaveErrors.InvalidDocument);
                }

                EntityId? activeWorker = string.IsNullOrWhiteSpace(saved.ActiveWorkerId)
                    ? (EntityId?)null
                    : EntityId.Parse(saved.ActiveWorkerId);
                PackableBuildingIterationClockSnapshot? clock = saved.IterationClock is null
                    ? null
                    : new PackableBuildingIterationClockSnapshot(
                        EntityId.Parse(saved.IterationClock.WorkerId),
                        saved.IterationClock.StartTick,
                        saved.IterationClock.DurationSeconds);
                snapshots.Add(new PackableBuildingExecutionSnapshot(
                    EntityId.Parse(saved.OperationId),
                    EntityId.Parse(saved.PackageId),
                    new BuildingDefinitionId(saved.DefinitionId),
                    (PackableBuildingOperationKind)saved.Operation,
                    (PackableBuildingExecutionStatus)saved.Status,
                    saved.TotalIterations,
                    saved.CompletedIterations,
                    activeWorker,
                    saved.CompletedByWorkers.Select(EntityId.Parse).ToArray(),
                    clock));
            }

            Result<PackableBuildingExecutionRegistry> restored =
                PackableBuildingExecutionRegistry.Restore(snapshots);
            return restored.IsFailure
                ? Result<PackableBuildingExecutionRegistry>.Failure(SaveErrors.InvalidDocument)
                : restored;
        }
        catch (Exception error) when (
            error is ArgumentException
            || error is FormatException
            || error is InvalidOperationException
            || error is OverflowException)
        {
            return Result<PackableBuildingExecutionRegistry>.Failure(
                SaveErrors.InvalidDocument);
        }
    }
}

}