# Назначение и улучшение пещерных комнат, укрепление тоннелей

Статус: `QUESTIONNAIRE`.

Tracking issue: `PENDING`.

Связанные системы:

- [`excavation-room-templates-and-deposits.md`](excavation-room-templates-and-deposits.md);
- [`excavation-command-execution.md`](excavation-command-execution.md);
- [`building-box-placement-and-packing.md`](building-box-placement-and-packing.md);
- [`building-production-and-internal-supply.md`](building-production-and-internal-supply.md);
- [`sleep-comfort-and-bed-assignment.md`](sleep-comfort-and-bed-assignment.md);
- [`skills-and-progression.md`](skills-and-progression.md);
- [`material-demand-and-hauling.md`](material-demand-and-hauling.md);
- [`energy-generation-and-production-pausing.md`](energy-generation-and-production-pausing.md);
- [`save-load-and-migrations.md`](save-load-and-migrations.md).

## 1. Назначение и границы

Система позволяет улучшить выкопанную пещерную комнату, назначить ей функциональный тип, применять room-specific bonuses и room-specific placement profiles, а также создавать и обслуживать укрепления горизонтальных тоннелей.

Система охватывает:

- persistent room identity и purpose;
- одноразовое материальное улучшение комнаты;
- временный room-upgrade stock, доставку и staged construction;
- room-specific bonuses и alternate building placement profiles;
- UI выбора типа и world overlays;
- автоматические и ручные tunnel reinforcement jobs;
- сохранение частичного прогресса при смене worker;
- deterministic delayed collapse неукреплённых горизонтальных тоннелей.

Система не переопределяет excavation ownership, BuildingBox lifecycle, Inventory quantity, resident needs, Production progress, Energy stock или Skills capacity. Она добавляет modifiers и новые authoritative room/reinforcement states поверх существующих owners.

Existing template aesthetic trim остаётся rebuildable Presentation из `ExcavationTemplateInstance` provenance. Платное улучшение комнаты создаёт отдельный functional improvement state и отдельную визуальную отделку; оно не заменяет и не расходует существующий template provenance.

## 2. Подтверждённый пользовательский workflow

### 2.1 Выбор комнаты и типа

1. После завершения выкопки над комнатой появляется небольшая world-space точка-кнопка.
2. Нажатие блокирует world click-through, выбирает room identity и открывает по центру HUD меню назначения.
3. Доступные подтверждённые purpose:
   - `Bedroom` / Спальня;
   - `KitchenDining` / Кухня-столовая;
   - `Workshop` / Мастерская;
   - `Farm` / Ферма;
   - `None` / Без типа.
4. Future purpose, уже подтверждённые как направление расширения:
   - учебная комната — ускоряет обучение;
   - энергетическая комната — увеличивает полезную работу fuel-based engines от одного fuel batch.
5. Улучшенную комнату разрешено оставить без типа и позднее назначить либо сменить purpose без повторного улучшения.
6. В режиме типов все комнаты с purpose показывают полупрозрачный overlay. Каждый purpose имеет собственный цвет, icon/pattern и text label, чтобы состояние не зависело только от цвета.

### 2.2 Первое назначение и материальное улучшение

Первый переход неулучшенной комнаты к функциональному purpose создаёт persistent room-upgrade operation.

1. В центре комнаты появляется временный внутренний stock только для материалов этой операции.
2. Создаются обычные hauling jobs для revealed, reachable и unreserved sources.
3. Upgrade work не начинается, пока stock не содержит полный required set.
4. После заполнения stock создаётся или активируется room improvement job.
5. Worker выполняет material stages последовательно; каждый committed material stage:
   - exactly once расходует соответствующую единицу из room stock;
   - обновляет persistent room progress;
   - обновляет соответствующую часть отделки;
   - выдаёт skill grant, связанный с использованным материалом.
6. После completion временный stock удаляется, room получает `Improved`, выбранный purpose становится active, bonuses и alternate placement profiles становятся доступны.
7. Если worker прерван прямым приказом, job остаётся non-terminal, room stock и completed stages сохраняются, reservations worker/position освобождаются, другой worker продолжает с первого незавершённого stage.

