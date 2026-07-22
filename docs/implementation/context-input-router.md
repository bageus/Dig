# Context input router

## Purpose

`ContextInputRouter` is an engine-independent decision layer for resident selection, inventory interaction, building boxes, movement, combat and excavation. It prevents Unity raycast order or overlapping colliders from deciding gameplay priority.

The router does not call repositories or Application handlers. Unity gathers confirmed facts, requests one decision, then applies:

- zero or more local Presentation effects; and
- at most one typed Application command intent.

This separation makes duplicate commands observable and testable.

## Inputs

A pointer event contains:

- surface: world, resident roster or resident inventory;
- left/right button;
- click count;
- Alt modifier;
- whether blocking UI shields the world pointer.

`ContextInputState` contains only confirmed selection and mode facts:

- selected resident and liveness;
- selected completed building;
- selected inventory stack and typed capabilities;
- active/valid building placement plus reason code;
- active excavation tool.

`ContextPointerTarget` contains the raycast/read-model result:

- typed target kind;
- optional stable entity id;
- optional logical cell;
- reachability;
- whether an Alt interaction contract exists;
- target liveness.

No target is inferred by parsing GameObject names or localized text.

## World left-click priority

The router evaluates exactly one path in this order:

1. confirm or reject active building placement;
2. targeted drop of the selected inventory stack;
3. Alt pickup of a supported BuildingBox, otherwise ground-move fallback;
4. normal BuildingBox placement mode;
5. attack a living hostile target;
6. move the selected living resident to reachable ground or unsupported item/box fallback;
7. apply the active excavation tool only when no resident is selected;
8. local resident/building/ground selection.

A stale selected resident is deselected with a reason before move, attack, drop or inventory-use commands can be produced. A friendly resident target remains a selection target and is never treated as free ground.

## Right click

Right-click priority is:

1. cancel active building placement;
2. deselect the current resident;
3. preserve the current excavation mode when no resident is selected.

This guarantees that one right click cannot both deselect a resident and designate terrain.

## HUD and inventory

Roster clicks are intentional UI input and bypass world UI shielding. A single click selects; a double click adds the local camera-focus effect. Dead or stale roster targets return a controlled reason without selection.

Inventory Alt-use requires a live selected resident, a selected usable stack and a confirmed use capability. A selected BuildingBox enters the same placement effect as a world box. Unsupported interactions produce no command and may fall through to a world movement path only when the pointer target supplies a reachable logical cell.

## Panel modes

`ResolvePanelMode` produces one mutually exclusive mode:

1. `BuildingPlacement` while placement is active;
2. `ResidentInventory` for a live selected resident;
3. `BuildingFunctions` for a selected completed building;
4. `ExcavationPalette` otherwise.

Unity should render panels from this enum rather than independently toggling several booleans.

## Command boundary

`ContextInputDecision.CommandKind` is a single enum value. The supported intents are:

- confirm building placement;
- use an inventory item;
- drop an inventory stack;
- pick up a BuildingBox;
- attack a target;
- move a resident;
- apply excavation.

Local effects include selection, focus, placement preview/cancel and reason display. Selection and camera focus never mutate Domain state.

## Unity adapter

`DigWorldInteraction` now converts resident, hostile creature, Job and cell raycasts into router facts. The runtime wires:

- resident single-click selection;
- resident double-click camera focus;
- selected resident movement to an open logical cell through `MoveAgentCommandHandler`;
- right-click resident deselection without a second terrain command;
- right-click digging designation when no resident is selected;
- ground and Job local selection;
- blocking HUD pointer shielding.
- hostile creature priority before resident movement and an `AttackTarget` adapter
  that issues one authoritative `IssueCombatIntentCommand` with
  `CombatIntentSource.PlayerOrder`;
- release-to-commit excavation eraser batches. A drag gathers preview cells;
  preflight then removes designations in one world version, cancels matching
  unfinished dig Jobs, releases their reservations and removes incomplete cave
  and tunnel plans. Invalid input changes neither aggregate.

`DigAgentSession.MoveResident` is a thin Application adapter. `DigAgentSimulationDriver.MoveResident` refreshes immutable Agent views, visuals and HUD after the accepted command. Camera focus changes only `DigCameraController` presentation state.

Attack input creates an intent only. Damage remains owned by the combat execution
handler and can never be committed by a click or animation callback. A stale,
dead or non-entity hostile visual produces the typed rejection path.

## Validation

Regression tests cover:

- full placement/use/drop/box/attack/move/excavation priority;
- unsupported Alt fallback;
- UI shielding;
- right-click cancellation and deselection;
- roster double-click focus;
- friendly resident selection;
- stale selected residents and dead targets;
- unreachable movement reasons;
- completed-building and ground selection;
- mutually exclusive panel modes.
- authoritative attack-intent dispatch;
- atomic eraser success, reservation cleanup and invalid-batch rollback.

Quality also validates C# 9 compatibility for the Unity adapter, normal headless smoke and both deterministic simulation soak profiles.
