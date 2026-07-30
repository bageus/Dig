# Хомяки и grub: блуждание, переноска и размножение

Статус: `QUESTIONNAIRE`.

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

Система задаёт lifecycle двух мирных ресурсных существ — хомяка и grub/личинки — как individual living material entities. Она отвечает за свободное плоское блуждание, ограниченное размножение, переход между Ecology и Inventory location, dormancy после выкладывания, campfire internal-stock поведение, сохранение и presentation projection.

Система не определяет рецепты переработки хомяков/grub, общий combat behavior существ, vertical falling, глобальную шкалу игрового времени или balance других видов из #149.

Canonical species/item IDs для нового runtime slice:

- `creature.hamster`;
- `creature.grub`.

`creature.larva` остаётся compatibility alias для presentation/read-model и старых данных, но новый authoritative content использует `creature.grub`.

## 2. Подтверждённый пользовательский workflow

### 2.1 Свободный hamster

1. Свободный hamster имеет stable identity, anchor cell, допустимый radius, текущую direction и activity state.
2. Он постепенно движется по ровной supported поверхности и не использует vertical/depth traversal.
3. Во время движения hamster периодически останавливается, копается/ищет, спит на боку, просыпается и продолжает движение.
4. При terrain/building obstacle hamster останавливается и выбирает новое deterministic-random направление.
5. Если hamster видит resident, он не мгновенно убегает, а медленно меняет траекторию в сторону от resident.
6. Hamster остаётся внутри своего radius и той же плоскости.

### 2.2 Свободный grub

1. Свободный grub непрерывно медленно движется в выбранном направлении.
2. При terrain/building obstacle он выбирает новое deterministic-random направление.
3. Resident не является препятствием для grub.
4. Grub остаётся внутри своего radius и той же плоскости.
5. Grub не останавливается для digging/sleep idle states.

### 2.3 Pickup и drop

1. Hamster/grub подбирается существующим ordinary unit-item pickup workflow.
2. Inventory transaction меняет authoritative item location; Ecology не создаёт второй inventory owner.
3. Пока individual находится в resident inventory, hauling transit, storage или building inventory, свободное движение и reproduction выключены.
4. Drop использует общий item placement workflow на допустимую supported surface cell.
5. После drop grub активен сразу.
6. После drop hamster лежит и не двигается 15 игровых минут; затем просыпается и возвращается к обычному behavior.
7. Pickup/drop сохраняет identity, reproduction counter и deterministic continuation; операция не создаёт копию.

### 2.4 Campfire internal stock

1. Hamster может быть доставлен в internal stock campfire существующим building-supply workflow.
2. Internal-stock hamster не считается свободным, не движется и не размножается.
3. Два hamster в internal stock одного campfire не образуют reproduction pair.
4. Presentation показывает hamster рядом с campfire, привязанным к маленькому столбику.
5. Tether/post являются presentation projection building-inventory location и не становятся вторым state owner.

### 2.5 Reproduction grub

1. Только свободный grub участвует в reproduction.
2. Один grub не требует пары.
3. Reproduction cycle равен одним игровым суткам.
4. Один individual выполняет не более двух successful cycles.
5. Spawn transaction создаёт одного нового grub с новой stable identity в legal cell той же плоскости.
6. Если resulting free population grub на плоскости стал бы больше 10, transaction не создаёт offspring и сохраняет исходный individual без расхода successful-cycle counter.
7. Если population уже больше 10 из-за legacy/load/admin state, reproduction остаётся blocked до снижения population.

### 2.6 Reproduction hamster

1. Только свободные hamster участвуют в pair detection.
2. Нужны минимум два свободных hamster на одной плоскости; пол не учитывается.
3. Если остаётся один свободный hamster, reproduction невозможна.
4. Один successful cycle создаёт одного нового hamster с новой stable identity в legal cell той же плоскости.
5. Один reproducing individual выполняет не более двух successful cycles.
6. Если resulting free hamster population на плоскости стал бы больше 10, transaction не создаёт offspring и не расходует successful-cycle counter.
7. Hamster в inventory/storage/internal stock не является partner.

### 2.7 Blocked/failure/retry

- Нет legal offspring cell: reproduction остаётся due/blocked и повторяет deterministic check без duplicate offspring и без расхода cycle.
- Нет legal next movement cell: individual сохраняет cell, выбирает новую direction и повторяет check на следующей разрешённой cadence.
- Pickup reservation появился раньше ecology step: Inventory location имеет приоритет, movement/reproduction не выполняются.
- Drop target invalid: Inventory transaction не меняется, Ecology free state не создаётся.
- Save/load восстанавливает следующий результат, а не начинает random sequence заново.

