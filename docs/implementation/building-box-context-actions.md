# BuildingBox context actions

## Ownership

`InventoryState` remains the authoritative owner of box quantity, reservation and location.
`BuildingsState` remains the owner of building and packing state.
The context presenter reads immutable `WorldItemViewModel` data and never mutates gameplay state.

## Packed box projection

A selected world BuildingBox exposes one action: `Unpack`.

The action is enabled only when the projected stack contains exactly one available,
unreserved BuildingBox. It is active when placement mode references that exact stack id.
Presentation may render the active state with a green highlight, but the color is not part
of the Domain or Presentation contract.

Pressing the action is routed through the existing placement input path. Pressing it again
while the same stack owns placement mode cancels that local placement mode. No second box,
reservation or building lifecycle is introduced.

## Existing transport path

The current production path already provides selected-resident `Alt + LMB` pickup,
BuildingBox pickup Jobs, exact stack reservations, movement into resident inventory,
cancellation cleanup, inventory-origin placement and inventory drop. Issue #330 is therefore
an integration/audit task rather than a new ownership layer.

## Follow-up

The Unity lower-center panel and action dispatch are completed in the next slice of #331.
Interrupted pack/unpack progress and the Continue action remain tied to #333 and #334.
