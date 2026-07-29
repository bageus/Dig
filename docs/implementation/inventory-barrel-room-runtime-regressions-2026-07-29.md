# Inventory stacking, barrel attack and cave-room runtime regressions — 2026-07-29

Статус: `IMPLEMENTED` after merge of the linked PR; licensed Unity Play Mode evidence remains required for `VERIFIED`.

Authoritative design:

- [`../design/runtime-stacking-barrel-and-room-recovery-decisions.md`](../design/runtime-stacking-barrel-and-room-recovery-decisions.md);
- [`../design/resident-inventory-expansion.md`](../design/resident-inventory-expansion.md);
- [`../design/destructible-barrels.md`](../design/destructible-barrels.md);
- [`../design/excavation-room-templates-and-deposits.md`](../design/excavation-room-templates-and-deposits.md);
- [`../design/runtime-selection-excavation-item-placement-decisions.md`](../design/runtime-selection-excavation-item-placement-decisions.md).

Tracking: [#67](https://github.com/bageus/Dig/issues/67), [#87](https://github.com/bageus/Dig/issues/87), [#443](https://github.com/bageus/Dig/issues/443).

## Resident inventory stacking

Resident ingress already preferred a compatible occupied slot, but legacy/separate unit stacks could remain in different resident slots indefinitely. `NormalizeResidentInventory` now deterministically consolidates unreserved, non-held stacks with the same `ItemId` up to `MaximumStackSize` before assigning slots. The earliest stable slotted stack survives; quantity movement emits the normal `ItemStackMoved` event. Different item ids and action-owned stacks are never merged. Pickup capacity projection now counts free capacity inside compatible stacks instead of requiring a new empty slot.

## Barrel attack surface routing

The previous air-route correction accepted only `SupportedWalk`. A barrel across a supported depth transition therefore became unreachable, which removed both the red hover and sword cursor and prevented attack creation. Barrel routes now accept `SupportedWalk` and `DepthTraverse` only while every route cell has full actor support. Attack work positions are same-height neighbours on `X` or `Z`; vertical neighbours, shaft gaps and climbing routes remain invalid.

## Paused cave rooms and repeated designation

Eraser no longer discards the identity/provenance of an unfinished room. It cancels the remaining designations/jobs while preserving the exact full plan as paused. Reapplying the same preset at the same entrance uses `CaveRoomResumePlanner`, skips already completed full/half-cell targets and designates only unfinished targets. Arbitrary open rock without paused provenance is still rejected as an upgrade.

## Medium-room preview

The old pointer resolver varied only the vertical offset and fixed the room anchor X to the pointer X. That assumption fails for even-width presets such as medium `8 -> 7 -> 6`, which have no single central cell. `CaveRoomPlacementCandidateResolver` now enumerates every vertical level and horizontal anchor whose centered row profile contains the pointer cell. The first valid deterministic candidate is rendered; invalid diagnostics still use the best candidate.

## Unity compile regressions after PR #514

The medium-room resolver introduced an explicit `IReadOnlyList<CellId>` local in `DigWorldInteraction.CaveRooms.cs`, but that Unity compilation unit did not import `System.Collections.Generic`. Unity therefore reported `CS0246` for `IReadOnlyList<>`; the following `candidates.Count` expression degraded into the secondary `CS0019` method-group diagnostic, and the missing runtime assembly caused the later Mono.Cecil EditMode assembly-resolution error. The runtime file now imports the required namespace. The source-contract regression requires both the namespace import and the typed candidate collection so this exact compile break cannot return while hosted Unity execution remains activation-blocked.

The first local compile after that runtime fix then reached `CaveRoomReapplyAndMediumPreviewPlayModeTests` and reported `CS1503`: NUnit resolved `Does.Not.Contain` as its string constraint and could not accept a `CellId`. The fixture now captures `resumed.Plan!` in a stable local and checks the excavation cell collection with `Has.No.Member(completed.Cell)`. `UnityRuntimeEvidenceGateTests` protects this exact collection-constraint contract.

## Regression coverage

- Domain tests for same-item consolidation, different-item isolation and reservation safety;
- pickup-capacity source contract for compatible stack capacity;
- barrel route contract for supported depth and rejection of air/climb paths;
- medium-room candidate enumeration from every front-silhouette cell;
- paused-room resume planner preserving full provenance and excluding completed targets;
- Unity source contracts for paused-plan lifecycle, required collection namespace, compilable collection assertion and the shared hover/click barrel resolver.
