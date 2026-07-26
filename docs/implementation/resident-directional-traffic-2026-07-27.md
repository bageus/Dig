# Resident directional traffic implementation

- Status: `IMPLEMENTED` pending Unity Play Mode evidence
- Authoritative design: [`../design/resident-movement-occupancy-and-vertical-traversal.md`](../design/resident-movement-occupancy-and-vertical-traversal.md)
- Tracking issue: [#386](https://github.com/bageus/Dig/issues/386)

## Runtime behavior

Horizontal resident movement now uses a deterministic direction-based presentation preference rather than sorting residents by entity id:

- `X+` selects the right visual lane with a bounded `+0.18` within-cell X offset;
- `X-` selects the left visual lane with a bounded `-0.18` offset;
- stationary, depth-only and vertical transitions remain centered;
- occupied preferred lanes do not block movement, create capacity reservations or enforce chain spacing;
- presentation offsets are disposable and are never written to save data.

`TunnelTrafficCoordinator` continues to reject a same-tick reverse horizontal edge exchange. The reverse-edge restriction no longer applies to vertical traversal, so two opposite climbers may cross the same vertical link in one simulation tick.

## Ownership

`AgentState.Position` remains the only authoritative resident location. Directional lanes belong to Presentation and are derived from the previous and current logical cells. Resident identity, render order and collection order do not participate in lane selection.

## Regression evidence

Automated coverage includes:

- pure deterministic lane selection for `X+`, `X-`, stationary, depth and vertical transitions;
- shared-cell occupancy and horizontal reverse-edge rejection;
- opposite vertical crossing in one tick;
- source contracts rejecting the former ID-sorted crowd-offset implementation;
- a Unity Play Mode scenario that renders opposite horizontal transitions and checks the resulting visual separation.

The Play Mode source is present, but current CI does not execute Unity Test Runner. The system therefore remains `APPROVED` in the system index until issue #15 provides retained Play Mode results and logs.
