using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Unity
{

public sealed partial class DigWorldInteraction
{
    private void ApplyProductionPackageUse(
        Dig.Presentation.Input.ContextInputDecision decision)
    {
        if (!decision.ActorId.HasValue
            || !decision.TargetEntityId.HasValue
            || _terrainSession == null
            || _agentRenderer?.SelectedModel == null)
        {
            _hud?.SetStatus("input.production_package.use_missing_target");
            return;
        }

        Dig.Presentation.Agents.AgentViewModel selected =
            _agentRenderer.SelectedModel;
        if (!string.Equals(
                selected.Id,
                decision.ActorId.Value.ToString(),
                System.StringComparison.Ordinal))
        {
            _hud?.SetStatus("input.production_package.resident_stale");
            return;
        }

        Result result = _terrainSession.StartDirectProductionPackageUse(
            decision.TargetEntityId.Value,
            decision.ActorId.Value,
            new CellId(selected.CellX, selected.CellY, selected.CellZ),
            _agentSession!.Tick);
        _hud?.SetStatus(result.IsSuccess
            ? "Using production box."
            : result.Error?.Message ?? "Production box is unavailable.");
    }
}

}
