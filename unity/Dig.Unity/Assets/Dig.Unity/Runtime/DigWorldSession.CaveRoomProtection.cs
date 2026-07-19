using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.World;
using Dig.Domain.World;

namespace Dig.Unity
{

internal sealed partial class DigWorldSession
{
    private readonly CaveRoomShellProtectionPolicy _caveRoomShellProtection =
        new CaveRoomShellProtectionPolicy();
    private readonly HashSet<CellId> _caveRoomProtectedCells = new HashSet<CellId>();

    internal void SynchronizeCompletedCaveRoomProtection(
        IReadOnlyList<CaveRoomPlan> completedPlans)
    {
        if (completedPlans == null)
        {
            throw new ArgumentNullException(nameof(completedPlans));
        }

        WorldSize size = LoadSnapshot().Size;
        for (int index = 0; index < completedPlans.Count; index++)
        {
            IReadOnlyList<CellId> shell = _caveRoomShellProtection.Resolve(
                completedPlans[index],
                size);
            for (int cellIndex = 0; cellIndex < shell.Count; cellIndex++)
            {
                _caveRoomProtectedCells.Add(shell[cellIndex]);
            }
        }
    }

    private bool IsCaveRoomProtected(CellId cell)
    {
        return _caveRoomProtectedCells.Contains(cell);
    }

    private IReadOnlyList<CellId> LoadAllProtectedCells()
    {
        return _boundaryPolicy.ProtectedCells
            .Concat(_caveRoomProtectedCells)
            .Distinct()
            .OrderBy(cell => cell)
            .ToArray();
    }
}

}