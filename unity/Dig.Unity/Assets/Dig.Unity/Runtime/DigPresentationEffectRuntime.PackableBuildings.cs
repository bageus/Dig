using System;
using System.Collections.Generic;
using Dig.Presentation.Buildings;
using Dig.Presentation.Rendering;

namespace Dig.Unity
{

public sealed partial class DigPresentationEffectRuntime
{
    private readonly Dictionary<string, int> _packableCompletedIterations =
        new Dictionary<string, int>(StringComparer.Ordinal);

    private void AddPackableBuildingIterationEffects(
        IDictionary<string, PresentationEffectFact> frame,
        long tick)
    {
        IReadOnlyList<PackableBuildingExecutionViewModel> operations =
            _terrain!.LoadPackableBuildingExecutions(tick);
        for (int index = 0; index < operations.Count; index++)
        {
            PackableBuildingExecutionViewModel operation = operations[index];
            if (!_packableCompletedIterations.TryGetValue(
                    operation.OperationId,
                    out int previousCompleted))
            {
                _packableCompletedIterations.Add(
                    operation.OperationId,
                    operation.CompletedIterations);
                continue;
            }

            if (operation.CompletedIterations <= previousCompleted)
            {
                _packableCompletedIterations[operation.OperationId] =
                    operation.CompletedIterations;
                continue;
            }

            if (_locations.TryGetValue(
                    operation.PackageId,
                    out PresentationEffectLocation location))
            {
                for (int iteration = previousCompleted + 1;
                    iteration <= operation.CompletedIterations;
                    iteration++)
                {
                    Add(frame, new PresentationEffectFact(
                        $"{operation.IterationEffectId}:{operation.OperationId}:{iteration}",
                        PresentationEffectKind.ConstructionProgress,
                        location.WorldX,
                        location.WorldY,
                        location.WorldZ,
                        0.7d,
                        tick));
                }
            }

            _packableCompletedIterations[operation.OperationId] =
                operation.CompletedIterations;
        }
    }
}

}