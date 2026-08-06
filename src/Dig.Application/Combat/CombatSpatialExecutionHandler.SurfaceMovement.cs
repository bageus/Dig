using Dig.Application.Agents;
using Dig.Domain.Agents;
using Dig.Domain.Combat;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Application.Combat
{

public sealed partial class CombatSpatialExecutionHandler
{
    private Result<CombatSpatialExecutionReport>? CompleteCombatSurfaceApproach(
        AdvanceCombatSpatialExecutionCommand command,
        CombatState combat,
        CombatExecutionSnapshot execution,
        AgentState actor,
        bool moved)
    {
        if (IsPursuingLastKnown(execution, actor))
            return null;

        if (!execution.TargetEntityId.HasValue
            || !execution.WeaponProfileId.HasValue
            || !execution.EngagementCell.HasValue)
            return Block(command, combat, execution,
                CombatSpatialApplicationErrors.EngagementUnavailable);

        AgentState? target = _agents.Get(execution.TargetEntityId.Value);
        if (!IsValidHostile(actor, target))
            return Advance(combat, execution, CombatExecutionStage.AcquireTarget,
                command.Tick, command.Tick, "target_requires_reacquire");

        WeaponProfile weapon = combat.Weapons.Get(execution.WeaponProfileId.Value);
        SurfacePose required = ResolveCombatSurfacePose(
            target!, weapon, execution.EngagementCell.Value);
        if (actor.SurfacePose == required)
            return null;

        Result positioned = _surfaceMoveHandler.Handle(
            new MoveAgentOnSurfaceCommand(actor.Id, required, command.Tick));
        if (positioned.IsFailure)
            return Block(command, combat, execution, positioned.Error!);

        return Report(combat.GetActiveExecution(actor.Id)!, true, null,
            moved ? "approach_surface_positioned" : "surface_approach_advanced");
    }

    private static bool IsAtCombatSurfacePose(
        AgentState actor,
        AgentState target,
        WeaponProfile weapon,
        CellId engagementCell) =>
        actor.SurfacePose == ResolveCombatSurfacePose(target, weapon, engagementCell);

    private static SurfacePose ResolveCombatSurfacePose(
        AgentState target,
        WeaponProfile weapon,
        CellId engagementCell) =>
        weapon.SpatialMode == CombatAttackSpatialMode.Melee
            ? WorkSurfacePositioning.Resolve(engagementCell, target.Position)
            : SurfacePose.FloorCentre(engagementCell);
}
}
