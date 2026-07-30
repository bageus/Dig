using System.Linq;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class TerrainDepositIntegrityDiagnosticsTests
{
    private static readonly MaterialId Rock = new MaterialId("terrain.rock");
    private static readonly MaterialId Air = new MaterialId("terrain.air");
    private static readonly ItemId Ore = new ItemId("ore.iron");
    private static readonly TerrainDepositDefinition Iron = new TerrainDepositDefinition(
        "deposit.iron_ore",
        "Iron ore",
        Ore,
        maximumYield: 8,
        generationWeight: 1,
        allowedHostMaterialIds: new[] { Rock });

    [Fact]
    public void Valid_hidden_revealed_and_depleted_state_reports_stable_counts()
    {
        WorldState world = CreateWorld();
        CellId hidden = new CellId(1, 1, 0);
        CellId revealed = new CellId(2, 1, 1);
        CellId depletedCell = new CellId(3, 1, 2);
        world.ReplaceTerrainDeposits(new[]
        {
            Deposit("hidden", hidden),
            Deposit("revealed", revealed).Reveal(2),
            Deposit("depleted", depletedCell),
        }, generatorVersion: 6);
        MiningOutputCommitState commits = new MiningOutputCommitState();
        MiningOutputPlan plan = new MiningOutputResolver().Resolve(
            worldSeed: 1,
            generatorVersion: 1,
            depletedCell,
            world.Materials.Get(Rock)!,
            world.TerrainDeposits);
        Assert.True(world.Excavate(
            depletedCell,
            Air,
            tick: 1,
            plan.DepositInstanceId,
            plan.Quantity).IsSuccess);
        commits.Record(plan, EntityId.Parse("71000000000000000000000000000001"));

        TerrainDepositIntegrityReport report =
            new TerrainDepositIntegrityDiagnostics().Inspect(world, commits);

        Assert.True(report.IsValid, string.Join("; ", report.Issues.Select(x => x.Code)));
        Assert.Equal(6, report.GeneratorVersion);
        Assert.Equal(1, report.HiddenCount);
        Assert.Equal(1, report.RevealedCount);
        Assert.Equal(1, report.DepletedCount);
    }

    [Fact]
    public void Depleted_without_output_commit_is_reported_without_state_mutation()
    {
        WorldState world = CreateWorld();
        CellId target = new CellId(2, 2, 3);
        world.ReplaceTerrainDeposits(
            new[] { Deposit("missing-output", target) },
            generatorVersion: 2);
        Assert.True(world.Excavate(target, Air, tick: 1).IsSuccess);
        long version = world.Version;

        TerrainDepositIntegrityReport report =
            new TerrainDepositIntegrityDiagnostics().Inspect(
                world,
                new MiningOutputCommitState());

        Assert.False(report.IsValid);
        Assert.Contains(
            report.Issues,
            value => value.Code
                == TerrainDepositIntegrityCodes.DepletedWithoutOutputCommit);
        Assert.Equal(version, world.Version);
    }

    [Fact]
    public void Deposit_commit_for_active_cell_is_reported()
    {
        WorldState world = CreateWorld();
        CellId target = new CellId(2, 2, 0);
        world.ReplaceTerrainDeposits(
            new[] { Deposit("active", target) },
            generatorVersion: 1);
        MiningOutputPlan plan = new MiningOutputResolver().Resolve(
            worldSeed: 1,
            generatorVersion: 1,
            target,
            world.Materials.Get(Rock)!,
            world.TerrainDeposits);
        MiningOutputCommitState commits = new MiningOutputCommitState();
        commits.Record(plan, EntityId.Parse("72000000000000000000000000000001"));

        TerrainDepositIntegrityReport report =
            new TerrainDepositIntegrityDiagnostics().Inspect(world, commits);

        Assert.Contains(
            report.Issues,
            value => value.Code
                == TerrainDepositIntegrityCodes.DepositCommitNotDepleted);
    }

    private static WorldState CreateWorld()
    {
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(Rock, isSolid: true, hardness: 100),
            new MaterialDefinition(Air, isSolid: false, hardness: 0),
        });
        return WorldState.CreateFilled(
            new WorldSize(5, 5, 4),
            chunkSize: 2,
            materials,
            Rock,
            explored: true).Value;
    }

    private static TerrainDepositInstance Deposit(string id, CellId cell)
    {
        return new TerrainDepositInstance(
            id,
            cell,
            Iron,
            isRevealed: false,
            remainingYield: Iron.MaximumYield,
            version: 1);
    }
}

}
