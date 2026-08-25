using System;
using Dig.Presentation.Rooms;
using UnityEngine;

namespace Dig.Unity
{

[DisallowMultipleComponent]
internal sealed class DigRoomInfrastructurePresentationDriver : MonoBehaviour
{
    private DigTerrainWorkSession? _session;
    private DigRoomInfrastructureRenderer? _renderer;
    private Func<bool>? _planningVisibility;
    private long _lastSignature = long.MinValue;

    internal void Initialize(
        DigTerrainWorkSession session,
        DigRoomInfrastructureRenderer renderer,
        Func<bool> planningVisibility)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _planningVisibility = planningVisibility
            ?? throw new ArgumentNullException(nameof(planningVisibility));
        Refresh(force: true);
    }

    private void LateUpdate()
    {
        Refresh(force: false);
    }

    private void Refresh(bool force)
    {
        if (_session == null || _renderer == null || _planningVisibility == null)
        {
            return;
        }

        _renderer.SetPlanningOverlayVisibility(_planningVisibility());
        var rooms = _session.LoadRoomInfrastructurePresentation();
        long signature = 17;
        for (int index = 0; index < rooms.Count; index++)
        {
            signature = unchecked((signature * 31) + rooms[index].Version);
            signature = unchecked((signature * 31) + rooms[index].ConsumedUnits);
            signature = unchecked((signature * 31) + rooms[index].UpgradeOrderCount);
        }

        if (!force && signature == _lastSignature)
        {
            return;
        }

        _lastSignature = signature;
        _renderer.Render(rooms);
    }
}

}
