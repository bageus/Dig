using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dig.Domain.World
{

public sealed class TerrainDepositSaveEntry
{
    public TerrainDepositSaveEntry(
        string instanceId,
        string definitionId,
        CellId cell,
        bool isRevealed,
        int remainingYield,
        long version)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("A stable deposit instance id is required.", nameof(instanceId));
        }

        if (string.IsNullOrWhiteSpace(definitionId))
        {
            throw new ArgumentException("A stable deposit definition id is required.", nameof(definitionId));
        }

        if (remainingYield < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(remainingYield));
        }

        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        InstanceId = instanceId;
        DefinitionId = definitionId;
        Cell = cell;
        IsRevealed = isRevealed;
        RemainingYield = remainingYield;
        Version = version;
    }

    public string InstanceId { get; }

    public string DefinitionId { get; }

    public CellId Cell { get; }

    public bool IsRevealed { get; }

    public int RemainingYield { get; }

    public long Version { get; }
}

public sealed class TerrainDepositSaveSnapshot
{
    public const int CurrentFormatVersion = 1;

    public TerrainDepositSaveSnapshot(
        int formatVersion,
        int generatorVersion,
        IEnumerable<TerrainDepositSaveEntry> deposits)
    {
        if (formatVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(formatVersion));
        }

        if (generatorVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(generatorVersion));
        }

        if (deposits == null)
        {
            throw new ArgumentNullException(nameof(deposits));
        }

        TerrainDepositSaveEntry[] values = deposits.ToArray();
        if (values.Any(value => value == null))
        {
            throw new ArgumentException(
                "Deposit save entries cannot contain null values.",
                nameof(deposits));
        }

        FormatVersion = formatVersion;
        GeneratorVersion = generatorVersion;
        Deposits = new ReadOnlyCollection<TerrainDepositSaveEntry>(
            values.OrderBy(value => value.Cell).ToArray());
    }

    public int FormatVersion { get; }

    public int GeneratorVersion { get; }

    public IReadOnlyList<TerrainDepositSaveEntry> Deposits { get; }
}

}