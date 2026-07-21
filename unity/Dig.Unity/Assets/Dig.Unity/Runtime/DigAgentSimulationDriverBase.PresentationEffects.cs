using Dig.Domain.World;
using Dig.Presentation.Rendering;
using UnityEngine;

namespace Dig.Unity
{
public abstract partial class DigAgentSimulationDriverBase
{
    private void PresentSpatialExcavationEffect(SpatialCellId cell, long tick)
    {
        DigPresentationEffectBridge? bridge =
            GetComponent<DigPresentationEffectBridge>();
        if (bridge == null) return;
        Vector3 position = DigTunnelProjection.CellWorldPosition(cell);
        PresentationEffectFact fact = new PresentationEffectFact(
            "spatial-excavation:" + cell + ":" + tick,
            PresentationEffectKind.ExcavationImpact,
            position.x,
            position.y,
            position.z,
            1d,
            tick);
        bridge.Present(new[] { fact }, Camera.main);
    }
}
}
