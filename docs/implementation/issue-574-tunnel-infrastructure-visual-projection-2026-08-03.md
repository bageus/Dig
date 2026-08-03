# Issue 574 — визуальная проекция завершённой инфраструктуры тоннелей

Статус: `IMPLEMENTED IN BRANCH`.

Authoritative specification: [`../design/room-purposes-upgrades-and-tunnel-reinforcement.md`](../design/room-purposes-upgrades-and-tunnel-reinforcement.md).  
Tracking issue: [#574](https://github.com/bageus/Dig/issues/574).  
Runtime provenance dependency: PR [#590](https://github.com/bageus/Dig/pull/590).  
Implementation PR: [#591](https://github.com/bageus/Dig/pull/591).

## 1. Цель slice

Завершить Slice `2B-2b2b`: отобразить уже подтверждённые authoritative completion facts для деревянных опор и каменной отделки вертикального перекрёстка, не создавая нового gameplay owner и не выбирая default для `Q-TUNNEL-008`.

## 2. Authoritative input

Единственный источник визуальных экземпляров — `TunnelInfrastructureSnapshot`:

- `TunnelStructuralAnchorKind.WoodenSupport` создаёт wooden-support visual;
- `TunnelStructuralAnchorKind.Origin` не создаёт visual;
- `TunnelStructuralAnchorKind.Door` остаётся building-owned visual и не дублируется;
- `CompletedJunctionStoneTrimCells` создаёт decorative junction-stone-trim visual;
- pending automatic targets и незавершённые jobs не становятся completed visuals.

Presentation не изменяет `TunnelInfrastructureState`, `JobSystem`, `InventoryState` или World.

## 3. Presentation projection

Добавлены:

- `TunnelInfrastructureVisualKind`;
- `TunnelInfrastructureVisualViewModel`;
- `TunnelInfrastructureVisualVolumeViewModel`;
- `TunnelInfrastructureVisualPresenter`.

Stable instance identity выводится только из вида и exact XYZ cell:

- `tunnel:wooden-support:{x}:{y}:{z}`;
- `tunnel:junction-stone-trim:{x}:{y}:{z}`.

Presenter:

- объединяет duplicate completed support cells между segment snapshots;
- объединяет duplicate completed trim cells;
- сортирует экземпляры по kind, cell и stable id;
- сохраняет authoritative snapshot version;
- не проецирует origin/door anchors как опоры.

## 4. Unity renderer

`DigTunnelInfrastructureRenderer` создаёт rebuildable collider-free visuals:

- wooden support — один вертикальный деревянный beam, сдвинутый к front side tunnel cell;
- junction stone trim — четыре низких каменных rail вокруг пола junction cell;
- exact position использует `CellId.X`, `DigTunnelProjection.WalkSurfaceY(CellId.Y)` и authoritative depth projection для `CellId.Z`;
- shader resolution переиспользует лёгкий `Dig/Stylized Unlit`, затем URP Unlit и только затем Standard fallback;
- renderer не добавляет `Collider`, не участвует в pointer input, navigation, reservations или collision;
- следующий snapshot удаляет visual, если authoritative completion fact исчез.

`DigWorldRenderer` хранит только последний immutable view model и делегирует materialized visuals renderer-компоненту.

## 5. Runtime synchronization

`DigTerrainWorkSession` получает `TunnelInfrastructureVisualPresenter` и visual sink.

Проекция публикуется:

1. после topology reconciliation и automatic support/trim synchronization;
2. немедленно после успешного `CompleteTunnelAutomaticWorkHandler` в `Finalize`.

`DigAgentSimulationDriverBase.Initialize` связывает sink с `DigWorldRenderer.SetTunnelInfrastructureVisuals`.

Визуальное обновление не является условием authoritative commit и не создаёт вторую completion ledger.

## 6. Regression coverage

Добавлены проверки:

- только completed wooden supports и completed junction trim попадают в projection;
- Origin и Door anchors не создают duplicate support visuals;
- duplicate support/trim cells проецируются один раз;
- repeated projection сохраняет stable ids и order;
- runtime sink связан с WorldRenderer;
- successful automatic-work Finalize публикует новый snapshot;
- Unity renderer использует `MeshFilter`/`MeshRenderer`, не содержит Collider owner и не использует random;
- checked-in Play Mode scenario создаёт оба visual kind, проверяет XYZ placement, child geometry, отсутствие colliders и removal после empty authoritative projection.

## 7. Фактическая проверка

Code head `9fcb643a3d9cd2d3ea5b428f88d941305772d66e` прошёл Quality run `30803914203`:

- architecture, file-size, C# 9 compatibility, compiler baseline, dependency и Domain-boundary checks — passed;
- Unity source contracts — passed;
- Release build — `0` warnings, `0` errors;
- full .NET suite — `1430/1430`;
- `TunnelInfrastructureVisualPresenterTests` — passed;
- `TunnelInfrastructureUnityRuntimeContractTests` — passed;
- headless smoke — passed at tick `20`;
- standard deterministic soak replay hash — `84DF20CCAE6B6CD42CB9B3B07415D468D45E117F8F3B6A1A675DA0A329CB3479`;
- large 64-resident deterministic soak replay hash — `28CF96B7C7F7FC12CD859AB20E837FAC091FA3FF7B6F20E1B693AA340A303F0C`.

Unity workflow `30803913001` completed only through blocked-evidence path:

- activation resolution — passed;
- actual EditMode/PlayMode execution — skipped;
- executed runtime evidence validation — skipped;
- blocked runtime evidence — recorded.

Поэтому checked-in Play Mode scenario не считается фактически исполненным, а runtime status не повышается до `VERIFIED`.

## 8. Не входит в slice

- save-document section и migration для `TunnelInfrastructureState`;
- runtime automatic-job sequence restore;
- player cancellation policy до ответа `Q-TUNNEL-008`;
- collapse workflow;
- room upgrade/purpose workflow;
- actual licensed Unity end-to-end evidence.

Следующий последовательный этап плана — Slice 3 persistence и migration.
