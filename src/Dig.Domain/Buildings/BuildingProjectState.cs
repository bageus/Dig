using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Buildings
{

internal sealed class BuildingProjectState
{
    public BuildingProjectState(
        EntityId id,
        BuildingDefinition definition,
        CellId origin,
        BuildingOrientation orientation,
        IReadOnlyCollection<CellId> footprint,
        CellId workPosition,
        BuildingStatus initialStatus = BuildingStatus.AwaitingMaterials)
    {
        if (initialStatus != BuildingStatus.AwaitingMaterials
            && initialStatus != BuildingStatus.AwaitingBox)
        {
            throw new ArgumentOutOfRangeException(nameof(initialStatus));
        }

        Id = id;
        Definition = definition;
        Origin = origin;
        Orientation = orientation;
        Footprint = footprint.OrderBy(cell => cell).ToArray();
        WorkPosition = workPosition;
        Status = initialStatus;
    }

    public EntityId Id { get; }

    public BuildingDefinition Definition { get; }

    public CellId Origin { get; }

    public BuildingOrientation Orientation { get; }

    public IReadOnlyList<CellId> Footprint { get; }

    public CellId WorkPosition { get; }

    public BuildingStatus Status { get; private set; }

    public int CompletedWork { get; private set; }

    public int Durability { get; private set; }

    public long Version { get; private set; }

    public string? DiagnosticReason { get; private set; }

    public void MarkMaterialsReady()
    {
        Status = BuildingStatus.ReadyToBuild;
        DiagnosticReason = null;
        IncrementVersion();
    }

    public void StartConstruction()
    {
        Status = BuildingStatus.UnderConstruction;
        DiagnosticReason = null;
        IncrementVersion();
    }

    public void AddWork(int amount)
    {
        CompletedWork = Math.Min(
            Definition.RequiredWork,
            checked(CompletedWork + amount));
        if (CompletedWork == Definition.RequiredWork)
        {
            Status = BuildingStatus.ReadyToComplete;
        }

        IncrementVersion();
    }

    public void Block(string reason)
    {
        DiagnosticReason = reason;
        IncrementVersion();
    }

    public void Complete()
    {
        Status = BuildingStatus.Completed;
        Durability = Definition.MaximumDurability;
        DiagnosticReason = null;
        IncrementVersion();
    }

    public void Cancel(string reason)
    {
        Status = BuildingStatus.Cancelled;
        DiagnosticReason = reason;
        IncrementVersion();
    }

    public void Remove(string reason)
    {
        Status = BuildingStatus.Removed;
        DiagnosticReason = reason;
        IncrementVersion();
    }

    public void Damage(int amount)
    {
        Durability = Math.Max(0, Durability - amount);
        Status = BuildingStatus.Damaged;
        IncrementVersion();
    }

    public void Repair(int amount)
    {
        Durability = Math.Min(Definition.MaximumDurability, checked(Durability + amount));
        if (Durability == Definition.MaximumDurability)
        {
            Status = BuildingStatus.Completed;
        }

        IncrementVersion();
    }

    public BuildingSnapshot CreateSnapshot()
    {
        return new BuildingSnapshot(
            Id,
            Definition,
            Origin,
            Orientation,
            Footprint,
            WorkPosition,
            Status,
            CompletedWork,
            Durability,
            Version,
            DiagnosticReason);
    }

    private void IncrementVersion()
    {
        Version = checked(Version + 1);
    }
}
}