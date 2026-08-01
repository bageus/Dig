using System;
using System.Linq;
using Dig.Application.Production;
using Dig.Domain.Core;
using Dig.Domain.Production;

namespace Dig.Tests
{

internal sealed partial class CampfireProductionTestHarness
{
    public Result AcquireMaterial(EntityId orderId, EntityId jobId, long tick)
    {
        return new AcquireProductionMaterialHandler(
            ProductionRepository,
            InventoryRepository,
            JobsRepository,
            Journal).Handle(new AcquireProductionMaterialCommand(
                orderId,
                jobId,
                Id(_nextTransitId++),
                tick));
    }

    public Result StageMaterial(EntityId orderId, EntityId jobId, long tick)
    {
        return new StageProductionMaterialHandler(
            ProductionRepository,
            InventoryRepository,
            JobsRepository,
            Journal).Handle(new StageProductionMaterialCommand(
                orderId,
                jobId,
                tick));
    }

    public Result ApplyMaterialWork(
        EntityId orderId,
        EntityId jobId,
        int elapsedTicks,
        long tick)
    {
        return new ApplyProductionWorkHandler(
            ProductionRepository,
            InventoryRepository,
            JobsRepository,
            Agents,
            Journal).Handle(new ApplyProductionWorkCommand(
                orderId,
                jobId,
                elapsedTicks,
                conditionEfficiencyBasisPoints: 10_000,
                tick));
    }

    public Result DepositMaterial(EntityId orderId, EntityId jobId, long tick)
    {
        ProductionOutputPackageSnapshot? package = Production
            .GetOutputPackageForOrder(orderId);
        if (package != null)
        {
            return new DepositProductionMaterialHandler(
                ProductionRepository,
                JobsRepository,
                Journal).Handle(new DepositProductionMaterialCommand(
                    orderId,
                    jobId,
                    package.StackId,
                    tick));
        }

        Result deposited = Production.DepositProcessedMaterial(orderId, tick);
        if (deposited.IsFailure)
        {
            return deposited;
        }

        if (Production.Get(orderId)!.Status == ProductionOrderStatus.ReadyToComplete)
        {
            Result advanced = Jobs.AdvanceStage(jobId, tick);
            if (advanced.IsFailure)
            {
                return advanced;
            }
        }

        ProductionRepository.Save(Production);
        JobsRepository.Save(Jobs);
        Journal.Append(Production.DequeueUncommittedEvents());
        Journal.Append(Jobs.DequeueUncommittedEvents());
        return Result.Success();
    }

    public Result Work(EntityId orderId, EntityId jobId, int elapsedTicks, long tick)
    {
        if (elapsedTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsedTicks));
        }

        int remaining = elapsedTicks;
        long currentTick = tick;
        while (remaining > 0)
        {
            ProductionOrderSnapshot order = Production.Get(orderId)!;
            ProductionMaterialStepSnapshot? step = order.MaterialSteps
                .Where(value => !value.Consumed)
                .Select(value => (ProductionMaterialStepSnapshot?)value)
                .FirstOrDefault();
            if (!step.HasValue)
            {
                return Result.Failure(ProductionErrors.InvalidStatus);
            }

            if (step.Value.Phase == ProductionMaterialStepPhase.AwaitingMaterial)
            {
                Result acquired = AcquireMaterial(orderId, jobId, currentTick++);
                if (acquired.IsFailure)
                {
                    return acquired;
                }

                Result staged = StageMaterial(orderId, jobId, currentTick++);
                if (staged.IsFailure)
                {
                    return staged;
                }

                step = Production.Get(orderId)!.MaterialSteps
                    .Where(value => !value.Consumed)
                    .Select(value => (ProductionMaterialStepSnapshot?)value)
                    .First();
            }

            if (step.Value.Phase is ProductionMaterialStepPhase.StagedOnWorkbench
                or ProductionMaterialStepPhase.Processing)
            {
                int applied = checked((int)Math.Min(
                    remaining,
                    step.Value.RequiredTicks - step.Value.CompletedTicks));
                Result worked = ApplyMaterialWork(
                    orderId,
                    jobId,
                    applied,
                    currentTick++);
                if (worked.IsFailure)
                {
                    return worked;
                }

                remaining -= applied;
            }

            ProductionMaterialStepSnapshot current = Production.Get(orderId)!
                .MaterialSteps
                .Where(value => !value.Consumed)
                .FirstOrDefault();
            if (current.Phase
                == ProductionMaterialStepPhase.ProcessedAwaitingPackage)
            {
                Result deposited = DepositMaterial(orderId, jobId, currentTick++);
                if (deposited.IsFailure)
                {
                    return deposited;
                }
            }
        }

        return Result.Success();
    }


}

}
