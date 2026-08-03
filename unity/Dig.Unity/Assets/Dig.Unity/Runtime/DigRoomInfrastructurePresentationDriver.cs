using System;
using UnityEngine;

namespace Dig.Unity
{

[DisallowMultipleComponent]
internal sealed class DigRoomInfrastructurePresentationDriver : MonoBehaviour
{
    private DigTerrainWorkSession? _session;
    private DigRoomInfrastructureRenderer? _renderer;
    private long _lastSignature = long.MinValue;

    internal void Initialize(
        DigTerrainWorkSession session,
        DigRoomInfrastructureRenderer renderer)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        Refresh(force: true);
    }

    private void LateUpdate()
    {
        Refresh(force: false);
    }

    private void Refresh(bool force)
    {
        if (_session == null || _renderer == null)
        {
            return;
        }

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
