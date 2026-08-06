# Issue 574 — room marker, HUD и partial upgrade visuals

Статус: `READY FOR REVIEW` в PR #599.

Authoritative specification: [`../design/room-purposes-upgrades-and-tunnel-reinforcement.md`](../design/room-purposes-upgrades-and-tunnel-reinforcement.md).  
Tracking issue: [#574](https://github.com/bageus/Dig/issues/574).  
Implementation plan: [`issue-574-room-infrastructure-implementation-plan-2026-08-03.md`](issue-574-room-infrastructure-implementation-plan-2026-08-03.md).

## Scope

Slice 4B-2b проецирует уже реализованный authoritative room-upgrade lifecycle в Unity Presentation:

- world marker для каждой completed template room;
- central HUD menu с order count `0|1`;
- requested/active purpose и delivery/work progress;
- pre-work cancellation availability и typed blocker;
- rebuildable partial visual для каждой committed material unit;
- input shielding до resident movement/excavation routing.

Slice не выбирает default для `Q-ROOM-003` или `Q-ROOM-007`, не добавляет purpose bonuses и не задаёт compact building layouts.

## Authoritative read model

`RoomInfrastructurePresenter` объединяет:

- `RoomInfrastructureSnapshot` как owner lifecycle, purpose и material progress;
- `CompletedRoomInfrastructureProvenance` как owner stable template identity и exact room cells;
- `RoomInfrastructureDiagnosticsProjector` как owner cancellation/block state.

Presentation model содержит stable room/template identity, template kind, lifecycle, count `0|1`, requested/active purpose, material required/delivered/consumed values, completed exact material units, room bounds и marker position.

Missing provenance или identity drift отклоняются. Presentation не хранит gameplay progress и не создаёт второй источник истины.

## Marker и central HUD

- одна clickable sphere-marker создаётся для каждой authoritative room projection;
- marker click обрабатывается раньше resident movement и excavation;
- click очищает competing resident/creature/job/building/BuildingBox/cell/tunnel selections и consumes input;
- existing central blocking bottom HUD показывает template, status, count, purpose, delivery/work percentages, material ledger и blocker;
- `Improve` enabled только при authoritative `Unimproved + count 0`;
- `Cancel improvement` enabled только при authoritative cancellation diagnostics;
- purpose choices: `None`, `Bedroom`, `KitchenDining`, `Workshop`, `Farm`;
- pre-order selected purpose является только transient UI intent; после successful order authoritative `RequestedPurpose` становится единственным source;
- resident, building, job, BuildingBox, marquee, Vuker и terrain-cell selection очищают selected room.

Runtime HUD strings соблюдают project English-only source contract.

## Partial visual projection

Каждый completed `RoomMaterialUnitId(item, ordinal)` создаёт stable collider-free piece:

- stone — распределённые floor tiles;
- mushroom leg — deterministic paired posts;
- iron — distributed diagonal braces;
- crystal — distributed accents.

Placement зависит от authoritative room bounds, exact ordinal и required material count. Удаление completion facts удаляет rebuildable pieces. Visuals не владеют reservations, navigation, jobs или room lifecycle.

## Runtime refresh

`DigRoomInfrastructurePresentationDriver` каждый LateUpdate читает authoritative session projection, вычисляет deterministic signature и обновляет renderer только при изменении room versions/progress/count.

Это гарантирует refresh после order, delivery, work commit, cancel, completion и load без обратной зависимости Application handlers от Presentation callback.

## Regression coverage

Добавлены:

- presenter regression для marker bounds, count, purposes, cancellation lock и delivery/work progress;
- rejection missing completed provenance;
- Unity source-contract regression для bootstrap/driver/HUD/commands/input ordering/selection clearing/collider-free visuals;
- checked-in Play Mode scenario для clickable marker, единственного enabled collider, selection retention и rebuildable progress removal.

## Validation

Code head `88e89a6d2d1545a80aae248f068d130f6c71694a`:

- Quality run `30829868966`: success;
- architecture, file-size, C# 9 compatibility, compiler baseline, dependency и Domain-boundary checks passed;
- Unity source contracts and native-field initialization checks passed;
- Release build: `0` warnings, `0` errors;
- full .NET suite: `1463/1463`;
- room presenter and Unity composition regressions passed;
- headless smoke passed at tick `20`;
- standard deterministic soak replay hash `84DF20CCAE6B6CD42CB9B3B07415D468D45E117F8F3B6A1A675DA0A329CB3479`;
- large deterministic soak with 64 residents replay hash `28CF96B7C7F7FC12CD859AB20E837FAC091FA3FF7B6F20E1B693AA340A303F0C`;
- Unity workflow `30829869713` recorded blocked evidence;
- actual Unity EditMode/PlayMode execution and executed-runtime-evidence validation were skipped because activation was unavailable.

Checked-in Play Mode coverage therefore exists, but no runtime verification claim is made.

## Remaining

- actual licensed Unity Play Mode execution for the complete marker → HUD → order → delivery → work → completion workflow;
- purpose bonuses and compact profiles remain blocked by `Q-ROOM-003` / `Q-ROOM-007`;
- manual tunnel `U` placement and collapse remain later slices.
