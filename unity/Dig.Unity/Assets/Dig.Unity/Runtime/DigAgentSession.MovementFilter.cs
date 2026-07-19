using System;
using System.Collections.Generic;
using Dig.Domain.World;

namespace Dig.Unity
{

internal sealed partial class DigAgentSession
{
    private Func<
        IReadOnlyDictionary<string, CellId>,
        long,
        IReadOnlyDictionary<string, CellId>>? _movementTargetFilter;

    internal void SetMovementTargetFilter(
        Func<
            IReadOnlyDictionary<string, CellId>,
            long,
            IReadOnlyDictionary<string, CellId>> filter)
    {
        _movementTargetFilter = filter
            ?? throw new ArgumentNullException(nameof(filter));
    }

    private IReadOnlyDictionary<string, CellId> ApplyMovementTargetFilter(
        IReadOnlyDictionary<string, CellId> movementTargets,
        long tick)
    {
        return _movementTargetFilter == null
            ? movementTargets
            : _movementTargetFilter(movementTargets, tick);
    }
}

}
