# Хомяки и grub: блуждание, переноска и размножение

Статус: `IMPLEMENTED`; `VERIFIED` требует фактического licensed Unity EditMode/PlayMode evidence.

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
- [`../implementation/hamsters-and-grubs-ecology.md`](../implementation/hamsters-and-grubs-ecology.md).

## 1. Назначение и границы

Система задаёт lifecycle двух мирных ресурсных существ — hamster и grub — как individual living-material entities. Она отвечает за fresh-world entry, свободное плоское блуждание, ограниченное размножение, связь с Inventory, dormancy после выкладывания, campfire internal-stock projection, сохранение и детерминированное продолжение.

Canonical IDs: `creature.hamster` и `creature.grub`. `creature.larva` — save/read-model compatibility alias, canonicalized в `creature.grub`.

Система не определяет рецепты переработки существ, combat behavior других creatures, падение в шахты или balance остальных видов из #149.

## 2. Подтверждённый workflow

### 2.0 Fresh-world entry

Текущий fresh demo bootstrap создаёт ровно:

- два free hamster;
- один free grub.

Начальная популяция создаётся только через authoritative quantity-one Inventory entities. Ecology не имеет отдельного spawn owner и после commit только reconciles эти Inventory stacks.

Распределение детерминированное:

1. Planner читает authoritative `NavigationSnapshot` и строит те же connected flat planes, которые используются wandering/reproduction.
2. Кандидаты содержат только walkable `SupportedWalk` cells и исключают клетки, уже занятые world-item stack.
3. Planes и cells сортируются по stable key/`CellId`; скрытая случайная fallback-логика запрещена.
4. Два hamster помещаются в разные клетки одной eligible plane, чтобы pair reproduction была возможна сразу после суточного cooldown.
5. Grub помещается в eligible plane, отличную от hamster plane, если такая существует.
6. Если существует только одна suitable plane, grub получает третью свободную клетку той же plane.
7. Если legal placement для полного набора `2 hamster + 1 grub` отсутствует, bootstrap завершается typed failure и не создаёт заведомо частичную популяцию.

Seed использует три stable entity IDs. Повторная initialization одной session является no-op. Если Inventory уже содержит хотя бы один canonical/legacy living-material individual, bootstrap не восстанавливает погибших/подобранных существ и не создаёт replacement population. Save/load не запускает fresh seed повторно.

### 2.1 Свободный hamster

1. Hamster имеет stable identity, linked quantity-one Inventory entity, anchor/current cell, connected flat plane, radius `6`, direction и activity.
2. Он движется только по `SupportedWalk` между соседними клетками с одинаковыми `Y/Z`; `DepthTraverse`, `VerticalClimb` и `ShaftGapTraverse` запрещены.
3. Средняя скорость равна `0.8` resident baseline через fixed-point cadence.
4. После `4..8` successful movement steps hamster выполняет search/dig `1..2` ecology steps.
5. После `16..32` successful movement steps hamster спит на боку `4..8` ecology steps.
6. Obstacle или radius boundary вызывает deterministic direction reselection.
7. Resident в радиусе `4` не является hard blocker: hamster постепенно меняет future direction от ближайшего resident.

### 2.2 Свободный grub

1. Grub непрерывно движется со скоростью `0.65` resident baseline и radius `4`.
2. Он использует только соседний `SupportedWalk` с одинаковыми `Y/Z`.
3. Obstacle или radius boundary вызывает deterministic direction reselection.
4. Resident не является препятствием и не влияет на grub.
5. Search/sleep states отсутствуют.

### 2.3 Plane definition

`LivingMaterialPlaneKey` — connected component открытых fully-supported cells с одинаковыми `Y/Z`, соединённых только горизонтальными `SupportedWalk` edges. Стены и разрывы делят components. Stable identity plane равна минимальному `CellId` компоненты.

Fresh seed, pair detection, cap и wandering используют component, а не весь `Y/Z` layer.

### 2.4 Pickup, storage и drop

- Pickup/drop используют обычный unit-item workflow; Inventory остаётся authoritative owner location/reservations.
- Любая non-world location переводит creature в `Stored`; movement/reproduction выключены.
- Dropped grub активен на следующем ecology step без дополнительной dormancy.
- Dropped hamster получает `ReleaseDormant` на один ecology step — ровно `15` игровых минут.
- Failed/repeated reconciliation не дублирует identity и не запускает dormancy без нового stored→world transition.

### 2.5 Campfire internal stock

Hamster/grub во внутреннем складе campfire являются `Stored` и не размножаются. Два hamster там не образуют pair. Presentation показывает до двух hamster у отдельных маленьких столбиков на привязи; tether является projection `ItemLocation.InBuilding(campfireId)`.

### 2.6 Reproduction

Ecology имеет `96` substeps в игровых сутках; один simulation tick выполняет четыре substeps. Resident schedule остаётся 24-часовым.

- Grub: один free individual, period `96`, один offspring, максимум два successful cycles.
- Hamster: минимум два free hamster одной component, period `96`, parent — eligible stable-lowest ID, partner только обеспечивает pair, cycle расходуется только у parent, максимум два cycles.
- Newborn получает собственные два cycles и первый cooldown `96`.
- Resulting free population species/component не превышает `10`.
- Cap/parent/offspring identity фиксируются атомарно; blocked cap/no-partner/no-cell не расходует cycle и не создаёт duplicate.

