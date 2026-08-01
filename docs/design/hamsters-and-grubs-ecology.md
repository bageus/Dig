# Хомяки и grub: блуждание, переноска и размножение

Статус: `APPROVED`; diagonal/depth wandering change is pending implementation and licensed Unity EditMode/PlayMode evidence.

Tracking issue: [#524](https://github.com/bageus/Dig/issues/524).
Original implementation PR: [#529](https://github.com/bageus/Dig/pull/529).
Parent ecology issue: [#149](https://github.com/bageus/Dig/issues/149).

Связанные системы:

- [`ecology-creatures-and-special-drops.md`](ecology-creatures-and-special-drops.md);
- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md);
- [`resident-inventory-expansion.md`](resident-inventory-expansion.md);
- [`building-production-and-internal-supply.md`](building-production-and-internal-supply.md);
- [`resident-movement-occupancy-and-vertical-traversal.md`](resident-movement-occupancy-and-vertical-traversal.md);
- [`save-load-and-migrations.md`](save-load-and-migrations.md);
- [`../implementation/hamsters-and-grubs-ecology.md`](../implementation/hamsters-and-grubs-ecology.md);
- [`../implementation/living-material-diagonal-depth-movement-2026-08-01.md`](../implementation/living-material-diagonal-depth-movement-2026-08-01.md).

## 1. Назначение и границы

Система задаёт lifecycle двух мирных ресурсных существ — hamster и grub — как individual living-material entities. Она отвечает за fresh-world entry, свободное блуждание по связной navigation component на постоянной высоте `Y`, ограниченное размножение, связь с Inventory, dormancy после выкладывания, campfire internal-stock projection, сохранение и детерминированное продолжение.

Canonical IDs: `creature.hamster` и `creature.grub`. `creature.larva` — save/read-model compatibility alias, canonicalized в `creature.grub`.

Система не определяет рецепты переработки существ, combat behavior других creatures, падение в шахты или balance остальных видов из #149.

## 2. Подтверждённый workflow

### 2.0 Fresh-world entry

Текущий fresh demo bootstrap создаёт ровно:

- два free hamster;
- один free grub.

Начальная популяция создаётся только через authoritative quantity-one Inventory entities. Ecology не имеет отдельного spawn owner и после commit только reconciles эти Inventory stacks.

Распределение детерминированное:

1. Planner читает authoritative `NavigationSnapshot` и строит те же connected movement regions, которые используются wandering/reproduction.
2. Region содержит walkable клетки одной высоты `Y`, соединённые legal `SupportedWalk` или `DepthTraverse` edges; `VerticalClimb`, `ShaftGapTraverse` и traversal links не входят в ecology region.
3. Кандидаты исключают клетки, уже занятые world-item stack или живым resident в момент bootstrap.
4. Regions и cells сортируются по stable key/`CellId`; скрытая случайная fallback-логика запрещена.
5. Два hamster помещаются в разные клетки одной eligible region, чтобы pair reproduction была возможна сразу после суточного cooldown.
6. Grub помещается в eligible region, отличную от hamster region, если такая существует.
7. Если существует только одна suitable region, grub получает третью свободную клетку той же region.
8. Если legal placement для полного набора `2 hamster + 1 grub` отсутствует, bootstrap завершается typed failure и не создаёт заведомо частичную популяцию.

Seed использует три stable entity IDs. Повторная initialization одной session является no-op. Если Inventory уже содержит хотя бы один canonical/legacy living-material individual, bootstrap не восстанавливает погибших/подобранных существ и не создаёт replacement population. Save/load не запускает fresh seed повторно.

Fresh hamster остаются свободными после initial production synchronization. Campfire hamster stock имеет capacity `2`, но delivery toggle по умолчанию выключен, поэтому continuous refill не резервирует животных и не переносит их транзитом через resident inventory без явного решения игрока включить hamster delivery.

### 2.1 Свободный hamster

1. Hamster имеет stable identity, linked quantity-one Inventory entity, anchor/current cell, connected movement region, radius `6`, direction и activity.
2. Он перемещается на соседние клетки в плоскости `X/Z` при неизменном `Y`: orthogonal `SupportedWalk`, orthogonal `DepthTraverse` и diagonal `X±1/Z±1`.
3. Diagonal step разрешён только без corner cutting: обе orthogonal стороны и оба пути через них должны быть legal ecology edges. `VerticalClimb`, `ShaftGapTraverse` и traversal links запрещены.
4. Средняя скорость равна `0.8` resident baseline через fixed-point cadence.
5. После `4..8` successful movement steps hamster выполняет search/dig `1..2` ecology steps.
6. После `16..32` successful movement steps hamster спит на боку `4..8` ecology steps.
7. Obstacle или radius boundary вызывает deterministic direction reselection.
8. Resident в Chebyshev radius `4` по `X/Z` на той же высоте `Y` не является hard blocker: hamster выбирает дальнейший legal candidate от ближайшего resident.

### 2.2 Свободный grub

1. Grub непрерывно движется со скоростью `0.65` resident baseline и radius `4`.
2. Он использует тот же набор orthogonal/diagonal `X/Z` candidates и `DepthTraverse` transitions при неизменном `Y`.
3. Diagonal step не может срезать заблокированный угол.
4. Obstacle или radius boundary вызывает deterministic direction reselection.
5. Resident не является препятствием и не влияет на grub.
6. Search/sleep states отсутствуют.

### 2.3 Movement region definition

`LivingMaterialPlaneKey` сохраняется как legacy save/API name, но обозначает connected movement region: component walkable cells одной высоты `Y`, соединённых orthogonal `SupportedWalk` и `DepthTraverse` edges. Diagonal candidates не объединяют иначе разорванные regions и разрешаются только при существовании обеих orthogonal проходов без corner cutting. Стены, shaft gaps, vertical climbs и traversal links делят components. Stable key равен минимальному `CellId` компоненты.

Fresh seed, pair detection, cap и wandering используют region, а не отдельный `Z` layer. Radius измеряется Chebyshev distance по `X/Z`, поэтому diagonal step имеет ту же единичную стоимость wandering radius, что orthogonal step.

### 2.4 Pickup, storage и drop

- Pickup/drop используют обычный unit-item workflow; Inventory остаётся authoritative owner location/reservations.
- Любая non-world location переводит creature в `Stored`; movement/reproduction выключены.
- Dropped grub активен на следующем ecology step без дополнительной dormancy.
- Dropped hamster получает `ReleaseDormant` на один ecology step — ровно `15` игровых минут.
- Failed/repeated reconciliation не дублирует identity и не запускает dormancy без нового stored→world transition.

### 2.5 Campfire internal stock

Hamster/grub во внутреннем складе campfire являются `Stored` и не размножаются. Два hamster там не образуют pair. Presentation показывает до двух hamster у отдельных маленьких столбиков на привязи; tether является projection `ItemLocation.InBuilding(campfireId)`.

Hamster stock является opt-in: default delivery выключен. Игрок может явно включить toggle, после чего обычный building-supply workflow вправе зарезервировать и перенести доступных free hamster до capacity `2`. Выключенный toggle не создаёт demand, reservation или resident transit.

### 2.6 Reproduction

Ecology имеет `96` substeps в игровых сутках; один simulation tick выполняет четыре substeps. Resident schedule остаётся 24-часовым.

- Grub: один free individual, period `96`, один offspring, максимум два successful cycles.
- Hamster: минимум два free hamster одной component, period `96`, parent — eligible stable-lowest ID, partner только обеспечивает pair, cycle расходуется только у parent, максимум два cycles.
- Newborn получает собственные два cycles и первый cooldown `96`.
- Resulting free population species/component не превышает `10`.
- Cap/parent/offspring identity фиксируются атомарно; blocked cap/no-partner/no-cell не расходует cycle и не создаёт duplicate.

## 3. Владение состоянием

- `InventoryState`: quantity-one item identity, authoritative location, reservations, fresh-seed item commit и transfer.
- `LivingMaterialEcologyState`: identity link, species, anchor/current cell, movement-region key, horizontal facing direction, activity/timers, fixed-point movement budget, cycles/cooldown и deterministic sequence.
- World/Navigation: walkable cells, `SupportedWalk`/`DepthTraverse` classification и initial-region candidates.
- Buildings/Inventory: campfire internal stock.
- Presentation: interpolation, poses, scale `0.25/0.20`, pickup proxy и tether geometry.

Ни Presentation, ни animation callbacks не создают, двигают или размножают creature и не меняют Inventory.

## 4. State machine и priority

```text
Fresh bootstrap           -> Inventory unit seed -> Ecology reconciliation
Stored + world grub       -> Moving
Stored + world hamster    -> ReleaseDormant --1 ecology step--> Moving
Moving hamster            -> Searching --1..2--> Moving
Moving hamster            -> Sleeping  --4..8--> Moving
Moving + obstacle/radius  -> direction reselection -> Moving/Blocked
Free + due reproduction   -> offspring + cooldown | blocked retry
Any free + non-world item -> Stored
```

Priority внутри ecology tick:

1. Inventory reconciliation;
2. timer completion;
3. reproduction transaction;
4. movement transaction;
5. Presentation projection.

Fresh seed выполняется до первого reconciliation и не является recurring ecology step.

## 5. Determinism

- initial regions/cells: stable region key, затем stable `CellId`;
- ecology step = `simulationTick * 4 + substep`;
- runtime order: movement region, species, creature ID;
- choices hash world seed + creature ID + sequence + purpose;
- movement threshold `4000`: hamster adds `800`, grub `650` per substep;
- activity bands/timers and sequence are saved;
- repeated save/load with одинаковым input даёт тот же следующий movement/reproduction result.

## 6. Инварианты

- fresh demo without saved living materials starts with exactly `2 hamster + 1 grub`;
- initial hamster pair shares one movement region and distinct cells;
- initial grub uses another movement region when available, otherwise a third distinct cell;
- occupied world-item cells and current cells of living residents are never selected for fresh seed;
- fresh seed remains `ItemLocation.InWorld` and cannot appear in a resident inventory slot without an explicit pickup transaction or player-enabled hamster supply;
- default campfire hamster delivery is disabled, so initial production synchronization creates no hamster reservation or resident transit;
- repeated initialization/save-load cannot reseed or duplicate the starting population;
- one creature ↔ one linked quantity-one Inventory entity;
- Inventory location determines Free/Stored;
- movement never changes `Y`; it may change `X`, `Z` or both by one cell;
- movement uses only legal `SupportedWalk`/`DepthTraverse` topology and never corner-cuts a diagonal;
- current cell remains inside Chebyshev `X/Z` radius and movement region;
- successful cycles `0..2`;
- free population after reproduction `<=10` per species/component;
- stored creatures never move/reproduce;
- save/load/retry cannot duplicate offspring;
- hamster internal-stock pair is excluded;
- Presentation is non-authoritative.

## 7. Commands, events и queries

Commands/use cases:

- plan/commit fresh demo population through Inventory;
- register/restore living material individual;
- reconcile containment from Inventory location;
- advance four ecology substeps for one simulation tick;
- commit one orthogonal/diagonal movement step or blocked direction change;
- plan and commit one offspring transaction;
- store/release individual.

Events:

- Inventory unit added for fresh seed;
- registered/restored;
- containment/activity/direction changed;
- navigation movement committed;
- reproduction committed;
- blocked reason recorded.

Queries/read models:

- deterministic initial placement plan;
- snapshots ordered by stable identity;
- free population by species/movement region;
- legal orthogonal/diagonal candidates and rejection reason;
- reproduction due/blocked state;
- creature activity visual projection;
- campfire tether slots.

## 8. Save/Load и migration

Save format `v12` introduced living-material IDs/link, species, anchor/current cell, plane root, direction, activity/timer, movement budget/counters, cycles, next reproduction step, deterministic sequence и version.

Inventory separately persists authoritative location/reservations. Load validates one-to-one links, canonicalizes `creature.larva -> creature.grub` and rebuilds derived movement-region candidates, nearby residents, interpolation and tether transforms. A saved legacy `PlaneKey` is rebound from the authoritative current world cell on first reconciliation; this does not trigger release dormancy or duplicate the entity. A restored population, including a population reduced to one individual, is never supplemented by demo seed.

Migration chain:

```text
v10 -> v11 deterministic terrain deposits
v11 -> v12 living material ecology
v12 -> v13 terrain output contract
```

## 9. Unity Presentation

- `DigTerrainWorkSession.InitializeLivingMaterials` receives the initial resident snapshot, excludes living resident cells, then seeds fresh Inventory before first Ecology synchronization.
- `DigAgentSimulationDriver` advances Ecology once per simulation tick with resident cells.
- `DigCreatureRenderer` consumes immutable living-material visual snapshots on the initial render and subsequent ticks.
- Hamster scale = `0.25`, grub scale = `0.20` относительно resident.
- Hamster activities project move/search/sleep/release-dormant poses.
- Grub projects continuous crawl and ignores resident overlap.
- Ordinary item collider/pickup proxy remains linked to the same Inventory entity.
- Campfire internal stock projects no more than two stable tether/post slots.

## 10. Failure, retry и concurrency

- No legal full initial plan after excluding world-item and living-resident cells: typed startup failure; no silent placement on unsupported/occupied cells.
- Stable seed ID collision or missing catalog item: validation fails before the first expected seed commit.
- Invalid drop cell: Inventory transfer does not commit; Ecology state is not released.
- Missing/invalid linked unit item: use case fails with typed diagnostic and does not create a second owner.
- No legal movement: cell is preserved, direction is deterministically reselected, retry occurs on future cadence.
- No partner/cap reached/no legal offspring cell: reproduction remains due, cycle is not spent, no duplicate appears.
- Multiple due creatures are processed в stable order; free population is rechecked after each committed offspring.
- Stored transition has priority over movement/reproduction in the same tick.

## 11. Diagnostics

Diagnostics expose initial-plan failure, seed ID/catalog conflict, species, containment, item link, plane, anchor/current cell, radius, direction, activity/timer, movement budget, completed cycles, next reproduction step and blocked reason.

Failures use typed Ecology/Application/Unity bootstrap errors; no Presentation-only state is accepted as authoritative evidence.

## 12. Acceptance и verification evidence

Automated coverage:

- Application: deterministic initial region distribution, hamster pair placement, distinct-region grub preference, one-region fallback and occupied-cell exclusion.
- Domain: profile constants, identity/link, fixed-point cadence, dormancy, activities, `X/Z` radius/step guards, pair/self reproduction, stable-lowest parent, newborn budget, max cycles and cap `10`.
- Production content: hamster stock capacity remains `2`, default delivery is disabled, non-living campfire stock defaults remain enabled.
- Application runtime: Inventory reconciliation, connected-region resolver, diagonal no-corner-cut candidates, depth transitions, resident steering, stored exclusion, atomic movement/reproduction and retry.
- Save: v12 round trip, deterministic continuation and migration chain through current v13.
- Unity source/contracts: seed wiring with living-resident exclusion, runtime session/driver, activity renderer, pickup proxy and tether projection.
- Checked-in Unity Play Mode: fresh demo contains exactly two hamster and one grub, none shares a living-resident cell or resident inventory slot, initial production synchronization creates no hamster supply reservation/transit, repeated initialization preserves the same three IDs, plus drop/dormancy/diagonal/depth movement/tether/no-vertical scenarios.

Verification boundary:

- Repository Quality/source tests may establish `IMPLEMENTED` after the correction PR passes.
- `VERIFIED` still requires the checked-in Unity EditMode/PlayMode scenarios to execute on a licensed runner.

## 13. Журнал решений

| Дата | Решение | Кто подтвердил | Изменённые разделы/issues |
|---|---|---|---|
| 2026-07-30 | Flat wandering, pickup/drop, hamster dormancy, campfire tether, pair/self reproduction, max two cycles и cap 10. | Пользователь | §§1–12, #524 |
| 2026-07-30 | Connected same-Y/Z plane; daily stable-lowest hamster parent; newborn two cycles; speed/radius `0.8×/6`, `0.65×/4`; 96 ecology steps/day; activity bands. | Пользователь | §§2–6, #524 |
| 2026-07-30 | Terrain deposits retain save v11; living material ecology advances save format to v12. | Реализация после merge reconciliation | §8, #524, PR #529 |
| 2026-07-30 | Licensed Unity Test Runner skipped activation gate, so system remained IMPLEMENTED rather than VERIFIED. | CI evidence | §12, #524, PR #529 |
| 2026-08-01 | Fresh world seeds two hamster and one grub; hamster remain a pair on one plane, grub is deterministically distributed to another suitable plane when available, otherwise to a third free cell of the same plane. | Пользователь | §§2.0, 3–13, #524 |
| 2026-08-01 | После runtime regression fresh seed также исключает клетки живых residents и остаётся world-owned; overlap не может выглядеть как немедленный resident pickup. | Пользовательский bug report | §§2.0, 6, 9–13, #524, PR #543 |
| 2026-08-01 | Hamster delivery в campfire internal stock по умолчанию выключена; игрок должен явно включить toggle, иначе continuous refill не резервирует и не переносит fresh free hamster. | Пользовательский runtime bug report со скриншотом `R:1` | §§2.0, 2.5, 6, 12–13, #433, #524 |
| 2026-08-01 | Flat same-`Y/Z` wandering заменено на orthogonal/diagonal movement по `X/Z` при неизменном `Y`, включая legal `DepthTraverse` между уровнями `Z`; diagonal corner cutting запрещён. | Пользователь | §§1–13, #524 |
