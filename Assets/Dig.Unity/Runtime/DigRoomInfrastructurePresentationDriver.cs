using System;
using System.Text;
using UnityEngine;

namespace Dig.Unity
{

[DisallowMultipleComponent]
internal sealed class DigRoomInfrastructurePresentationDriver : MonoBehaviour
{
    private DigTerrainWorkSession? _session;
    private DigRoomInfrastructureRenderer? _renderer;
    private Func<bool>? _planningVisibility;
    private string _lastSignature = string.Empty;

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

        bool planningVisible = _planningVisibility();
        _renderer.SetPlanningOverlayVisibility(_planningVisibility());
        var rooms = _session.LoadRoomInfrastructurePresentation();
        StringBuilder signature = new StringBuilder(
            planningVisible ? "visible:" : "hidden:");
        for (int index = 0; index < rooms.Count; index++)
        {
            RoomInfrastructureViewModel room = rooms[index];
            signature.Append(room.Id).Append(':')
                .Append(room.Version).Append(':')
                .Append(room.Status).Append(':')
                .Append(room.UpgradeOrderCount).Append(':')
                .Append(room.DeliveredUnits).Append(':')
                .Append(room.ConsumedUnits).Append(':')
                .Append(room.RequestedPurpose).Append(':')
                .Append(room.ActivePurpose).Append(':')
                .Append(room.BlockReason).Append(':')
                .Append(room.CancellationAllowed).Append(';');
        }

        string signatureValue = signature.ToString();
        if (!force && signatureValue == _lastSignature)
        {
            return;
        }

        _lastSignature = signatureValue;
        _renderer.Render(rooms);
    }
}

}
