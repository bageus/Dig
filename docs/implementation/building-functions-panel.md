# Building functions panel contract

## Purpose

`BuildingFunctionsPresenter` provides the immutable read model for the mutually exclusive building-functions context panel described by issues #115 and #118.

The presenter does not mutate Buildings, Jobs or Inventory. It consumes one authoritative `BuildingSnapshot` and returns:

- stable building and definition ids;
- lifecycle and durability state;
- active packing progress;
- typed function actions with localization keys;
- disabled reason codes that Unity may localize;
- a validated packing-command draft.

## Pack action

The panel always exposes the typed `Pack` action so layout remains stable. Availability is derived only from the snapshot:

- enabled for a completed BuildingBox building with packing enabled and no active packing plan;
- disabled with `building_box.packing.already_active` while packing is active;
- disabled with `building_box.packing.damaged` for damaged buildings;
- disabled with `building_box.packing.incomplete` before completion;
- disabled with `building_box.packing.inactive` after cancellation or removal;
- disabled with `building_box.packing.disabled` for legacy or non-packable definitions.

Cancelled packing history does not render as current progress and permits a retry with new stable Job/output ids.

## Progress

Packing progress is visible only while commitment is `Active`. The view model exposes completed work, required work and a bounded ratio. Terminal packing history remains available in the authoritative snapshot but does not look like an active operation.

## Command boundary

Unity first calls `TryCreatePackingDraft` with newly allocated stable Job and output-stack ids. A draft is produced only when the current immutable action is enabled.

The adapter may then call `CreateCommand`, which maps the draft to the existing `StartBuildingBoxPackingCommand`. The Application handler repeats all authoritative checks, so a stale panel cannot start invalid packing.

The presenter never calls the handler, repository or event sink.

## Unity integration

The next adapter slice should:

1. select a completed building through the context input router;
2. request this read model from the current Buildings snapshot;
3. render the building-functions panel as the only bottom panel mode;
4. allocate deterministic command ids when the Pack button is pressed;
5. dispatch the typed Application command;
6. refresh the building, Job and Inventory views after the result.

No GameObject name or localized label is used as an identifier.