### 2.3 Стоимость, отделка и progression

| Template | Материалы | Визуальные этапы | Итоговый gain |
|---|---|---|---|
| Small | 4 `material.stone`, 4 `material.mushroom_leg` | деревянная окантовка по периметру, каменное обрамление/плитка пола | Stonework +2, Woodworking +2 |
| Medium | 8 stone, 8 mushroom leg | окантовка, каменный пол, передние подпирающие колонны | Stonework +4, Woodworking +4 |
| Large | 12 stone, 8 mushroom leg, 4 `material.iron` | окантовка, усиленный каменный пол, дополнительные передние подпорки и распорки | Stonework +6, Woodworking +4, Metallurgy +2 |
| Tall | 10 stone, 6 mushroom leg, 4 iron, 4 `material.crystal` | высокий свод, сложное обрамление, передние подпорки и диагональные распорки | Stonework +5, Woodworking +3, Metallurgy +2, Alchemy +2 |

Подтверждённая арифметика progression: каждая committed material unit room upgrade выдаёт `+0.5` point соответствующего навыка (`50` fixed-point units): stone → `skill.stonework`, mushroom leg → `skill.woodworking`, iron → `skill.metallurgy`, crystal → `skill.alchemy`. Bundle имеет stable idempotency key на room/stage/material-unit; interruption, retry и load не повторяют grant.

### 2.4 Purpose bonuses

#### Спальня

- повышает скорость восстановления Alertness во время Sleep action внутри active Bedroom room;
- разрешает data-driven Bedroom placement profiles для sleep buildings;
- для текущего `building.tent` подтверждены target counts по зданиям:
  - Small: normal 1 tent, Bedroom 2 tents у левой и правой стены;
  - Medium: normal 2 tents, Bedroom 4 tents — по две боковые позиции и две позиции у дальней стены;
  - Large: normal 3 tents, Bedroom 6 tents;
- точный Tall layout остаётся открытым;
- room modifier не создаёт Bed slots самостоятельно: slots остаются capability конкретного completed sleep building.

#### Кухня-столовая

- cooking action совместимого kitchen building получает speed multiplier;
- Eat action внутри active KitchenDining room получает Nutrition restoration multiplier;
- recipe ingredients, output quantity, bites и authoritative food identity не меняются.

#### Мастерская

- совместимые building production cycles получают speed multiplier;
- effective capacity каждого existing internal-stock rule совместимого здания увеличивается на `+1`;
- incoming/reservation accounting использует effective capacity и не создаёт предметы;
- room-specific `BuildingDefinition × RoomTemplate × Purpose` placement profiles могут разрешать более плотные заранее заданные layouts у боковых стен;
- подтверждённый target example: в Small Workshop должны помещаться одновременно каменная и деревянная мастерские, тогда как без Workshop purpose помещается только одно из этих зданий;
- exact profiles задаются отдельно для каждого здания и template, generic automatic packing algorithm не является источником истины.

#### Ферма

- совместимые farm production cycles получают speed multiplier;
- Medium Farm room разрешает 3 farm buildings вместо обычных 2;
- остальные template-specific layouts остаются открытыми/content-defined.

### 2.5 Переключение purpose

После completion одноразового improvement игрок может оперативно переключать `None`, Bedroom, KitchenDining, Workshop и Farm без новой доставки и construction job. Bonuses и overlays читают текущий active purpose.

Правило для уже размещённых зданий, которые были допустимы только по прежнему purpose, остаётся открытым в Q-ROOM-004.

### 2.6 Автоматическое укрепление тоннелей

