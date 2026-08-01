using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Inventory;

namespace Dig.Domain.Production
{

internal sealed partial class ProductionOrderState
{
    public ProductionMaterialStepSnapshot GetCurrentMaterialStep()
    {
        if (!Recipe.UsesMaterialSteps
            || _currentStepIndex < 0
            || _currentStepIndex >= Recipe.MaterialSteps.Count)
        {
            throw new InvalidOperationException("Production order has no active material step.");
        }

        return CreateMaterialStep(_currentStepIndex);
    }

    public void StageCurrentMaterial()
    {
        EnsureActiveMaterialPhase(ProductionMaterialStepPhase.AwaitingMaterial);
        _currentStepPhase = ProductionMaterialStepPhase.StagedOnWorkbench;
        _currentStepWork = 0;
        IncrementVersion();
    }

    public ProductionMaterialWorkResult PreviewMaterialWork(long elapsedTicks)
    {
        ValidateMaterialWork(elapsedTicks);
        long required = _resolvedStepDurations[_currentStepIndex];
        long applied = Math.Min(elapsedTicks, required - _currentStepWork);
        bool processed = _currentStepWork + applied == required;
        return new ProductionMaterialWorkResult(
            processed
                ? new[] { Recipe.MaterialSteps[_currentStepIndex].ItemId }
                : Array.Empty<ItemId>(),
            processed,
            applied);
    }

    public ProductionMaterialWorkResult AddMaterialWork(long elapsedTicks)
    {
        ProductionMaterialWorkResult result = PreviewMaterialWork(elapsedTicks);
        _currentStepWork = checked(_currentStepWork + result.AppliedTicks);
        _currentStepPhase = result.ReadyForPackageDeposit
            ? ProductionMaterialStepPhase.ProcessedAwaitingPackage
            : ProductionMaterialStepPhase.Processing;
        IncrementVersion();
        return result;
    }

    public bool DepositProcessedMaterial()
    {
        EnsureActiveMaterialPhase(
            ProductionMaterialStepPhase.ProcessedAwaitingPackage);
        _currentStepIndex = checked(_currentStepIndex + 1);
        CompletedWork = _currentStepIndex;
        _currentStepWork = 0;
        bool final = _currentStepIndex == _resolvedStepDurations.Length;
        _currentStepPhase = final
            ? ProductionMaterialStepPhase.Deposited
            : ProductionMaterialStepPhase.AwaitingMaterial;
        if (final)
        {
            Status = ProductionOrderStatus.ReadyToComplete;
        }

        IncrementVersion();
        return final;
    }

    public void RestoreMaterialProgress(
        IReadOnlyCollection<ProductionMaterialStepSnapshot> savedSteps)
    {
        if (!Recipe.UsesMaterialSteps)
        {
            throw new InvalidOperationException("Recipe does not use material steps.");
        }

        ProductionMaterialStepSnapshot[] ordered = (savedSteps
                ?? throw new ArgumentNullException(nameof(savedSteps)))
            .OrderBy(value => value.Index)
            .ToArray();
        if (ordered.Length != Recipe.MaterialSteps.Count)
        {
            throw new ArgumentException("Saved material steps do not match recipe.");
        }

        for (int index = 0; index < ordered.Length; index++)
        {
            if (ordered[index].Index != index
                || ordered[index].ItemId != Recipe.MaterialSteps[index].ItemId
                || ordered[index].RequiredTicks != _resolvedStepDurations[index])
            {
                throw new ArgumentException("Saved material step identity is invalid.");
            }
        }

        int deposited = ordered.TakeWhile(value =>
            value.Phase == ProductionMaterialStepPhase.Deposited).Count();
        if (ordered.Skip(deposited + 1).Any(value =>
                value.Phase != ProductionMaterialStepPhase.AwaitingMaterial
                || value.CompletedTicks != 0))
        {
            throw new ArgumentException("Saved future material steps are invalid.");
        }

        _currentStepIndex = deposited;
        CompletedWork = deposited;
        if (deposited == ordered.Length)
        {
            _currentStepPhase = ProductionMaterialStepPhase.Deposited;
            _currentStepWork = 0;
            Status = ProductionOrderStatus.ReadyToComplete;
            IncrementVersion();
            return;
        }

        ProductionMaterialStepSnapshot current = ordered[deposited];
        ValidateRestoredCurrentStep(current);
        _currentStepPhase = current.Phase;
        _currentStepWork = current.CompletedTicks;
        Status = ProductionOrderStatus.InProgress;
        IncrementVersion();
    }

    private void ValidateMaterialWork(long elapsedTicks)
    {
        if (!Recipe.UsesMaterialSteps)
        {
            throw new InvalidOperationException("Recipe does not use material steps.");
        }

        if (elapsedTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsedTicks));
        }

        if (_currentStepPhase is not ProductionMaterialStepPhase.StagedOnWorkbench
            and not ProductionMaterialStepPhase.Processing)
        {
            throw new InvalidOperationException(
                "Only staged material can receive production work.");
        }
    }

    private void EnsureActiveMaterialPhase(ProductionMaterialStepPhase expected)
    {
        if (!Recipe.UsesMaterialSteps
            || _currentStepIndex >= Recipe.MaterialSteps.Count
            || _currentStepPhase != expected)
        {
            throw new InvalidOperationException(
                "Production material step is not in the required phase.");
        }
    }

    private void ValidateRestoredCurrentStep(ProductionMaterialStepSnapshot current)
    {
        long required = _resolvedStepDurations[_currentStepIndex];
        bool valid = current.Phase switch
        {
            ProductionMaterialStepPhase.AwaitingMaterial => current.CompletedTicks == 0,
            ProductionMaterialStepPhase.StagedOnWorkbench => current.CompletedTicks == 0,
            ProductionMaterialStepPhase.Processing =>
                current.CompletedTicks > 0 && current.CompletedTicks < required,
            ProductionMaterialStepPhase.ProcessedAwaitingPackage =>
                current.CompletedTicks == required,
            _ => false,
        };
        if (!valid)
        {
            throw new ArgumentException("Saved current material phase is invalid.");
        }
    }

    private IReadOnlyList<ProductionMaterialStepSnapshot> CreateMaterialSteps()
    {
        if (!Recipe.UsesMaterialSteps)
        {
            return Array.Empty<ProductionMaterialStepSnapshot>();
        }

        List<ProductionMaterialStepSnapshot> values =
            new List<ProductionMaterialStepSnapshot>(Recipe.MaterialSteps.Count);
        for (int index = 0; index < Recipe.MaterialSteps.Count; index++)
        {
            values.Add(CreateMaterialStep(index));
        }

        return values;
    }

    private ProductionMaterialStepSnapshot CreateMaterialStep(int index)
    {
        long required = index < _resolvedStepDurations.Length
            ? _resolvedStepDurations[index]
            : 0;
        ProductionMaterialStepPhase phase;
        long completed;
        if (index < _currentStepIndex)
        {
            phase = ProductionMaterialStepPhase.Deposited;
            completed = required;
        }
        else if (index == _currentStepIndex && index < Recipe.MaterialSteps.Count)
        {
            phase = _currentStepPhase;
            completed = phase is ProductionMaterialStepPhase.ProcessedAwaitingPackage
                or ProductionMaterialStepPhase.Deposited
                    ? required
                    : _currentStepWork;
        }
        else
        {
            phase = ProductionMaterialStepPhase.AwaitingMaterial;
            completed = 0;
        }

        return new ProductionMaterialStepSnapshot(
            index,
            Recipe.MaterialSteps[index].ItemId,
            required,
            completed,
            phase);
    }


}
}
