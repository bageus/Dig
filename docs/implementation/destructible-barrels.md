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

Fresh demo создаёт четыре stable barrels:

- две на supported cells верхней поверхности (`Z0`);
- две на supported cells нижней пещеры (`Z > 0`).

Каждый stable barrel id детерминированно выбирает и сохраняет ровно один item: `material.stone` либо `ore.iron`. Retry/load не должны reroll-ить manifest. Visual height равна `1.05` world unit и остаётся ниже resident interaction height.

## Support loss и placement

После authoritative excavation/world commit support reconciliation проверяет supported barrels. Unsupported barrel использует общий vertical landing resolver, переходит через `Falling` к первой свободной supported cell и не получает damage, не разрушается и не materialize-ит contents.

Supported barrel cell входит в building-placement blocked set. Barrel не добавляется в Navigation occupancy и не имеет pickup/placement interaction.

## Save/load

Save format v7 добавляет barrel section:

- id, definition, cell и lifecycle;
- contents item id, generation и materialized marker;
- falling source/landing cells;
- optimistic version;
- active barrel jobs через `BarrelAttackJobSaveCodec`.

v6→v7 migration добавляет пустую barrel section. Fresh demo bootstrap создаёт fixtures только для новой session; load не должен создавать дополнительные barrels или повторять output.

## CI regression fix

Quality run `30310039384` собрал solution, но один тест завершился ошибкой:

- `Concurrent_attacks_are_allowed_but_only_first_commit_creates_contents` ожидал две записи в общем reservation ledger после запуска двух attack jobs;
- фактически `JobSystem.Claim` корректно создаёт для каждого job три записи: `ForJob`, `ForAgent` и `ForPosition`;
- barrel target при этом по-прежнему не имеет `ForEcologyTarget` reservation, поэтому concurrent first-commit-wins contract не нарушался;
- regression теперь проверяет точные ключи каждого job и отдельно запрещает reservation самой barrel вместо хрупкой проверки общего количества `2`.

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
- full Play Mode fixture is checked in for barrel rendering/disappearance.

Unity Editor/Play Mode нельзя считать пройденным, пока fixture реально не выполнен Unity Test Runner. Поэтому система не получает статус `VERIFIED` только по source-contract или .NET checks.