- укрепляется только horizontal tunnel;
- vertical tunnel не получает распорки;
- continuous horizontal tunnel длиной более 10 completed cells создаёт low-priority reinforcement work согласно открытой cadence/coverage policy Q-TUNNEL-002;
- соединение vertical и horizontal tunnel всегда является отдельным reinforcement target и после reinforcement не обрушается;
- horizontal reinforcement расходует 1 `material.mushroom_leg`, визуально создаёт перед камерой вертикальную деревянную балку и выдаёт Woodworking `+0.7` (`70` units);
- vertical/horizontal junction reinforcement расходует 1 `material.stone`, визуально создаёт каменное обрамление пола в месте стыка и выдаёт Stonework `+0.7`;
- worker переносит одну зарезервированную единицу к target и committed placement atomically расходует item, создаёт reinforcement state/visual и skill grant;
- если revealed/reachable available source отсутствует, job сохраняется pending/blocked без phantom reservation и переоценивается после появления источника;
- reinforcement jobs имеют минимальный ordinary-work priority;
- interruption освобождает worker/route claims, но не удаляет job; уже committed reinforcement не повторяется.

### 2.7 Ручное укрепление

1. Игрок удерживает `U` и наводит pointer на поддерживаемый материал в resident inventory UI.
2. `material.mushroom_leg` включает preview деревянной опоры только на legal horizontal tunnel target.
3. `material.stone` включает preview только на legal vertical/horizontal junction target.
4. LMB по valid preview создаёт один reinforcement intent/job; invalid target возвращает typed reason и не падает в pickup/movement/excavation.
5. Exact source semantics — выбранный stack либо только выбор reinforcement kind — остаётся открытым в Q-TUNNEL-004.
6. RMB/отпускание mode input отменяет uncommitted preview без расхода материала.

### 2.8 Обрушение

- room template volume не обрушается;
- reinforced horizontal target не обрушается;
- reinforced vertical/horizontal junction не обрушается;
- vertical tunnel не требует reinforcement и не участвует в этом collapse contract;
- eligible unreinforced horizontal tunnel не может обрушиться раньше чем через 1 игровой день после authoritative excavation completion;
- deterministic collapse event выбирает 1 или 2 места, в каждом восстанавливает mineable rock на 1–3 последовательных cells;
- collapsed cells снова требуют обычной excavation;
- due times, selected cells и random sequence должны быть deterministic и сохраняться, чтобы save/load не перебрасывал событие.

Правила occupants/items/buildings и точная повторяемость collapse rolls остаются открытыми в Q-TUNNEL-005/Q-TUNNEL-006.

## 3. Владение состоянием

- World владеет exact room/template cells, tunnel cells, terrain solidity, excavation completion tick и collapse mutation.
- `RoomInfrastructureState` (новый Domain owner) владеет stable RoomInfrastructureId, linked template/room identity, improvement lifecycle, required/delivered/consumed ledger, purpose, version и room placement modifier reference.
- `TunnelReinforcementState` (новый Domain owner либо отдельная часть World infrastructure aggregate) владеет reinforcement targets, kind, protected cells, completion и deterministic collapse schedule. Окончательная module boundary должна сохранить одного owner и не дублировать terrain state World.
- Inventory владеет material stack quantity/location/reservations и temporary room-stock item locations.
- Jobs владеет delivery, improvement и reinforcement job lifecycle, worker/position claims и priority.
- Skills владеет grants/capacity/report.
- Buildings владеет building identity, footprint, functions, stock rules и production state; room system только предоставляет validated modifier/profile.
- Needs владеет Alertness/Nutrition effects; room system предоставляет typed multiplier context.
- Energy владеет fuel stock/output; room system предоставляет typed efficiency modifier.
- Presentation владеет button, selection, uncommitted preview, overlays and rebuildable trim/support visuals.

## 4. Модель данных

```text
RoomInfrastructureDefinition
- RoomTemplateId / supported room kind
- UpgradeMaterialStages[]
- VisualStageProfile
- SkillGrantPerMaterial
- AllowedPurposes[]

RoomInfrastructureState
- RoomInfrastructureId
- SourceRoomId / TemplateInstanceId
- ImprovementStatus
- ActivePurpose
- Required/Delivered/Consumed quantities
- CompletedStageMask
- ActiveJobIds
- Version

RoomPurposeDefinition
- PurposeId
- OverlayStyle
- Need/Production/Energy multipliers
- InternalStockCapacityDelta
- PlacementProfileRefs[]

RoomPlacementProfile
- BuildingDefinitionId
- RoomTemplateId
- PurposeId
- Allowed origins/orientations
- Logical footprint/work-position constraints
- Optional visual wall inset

TunnelReinforcementTarget
- TargetId
- Kind: HorizontalSupport | VerticalHorizontalJunction
- Cell/covered cells
- ExcavatedAtTick
- ReinforcementStatus
- CollapseEligibility/ScheduledTick/Sequence
- Version
```

