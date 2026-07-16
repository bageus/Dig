# Resident carried-item display

This slice reads equipped item locations from the authoritative inventory snapshots and projects them onto resident visuals.

The inventory aggregate now rejects a stack that is not carried by the acting resident and rejects a second occupied slot. The Unity session also checks the combined building and terrain inventories before routing the action.

The presentation model contains resident, stack, and item identifiers. Duplicate equipped entries for one resident are reported as invalid state.

Unity caches the projection independently of resident creation order. It refreshes during initialization, each simulation tick, and immediately after successful inventory actions. The child geometry uses the Ignore Raycast layer and has no collider, preserving resident selection.

Focused tests cover world-stack rejection, foreign-owner rejection, second-slot rejection, and duplicate cross-inventory projection detection. The Quality workflow covers architecture, compatibility, Unity validation, build, tests, smoke, and deterministic soak runs.
