using System;
using System.Collections.Generic;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.World;

namespace Dig.Application.Ecology
{

public interface ILivingMaterialEcologyRepository
{
    LivingMaterialEcologyState Get();

    void Save(LivingMaterialEcologyState state);
}

public sealed class AdvanceLivingMaterialEcologyCommand : ICommand<Result>
{
    public AdvanceLivingMaterialEcologyCommand(
        long simulationTick,
        IReadOnlyCollection<CellId> residentCells)
    {
        if (simulationTick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(simulationTick));
        }

        SimulationTick = simulationTick;
        ResidentCells = residentCells ?? throw new ArgumentNullException(nameof(residentCells));
    }

    public long SimulationTick { get; }

    public IReadOnlyCollection<CellId> ResidentCells { get; }
}

public sealed class LivingMaterialMovementDecision
{
    public LivingMaterialMovementDecision(
        bool canMove,
        CellId target,
        int nextDirection,
        string reason)
    {
        CanMove = canMove;
        Target = target;
        NextDirection = nextDirection;
        Reason = reason ?? string.Empty;
    }

    public bool CanMove { get; }
    public CellId Target { get; }
    public int NextDirection { get; }
    public string Reason { get; }
}

public static class LivingMaterialApplicationErrors
{
    public static readonly DomainError NavigationUnavailable = new DomainError(
        "ecology.living_material.navigation_unavailable",
        "The living material flat-plane navigation snapshot is unavailable.");

    public static readonly DomainError InvalidWorldCell = new DomainError(
        "ecology.living_material.invalid_world_cell",
        "The living material item is not on a supported flat navigation cell.");

    public static readonly DomainError MissingLinkedItem = new DomainError(
        "ecology.living_material.missing_linked_item",
        "The living material creature has no linked Inventory unit item.");

    public static readonly DomainError UnknownItem = new DomainError(
        "ecology.living_material.unknown_item",
        "The canonical living material item is missing from the Inventory catalog.");
}

}