Room and reinforcement visual geometry is derived and never authoritative identity.

## 5. Commands, events и queries

Commands/use cases:

- select/query room infrastructure;
- request first room purpose/improvement;
- switch improved room purpose;
- cancel room improvement, pending Q-ROOM-002;
- synchronize room material demand;
- commit one room material stage;
- create/synchronize automatic reinforcement targets;
- request manual reinforcement;
- commit reinforcement placement;
- evaluate and commit tunnel collapse.

Events:

- room improvement requested/stock filled/stage committed/completed;
- room purpose changed;
- room placement profile enabled/disabled;
- reinforcement required/blocked/completed;
- collapse scheduled/committed;
- typed skill grant source result.

Queries:

- room marker/overlay/menu view;
- required/current/incoming materials;
- effective room bonuses;
- effective placement profiles and validation reasons;
- reinforcement status, source shortage and covered cells;
- collapse eligibility/due diagnostics.

## 6. Состояния и переходы

Room:

```text
Unimproved/None
-> ImprovementRequested(purpose)
-> AwaitingMaterials
-> ReadyForWork
-> Improving(stage N)
-> Improved(active purpose)
-> Improved(other purpose | None)
```

Worker interruption:

```text
Improving -> Improving (worker released, stage ledger preserved)
```

Tunnel target:

```text
Required -> WaitingForMaterial -> Assigned -> Delivered/Committed -> Reinforced
Required/Unreinforced -> CollapseEligible -> Scheduled -> Collapsed
```

A committed room material unit or reinforcement placement is never rolled back by worker interruption.

## 7. Input, UI и Presentation

- room point-button uses stable room identity and blocks world click-through;
- central context menu shows purpose icons, active state, improvement status, costs, delivered/incoming, current worker/stage and typed reasons;
- type mode highlights typed rooms with translucent overlays plus icon/pattern/label;
- `None` improved room remains selectable but uses neutral improved-room marker without purpose bonus;
- placement preview must show room-specific profile and reason; ordinary placement remains unchanged outside active compatible room;
- `U` material-hover mode has priority after blocking UI and exact inventory slot resolution, before world movement/excavation;
- support preview is wooden beam or stone junction trim; preview is non-authoritative;
- collapse publishes notification and navigation target; terrain, colliders, routes and overlays refresh from authoritative World mutation.

## 8. Зависимости и конфликты

- room improvement delivery uses existing hauling availability, revealed/reachable source rules and item reservations;
- production/needs/energy multipliers compose once from authoritative active room purpose and cannot be applied twice after load/refresh;
- a building may receive at most one room-purpose modifier from one containing room;
- exact room containment and building membership are open in Q-ROOM-003;
- alternate room placement is content-defined, not inferred from mesh bounds;
- tunnel collapse must validate protected cells/occupants and update Navigation, tunnel topology, jobs, items and Presentation atomically or through explicit recoverable derived refresh;
- direct commands can replace current worker without canceling persistent infrastructure job.

## 9. Инварианты

- one source room has at most one RoomInfrastructureState;
- improvement cost is consumed exactly once;
- delivered + consumed + released quantities reconcile with Inventory ledger;
- room purpose switching never repeats improvement cost or grants;
- one material stage grants skill once;
- room bonus is active only after improvement completion and only for current purpose;
- room modifier never creates building, item, food, energy or needs state directly;
- one tunnel reinforcement target commits at most once;
- no source material means pending job, not free support;
- collapse cannot occur before one day;
- room cells, vertical tunnel cells and protected junction cells do not collapse under this system;
- save/load/retry cannot duplicate stock, visual supports, grants or collapse events.

