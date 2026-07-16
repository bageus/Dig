# Resident BuildingBox inventory UI

The selected-resident HUD reads an immutable inventory snapshot and lists only stacks owned by that resident.

A typed presenter marks BuildingBox stacks without relying on display names. Free boxes expose a Place action; reserved boxes remain visible but disabled.

The Place action is routed through `ContextInputRouter` on the `ResidentInventory` surface. It reuses the existing BuildingBox placement mode and starts the preview at the resident cell. Confirmation remains authoritative through `ConfirmBuildingBoxPlacementHandler`.

The HUD refreshes from the shared inventory repository while a resident is selected, so pickup, reservation, site deposit, cancellation, and consumption appear without a second UI state store.
