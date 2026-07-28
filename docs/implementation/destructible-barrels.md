# Destructible barrels implementation

Статус: `IMPLEMENTED`. Domain/Application/runtime slice реализован в #444, полный observable workflow выровнен в #468, а screenshot-driven orientation/scale correction — в #478. Статус `VERIFIED` требует фактического licensed Unity Test Runner run.

Authoritative design: [`../design/destructible-barrels.md`](../design/destructible-barrels.md).

Tracking issue: [#443](https://github.com/bageus/Dig/issues/443).

Связанный cross-system regression note: [`runtime-screenshot-barrel-mushroom-stock-regression-2026-07-28.md`](runtime-screenshot-barrel-mushroom-stock-regression-2026-07-28.md).

## Владение состоянием

- `Dig.Domain.WorldObjects.BarrelState` владеет stable identity, `Supported/Falling/Destroyed`, authoritative cell, quantity-1 contents manifest, materialized marker и version.
- ordinary `JobSystem` владеет `BarrelAttackJobDefinition`, resident, travel/work/finalize lifecycle и work-position reservation.
- barrel target намеренно не имеет exclusive reservation: concurrent jobs разрешены, optimistic version/generation допускает один destruction commit.
- `InventoryState` создаёт ровно один quantity-1 unit в бывшей barrel cell.
- World/Navigation предоставляет support, deterministic landing и reachable attack positions.
- Buildings читает отдельный immutable set supported barrel cells; movement occupancy не создаётся.
- Presentation отображает world-upright geometry/collider, red highlight, sword cursor, resident Hit pose, landing projection и status, но не владеет lifecycle/contents.

## Runtime workflow

```text
selected resident + reachable barrel hover
-> shared TryResolveBarrelHit
-> red highlight + animated sword cursor
-> ContextInputRouter: AttackBarrel
-> DigWorldInteraction.ApplyDecision: ApplyBarrelAttack
-> StartDirectBarrelAttackCommand
-> ordinary JobSystem travel to adjacent work position
-> ArriveAtBarrelCommand
-> resident Hit pose during PerformWork
-> CompleteBarrelHitCommand
-> CompleteBarrelDestructionCommand
-> optimistic first commit wins
-> visual/collider disappears
-> one saved stone/iron-ore unit appears in former cell
```

Другие simultaneous jobs получают generation/version conflict, завершаются без второго output и освобождают reservations. Атака и destruction не выдают skill/stat/combat progression.

## Demo bootstrap и contents

Fresh demo создаёт четыре stable barrels: две на supported upper-surface cells (`Z0`) и две в lower cave (`Z > 0`). Каждый manifest выбирается named deterministic stream из world seed + stable barrel id и сразу сохраняется как ровно один `material.stone` либо `ore.iron`.

## Presentation contract

После screenshot correction #478:

- barrel renderer root сохраняет world orientation через `SetParent(..., worldPositionStays: true)`;
- каждый tracked visual использует `Quaternion.identity`, поэтому не наследует поворот bootstrap/terrain root;
- все предыдущие presentation dimensions равномерно умножены на `PresentationScale = 0.70`;
- итоговая visual/collider height равна `0.49` world unit;
- основание остаётся на walk surface;
- Z projection использует authoritative `DepthOrigin + CellId.Z * DepthSpacing` без отдельного front/back offset.

Эти изменения не меняют collider interaction kind, target identity, attack reachability, lifecycle или save data.

## Support loss и placement

После authoritative excavation/world commit support reconciliation проверяет supported barrels. Unsupported barrel использует vertical landing resolver, проходит `Falling -> Supported` в первой допустимой cell и не получает damage, не разрушается и не materialize-ит contents. Supported barrel cell блокирует building footprint, но не Navigation movement.

## Save/load

Save format v8 хранит:

- id, definition, cell и lifecycle;
- contents item id, generation и materialized marker;
- falling source/landing cells;
- optimistic version;
- active barrel jobs через `BarrelAttackJobSaveCodec`.

Legacy migration добавляет пустую barrel section. Fresh bootstrap выполняется только для новой session; load не создаёт дополнительные barrels и не повторяет output.

## Regression coverage

Domain/Application:

- one-hit destruction и zero progression;
- exactly-once quantity-1 output;
- concurrent first-commit-wins;
- cancel/interruption до hit сохраняет бочку;
- safe landing;
- building blocking без movement occupancy;
- save/load и migration.

Unity/source contracts:

- renderer/interaction wiring и `AttackBarrel` dispatch;
- unique cursor IDs `Sword = 5`, `Eat = 6`;
- four demo fixtures и world-seed contents;
- red highlight, sword cursor, Russian status и Hit pose;
- world-upright root, identity rotation, `PresentationScale = 0.70`, height `0.49`;
- authoritative depth slab и support-loss wiring.

Checked-in Play Mode fixtures:

- four supported barrels under a rotated parent remain world-upright, height `0.49` and inside their Z slabs;
- highlight and destroyed visual removal;
- Application start -> arrive -> hit -> finalize -> exactly-one Inventory world output;
- unsupported landing without destruction or contents release.

`Dig.Unity.PlayModeTests.asmdef` explicitly references `Dig.Application` and `Dig.Infrastructure`, required by the integration fixture.

## Validation

Behavioral head `47205ee1336eae9b3d865e36264cd55d3bff9605` passed:

- Quality `30378034458` / run 6296 — architecture/file-size/C# compatibility, all Unity source contracts, Release restore/build, full `.NET` tests, headless smoke, standard deterministic soak и large-settlement soak;
- Export Stage 2 v2 `30378034174` / run 573;
- Export Stage 2 v3 `30378034162` / run 578.

Final documentation head `fee073f9b86d5cae101284c0f51d3fd656551cdc` passed:

- Quality `30378404874` / run 6306;
- Export Stage 2 v2 `30378405095` / run 578;
- Export Stage 2 v3 `30378404856` / run 583.

Unity Play Mode workflow `30378404852` / run 65 completed at workflow level, but `Run Play Mode tests` was skipped because activation credentials are not configured. Therefore status remains `IMPLEMENTED`, not `VERIFIED`.