## 10. Save/Load и migration

Save stores:

- stable room infrastructure identity and source room/template reference;
- improvement status, active purpose, required/delivered/consumed ledger and completed stages;
- temporary room-stock item locations/reservations;
- active job ids and resumable stage progress;
- reinforcement target/status/covered cells;
- excavation completion tick, collapse eligibility/scheduled tick/random sequence;
- idempotency keys for material-stage and reinforcement skill grants.

Load validates source room cells and item quantities, rebuilds overlays/trim/support visuals, rebinds jobs/reservations and preserves exact next stage/event. Legacy saves create no automatic room purpose and derive reinforcement candidates from authoritative tunnel history only according to an explicit migration policy still to be selected.

## 11. Диагностика

Inspector/HUD exposes:

- room id/template/purpose/improvement state;
- material required/current/incoming/consumed;
- active delivery/improvement worker, job, route and work position;
- current visual/material stage and skill source id;
- effective bonuses and why they do/do not apply;
- selected room placement profile and rejected cell reason;
- reinforcement target kind, distance policy, shortage, assigned worker and source stack;
- collapse eligibility, earliest/scheduled tick, protected reason and selected cells;
- World/Navigation versions after collapse.

## 12. Тестовая матрица

Domain/unit:

- costs and `+0.5` per room material unit;
- persistent purpose and free switching after improvement;
- multiplier/capacity modifier composition exactly once;
- exact room placement profile selection;
- reinforcement cost/grant/idempotency;
- deterministic collapse scheduling and excluded cells.

Application/integration:

- full delivery -> stock -> staged improvement -> completion;
- interruption and another worker resumes without reset;
- no source keeps job pending, later source activates it;
- placement/profile conflict and purpose switch policy;
- collapse commits rock and refreshes excavation/navigation/jobs.

Save/load:

- every room improvement stage;
- delivered but unconsumed stock;
- worker interruption;
- pending/complete reinforcement;
- collapse scheduled before/after due tick.

Unity Play Mode/end-to-end:

- room point/menu/overlay and UI shielding;
- Small/Medium/Large confirmed placement counts for each defined profile;
- room finish stages and resume by second worker;
- bonuses observed in authoritative rates/capacity;
- manual `U` preview/confirm/cancel;
- automatic support pending without material and completion after source appears;
- actual deterministic collapse and re-excavation workflow.

## 13. Acceptance

- completed room exposes one selectable point and central purpose menu;
- first purpose request consumes exact template cost through real hauling and staged work;
- worker replacement preserves completed room work and grants only committed material gains;
- improved room can become `None` or switch purpose without second construction;
- Bedroom/KitchenDining/Workshop/Farm apply only their approved modifiers;
- alternate building placement uses explicit data profiles and does not silently weaken global placement rules;
- overlays remain readable without color alone;
- horizontal and junction supports use exact materials, jobs, visuals and `+0.7` grants;
- missing source keeps reinforcement pending;
- eligible unreinforced horizontal tunnel can collapse only after one day, changes World back to mineable rock and can be excavated again;
- room, vertical tunnel and protected junction exclusions hold;
- save/load and retry conserve quantity, progress, grants and deterministic collapse state.

## 14. Открытые вопросы

### Room