## 3. Владение состоянием

- `InventoryState`: quantity-one item identity, authoritative location, reservations, fresh-seed item commit и transfer.
- `LivingMaterialEcologyState`: identity link, species, anchor/current cell, plane, direction, activity/timers, fixed-point movement budget, cycles/cooldown и deterministic sequence.
- World/Navigation: open/supported cells, traversal classification и initial-plane candidates.
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

- initial planes/cells: stable plane key, затем stable `CellId`;
- ecology step = `simulationTick * 4 + substep`;
- runtime order: plane, species, creature ID;
- choices hash world seed + creature ID + sequence + purpose;
- movement threshold `4000`: hamster adds `800`, grub `650` per substep;
- activity bands/timers and sequence are saved;
- repeated save/load with одинаковым input даёт тот же следующий movement/reproduction result.

## 6. Инварианты

- fresh demo without saved living materials starts with exactly `2 hamster + 1 grub`;
- initial hamster pair shares one plane and distinct cells;
- initial grub uses another plane when available, otherwise a third distinct cell;
- occupied world-item cells are never selected for fresh seed;
- repeated initialization/save-load cannot reseed or duplicate the starting population;
- one creature ↔ one linked quantity-one Inventory entity;
- Inventory location determines Free/Stored;
- movement never changes `Y/Z` and never uses non-`SupportedWalk` edge;
- current cell remains inside radius/component;
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
- commit one flat movement step or blocked direction change;
- plan and commit one offspring transaction;
- store/release individual.

Events:

- Inventory unit added for fresh seed;
- registered/restored;
- containment/activity/direction changed;
- flat movement committed;
- reproduction committed;
- blocked reason recorded.

Queries/read models:

- deterministic initial placement plan;
- snapshots ordered by stable identity;
- free population by species/plane;
- legal flat candidates and rejection reason;
- reproduction due/blocked state;
- creature activity visual projection;
- campfire tether slots.

## 8. Save/Load и migration

Save format `v12` introduced living-material IDs/link, species, anchor/current cell, plane root, direction, activity/timer, movement budget/counters, cycles, next reproduction step, deterministic sequence и version.

Inventory separately persists authoritative location/reservations. Load validates one-to-one links, canonicalizes `creature.larva -> creature.grub` and rebuilds derived plane candidates, nearby residents, interpolation and tether transforms. A restored population, including a population reduced to one individual, is never supplemented by demo seed.

Migration chain:

```text
v10 -> v11 deterministic terrain deposits
v11 -> v12 living material ecology
v12 -> v13 terrain output contract
```

## 9. Unity Presentation

- `DigTerrainWorkSession.InitializeLivingMaterials` seeds fresh Inventory before first Ecology synchronization.
- `DigAgentSimulationDriver` advances Ecology once per simulation tick with resident cells.
- `DigCreatureRenderer` consumes immutable living-material visual snapshots on the initial render and subsequent ticks.
- Hamster scale = `0.25`, grub scale = `0.20` относительно resident.
- Hamster activities project move/search/sleep/release-dormant poses.
- Grub projects continuous crawl and ignores resident overlap.
- Ordinary item collider/pickup proxy remains linked to the same Inventory entity.
- Campfire internal stock projects no more than two stable tether/post slots.

## 10. Failure, retry и concurrency

- No legal full initial plan: typed startup failure; no silent placement on unsupported/occupied cells.
- Stable seed ID collision or missing catalog item: validation fails before the first expected seed commit.
- Invalid drop cell: Inventory transfer does not commit; Ecology state is not released.
- Missing/invalid linked unit item: use case fails with typed diagnostic and does not create a second owner.
- No legal movement: cell is preserved, direction is deterministically reselected, retry occurs on future cadence.
- No partner/cap reached/no legal offspring cell: reproduction remains due, cycle is not spent, no duplicate appears.
- Multiple due creatures are processed in stable order; free population is rechecked after each committed offspring.
- Stored transition has priority over movement/reproduction in the same tick.

## 11. Diagnostics

Diagnostics expose initial-plan failure, seed ID/catalog conflict, species, containment, item link, plane, anchor/current cell, radius, direction, activity/timer, movement budget, completed cycles, next reproduction step and blocked reason.

Failures use typed Ecology/Application/Unity bootstrap errors; no Presentation-only state is accepted as authoritative evidence.

## 12. Acceptance и verification evidence

Automated coverage:

- Application: deterministic initial plane distribution, hamster pair placement, distinct-plane grub preference, one-plane fallback and occupied-cell exclusion.
- Domain: profile constants, identity/link, fixed-point cadence, dormancy, activities, flat/radius guards, pair/self reproduction, stable-lowest parent, newborn budget, max cycles and cap `10`.
- Application runtime: Inventory reconciliation, connected-plane resolver, resident steering, stored exclusion, atomic movement/reproduction and retry.
- Save: v12 round trip, deterministic continuation and migration chain through current v13.
- Unity source/contracts: seed wiring, runtime session/driver, activity renderer, pickup proxy and tether projection.
- Checked-in Unity Play Mode: fresh demo contains exactly two hamster and one grub, repeated initialization preserves the same three IDs, plus drop/dormancy/movement/tether/no-vertical scenarios.

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
