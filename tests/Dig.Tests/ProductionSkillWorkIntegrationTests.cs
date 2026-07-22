using Dig.Domain.Agents;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Xunit;

namespace Dig.Tests
{

public sealed class ProductionSkillWorkIntegrationTests
{
    [Fact]
    public void Apply_work_derives_cooking_speed_from_assigned_resident()
    {
        RecipeId recipeId = new RecipeId("recipe.skill-speed");
        RecipeDefinition recipe = new RecipeDefinition(
            recipeId,
            "Skill speed recipe",
            ProductionTestHarness.Workshop,
            new[] { new ContentItemQuantity(ProductionTestHarness.Ore, 1) },
            new[] { new ContentItemQuantity(ProductionTestHarness.Plate, 1) },
            requiredWork: 100,
            energyPerWorkTick: 1,
            skillGrantProfile: SkillGrantProfile.Single(
                AgentSkillCatalog.Cooking,
                units: 10),
            workSpeedCurve: new SkillWorkSpeedCurve(
                AgentSkillCatalog.Cooking,
                minimumEfficiencyBasisPoints: 5_000,
                maximumEfficiencyBasisPoints: 15_000));
        ProductionTestHarness harness = new ProductionTestHarness(new[] { recipe });
        EntityId orderId = EntityId.Parse("86000000000000000000000000000031");
        EntityId jobId = EntityId.Parse("86000000000000000000000000000032");
        Assert.True(harness.Agents.Get(ProductionTestHarness.WorkerId)!
            .ApplySkillGrant(new SkillGrantBundle(
                ProductionTestHarness.WorkerId,
                SkillGrantSourceKind.TrainingCompleted,
                "cooking-speed-training",
                tick: 1,
                new[] { new SkillGrant(AgentSkillCatalog.Cooking, 10_000) }))
            .IsSuccess);
        Assert.True(harness.Enqueue(orderId, recipeId, tick: 2).IsSuccess);
        Assert.True(harness.Prepare(jobId, tick: 3).IsSuccess);
        harness.AssignAndBegin(orderId, jobId, tick: 4);

        Assert.True(harness.ApplyWork(orderId, jobId, tick: 7).IsSuccess);

        Assert.Equal(18, harness.Production.Get(orderId)!.CompletedWork);
    }
}

}