- **Q-ROOM-001 — room identity scope.** Purpose доступен только для completed `ExcavationTemplateInstance` Small/Medium/Large/Tall либо также для произвольно выкопанной области, которую игрок вручную выделяет как комнату? Второй вариант требует отдельного room-zone creation/validation workflow и определения size class.
- **Q-ROOM-002 — cancel upgrade.** Можно ли явно отменить незавершённое улучшение? Если да, что происходит с доставленными материалами, уже consumed stages, частичной отделкой и первоначально выбранным purpose?
- **Q-ROOM-003 — membership.** Для bonus/placement building должен целиком находиться в room volume, достаточно origin/functional place, либо используется profile-specific rule? Для eating/sleep bonus проверяется position resident на каждом interval, action start или destination place.
- **Q-ROOM-004 — switching with compact buildings.** Что происходит при смене Bedroom/Workshop/Farm на несовместимый purpose, если в комнате стоят здания, допустимые только по старому compact profile: смена блокируется, здания остаются legacy-valid без bonus, автоматически создаются packing jobs или placement становится invalid до ручного исправления?
- **Q-ROOM-005 — wall overlap semantics.** Разрешается ли logical footprint занимать solid wall cell, либо только visual mesh может входить в стену, а все authoritative footprint/work cells остаются открытыми? Текущий Building placement запрещает solid footprint, поэтому это изменение должно быть явным.
- **Q-ROOM-006 — Tent slots conflict.** Текущий authoritative `sleep-comfort-and-bed-assignment.md` определяет completed Tent как два Bed slots, тогда как новое описание считает 2 tents в Small Bedroom как 2 sleeping places. Какое правило истинно: одна палатка = 2 sleeping slots или одна палатка = 1 sleeping place?
- **Q-ROOM-007 — Tall and remaining layouts.** Сколько палаток помещается в Tall Bedroom; сколько farm/workshop buildings помещается в Small/Large/Tall; какие exact building pairs/profiles входят в первый implementation slice?
- **Q-ROOM-008 — bonus values.** Точные multipliers Bedroom Alertness, Kitchen cooking, Kitchen Nutrition, Workshop production, Farm production, future Training и Energy efficiency.
- **Q-ROOM-009 — upgrade around occupants.** Можно ли начинать/выполнять improvement при уже размещённых зданиях, residents/items внутри комнаты и занятом центре; где тогда создаётся temporary stock и work position?
- **Q-ROOM-010 — purpose during improvement.** Разрешено ли менять requested purpose до completion и влияет ли это только на future active purpose или отменяет/перезапускает operation?

### Tunnel

- **Q-TUNNEL-001 — automatic range conflict.** В запросе названы `50` и `60` cells от крайнего здания. Какое максимальное расстояние используется? Измеряется Manhattan/grid distance до target, Navigation path либо connected tunnel distance?
- **Q-TUNNEL-002 — support coverage.** После длины >10 сколько automatic wooden supports создаётся: одна на весь connected run, одна каждые 10 cells, support с coverage radius, либо target выбирается иначе? Как split/merge/branch изменяет targets?
- **Q-TUNNEL-003 — junction range.** Vertical/horizontal junction job создаётся всегда независимо от зданий либо также только внутри automatic range? Сам junction безопасен до выполнения pending job или только после stone commit?
- **Q-TUNNEL-004 — manual source.** `U + hover inventory material` резервирует exact stack выбранного resident либо лишь выбирает reinforcement kind, после чего hauling ищет любой revealed/reachable source?
- **Q-TUNNEL-005 — collapse occupants.** Что происходит с resident, creature, world item, active job, ladder/door/building footprint в выбранных collapse cells: такие cells исключаются, actors получают damage/fall/displacement, items погребаются/перемещаются либо collapse откладывается?
- **Q-TUNNEL-006 — collapse cadence.** После первого дня rolls выполняются раз в день до collapse, один раз в случайно выбранный срок, либо по другому интервалу? Может ли один и тот же unreinforced segment обрушаться повторно после повторной excavation?
- **Q-TUNNEL-007 — collapse material.** Восстанавливается исходный source terrain/deposit provenance либо всегда обычная `terrain.stone_rock` без deposit/output duplication?
- **Q-TUNNEL-008 — explicit cancellation.** Можно ли отменять pending automatic/manual reinforcement job и, если да, создаётся ли он снова при следующей synchronization?

## 15. Журнал решений

| Дата | Решение | Кто подтвердил | Изменённые разделы/issues |
|---|---|---|---|
| 2026-08-02 | Подтверждены назначения Bedroom/KitchenDining/Workshop/Farm, одноразовое улучшение комнат с costs/visual stages/material-based skills, free purpose switching after improvement, автоматические и ручные reinforcement jobs, delayed horizontal tunnel collapse и future Training/Energy purposes. | Пользователь | Первичная спецификация |
