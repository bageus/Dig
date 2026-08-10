using System;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Input;
using UnityEngine;

namespace Dig.Unity
{

public sealed partial class DigWorldInteraction
{
    private bool TryResolveBarrelHit(
        RaycastHit[] hits,
        out DigBarrelVisual barrel)
    {
        if (hits == null)
        {
            throw new ArgumentNullException(nameof(hits));
        }

        for (int index = 0; index < hits.Length; index++)
        {
            if (_barrelRenderer != null
                && _barrelRenderer.TryGetBarrel(hits[index], out DigBarrelVisual candidate))
            {
                barrel = candidate;
                return true;
            }
        }

        barrel = null!;
        return false;
    }

    private void ApplyBarrelAttack(ContextInputDecision decision)
    {
        if (!decision.ActorId.HasValue || !decision.TargetEntityId.HasValue)
        {
            _hud!.SetStatus("Select a dwarf before attacking a barrel.");
            return;
        }

        Dig.Presentation.Agents.AgentViewModel? selected = _agentRenderer!.SelectedModel;
        if (selected == null
            || !string.Equals(
                selected.Id,
                decision.ActorId.Value.ToString(),
                StringComparison.Ordinal))
        {
            _hud!.SetStatus(
                "The selected dwarf changed before the barrel order committed.");
            return;
        }

        CellId workerCell = new CellId(selected.CellX, selected.CellY, selected.CellZ);
        Result result = _terrainSession!.StartDirectBarrelAttack(
            decision.TargetEntityId.Value,
            decision.ActorId.Value,
            workerCell,
            _agentSession!.Tick);
        _hud!.SetCommandResult(result);
        if (result.IsSuccess)
        {
            _hud.SetStatus("Атакует бочку");
        }
    }
}

}
