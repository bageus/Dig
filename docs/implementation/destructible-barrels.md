# Destructible barrels implementation

Статус: `IMPLEMENTED` — Domain/Application/runtime slice и автоматические regression fixtures добавлены в draft PR #444. Статус `VERIFIED` требует фактического запуска Unity Test Runner.

Authoritative design: [`../design/destructible-barrels.md`](../design/destructible-barrels.md).

Tracking issue: [#443](https://github.com/bageus/Dig/issues/443).

## Владение состоянием

- `Dig.Domain.WorldObjects.BarrelState` владеет stable barrel identity, supported/falling/destroyed lifecycle, authoritative cell, сохранённым quantity-1 contents manifest, materialized marker и version.
- ordinary `JobSystem` владеет `BarrelAttackJobDefinition`, назначенным resident, travel/work/finalize lifecycle и work-position reservation.
- barrel target намеренно не имеет exclusive reservation: несколько residents могут одновременно выполнять attack jobs, а optimistic barrel version/generation разрешает только один destruction commit.
- `InventoryState` создаёт ровно один quantity-1 unit в former barrel cell после успешного commit.
- World/Navigation предоставляет support, deterministic landing и reachable attack positions.
- building placement получает отдельный immutable set supported barrel cells; movement occupancy не создаётся.
- Presentation отображает barrel geometry/collider, red hover highlight, sword cursor, attack/fall result и status, но не владеет lifecycle или contents.

## Runtime workflow

```text
selected resident + reachable barrel hover
-> red highlight + animated sword cursor
-> ContextInputRouter: AttackBarrel
-> PrepareResidentsForDirectCommand
-> StartDirectBarrelAttackCommand
-> ordinary JobSystem travel to adjacent work position
-> ArriveAtBarrelCommand
-> one CompleteBarrelHitCommand
-> CompleteBarrelDestructionCommand
-> optimistic first commit wins
-> barrel visual/collider disappears
-> one saved stone/iron-ore unit appears in former cell
```

Concurrent jobs reserve only independent work positions. Когда два residents достигают finalize, первый valid barrel version commit создаёт output; остальные получают typed generation/version conflict, завершаются без второго output и освобождают reservations.

## Demo bootstrap

Fresh demo создаёт четыре stable barrels: две на supported cells верхней поверхности (`Z0`) и две на supported cells нижней пещеры (`Z > 0`). Каждый stable barrel id детерминированно выбирает и сохраняет ровно один item: `material.stone` либо `ore.iron`. Visual height равна `1.05` world unit.

## Support loss и placement

После authoritative excavation/world commit support reconciliation проверяет supported barrels. Unsupported barrel использует общий vertical landing resolver, переходит через `Falling` к первой свободной supported cell и не получает damage, не разрушается и не materialize-ит contents. Supported barrel cell входит в building-placement blocked set, но не добавляется в Navigation occupancy.

## Save/load

Save format v7 хранит production и additive optional barrel section:

- id, definition, cell и lifecycle;
- contents item id, generation и materialized marker;
- falling source/landing cells;
- optimistic version;
- active barrel jobs через `BarrelAttackJobSaveCodec`.

v6→v7 migration добавляет production и пустую barrel section; существующий v7 document без barrel section трактуется как пустой. Fresh demo bootstrap создаёт fixtures только для новой session; load не должен создавать дополнительные barrels или повторять output.

## CI regression fixes

- Quality run `30310039384` показал, что concurrent reservation test ожидал две записи, хотя `JobSystem.Claim` корректно создаёт для каждого job `ForJob`, `ForAgent` и `ForPosition`. Regression теперь проверяет точные ключи и отдельно запрещает exclusive barrel target reservation.
- PR #444 синхронизирован с актуальным `main`; production и barrels совместно используют backward-compatible format v7; отсутствующая barrel section восстанавливается как пустая.

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

- four demo fixtures and stone/iron-ore contents;
- red highlight, animated sword cursor and Russian status;
- geometry/collider lifecycle;
- support removal and landing wiring;
- Play Mode fixture for rendering/disappearance.

Unity Editor/Play Mode нельзя считать пройденным, пока fixture реально не выполнен Unity Test Runner. Поэтому система не получает статус `VERIFIED` только по source-contract или .NET checks.
