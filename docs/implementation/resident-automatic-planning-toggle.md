# Resident automatic planning toggle

The selected-resident clock HUD exposes an `AUTO ON` / `AUTO OFF` button. The
button is visible only when exactly one living resident is selected and changes
the authoritative `AgentState.AutomaticPlanningEnabled` preference through an
Application command.

The preference applies to future automatic job assignments. Candidate creation
for excavation, hauling, building packing and assembly reads the same projected
resident flag. Disabling the preference does not cancel an already claimed or
in-progress job and does not block explicit player assignment or movement.

The preference defaults to enabled for new residents and for older saves that do
not contain the field. Save/load stores it alongside the resident skill payload
and restores it into the live agent repository.

Regression coverage verifies the Domain event and version change, command
persistence, snapshot and presentation projection, save compatibility, the HUD
bridge, and every Unity automatic-candidate production path.
