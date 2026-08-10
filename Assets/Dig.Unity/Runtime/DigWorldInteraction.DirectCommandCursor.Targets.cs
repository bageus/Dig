using System;
using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{

public sealed partial class DigWorldInteraction
{
    private bool TryResolveBarrelHoverTarget(RaycastHit[] hits)
    {
        if (!TryResolveBarrelHit(hits, out DigBarrelVisual barrel))
        {
            return false;
        }

        Dig.Presentation.Agents.AgentViewModel? selected =
            _agentRenderer!.SelectedModel;
        bool reachable = selected != null
            && _terrainSession!.CanDirectAttackBarrel(
                barrel.Model.BarrelId,
                new CellId(selected.CellX, selected.CellY, selected.CellZ),
                out _);
        if (reachable)
        {
            _barrelRenderer!.SetHighlighted(barrel.Model.BarrelId);
            SetBarrelTargetHoverInfo();
        }

        return reachable;
    }

    private bool TryResolveMushroomHoverTarget(RaycastHit[] hits)
    {
        if (!TryResolveReachableMushroomHit(hits, out _))
        {
            return false;
        }

        SetMushroomTargetHoverInfo();
        return true;
    }

    private bool TryResolveExplicitExcavationHoverTarget(RaycastHit[] hits)
    {
        if (hits == null)
        {
            throw new ArgumentNullException(nameof(hits));
        }

        for (int index = 0; index < hits.Length; index++)
        {
            RaycastHit hit = hits[index];
            if (_renderer!.TryGetDepthDesignation(hit, out _))
            {
                return true;
            }

            if (_agentRenderer!.TryGetAgent(hit, out _)
                || (_buildingRenderer != null
                    && _buildingRenderer.TryGetBuilding(hit, out _))
                || (_itemRenderer != null
                    && _itemRenderer.TryGetItem(hit, out _)))
            {
                continue;
            }

            if (ResolveExcavationTarget(hit).HasValue)
            {
                return true;
            }
        }

        return false;
    }


}

}
