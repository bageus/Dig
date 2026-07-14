using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Dig.Domain.Core;

namespace Dig.Domain.World
{

public enum CellDesignation
{
    None = 0,
    Dig = 1,
}

public readonly struct CellState : IEquatable<CellState>
{
    public CellState(
        MaterialId materialId,
        CellDesignation designation,
        bool isExplored,
        ushort damage,
        short temperature)
    {
        if (materialId.IsEmpty)
        {
            throw new ArgumentException("Material id cannot be empty.", nameof(materialId));
        }

        if (designation is not CellDesignation.None and not CellDesignation.Dig)
        {
            throw new ArgumentOutOfRangeException(nameof(designation));
        }

        MaterialId = materialId;
        Designation = designation;
        IsExplored = isExplored;
        Damage = damage;
        Temperature = temperature;
    }

    public MaterialId MaterialId { get; }

    public CellDesignation Designation { get; }

    public bool IsExplored { get; }

    public ushort Damage { get; }

    public short Temperature { get; }

    public CellState WithDesignation(CellDesignation designation)
    {
        return new CellState(
            MaterialId,
            designation,
            IsExplored,
            Damage,
            Temperature);
    }

    public CellState WithTerrain(MaterialId materialId, ushort damage = 0)
    {
        return new CellState(
            materialId,
            CellDesignation.None,
            IsExplored,
            damage,
            Temperature);
    }

    public CellState WithExplored(bool isExplored)
    {
        return new CellState(
            MaterialId,
            Designation,
            isExplored,
            Damage,
            Temperature);
    }

    public bool Equals(CellState other)
    {
        return MaterialId == other.MaterialId
            && Designation == other.Designation
            && IsExplored == other.IsExplored
            && Damage == other.Damage
            && Temperature == other.Temperature;
    }

    public override bool Equals(object? obj)
    {
        return obj is CellState other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            MaterialId,
            Designation,
            IsExplored,
            Damage,
            Temperature);
    }

    public static bool operator ==(CellState left, CellState right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CellState left, CellState right)
    {
        return !left.Equals(right);
    }
}

public readonly struct TerrainChange
{
    public TerrainChange(CellId cellId, CellState targetState)
    {
        CellId = cellId;
        TargetState = targetState;
    }

    public CellId CellId { get; }

    public CellState TargetState { get; }
}

public sealed class WorldMutationResult
{
    public WorldMutationResult(
        long worldVersion,
        int changedCellCount,
        IReadOnlyList<ChunkId> invalidatedChunks)
    {
        if (worldVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(worldVersion));
        }

        if (changedCellCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(changedCellCount));
        }

        if (invalidatedChunks is null)
        {
            throw new ArgumentNullException(nameof(invalidatedChunks));
        }

        WorldVersion = worldVersion;
        ChangedCellCount = changedCellCount;
        InvalidatedChunks = new ReadOnlyCollection<ChunkId>(invalidatedChunks.ToArray());
    }

    public long WorldVersion { get; }

    public int ChangedCellCount { get; }

    public IReadOnlyList<ChunkId> InvalidatedChunks { get; }
}

public static class WorldErrors
{
    public static readonly DomainError CellOutOfBounds = new DomainError(
        "world.cell.out_of_bounds",
        "The cell is outside the world bounds.");

    public static readonly DomainError ChunkOutOfBounds = new DomainError(
        "world.chunk.out_of_bounds",
        "The chunk is outside the world bounds.");

    public static readonly DomainError UnknownMaterial = new DomainError(
        "world.material.unknown",
        "The requested material is not present in the world catalog.");

    public static readonly DomainError DuplicateCellChange = new DomainError(
        "world.change.duplicate_cell",
        "A terrain change batch contains the same cell more than once.");

    public static readonly DomainError DigDesignationRequiresSolidCell = new DomainError(
        "world.designation.dig_requires_solid",
        "Only a solid cell can be designated for digging.");

    public static readonly DomainError ExcavationRequiresEmptyMaterial = new DomainError(
        "world.excavation.requires_empty_material",
        "Excavation must replace terrain with a non-solid material.");

    public static readonly DomainError InvalidDesignation = new DomainError(
        "world.designation.invalid",
        "The requested cell designation is invalid for the target material.");
}
}