## 3. Владение состоянием

- `EcologyState` владеет creature identity, species, lifecycle/activity, free anchor plane/radius, direction, position projection для свободного individual, reproduction counter/cooldown, release dormancy и deterministic stream cursor.
- `InventoryState` владеет unit item identity, location, reservations, pickup/drop/building transfer. Ecology хранит stable link на item entity, но не копирует location.
- World/Navigation владеет open/supported cells и traversal classification.
- Buildings/Production владеют campfire definition/internal-stock demand, но не creature lifecycle.
- Presentation владеет interpolation, pose, scale, sleep-side rotation, digging animation, avoidance steering visual и tether/post geometry.

Derived data:

- connected flat plane membership;
- legal next-step candidates;
- nearby resident visibility;
- free population counts per plane;
- tether slot transform.

## 4. Модель данных

```text
CreatureId / InventoryStackId link
CreatureSpeciesId: Hamster | Grub
CreatureContainment: Free | Carried | Stored
CreatureActivity:
- ReleaseDormant
- Moving
- HamsterSearching
- HamsterSleeping
- Blocked

CreatureSnapshot
- CreatureId
- ItemStackId
- Species
- Cell?                  # only free
- AnchorCell
- PlaneKey
- WanderRadius
- DirectionX
- ReproductionCyclesCompleted
- NextReproductionTick
- Activity
- ActivityUntilTick?
- LastMovementTick
- Version
```

`PlaneKey`, exact movement cadence and hamster reproduction policy remain questionnaire-dependent. Inventory location remains authoritative and is not duplicated in the snapshot.

## 5. Commands, events и queries

Commands:

- register/restore living material individual;
- advance ecology cycle;
- reconcile creature containment from Inventory location;
- commit one flat movement step;
- commit reproduction offspring;
- begin hamster release dormancy;
- complete release dormancy.

Events:

- creature registered;
- containment changed;
- direction changed;
- activity changed;
- flat step committed;
- reproduction blocked/completed;
- release dormancy started/completed.

Queries:

- individual snapshot;
- free individuals ordered by stable identity;
- free population by species/plane;
- legal flat candidates/rejection reasons;
- campfire tether projections;
- reproduction due/blocked reason.

## 6. Состояния и переходы

```text
Inventory non-world location -> Stored
Inventory world location + Grub -> Moving
Inventory world location + Hamster after pickup/drop -> ReleaseDormant
ReleaseDormant --timer--> Moving
Hamster Moving -> Searching -> Moving
Hamster Moving -> Sleeping -> Moving
Moving --obstacle/radius--> direction change -> Moving/Blocked
Free due reproduction --guards pass--> offspring + next cooldown
Free due reproduction --cap/no cell/no partner--> blocked retry
pickup/storage transfer -> Stored
Stored drop -> species-specific free state
```

## 7. Input, UI и Presentation

- Pickup/drop cursor, click priority, placement ghost и resident arrival используют существующий world-item/inventory router.
- Creature renderer получает immutable Ecology snapshot; animation callbacks не двигают, не размножают и не меняют Inventory.
- Hamster scale = `0.25` resident reference scale.
- Grub scale = `0.20` resident reference scale.
- Hamster presentation states: move, search/dig, sleep-side, release-dormant prone.
- Grub presentation state: continuous crawl; resident overlap не создаёт visual collision response.
- Campfire internal-stock hamster получает tether/post projection; до двух hamster отображаются отдельными unit identities.
- Inspector показывает species, containment, plane, radius, activity, cycles, next reproduction/release tick и blocked reason.

## 8. Зависимости и конфликты

Priority внутри ecology tick:

1. Inventory location/reconciliation;
2. death/removal lifecycle, если будет добавлен Combat owner;
3. release dormancy completion;
4. reproduction transaction;
5. movement step;
6. Presentation projection.

- Flat creature movement не использует resident Movement occupancy/reservation ledger.
- Grub игнорирует resident cells.
- Hamster resident avoidance влияет только на выбор будущей direction; resident не становится hard blocker.
- Terrain/building blockers имеют приоритет над desired direction.
- Reproduction cap проверяется атомарно в Ecology transaction, а не через Presentation count.

## 9. Инварианты

