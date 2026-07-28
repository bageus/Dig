# Destructible barrels implementation

Статус: `IMPLEMENTED` — Domain/Application/runtime slice и автоматические regression fixtures реализованы в #444 и выровнены с полным observable workflow в follow-up PR #468. Статус `VERIFIED` требует фактического запуска Unity Test Runner.

Authoritative design: [`../design/destructible-barrels.md`](../design/destructible-barrels.md).

Tracking issue: [#443](https://github.com/bageus/Dig/issues/443).

## Владение состоянием

- `Dig.Domain.WorldObjects.BarrelState` владеет stable barrel identity, supported/falling/destroyed lifecycle, authoritative cell, сохранённым quantity-1 contents manifest, materialized marker и version.
- ordinary `JobSystem` владеет `BarrelAttackJobDefinition`, назначенным resident, travel/work/finalize lifecycle и work-position reservation.
- barrel target намеренно не имеет exclusive reservation: несколько residents могут одновременно выполнять attack jobs, а optimistic barrel version/generation разрешает только один destruction commit.
- `InventoryState` создаёт ровно один quantity-1 unit в former barrel cell после успешного commit.
- World/Navigation предоставляет support, deterministic landing и reachable attack positions.
- building placement получает отдельный immutable set supported barrel cells; movement occupancy не создаётся.
- Presentation отображает barrel geometry/collider, red hover highlight, animated sword cursor, resident attack pose, safe landing result и status, но не владеет lifecycle или contents.

## Runtime workflow

```text
selected resident + reachable barrel hover
-> shared TryResolveBarrelHit target
-> red highlight + animated sword cursor
-> ContextInputRouter: AttackBarrel
-> DigWorldInteraction.ApplyDecision: ApplyBarrelAttack
-> PrepareResidentsForDirectCommand
-> StartDirectBarrelAttackCommand
-> ordinary JobSystem travel to adjacent work position
-> ArriveAtBarrelCommand
-> resident Hit pose during PerformWork
-> one CompleteBarrelHitCommand
-> CompleteBarrelDestructionCommand
-> optimistic first commit wins
-> barrel visual/collider disappears
-> one saved stone/iron-ore unit appears in former cell
```

Concurrent jobs reserve only independent work positions. Когда два residents достигают finalize, первый valid barrel version commit создаёт output; остальные получают typed generation/version conflict, завершаются без второго output и освобождают reservations.

## Demo bootstrap

Fresh demo создаёт четыре stable barrels: две на supported cells верхней поверхности (`Z0`) и две на supported cells нижней пещеры (`Z > 0`). Каждый manifest выбирается named deterministic stream из world seed + stable barrel id и сразу сохраняется как ровно один `material.stone` либо `ore.iron`.

Barrel collider/geometry имеет высоту `0.70` world unit. Resident interaction height равна `1.52 * 0.5 = 0.76`, поэтому barrel действительно немного ниже resident. Renderer использует authoritative `DepthOrigin + CellId.Z * DepthSpacing` без отдельного front/back offset.

## Support loss и placement

После authoritative excavation/world commit support reconciliation проверяет supported barrels. Unsupported barrel использует vertical landing resolver, переходит через `Falling` к первой свободной supported cell и не получает damage, не разрушается и не materialize-ит contents. Supported barrel cell входит в combined building-placement blocked set, но не добавляется в Navigation occupancy.

## Save/load

Save format v8 хранит additive barrel section:

- id, definition, cell и lifecycle;
- contents item id, generation и materialized marker;
- falling source/landing cells;
- optimistic version;
- active barrel jobs через `BarrelAttackJobSaveCodec`.

Legacy migration pipeline добавляет пустую barrel section и последовательно доводит document до текущего формата. Fresh demo bootstrap создаёт fixtures только для новой session; load не создаёт дополнительные barrels и не повторяет output.

## Post-merge alignment fixes

- сохранены stable input identities из #467: `Sword = 5`, `Eat = 6`, barrel/food command IDs различны;
- добавлен общий `TryResolveBarrelHit`, используемый hover/cursor/LMB;
- `AttackBarrel` подключён к `ApplyBarrelAttack`; команда больше не попадает в unwired default branch;
- cursor priority выровнен с direct click priority и parser boundary защищён regression;
- barrel visual уменьшен с `1.05` до `0.70`, depth offset `-0.70` удалён;
- Job overlay/read model знает `IsBarrelAttack`; resident status — `Атакует бочку`, work pose — `Hit`;
- demo contents используют `RandomStreamCatalog` с authoritative world seed и stable barrel id;
- barrel system возвращена в `docs/systems/README.md`; удалена дублирующая строка building production.

## Regression coverage

Domain/Application:

- one-hit destruction;
- exactly-once quantity-1 output;
- concurrent first-commit-wins;
- cancel keeps intact barrel;
- safe support-loss landing;
- building blocked cell without movement occupancy;
- input routing and unavailable-target shielding;
- save/load and migration fixtures.

Unity/source contracts:

- exact interaction renderer wiring and `AttackBarrel` dispatch;
- balanced/unique sword cursor contract;
- four demo fixtures and world-seed-based stone/iron-ore contents;
- red highlight, animated sword cursor, Russian status and resident Hit pose;
- visual below resident and no depth-slab offset;
- support removal and landing wiring.

Checked-in Unity Play Mode fixtures cover:

- four supported visuals below resident height and inside their authoritative Z slabs;
- red highlight and destroyed visual removal;
- Application start → arrive → hit → finalize → exactly-one Inventory world output;
- unsupported barrel landing without destruction or contents release.

## Validation

Behavioral head `32b1e7e509c6c3f754fd5a910feb97d337865e06` passed:

- Quality run `30345393967` / run 6001: architecture and Unity source contracts, .NET restore/build/test, headless smoke, standard deterministic soak and large-settlement soak;
- Export Stage 2 v2 run `30345394196` / run 519;
- Export Stage 2 v3 run `30345393753` / run 524.

Unity Editor/Play Mode нельзя считать пройденным, пока fixtures реально не выполнены Unity Test Runner. Поэтому система остаётся `IMPLEMENTED`, а не `VERIFIED`.

## Play Mode assembly-reference regression (2026-07-28)

Unity Safe Mode reported `CS0234` in `BarrelDestructionPlayModeTests.cs` because the fixture directly composes `Dig.Application.WorldObjects` handlers and `Dig.Infrastructure.InMemory` repositories while `Dig.Unity.PlayModeTests.asmdef` had `overrideReferences: true` without references to `Dig.Application` or `Dig.Infrastructure`.

The test assembly now explicitly references both project assemblies. `BarrelPlayModeAssemblyReferenceContractTests` requires those references whenever the checked-in barrel integration fixture keeps its Application/Infrastructure imports. Barrel behavior and authoritative ownership are unchanged.
