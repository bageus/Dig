using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class TerrainDepositGenerationTests
{
    private static readonly TerrainDepositDefinition[] Definitions =
    {
        new TerrainDepositDefinition(
            "deposit.iron_ore",
            "Iron ore",
            new ItemId("ore.iron"),
            maximumYield: 8,
            generationWeight: 40),
        new TerrainDepositDefinition(
            "deposit.coal",
            "Coal",
            new ItemId("material.coal"),
            maximumYield: 10,
            generationWeight: 60),
    };

    [Fact]
    public void Same_seed_and_version_generate_identical_sparse_cells()
    {
        TerrainDepositGenerator generator = new TerrainDepositGenerator();
        SpatialCellId[] candidates = CreateCandidates(width: 10, height: 10);
        TerrainDepositGenerationSettings settings =
            new TerrainDepositGenerationSettings(
                seed: 42,
                algorithmVersion: 1,
                densityPermille: 180,
                maximumClusterSize: 4);

        var first = generator.Generate(10, 10, 4, candidates, Definitions, settings);
        var second = generator.Generate(10, 10, 4, candidates, Definitions, settings);

        Assert.NotEmpty(first);
        Assert.True(first.Count < candidates.Length);
        Assert.Equal(
            first.Select(Describe),
            second.Select(Describe));
        Assert.Equal(first.Count, first.Select(value => value.Cell).Distinct().Count());
    }

    [Fact]
    public void Different_seed_changes_the_generated_layout()
    {
        TerrainDepositGenerator generator = new TerrainDepositGenerator();
        SpatialCellId[] candidates = CreateCandidates(width: 10, height: 10);

        var first = generator.Generate(
            10,
            10,
            4,
            candidates,
            Definitions,
            new TerrainDepositGenerationSettings(42, 1, 180, 4));
        var second = generator.Generate(
            10,
            10,
            4,
            candidates,
            Definitions,
            new TerrainDepositGenerationSettings(43, 1, 180, 4));

        Assert.NotEqual(
            first.Select(Describe).ToArray(),
            second.Select(Describe).ToArray());
    }

    [Fact]
    public void Generated_deposits_remain_inside_the_mineable_candidate_set()
    {
        TerrainDepositGenerator generator = new TerrainDepositGenerator();
        SpatialCellId[] candidates =
        {
            new SpatialCellId(2, 2, 0),
            new SpatialCellId(3, 2, 0),
            new SpatialCellId(4, 2, 0),
            new SpatialCellId(4, 3, 0),
        };
        HashSet<SpatialCellId> allowed = new HashSet<SpatialCellId>(candidates);

        var deposits = generator.Generate(
            8,
            8,
            4,
            candidates,
            Definitions,
            new TerrainDepositGenerationSettings(7, 1, 1_000, 4));

        Assert.NotEmpty(deposits);
        Assert.All(deposits, value => Assert.Contains(value.Cell, allowed));
        Assert.All(deposits, value => Assert.InRange(value.RemainingYield, 1, value.Definition.MaximumYield));
    }

    private static SpatialCellId[] CreateCandidates(int width, int height)
    {
        List<SpatialCellId> cells = new List<SpatialCellId>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                cells.Add(new SpatialCellId(x, y, 0));
            }
        }

        return cells.ToArray();
    }

    private static string Describe(TerrainDepositInstance value)
    {
        return $"{value.InstanceId}:{value.Cell}:{value.Definition.Id}:{value.RemainingYield}";
    }
}

}