using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Content;
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
        CellId[] candidates = CreateCandidates(width: 10, height: 10);
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
        CellId[] candidates = CreateCandidates(width: 10, height: 10);

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
        CellId[] candidates =
        {
            new CellId(2, 2, 0),
            new CellId(3, 2, 0),
            new CellId(4, 2, 0),
            new CellId(4, 3, 0),
        };
        HashSet<CellId> allowed = new HashSet<CellId>(candidates);

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

    [Fact]
    public void Deposit_definitions_carry_data_driven_extraction_skill_profiles()
    {
        TerrainDepositDefinition iron = new TerrainDepositDefinition(
            "deposit.iron_ore",
            "Iron ore",
            new ItemId("ore.iron"),
            maximumYield: 8,
            generationWeight: 1,
            skillGrantProfile: DefaultSkillProgressionContent.Catalog.GetProfile(
                DefaultSkillGrantProfileIds.Metallurgy));
        TerrainDepositDefinition crystal = new TerrainDepositDefinition(
            "deposit.crystal_ore",
            "Crystal ore",
            new ItemId("ore.crystal"),
            maximumYield: 6,
            generationWeight: 1,
            skillGrantProfile: DefaultSkillProgressionContent.Catalog.GetProfile(
                DefaultSkillGrantProfileIds.Alchemy));

        Assert.Contains(iron.SkillGrantProfile.PerUnit,
            grant => grant.SkillId == AgentSkillCatalog.Metallurgy);
        Assert.Contains(crystal.SkillGrantProfile.PerUnit,
            grant => grant.SkillId == AgentSkillCatalog.Alchemy);
        Assert.DoesNotContain(iron.SkillGrantProfile.PerUnit,
            grant => grant.SkillId == AgentSkillCatalog.Stonework);
    }

    private static CellId[] CreateCandidates(int width, int height)
    {
        List<CellId> cells = new List<CellId>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                cells.Add(new CellId(x, y, 0));
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
