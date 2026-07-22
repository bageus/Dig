using System;
using Dig.Domain.World;

namespace Dig.Presentation.World
{

public sealed class TerrainDepositPresentationInput
{
    public TerrainDepositPresentationInput(
        CellId cell,
        string depositId,
        bool isRevealed,
        int remainingYield,
        int maximumYield,
        long version)
    {
        if (string.IsNullOrWhiteSpace(depositId))
        {
            throw new ArgumentException(
                "A stable deposit id is required.",
                nameof(depositId));
        }

        if (maximumYield <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumYield));
        }

        if (remainingYield < 0 || remainingYield > maximumYield)
        {
            throw new ArgumentOutOfRangeException(nameof(remainingYield));
        }

        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        Cell = cell;
        DepositId = depositId;
        IsRevealed = isRevealed;
        RemainingYield = remainingYield;
        MaximumYield = maximumYield;
        Version = version;
    }

    public CellId Cell { get; }

    public string DepositId { get; }

    public bool IsRevealed { get; }

    public int RemainingYield { get; }

    public int MaximumYield { get; }

    public long Version { get; }
}

}
