# Хомяки и grub: блуждание, переноска и размножение

Статус: `APPROVED`.

Tracking issue: [#524](https://github.com/bageus/Dig/issues/524).

Parent ecology issue: [#149](https://github.com/bageus/Dig/issues/149).

Связанные системы:

- [`ecology-creatures-and-special-drops.md`](ecology-creatures-and-special-drops.md);
- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md);
- [`resident-inventory-expansion.md`](resident-inventory-expansion.md);
- [`building-production-and-internal-supply.md`](building-production-and-internal-supply.md);
- [`resident-movement-occupancy-and-vertical-traversal.md`](resident-movement-occupancy-and-vertical-traversal.md);
- [`save-load-and-migrations.md`](save-load-and-migrations.md).

## 1. Назначение и границы

Система задаёт lifecycle двух мирных ресурсных существ — хомяка и grub — как individual living material entities. Она отвечает за свободное плоское блуждание, ограниченное размножение, связь с Inventory, dormancy после выкладывания, campfire internal-stock projection, сохранение и детерминированное воспроизведение.

Canonical IDs: `creature.hamster` и `creature.grub`. `creature.larva` — save/read-model compatibility alias, мигрируемый в `creature.grub`.

## 2. Подтверждённый workflow

### 2.1 Свободный hamster

1. Hamster имеет stable identity, linked quantity-one Inventory entity, anchor/current cell, connected flat plane, radius `6`, direction и activity.
2. Он движется только по `SupportedWalk` между соседними клетками с одинаковыми `Y/Z`; `DepthTraverse`, `VerticalClimb` и `ShaftGapTraverse` запрещены.
3. Средняя скорость равна `0.8` resident baseline через fixed-point cadence.
4. После `4..8` successful steps hamster выполняет search/dig `1..2` ecology steps.
5. После `16..32` successful steps hamster спит на боку `4..8` ecology steps.
6. Obstacle/radius boundary вызывает deterministic direction reselection.
7. Resident в радиусе `4` не является hard blocker: hamster меняет будущую direction от ближайшего resident.

### 2.2 Свободный grub

1. Grub непрерывно движется со скоростью `0.65` resident baseline и radius `4`.
2. Он использует только соседний `SupportedWalk` с одинаковыми `Y/Z`.
3. Obstacle/radius boundary вызывает deterministic direction reselection.
4. Resident не является препятствием и не влияет на grub.
5. Search/sleep states отсутствуют.

### 2.3 Plane definition

`LivingMaterialPlaneKey` — connected component открытых fully-supported cells с одинаковыми `Y/Z`, соединённых только горизонтальными `SupportedWalk` edges. Стены и разрывы делят components. Stable identity равна минимальному `CellId` компоненты.

Pair detection, cap и wandering используют component, а не весь `Y/Z` layer.

### 2.4 Pickup, storage и drop

- Pickup/drop используют обычный unit-item workflow; Inventory остаётся authoritative owner location/reservations.
- Любая non-world location переводит creature в `Stored`; movement/reproduction выключены.
- Dropped grub активен на следующем ecology step.
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

- `LivingMaterialEcologyState`: identity/link, species, anchor/current cell, plane, direction, activity/timers, fixed-point budget, cycles/cooldown и deterministic sequence.
- `InventoryState`: quantity-one item identity, location, reservations и transfer.
- World/Navigation: open/supported cells и traversal.
- Buildings/Inventory: campfire internal stock.
- Presentation: interpolation, poses, scale `0.25/0.20`, tether geometry.

## 4. State machine и priority

```text
Stored + world grub       -> Moving
Stored + world hamster    -> ReleaseDormant --1 step--> Moving
Moving hamster            -> Searching --1..2--> Moving
Moving hamster            -> Sleeping  --4..8--> Moving
Moving + obstacle/radius  -> direction reselection -> Moving/Blocked
Free + due reproduction   -> offspring + cooldown | blocked retry
Any free + non-world item -> Stored
```

Priority: Inventory reconciliation → timer completion → reproduction → movement → Presentation.

## 5. Determinism

- ecology step = `simulationTick * 4 + substep`;
- stable order: plane, species, creature ID;
- choices hash world seed + creature ID + sequence + purpose;
- movement threshold `4000`: hamster adds `800`, grub `650` per substep;
- activity bands/timers and sequence are saved.

## 6. Инварианты

- one creature ↔ one linked quantity-one Inventory entity;
- location determines Free/Stored;
- movement never changes Y/Z and never uses non-`SupportedWalk` edge;
- current cell remains inside radius/component;
- successful cycles `0..2`;
- free population after reproduction `<=10` per species/component;
- stored creatures never move/reproduce;
- save/load/retry cannot duplicate offspring;
- Presentation is non-authoritative.

## 7. Save/Load

Сохраняются IDs/link, species, anchor/current cell, plane root, direction, activity/timer, movement budget/counters, cycles, next reproduction step, deterministic sequence и version. Inventory отдельно сохраняет location/reservations. Load validates links, canonicalizes `creature.larva -> creature.grub` and recalculates candidates/residents/tether transforms.

## 8. Acceptance и tests

Domain/Application: plane separation, radius/flat traversal, exact `0.8/0.65` cadence, activity bands, resident steering, grub continuous movement, pickup/drop containment, stable-lowest pair, newborn budget, max cycles, atomic cap 10, blocked retry, deterministic replay and save/load migration.

Unity Play Mode: drop dormancy/immediate crawl, no vertical/depth traversal, search/sleep/move poses, scale `0.25/0.20`, two campfire tether projections and no internal-stock reproduction. Licensed Unity Test Runner evidence is required for `VERIFIED`.

## 9. Журнал решений

| Дата | Решение | Кто подтвердил |
|---|---|---|
| 2026-07-30 | Flat wandering, pickup/drop, hamster dormancy, campfire tether, pair/self reproduction, max two cycles и cap 10. | Пользователь |
| 2026-07-30 | Connected same-Y/Z plane; daily stable-lowest hamster parent; newborn two cycles; speed/radius `0.8×/6`, `0.65×/4`; 96 ecology steps/day; activity bands. | Пользователь |
