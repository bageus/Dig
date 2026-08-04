# Service marker visibility and tunnel junction correction — 2026-08-04

Status: `IMPLEMENTED IN BRANCH`; material-recovery policy remains `QUESTIONNAIRE`.

Authoritative correction: [`../design/service-markers-and-tunnel-overwrite-correction.md`](../design/service-markers-and-tunnel-overwrite-correction.md).

Related specifications:

- [`../design/world-item-gravity-selection-and-pickup.md`](../design/world-item-gravity-selection-and-pickup.md);
- [`../design/contextual-input-cursors-and-selection.md`](../design/contextual-input-cursors-and-selection.md);
- [`../design/presentation-input-ui-and-diagnostics.md`](../design/presentation-input-ui-and-diagnostics.md);
- [`../design/room-purposes-upgrades-and-tunnel-reinforcement.md`](../design/room-purposes-upgrades-and-tunnel-reinforcement.md).

Tracking issues: [#390](https://github.com/bageus/Dig/issues/390), [#574](https://github.com/bageus/Dig/issues/574).

## Report

Local Unity screenshots showed large grey cylinders above ordinary world items, a selection circle above a completed building and a clickable cylinder at a vertical/horizontal tunnel junction. The junction marker opened a `TUNNELAUTOMATIC:JUNCTIONSTONETRIM` job panel even though the confirmed design makes junction/floor stone trim placement-only.

Completed support/trim geometry also disappears when topology removes its cell (room/cave expansion or downward excavation). The current aggregate drops the infrastructure provenance, but no authoritative rule defines where the already consumed leg/stone must be recovered.

## Root causes

- `DigJobRenderer` created a visible cylinder for every surface job target.
- the Jobs overlay was visible by default in Release/Debug profiles, so ordinary pickup/haul/production targets looked like item interaction cylinders.
- `DigWorldOverlayRenderer` created one elevated selection cylinder for every footprint cell of the selected building.
- the placement-only correction from PR #606 was merged only into a stacked feature branch and never reached `main`; current runtime still synchronized automatic junction-trim jobs.
- topology replacement removes anchors/completed trim whose cell is no longer in the segment. Material was already consumed at completion and no recovery destination exists in the specification.

## Implemented correction

- Jobs world markers start hidden and remain an explicit `F3` diagnostic toggle.
- tunnel-infrastructure jobs are typed in the job read model and never create world markers even when Jobs diagnostics are enabled.
- item interaction continues through its invisible trigger collider and visible item geometry; no service cylinder is introduced.
- selected buildings retain model tint/scale, roster-row highlight and panel state but no overhead footprint cylinders.
- automatic junction-trim synchronization is replaced with placement-only cleanup; legacy non-terminal jobs are cancelled and release Inventory reservations.
- automatic candidates and automatic finalization accept only wooden supports; junction stone trim cannot consume stone or grant Stonework through the legacy automatic path.

## Deliberately unresolved

`Q-TUNNEL-009` owns material recovery when excavation/topology destroys completed support or stone trim. No destination is invented in this branch. The existing geometry/provenance removal remains unchanged until the user confirms whether the material drops in the exact world cell, enters the excavating resident inventory, or follows another rule.

## Regression coverage

- overlay visibility contract: Jobs hidden by default and opt-in by user toggle;
- job projection marks tunnel infrastructure explicitly and renderer rejects its world marker;
- source contract rejects selected-building overhead cylinders and world-item cylinder geometry;
- Application tests verify placement-only junction sync and legacy reservation cleanup;
- automatic-work tests reject junction finalization before material/skill mutation;
- Unity runtime source contract requires only wooden-support automatic candidates.

## Verification boundary

Repository Quality, Release build, full .NET suite, headless smoke, deterministic soaks and Stage 2 exports are required on the final PR head. Checked-in Unity interaction regressions are runtime evidence only when a licensed Unity Test Runner actually executes them; blocked activation does not produce `VERIFIED` evidence.
