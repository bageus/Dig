# Unity tool preparation policy controls

## Purpose

The Unity HUD exposes the existing `JobToolPreparationMode` choice without moving simulation ownership into the presentation layer.

The selected policy applies only to future Job assignment attempts. Existing claimed or in-progress Jobs keep their resident, tool reservation and Inventory state.

## Modes

- `Automatic` allows `InventoryJobToolPreparationService` to perform a safe carried-tool switch before the Job claim.
- `Suggest only` selects and reserves the matching tool but leaves Inventory locations unchanged. The Job overlay records `Suggested` instead of `Switched`.

The runtime starts in `Automatic` mode.

## Composition

`JobToolPreparationModeControl` is a small Presentation-owned selection state implementing `IJobToolPreparationModeSource`.

Unity passes the same control to the tool-aware assignment handlers for dynamic digging, BuildingBox packing and BuildingBox assembly. `AssignAvailableJobsHandler` resolves the live source when present; command-only callers without a source continue to use `AssignAvailableJobsCommand.ToolPreparationMode` exactly as before.

HUD buttons update only the selection state. They do not execute a Job command, mutate Inventory or rewrite retained assignment diagnostics.

## Validation

Engine-independent tests verify:

- the control defaults to `Automatic` and changes only on an explicit selection;
- a live `Suggest` source overrides an automatic command without invoking tool preparation;
- a live `Automatic` source overrides a suggest command and invokes tool preparation;
- the reported preparation outcome matches the effective mode.

The normal Quality workflow validates architecture boundaries, file sizes, C# 9 compatibility, Release build, all tests, headless smoke and deterministic soak profiles. Unity Editor button interaction remains a local Play Mode check.
