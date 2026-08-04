# Runtime building/UI, Health bar, unpack and forced-pickup correction

Статус: `APPROVED`.

Tracking issue: [#634](https://github.com/bageus/Dig/issues/634).

Связанные authoritative specifications:

- [`demo-starting-scenario.md`](demo-starting-scenario.md);
- [`building-box-placement-and-packing.md`](building-box-placement-and-packing.md);
- [`building-production-and-internal-supply.md`](building-production-and-internal-supply.md);
- [`contextual-input-cursors-and-selection.md`](contextual-input-cursors-and-selection.md);
- [`item-interaction-capabilities.md`](item-interaction-capabilities.md);
- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md);
- [`enemy-combat-and-cave-encounters.md`](enemy-combat-and-cave-encounters.md).

## 1. Confirmed observable behavior

### Demo buildings

- obsolete `Box Workshop` / `demo.workshop.box` / `demo.building_box.workshop` content is removed;
- fresh demo contains only the completed campfire and the separate packed campfire BuildingBox defined by `demo-starting-scenario.md`;
- no runtime roster row, completed visual, box item or representative-profile alias for Box Workshop remains.

### Building selection and production HUD

- completed-building selection keeps model highlight and roster-row synchronization;
- no footprint platform, flat green selection surface or separate world-space selection base is rendered;
- production panel does not repeat the selected building name above its controls;
- production panel does not show the persistent hover instruction or a required-material tooltip area;
- product/stock icons, queue counters, progress, toggles and Pack action remain functional;
- flat input/output tray platforms are hidden; real internal-stock units and output packages remain visible and interactive.

### Health bars

- resident and hostile Health bars are notification indicators, not differently sized actor-specific gauges;
- all bars have the same world-space width regardless of parent/model scale;
- each bar is positioned above the top renderer bound of its owner with a stable gap and follows animation/movement;
- bar geometry remains collider-free and camera-facing;
- visibility rules and authoritative Health values are unchanged.

### Production output placement

- output packages/BuildingBoxes use the nearest eligible right-side row;
- cells in that row are filled contiguously from the building edge outward;
- no empty cell is reserved for possible future unpacking;
- only after the primary row cannot provide enough supported cells may later footprint rows be considered in stable order;
- occupied/unsupported cells remain skipped and side/left/rear fallback remains forbidden.

### BuildingBox confirmation

- hover owns the currently visible preview;
- one LMB commits exactly that visible valid preview without a second pointer-cell resolution in the same click frame;
- confirmation uses the shared `ContextInputRouter` placement decision and creates exactly one relocation or assembly plan/job;
- success closes interactive placement mode; failure retains the preview and exposes the typed reason.

### Forced item/material pickup

- ordinary item/material LMB with one selected living resident creates one exact-stack pickup job or one typed rejection;
- direct-command preparation must cancel/release the resident's previous active work before creating the pickup;
- cancellation writes may not be overwritten by an older Inventory snapshot;
- item targets consume the pointer before movement/excavation fallback.

## 2. Ownership

- Demo composition owns which initial entities exist.
- Buildings/Inventory/Jobs remain authoritative for boxes, plans, reservations and pickup jobs.
- Production owns output package placement.
- Presentation owns only selection visuals, HUD layout and Health-bar geometry.
- `ContextInputRouter` remains the single owner of pointer priority and placement confirmation intent.

## 3. Regression acceptance

- source and runtime tests reject every obsolete Box Workshop stable id/name in production/demo code and catalogs;
- fresh demo projects exactly one completed campfire and one packed campfire box;
- no building footprint platform or production tray is created;
- production HUD has no heading/tooltip text but preserves controls;
- scaled resident/enemy parents produce equal Health-bar world width above renderer bounds;
- multi-row output placement fills the primary row contiguously;
- visible valid BuildingBox preview commits on one LMB without re-resolving hover;
- replacing an active job with exact-stack pickup leaves old reservations released and creates the new pickup job;
- build, unit/integration, deterministic and source-contract suites pass;
- licensed Unity Play Mode remains required for `VERIFIED` status.
