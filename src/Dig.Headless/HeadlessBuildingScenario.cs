using Dig.Application.Buildings;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Runtime;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;

namespace Dig.Headless;

internal static class HeadlessBuildingScenario
{
    public static BuildingSnapshot Run(
        SimulationState state,
        InMemoryExecutionJournal journal,
        InMemoryWorldRepository worldRepository,
        InMemoryInventoryRepository inventoryRepository,
        InMemoryJobRepository jobRepository,
        InMemoryJobCandidateProvider candidates,
        EntityId residentId,
        AgentSnapshot resident,
        EntityId sourceStackId,
        ItemId materialId,
        CellId origin,
        long startTick)
    {
        EntityId buildingId = Require(state.Entities.RegisterNew());
        BuildingDefinition definition = new BuildingDefinition(
            new BuildingDefinitionId("headless.workbench"),
            "Headless Workbench",
            new[] { new CellOffset(0, 0) },
            new[] { new CellOffset(0, 0) },
            new[] { new BuildingMaterialRequirement(materialId, 1) },
            requiredWork: 5,
            maximumDurability: 100);
        BuildingCatalog catalog = new BuildingCatalog(new[] { definition });
        InMemoryBuildingsRepository buildingsRepository =
            new InMemoryBuildingsRepository();
        PlaceBuildingHandler place = new PlaceBuildingHandler(
            catalog,
            worldRepository,
            buildingsRepository,
            new BuildingPlacementValidator(),
            journal);
        Require(place.Handle(new PlaceBuildingCommand(
            buildingId,
            definition.Id,
            origin,
            BuildingOrientation.North,
            new[] { origin },
            startTick)));

        EntityId deliveryJobId = Require(state.Entities.RegisterNew());
        CreateBuildingDeliveryHandler createDelivery = new CreateBuildingDeliveryHandler(
            buildingsRepository,
            inventoryRepository,
            jobRepository,
            journal);
        Require(createDelivery.Handle(new CreateBuildingDeliveryCommand(
            deliveryJobId,
            buildingId,
            sourceStackId,
            quantity: 1,
            priority: 650,
            tick: startTick + 1)));
        Assign(
            jobRepository,
            candidates,
            journal,
            deliveryJobId,
            residentId,
            resident,
            startTick + 2);
        AdvanceJobHandler advance = new AdvanceJobHandler(jobRepository, journal);
        Require(advance.Handle(new AdvanceJobCommand(deliveryJobId, startTick + 3)));
        Require(advance.Handle(new AdvanceJobCommand(deliveryJobId, startTick + 4)));
        Require(advance.Handle(new AdvanceJobCommand(deliveryJobId, startTick + 5)));
        CompleteBuildingDeliveryHandler completeDelivery =
            new CompleteBuildingDeliveryHandler(
                inventoryRepository,
                jobRepository,
                journal);
        Require(completeDelivery.Handle(new CompleteBuildingDeliveryCommand(
            deliveryJobId,
            splitStackId: default,
            tick: startTick + 6)));

        RefreshBuildingMaterialsHandler refresh = new RefreshBuildingMaterialsHandler(
            buildingsRepository,
            inventoryRepository,
            journal);
        Require(refresh.Handle(new RefreshBuildingMaterialsCommand(buildingId)));
        EntityId constructionJobId = Require(state.Entities.RegisterNew());
        CreateConstructionJobHandler createConstruction = new CreateConstructionJobHandler(
            buildingsRepository,
            jobRepository,
            journal);
        Require(createConstruction.Handle(new CreateConstructionJobCommand(
            constructionJobId,
            buildingId,
            new[] { origin },
            priority: 700,
            tick: startTick + 7)));
        Assign(
            jobRepository,
            candidates,
            journal,
            constructionJobId,
            residentId,
            resident,
            startTick + 8);
        Require(advance.Handle(new AdvanceJobCommand(constructionJobId, startTick + 9)));
        Require(advance.Handle(new AdvanceJobCommand(constructionJobId, startTick + 10)));
        AddConstructionWorkHandler addWork = new AddConstructionWorkHandler(
            buildingsRepository,
            jobRepository,
            journal);
        Require(addWork.Handle(new AddConstructionWorkCommand(
            constructionJobId,
            buildingId,
            workAmount: definition.RequiredWork,
            tick: startTick + 11)));
        CompleteConstructionHandler completeConstruction = new CompleteConstructionHandler(
            buildingsRepository,
            inventoryRepository,
            jobRepository,
            journal);
        Require(completeConstruction.Handle(new CompleteConstructionCommand(
            constructionJobId,
            buildingId,
            tick: startTick + 12)));

        BuildingSnapshot completed = buildingsRepository.Get().Get(buildingId)
            ?? throw new InvalidOperationException("Headless building disappeared.");
        if (completed.Status != BuildingStatus.Completed
            || inventoryRepository.Get().GetTotal(materialId) != 0
            || jobRepository.Get().GetReservations().Count != 0)
        {
            throw new InvalidOperationException(
                "Headless construction did not consume materials and complete cleanly.");
        }

        return completed;
    }

    private static void Assign(
        InMemoryJobRepository jobs,
        InMemoryJobCandidateProvider candidates,
        InMemoryExecutionJournal journal,
        EntityId jobId,
        EntityId residentId,
        AgentSnapshot resident,
        long tick)
    {
        candidates.SetCandidates(jobId, new[]
        {
            new JobCandidate(
                residentId,
                resident.GetSkillLevel(new AgentSkillId("general.work")),
                distanceCost: 1,
                isAvailable: true),
        });
        JobAssignmentReport report = new AssignAvailableJobsHandler(
            jobs,
            candidates,
            journal).Handle(new AssignAvailableJobsCommand(tick));
        if (report.Assignments.Count != 1 || report.Failures.Count != 0)
        {
            throw new InvalidOperationException("Headless building job was not assigned.");
        }
    }

    private static T Require<T>(Result<T> result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error!.ToString());
        }

        return result.Value;
    }

    private static void Require(Result result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error!.ToString());
        }
    }
}
