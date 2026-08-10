using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.World;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Unity
{

internal sealed partial class DigWorldSession
{
    private readonly NaturalCaveShellProtectionPolicy _naturalCaveProtection =
        new NaturalCaveShellProtectionPolicy();
    private readonly HashSet<CellId> _naturalCaveProtectedCells =
        new HashSet<CellId>();

    internal void InitializeNaturalCaveProtection(TunnelDemoLayout layout)
    {
        if (layout == null)
        {
            throw new ArgumentNullException(nameof(layout));
        }

        IReadOnlyList<CellId> perimeter = _naturalCaveProtection.Resolve(
            layout,
            LoadSnapshot().Size);
        _naturalCaveProtectedCells.Clear();
        for (int index = 0; index < perimeter.Count; index++)
        {
            _naturalCaveProtectedCells.Add(perimeter[index]);
        }
    }

    private bool IsNaturalCaveProtected(CellId cell)
    {
        return _naturalCaveProtectedCells.Contains(cell);
    }

    private IReadOnlyList<CellId> LoadAllProtectedCells()
    {
        return _boundaryPolicy.ProtectedCells
            .Concat(_naturalCaveProtectedCells)
            .Distinct()
            .OrderBy(cell => cell)
            .ToArray();
    }
}

}