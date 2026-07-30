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
    private static readonly MaterialDefinition Stone = new MaterialDefinition(
        new MaterialId("terrain.stone_rock"),
        isSolid: true,
        hardness: 100);
    private static readonly MaterialDefinition MetalBearing = new MaterialDefinition(
        new MaterialId("terrain.metal_bearing_rock"),
        isSolid: true,
        hardness: 120);
    private static readonly TerrainDepositDefinition Iron = new TerrainDepositDefinition(
        "deposit.iron_ore",
        "Iron ore",
        new ItemId("ore.iron"),
        maximumYield: 8,
        generationWeight: 40,
        allowedHostMaterialIds: new[] { MetalBearing.Id });
    private static readonly TerrainDepositDefinition Coal = new TerrainDepositDefinition(
        "deposit.coal",
        "Coal",
        new ItemId("material.coal"),
        maximumYield: 10,
        generationWeight: 60,
        allowedHostMaterialIds: new[] { Stone.Id, MetalBearing.Id });

    [Fact]
    public void Same_seed_version_and_hosts_generate_identical_hidden_xyz_cells()
    {
        TerrainDepositGenerator generator = new TerrainDepositGenerator();
        TerrainDepositHostCell[] candidates = CreateCandidates(10, 10, depth: 4);
        TerrainDepositGenerationSettings settings = new TerrainDepositGenerationSettings(
            seed: 42,
            algorithmVersion: 3,
            densityPermille: 180,
            maximumClusterSize: 4);
        TerrainDepositCatalog catalog = new TerrainDepositCatalog(new[] { Iron, Coal });

        TerrainDepositGenerationResult first = generator.Generate(
            new WorldSize(10, 10, 4),
            candidates,
            catalog,
            settings);
        TerrainDepositGenerationResult second = generator.Generate(
            new WorldSize(10, 10, 4),
            candidates.Reverse().ToArray(),
            new TerrainDepositCatalog(new[] { Coal, Iron }),
            settings);

        Assert.Equal(3, first.AlgorithmVersion);
        Assert.NotEmpty(first.Deposits);
        Assert.True(first.Deposits.Count < candidates.Length);
        Assert.Equal(first.Deposits.Select(Describe), second.Deposits.Select(Describe));
        Assert.All(first.Deposits, value => Assert.False(value.IsRevealed));
        Assert.Contains(first.Deposits, value => value.Cell.Z > 0);
        Assert.Equal(
            first.Deposits.Count,
            first.Deposits.Select(value => value.Cell).Distinct().Count());
    }

    [Fact]
    public void Seed_or_algorithm_version_changes_identity_or_layout()
    {
        TerrainDepositGenerator generator = new TerrainDepositGenerator();
        TerrainDepositHostCell[] candidates = CreateCandidates(8, 8, depth: 4);
        TerrainDepositCatalog catalog = new TerrainDepositCatalog(new[] { Iron, Coal });

        TerrainDepositGenerationResult baseline = generator.Generate(
            new WorldSize(8, 8, 4),
            candidates,
            catalog,
            new TerrainDepositGenerationSettings(42, 1, 240, 4));
        TerrainDepositGenerationResult changedSeed = generator.Generate(
            new WorldSize(8, 8, 4),
            candidates,
            catalog,
            new TerrainDepositGenerationSettings(43, 1, 240, 4));
        TerrainDepositGenerationResult changedVersion = generator.Generate(
            new WorldSize(8, 8, 4),
            candidates,
            catalog,
            new TerrainDepositGenerationSettings(42, 2, 240, 4));

        Assert.NotEqual(
            baseline.Deposits.Select(Describe).ToArray(),
            changedSeed.Deposits.Select(Describe).ToArray());
        Assert.NotEqual(
            baseline.Deposits.Select(Describe).ToArray(),
            changedVersion.Deposits.Select(Describe).ToArray());
    }

    [Fact]
    public void Generation_respects_host_material_constraints_and_bounds()
    {
        TerrainDepositHostCell[] hosts =
        {
            new TerrainDepositHostCell(new CellId(1, 1, 0), Stone),
            new TerrainDepositHostCell(new CellId(2, 1, 0), MetalBearing),
            new TerrainDepositHostCell(new CellId(2, 1, 1), MetalBearing),
        };
        TerrainDepositGenerationResult generated = new TerrainDepositGenerator().Generate(
            new WorldSize(4, 4, 4),
            hosts,
            new TerrainDepositCatalog(new[] { Iron }),
            new TerrainDepositGenerationSettings(7, 1, 1_000, 4));

        Assert.NotEmpty(generated.Deposits);
        Assert.All(generated.Deposits, value =>
        {
            Assert.NotEqual(new CellId(1, 1, 0), value.Cell);
            Assert.Equal(Iron.Id, value.Definition.Id);
            Assert.InRange(value.Cell.Z, CellId.MinimumDepth, CellId.MaximumDepth);
        });
    }

    [Fact]
    public void Cluster_cells_are_independent_and_bounded_to_four()
    {
        TerrainDepositHostCell[] hosts = CreateCandidates(3, 3, depth: 2);
        TerrainDepositGenerationResult generated = new TerrainDepositGenerator().Generate(
            new WorldSize(3, 3, 4),
            hosts,
            new TerrainDepositCatalog(new[] { Coal }),
            new TerrainDepositGenerationSettings(3, 1, 1_000, 4));

        Assert.NotEmpty(generated.Deposits);
        Assert.All(generated.Deposits, value =>
            Assert.Equal(Coal.MaximumYield, value.RemainingYield));
        Assert.Equal(
            generated.Deposits.Count,
            generated.Deposits.Select(value => value.InstanceId).Distinct().Count());
        Assert.Equal(
            generated.Deposits.Count,
            generated.Deposits.Select(value => value.Cell).Distinct().Count());
    }

    [Fact]
    public void Deposit_definitions_carry_version_effort_hosts_and_skill_profiles()
    {
        TerrainDepositDefinition crystal = new TerrainDepositDefinition(
            "deposit.crystal_ore",
            "Crystal ore",
            new ItemId("ore.crystal"),
            maximumYield: 6,
            generationWeight: 1,
            skillGrantProfile: DefaultSkillProgressionContent.Catalog.GetProfile(
                DefaultSkillGrantProfileIds.Alchemy),
            version: 4,
            workEffortPermille: 1_250,
            allowedHostMaterialIds: new[] { MetalBearing.Id });

        Assert.Equal(4, crystal.Version);
        Assert.Equal(1_250, crystal.WorkEffortPermille);
        Assert.Equal(new[] { MetalBearing.Id }, crystal.AllowedHostMaterialIds);
        Assert.True(crystal.CanOccupy(MetalBearing));
        Assert.False(crystal.CanOccupy(Stone));
        Assert.Contains(
            crystal.SkillGrantProfile.PerUnit,
            grant => grant.SkillId == AgentSkillCatalog.Alchemy);
    }

    private static TerrainDepositHostCell[] CreateCandidates(
        int width,
        int height,
        int depth)
    {
        List<TerrainDepositHostCell> cells = new List<TerrainDepositHostCell>();
        for (int z = 0; z < depth; z++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    MaterialDefinition material = (x + y + z) % 3 == 0
                        ? MetalBearing
                        : Stone;
                    cells.Add(new TerrainDepositHostCell(
                        new CellId(x, y, z),
                        material));
                }
            }
        }

        return cells.ToArray();
    }

    private static string Describe(TerrainDepositInstance value)
    {
        return $"{value.InstanceId}:{value.Cell}:{value.Definition.Id}:"
            + $"{value.DefinitionVersion}:{value.RemainingYield}:{value.IsRevealed}";
    }
}

}
