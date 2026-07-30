using System;
using System.Collections.Generic;
using Dig.Application.World;

namespace Dig.Application.Saving
{

public sealed class SaveVersionElevenTerrainOutputContractMigration : ISaveMigration
{
    private const string LegacyMetal = "material.metal";
    private const string Iron = "material.iron";

    public string Id => "save.v11_to_v12.terrain_output_contract";
    public int FromVersion => 11;
    public int ToVersion => 12;

    public void Apply(SaveGameDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (document.FormatVersion != FromVersion)
        {
            throw new InvalidOperationException(
                "Migration received the wrong source version.");
        }

        MigrateInventory(document);
        MigrateJobs(document);
        MigrateProduction(document);
        MigrateRuntimeAndBarrels(document);
        MigrateMiningOutput(document);
        document.FormatVersion = ToVersion;
    }

    private static void MigrateInventory(SaveGameDocument document)
    {
        document.Inventory ??= new InventorySaveData();
        document.Inventory.Stacks ??= new List<ItemStackSaveData>();
        foreach (ItemStackSaveData stack in document.Inventory.Stacks)
        {
            if (stack != null)
            {
                stack.ItemId = Map(stack.ItemId);
            }
        }

        document.Inventory.ResidentSlotClaims ??= new List<ResidentSlotClaimSaveData>();
        foreach (ResidentSlotClaimSaveData claim in document.Inventory.ResidentSlotClaims)
        {
            if (claim != null)
            {
                claim.ItemId = Map(claim.ItemId);
            }
        }
    }

    private static void MigrateJobs(SaveGameDocument document)
    {
        document.Jobs ??= new JobsSaveData();
        document.Jobs.Jobs ??= new List<JobSaveData>();
        foreach (JobSaveData job in document.Jobs.Jobs)
        {
            if (job?.Definition?.Properties == null)
            {
                continue;
            }

            foreach (SavePropertyData property in job.Definition.Properties)
            {
                if (property != null)
                {
                    property.Value = Map(property.Value);
                }
            }
        }
    }

    private static void MigrateProduction(SaveGameDocument document)
    {
        document.BuildingProduction ??= new BuildingProductionSaveData();
        document.BuildingProduction.Orders ??= new List<ProductionOrderSaveData>();
        foreach (ProductionOrderSaveData order in document.BuildingProduction.Orders)
        {
            if (order == null)
            {
                continue;
            }

            order.InputAllocations ??= new List<ProductionInputAllocationSaveData>();
            foreach (ProductionInputAllocationSaveData allocation in order.InputAllocations)
            {
                if (allocation != null)
                {
                    allocation.ItemId = Map(allocation.ItemId);
                }
            }

            order.MaterialSteps ??= new List<ProductionMaterialStepSaveData>();
            foreach (ProductionMaterialStepSaveData step in order.MaterialSteps)
            {
                if (step != null)
                {
                    step.ItemId = Map(step.ItemId);
                }
            }
        }

        document.BuildingProduction.Supplies ??= new List<BuildingSupplySaveData>();
        foreach (BuildingSupplySaveData supply in document.BuildingProduction.Supplies)
        {
            if (supply?.Stocks == null)
            {
                continue;
            }

            foreach (BuildingStockRuleSaveData stock in supply.Stocks)
            {
                if (stock != null)
                {
                    stock.ItemId = Map(stock.ItemId);
                }
            }
        }
    }

    private static void MigrateRuntimeAndBarrels(SaveGameDocument document)
    {
        document.AgentRuntime ??= new AgentRuntimeSaveData();
        document.AgentRuntime.Agents ??= new List<AgentRuntimeStateSaveData>();
        foreach (AgentRuntimeStateSaveData agent in document.AgentRuntime.Agents)
        {
            if (agent?.ActiveMeal != null)
            {
                agent.ActiveMeal.ItemId = Map(agent.ActiveMeal.ItemId);
            }
        }

        document.Barrels ??= new BarrelSaveData();
        document.Barrels.Barrels ??= new List<BarrelEntitySaveData>();
        foreach (BarrelEntitySaveData barrel in document.Barrels.Barrels)
        {
            if (barrel != null)
            {
                barrel.ContentsItemId = Map(barrel.ContentsItemId);
            }
        }
    }

    private static void MigrateMiningOutput(SaveGameDocument document)
    {
        document.MiningOutput ??= new MiningOutputCommitsSaveData();
        document.MiningOutput.Commits ??= new List<MiningOutputCommitSaveData>();
        int sourceFormat = document.MiningOutput.FormatVersion <= 0
            ? 1
            : document.MiningOutput.FormatVersion;
        if (sourceFormat > MiningOutputCommitSaveSnapshot.CurrentFormatVersion)
        {
            throw new InvalidOperationException(
                "Mining output save data uses a future format version.");
        }

        foreach (MiningOutputCommitSaveData commit in document.MiningOutput.Commits)
        {
            if (commit == null)
            {
                continue;
            }

            commit.ItemId = Map(commit.ItemId);
            commit.Outputs ??= new List<MiningOutputCommitOutputSaveData>();
            if (commit.Outputs.Count == 0 && commit.HasStack)
            {
                if (string.IsNullOrWhiteSpace(commit.StackId))
                {
                    throw new InvalidOperationException(
                        "Legacy mining output stack id is missing.");
                }

                commit.Outputs.Add(new MiningOutputCommitOutputSaveData
                {
                    ItemId = Map(commit.ItemId),
                    Quantity = commit.Quantity,
                    StackIds = new List<string> { commit.StackId },
                });
            }

            foreach (MiningOutputCommitOutputSaveData output in commit.Outputs)
            {
                if (output != null)
                {
                    output.ItemId = Map(output.ItemId);
                    output.StackIds ??= new List<string>();
                }
            }

            if (string.IsNullOrWhiteSpace(commit.SourceId))
            {
                commit.SourceId = commit.SourceKind == (int)MiningOutputSourceKind.Deposit
                    ? "legacy.deposit-output"
                    : "legacy.terrain-output";
            }

            if (commit.SourceVersion <= 0)
            {
                commit.SourceVersion = 1;
            }
        }

        document.MiningOutput.FormatVersion =
            MiningOutputCommitSaveSnapshot.CurrentFormatVersion;
    }

    private static string Map(string value)
    {
        return string.Equals(value, LegacyMetal, StringComparison.Ordinal)
            ? Iron
            : value ?? string.Empty;
    }
}

}
