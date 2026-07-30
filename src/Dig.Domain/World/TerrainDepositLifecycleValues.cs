using System;

namespace Dig.Domain.World
{

public sealed class TerrainDepositHostCell
{
    public TerrainDepositHostCell(CellId cell, MaterialDefinition material)
    {
        Cell = cell;
        Material = material ?? throw new ArgumentNullException(nameof(material));
    }

    public CellId Cell { get; }

    public MaterialDefinition Material { get; }
}

public enum TerrainDepositChangeKind
{
    Revealed = 0,
    Depleted = 1,
}

public sealed class TerrainDepositChange
{
    public TerrainDepositChange(
        TerrainDepositChangeKind kind,
        string instanceId,
        string definitionId,
        CellId cell,
        long version)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException(
                "Deposit instance id is required.",
                nameof(instanceId));
        }

        if (string.IsNullOrWhiteSpace(definitionId))
        {
            throw new ArgumentException(
                "Deposit definition id is required.",
                nameof(definitionId));
        }

        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        Kind = kind;
        InstanceId = instanceId;
        DefinitionId = definitionId;
        Cell = cell;
        Version = version;
    }

    public TerrainDepositChangeKind Kind { get; }

    public string InstanceId { get; }

    public string DefinitionId { get; }

    public CellId Cell { get; }

    public long Version { get; }
}

}
