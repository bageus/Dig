using Dig.Application.Buildings;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class BuildingEquipmentCadenceTests
{
    [Fact]
    public void Assembly_applies_work_only_when_interval_allows_it()
    {
        BuildingBoxHarness harness = new BuildingBoxHarness();
        Assert.True(harness.Confirm(
            harness.BuildingId,
            harness.JobId,
            new CellId(3, 3)).IsSuccess);
        harness.AssignAndAdvanceToDeposit();
        Assert.True(harness.CommitToSite().IsSuccess);
        harness.AdvanceToPerformWork();
        AddBuildingBoxAssemblyWorkHandler handler = new AddBuildingBoxAssemblyWorkHandler(
            harness.BuildingsRepository,
            harness.JobRepository,
            harness.Journal);
        handler.SetWorkDuePolicy((_, _) => false);

        Result first = handler.Handle(new AddBuildingBoxAssemblyWorkCommand(
            harness.BuildingId,
            harness.JobId,
            workAmount: 1,
            tick: 100));

        Assert.True(first.IsSuccess, first.Error?.ToString());
        Assert.Equal(
            BuildingStatus.ReadyToBuild,
            harness.Buildings.Get(harness.BuildingId)!.Status);

        handler.SetWorkDuePolicy((_, _) => true);
        Result second = handler.Handle(new AddBuildingBoxAssemblyWorkCommand(
            harness.BuildingId,
            harness.JobId,
            workAmount: 1,
            tick: 101));

        Assert.True(second.IsSuccess, second.Error?.ToString());
        Assert.Equal(
            BuildingStatus.UnderConstruction,
            harness.Buildings.Get(harness.BuildingId)!.Status);
    }

    [Fact]
    public void Packing_applies_work_only_when_interval_allows_it()
    {
        BuildingBoxPackingHarness harness = new BuildingBoxPackingHarness();
        Assert.True(harness.Start().IsSuccess);
        harness.AssignAndAdvanceToWork();
        AddBuildingBoxPackingWorkHandler handler = new AddBuildingBoxPackingWorkHandler(
            harness.Assembly.BuildingsRepository,
            harness.Assembly.JobRepository,
            harness.Assembly.Journal);
        handler.SetWorkDuePolicy((_, _) => false);

        Result first = handler.Handle(new AddBuildingBoxPackingWorkCommand(
            harness.BuildingId,
            harness.PackingJobId,
            workAmount: 1,
            tick: 200));

        Assert.True(first.IsSuccess, first.Error?.ToString());
        Assert.Equal(
            0,
            harness.Buildings.Get(harness.BuildingId)!.PackingPlan!.CompletedWork);

        handler.SetWorkDuePolicy((_, _) => true);
        Result second = handler.Handle(new AddBuildingBoxPackingWorkCommand(
            harness.BuildingId,
            harness.PackingJobId,
            workAmount: 1,
            tick: 201));

        Assert.True(second.IsSuccess, second.Error?.ToString());
        Assert.Equal(
            1,
            harness.Buildings.Get(harness.BuildingId)!.PackingPlan!.CompletedWork);
    }
}

}