- Один living material individual имеет одну stable creature identity и один linked quantity-one Inventory entity.
- Inventory location определяет free/stored containment; creature не может быть одновременно свободным и carried/stored.
- Hamster/grub никогда не применяют vertical/depth traversal edge.
- Movement остаётся в radius и текущей plane component после её утверждения.
- Successful cycles `<= 2` для каждого individual.
- Reproduction не создаёт resulting free population больше 10 для species/plane.
- Stored/carrying/internal-stock creatures не двигаются и не размножаются.
- One reproduction transaction creates at most one offspring.
- Retry/save/load не дублирует offspring.
- Presentation не создаёт и не удаляет authoritative creature/item entity.

## 10. Save/Load и migration

Сохраняются:

- stable creature/item link;
- species;
- free anchor/current cell и plane/radius facts;
- direction;
- activity и timer;
- successful cycle counter;
- next reproduction tick;
- deterministic random stream state/version.

Inventory уже сохраняет authoritative item location/reservations. Load валидирует link и вычисляет containment из restored Inventory. Derived candidate cells, nearby resident list, interpolation, pose и tether transforms не сохраняются.

Legacy `creature.larva` records мигрируются в canonical `creature.grub` без создания нового item/entity.

## 11. Диагностика

Для каждого ecology step:

- tick, creature ID, species, item location;
- plane/radius/current/destination cell;
- direction/activity previous→next;
- movement rejection reason;
- free population count;
- partner/cycle/cap/spawn-cell decision;
- release dormancy remaining ticks;
- deterministic stream name/version.

## 12. Тестовая матрица

Domain unit:

- flat-only candidate filter;
- radius boundary;
- hamster activity transitions;
- grub continuous movement;
- cap and cycle guards;
- pair/no-pair behavior;
- atomic offspring identity.

Application/integration:

- pickup/drop containment reconciliation;
- resident inventory and building internal stock;
- campfire pair excluded from reproduction;
- concurrent due reproduction cannot exceed cap;
- blocked spawn retry without duplication.

Deterministic simulation:

- same seed/input yields identical directions, pauses, sleep and offspring;
- multiple hamster/grub on disconnected planes;
- resident avoidance without hard occupancy.

Save/load/migration:

- release timer continuation;
- reproduction due/blocked continuation;
- identity/location link;
- `larva -> grub` migration.

Unity Play Mode:

- grub drop moves immediately;
- hamster drop remains prone then resumes;
- no vertical tunnel traversal;
- hamster search/sleep/move projection;
- campfire tether/post and no internal-stock reproduction;
- scale `0.25/0.20` relative to resident.

## 13. Acceptance

- Пользователь может подобрать и выложить hamster/grub через общий inventory workflow без duplicate identity/quantity.
- Dropped grub начинает свободное движение сразу.
- Dropped hamster сначала лежит утверждённые 15 игровых минут, затем возвращается к движению.
- Оба вида остаются на ровной поверхности в radius и не пересекают vertical tunnels.
- Grub движется непрерывно; resident не блокирует его.
- Hamster демонстрирует move/search/sleep и мягко меняет траекторию от resident.
- Grub и hamster выполняют максимум два successful reproduction cycles на individual.
- Hamster не размножается без второго свободного hamster той же плоскости.
- Ни один concurrent spawn/reproduction path не создаёт больше 10 свободных individuals species на плоскости.
- Stored/internal-stock hamster/grub не движутся и не размножаются.
- Campfire internal-stock hamster виден на привязи у столбика.
- Save/load/retry сохраняет следующий deterministic lifecycle result.

## 14. Открытые вопросы

1. **Q-HG-001 — Plane boundary.** Плоскость — одна connected component supported cells на одинаковых `Y/Z` или весь `Y/Z` layer, включая разделённые стенами области? Это меняет pair/cap и wander radius membership.
2. **Q-HG-002 — Hamster cooldown.** Hamster reproduction cycle тоже один игровой день либо другой период?
3. **Q-HG-003 — Pair cycle owner.** При рождении cycle расходует stable-lowest-id parent, deterministic-random один parent или оба hamster?
4. **Q-HG-004 — Newborn budget.** Newborn hamster/grub получает собственные два cycles либо рождается с исчерпанным reproduction budget?
5. **Q-HG-005 — Exact speed.** Какой точный movement interval/multiplier у hamster и grub относительно resident?
6. **Q-HG-006 — 15 minutes/ticks.** Текущая demo schedule использует 24 ticks/day. Ecology получает finer sub-ticks, 15 минут округляются до одного simulation tick или глобальная day scale меняется?

## 15. Журнал решений

| Дата | Решение | Кто подтвердил | Изменённые разделы/issues |
|---|---|---|---|
| 2026-07-30 | Подтверждены flat wandering, pickup/drop, hamster dormancy, campfire tether, pair/self reproduction, max two cycles и per-plane free cap 10. | Пользователь | §§1–13, #524 |
