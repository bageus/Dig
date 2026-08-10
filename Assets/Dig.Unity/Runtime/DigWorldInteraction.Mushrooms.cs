using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Input;

namespace Dig.Unity
{
public sealed partial class DigWorldInteraction
{
    private void ApplyMushroomChop(ContextInputDecision decision)
    {
        if (!decision.ActorId.HasValue || !decision.TargetEntityId.HasValue)
        {
            _hud!.SetStatus("Select a dwarf before chopping a mushroom.");
            return;
        }

        Dig.Presentation.Agents.AgentViewModel? selected = _agentRenderer!.SelectedModel;
        if (selected == null
            || !string.Equals(
                selected.Id,
                decision.ActorId.Value.ToString(),
                System.StringComparison.Ordinal))
        {
            _hud!.SetStatus("The selected dwarf changed before the command was committed.");
            return;
        }

        CellId workerCell = new CellId(selected.CellX, selected.CellY, selected.CellZ);
        Result result = _terrainSession!.StartDirectMushroomChop(
            decision.TargetEntityId.Value,
            decision.ActorId.Value,
            workerCell,
            _agentSession!.Tick);
        _hud!.SetCommandResult(result);
        if (result.IsSuccess)
        {
            _hud!.SetStatus("Dwarf ordered to chop mushroom.");
        }
    }
}
}
