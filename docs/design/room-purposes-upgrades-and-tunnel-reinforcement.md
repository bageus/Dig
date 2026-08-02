# Назначение и улучшение пещерных комнат, укрепление тоннелей

Статус: `QUESTIONNAIRE`.

Tracking issue: [#574](https://github.com/bageus/Dig/issues/574).

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

Система добавляет persistent infrastructure только для завершённых шаблонных комнат `Small`, `Medium`, `Large` и `Tall`, а также функциональные укрепления горизонтальных тоннелей.

Система охватывает:

- одноразовое материальное улучшение комнаты;
- оперативно переключаемое назначение комнаты;
- room-specific bonuses и explicit placement/visual profiles;
- temporary room stock, hauling и staged construction;
- автоматические и ручные tunnel reinforcement jobs;
- сохранение частичного прогресса при смене worker;
- deterministic delayed collapse неукреплённых горизонтальных тоннелей;
- декоративное развитие тоннелей как поздний material sink для камня и ножек гриба.

Произвольно выкопанные области не получают room identity и purpose. Existing template aesthetic trim остаётся бесплатной rebuildable Presentation из `ExcavationTemplateInstance` provenance. Платное улучшение создаёт отдельный authoritative functional state и отдельную визуальную отделку.

## 2. Подтверждённый пользовательский workflow

### 2.1 Выбор комнаты и типа

1. После completion шаблонной комнаты над ней появляется небольшая world-space точка-кнопка.
2. Click блокирует world click-through, выбирает stable room identity и открывает центральное HUD-меню.
3. Доступные purpose:
   - `Bedroom` / Спальня;
   - `KitchenDining` / Кухня-столовая;
   - `Workshop` / Мастерская;
   - `Farm` / Ферма;
   - `None` / Без типа.
4. Future purpose:
   - `TrainingRoom` — повышает скорость обучения;
   - `EnergyRoom` — увеличивает полезную работу fuel-based engines от одного fuel batch.
5. Улучшенную комнату можно оставить без типа и позднее переключать purpose без новой оплаты.
6. В режиме типов комнаты с purpose показывают полупрозрачный overlay. Каждый purpose имеет отдельные цвет, icon/pattern и text label.

### 2.2 Первое назначение и улучшение

Первый переход неулучшенной комнаты к функциональному purpose создаёт persistent room-upgrade operation.

1. В допустимой центральной позиции комнаты создаётся временный internal stock только для материалов upgrade.
2. Обычные hauling jobs доставляют revealed, reachable и unreserved материалы.
3. Improvement work не начинается до полной доставки required set.
4. Worker выполняет material stages последовательно.
5. Каждый committed material unit exactly once:
   - расходуется из room stock;
   - обновляет persistent progress;
   - добавляет соответствующую часть отделки;
   - выдаёт skill grant материала.
6. После completion временный stock удаляется, room получает `Improved`, выбранный purpose активируется.
7. Direct interruption освобождает worker/position claims, но сохраняет stock, consumed ledger, partial visual progress и следующий stage. Другой допустимый worker продолжает с первого незавершённого material unit.

### 2.3 Стоимость и progression

| Template | Материалы | Визуальные этапы | Итоговый gain |
|---|---|---|---|
| Small | 4 `material.stone`, 4 `material.mushroom_leg` | деревянная окантовка, каменное обрамление/плитка пола | Stonework +2, Woodworking +2 |
| Medium | 8 stone, 8 mushroom leg | окантовка, каменный пол, передние подпирающие колонны | Stonework +4, Woodworking +4 |
| Large | 12 stone, 8 mushroom leg, 4 `material.iron` | усиленный пол, дополнительные подпорки и распорки | Stonework +6, Woodworking +4, Metallurgy +2 |
| Tall | 10 stone, 6 mushroom leg, 4 iron, 4 `material.crystal` | высокий свод, сложное обрамление, диагональные распорки | Stonework +5, Woodworking +3, Metallurgy +2, Alchemy +2 |

Каждая committed material unit выдаёт `+0.5` point (`50` fixed-point units): stone → `skill.stonework`, mushroom leg → `skill.woodworking`, iron → `skill.metallurgy`, crystal → `skill.alchemy`. Idempotency key включает room/stage/material-unit.

### 2.4 Purpose bonuses

Room multiplier применяется exactly once к уже вычисленной базовой скорости соответствующей системы.

#### Спальня

- Sleep interval в active Bedroom использует Alertness multiplier `1.20`;
- room не создаёт sleeping slots самостоятельно;
- completed `building.tent` всегда предоставляет два Bed slots;
- подтверждённые building counts:
  - Small: normal 1 tent = 2 slots; Bedroom 2 tents = 4 slots;
  - Medium: normal 2 tents = 4 slots; Bedroom 4 tents = 8 slots;
  - Large: normal 3 tents = 6 slots; Bedroom 6 tents = 12 slots;
  - Tall layout остаётся content decision первого implementation slice.

#### Кухня-столовая

- compatible cooking action получает speed multiplier `1.15`;
- Eat intervals внутри active KitchenDining получают Nutrition multiplier `1.15` с обычным cap Need;
- ingredients, output quantity, food identity и число bites не меняются.

#### Мастерская

- compatible production cycles получают speed multiplier `1.15`;
- effective capacity каждого existing internal-stock rule увеличивается на `+1`;
- incoming/reservation accounting использует effective capacity и не создаёт предметы;
- Small Workshop должен поддерживать одновременное размещение каменной и деревянной мастерских по explicit profiles.

#### Ферма

- compatible farm production cycles получают speed multiplier `1.15`;
- Medium Farm допускает 3 farm buildings вместо обычных 2;
- остальные layouts задаются content profiles.

TrainingRoom и EnergyRoom остаются future content; их multipliers не утверждены.

### 2.5 Компактное размещение и wall attachment

Global Building placement не ослабляется. Compact placement доступен только через explicit `BuildingDefinition × RoomTemplate × Purpose` profile.

- authoritative footprint, support cells, work positions, internal-stock anchors и output anchors остаются только в открытых room cells;
- solid wall cells никогда не становятся occupied logical footprint;
- visual mesh может входить в стену через специальный wall-attached variant;
- обычная двускатная крыша wall-attached здания заменяется односкатным variant;
- `WallAttachment = Left | Right | Back | None` задаётся profile;
- service side всегда обращена внутрь комнаты, противоположно стене;
- если мастерская примыкает к правой стене, internal-stock rack и output floor zone находятся слева;
- если мастерская примыкает к левой стене, rack и output zone находятся справа;
- internal stock визуализируется не плоской площадкой, а небольшим стеллажом с уровнями и ячейками;
- ready output остаётся на полу перед стеллажом;
- preview показывает конечный roof/rack/output variant и проверяет те же authoritative anchors, что confirmation.

### 2.6 Переключение purpose и несовместимые здания

После completion purpose меняется без новой доставки.

1. Новый purpose активируется сразу; bonuses прежнего purpose отключаются и новые placements по прежнему profile запрещаются.
2. Для зданий, которые больше не legal по новому purpose/layout, автоматически создаются packing jobs.
3. Пока packing job ещё не начал фактическую packing work, возврат прежнего purpose отменяет pending packing job и восстанавливает legal profile.
4. После начала packing work обратное переключение purpose не отменяет job; worker завершает упаковку по обычному BuildingBox lifecycle.
5. Для нескольких несовместимых зданий selection packing targets выполняется детерминированно по profile validity, затем stable BuildingId.

### 2.7 Автоматическое укрепление

- automatic range равен `20` cells по 3D Manhattan distance до ближайшей occupied cell любого completed building;
- правило одинаково для horizontal support и vertical/horizontal junction target;
- vertical tunnel не укрепляется распорками и не обрушается;
- maximal continuous horizontal run длиной более 10 cells требует одну structural support на каждые 10 cells;
- exact anchor при split/merge/branch остаётся Q-TUNNEL-002;
- automatic horizontal support расходует 1 `material.mushroom_leg`, создаёт wooden beam и выдаёт Woodworking `+0.7` (`70` units);
- vertical/horizontal junction создаёт low-priority stone reinforcement job стоимостью 1 `material.stone` и Stonework `+0.7`;
- junction безопасен от collapse сразу после excavation независимо от delivery stone; stone trim является улучшением/визуальным благоустройством;
- doors считаются structural reinforcement для своего tunnel target и coverage calculation;
- no source оставляет job pending/blocked без phantom reservation;
- automatic jobs имеют минимальный ordinary-work priority;
- interruption сохраняет target/job и позволяет другому worker продолжить.

### 2.8 Ручное укрепление

Ручной режим использует material, уже находящийся в inventory выбранного resident.

1. Игрок удерживает `U`, наводит pointer на inventory slot с `material.mushroom_leg` или `material.stone` и нажимает LMB по предмету.
2. Выбранный exact stack резервируется; placement job первоначально owner-locked текущему resident.
3. Mushroom leg preview:
   - зелёный только на legal horizontal tunnel support target;
   - final visual — вертикальная деревянная балка перед камерой.
4. Stone preview:
   - зелёный на vertical/horizontal junction;
   - также зелёный на legal horizontal tunnel floor target;
   - junction visual — каменное обрамление стыка;
   - ordinary floor visual — каменное укрепление/обрамление пола;
   - ordinary stone support structurally заменяет wooden support для того же target/coverage и выдаёт Stonework `+0.7`.
5. LMB по valid world target создаёт ghost и job текущему resident.
6. Resident идёт к work position, выкладывает exact material, выполняет один work cycle; commit расходует item, создаёт reinforcement state/visual и grant exactly once.
7. Invalid target возвращает typed reason и не падает в movement/excavation.
8. RMB отменяет unconfirmed preview без расхода.

Поведение exact resident-owned source после direct interruption остаётся Q-TUNNEL-004A.

### 2.9 Обрушение

- template room volume, vertical tunnel и junction никогда не обрушаются;
- reinforced targets и door-protected targets не обрушаются;
- eligible unreinforced horizontal tunnel не может обрушиться раньше одного game day после excavation completion;
- event выбирает 1–2 locations, каждое размером 1–3 consecutive cells;
- occupied actor cell не обрушается: resolver сначала ищет соседнюю eligible cell того же segment; если замены нет, event откладывается;
- world items/material stacks в collapsed cell становятся buried, невидимыми и недоступными для hauling/use;
- re-excavation восстанавливает те же buried stack identities и quantities exactly once;
- ladder находится в vertical tunnel и этим contract не затрагивается;
- collapse восстанавливает mineable terrain, обновляет Navigation/Jobs/Presentation, после чего cell можно выкопать снова;
- due time, candidate order, substitution и selected cells детерминированы и сохраняются.

Точная cadence повторных collapse events и restored terrain provenance остаются открытыми.

## 3. Владение состоянием

- World владеет room/template cells, tunnel topology, terrain solidity, excavation tick, buried-item attachment и collapse mutation.
- `RoomInfrastructureState` владеет RoomInfrastructureId, TemplateInstanceId, improvement lifecycle, material ledger, purpose и profile refs.
- `TunnelReinforcementState` владеет target kind, coverage, status и collapse schedule; terrain owner остаётся World.
- Inventory владеет stack identity, quantity, locations/reservations, room stock и buried stack identity.
- Jobs владеет delivery/improvement/reinforcement/packing lifecycles и claims.
- Buildings владеет identity, footprint, functions, stocks и packing.
- Skills владеет grants/capacity/idempotency.
- Needs/Production предоставляют authoritative rates; room system предоставляет typed multiplier context.
- Presentation владеет buttons, menus, previews, overlays и rebuildable visuals.

## 4. Модель данных

```text
RoomInfrastructureState
- RoomInfrastructureId
- TemplateInstanceId
- ImprovementStatus
- ActivePurpose
- Required/Delivered/Consumed ledger
- CompletedMaterialUnitIds
- ActiveJobIds
- Version

RoomPurposeDefinition
- PurposeId
- OverlayStyle
- RateMultiplier
- InternalStockCapacityDelta
- PlacementProfileRefs[]

RoomPlacementProfile
- BuildingDefinitionId
- RoomTemplateId
- PurposeId
- AllowedOrigin/Orientation
- WallAttachment
- VisualVariantId
- LogicalFootprint
- WorkPositions
- InternalStockAnchors
- OutputAnchors

TunnelReinforcementTarget
- TargetId
- Kind: WoodenSupport | StoneFloorSupport | JunctionStoneTrim | DoorProtection
- Cell/CoveredCells
- AutomaticRangeDistance
- Status
- ExactSourceStackId for manual job
- CollapseEligibility/ScheduledTick/Sequence
- Version
```

## 5. Commands, events и queries

Commands:

- request first room purpose/improvement;
- switch improved room purpose;
- synchronize material demand;
- commit room material unit;
- synchronize automatic support targets;
- request manual reinforcement from exact resident stack;
- commit reinforcement;
- create/cancel purpose-invalid packing jobs;
- evaluate/commit tunnel collapse;
- recover buried items after excavation.

Events:

- improvement requested/stock filled/material committed/completed;
- purpose changed;
- packing required/cancelled/started;
- reinforcement required/blocked/completed;
- collapse scheduled/substituted/deferred/committed;
- item buried/recovered;
- typed skill grant result.

Queries expose room costs/progress/purpose/bonuses/profile, reinforcement target/source/coverage и collapse diagnostics.

## 6. Состояния и переходы

```text
Room:
Unimproved -> AwaitingMaterials -> ReadyForWork -> Improving -> Improved(Purpose|None)
Improved(A) -> Improved(B) + create invalid-building packing jobs
Improved(B) -> Improved(A) + cancel not-started invalid-building packing jobs

Reinforcement:
Required -> WaitingForMaterial -> Assigned -> Working -> Reinforced
ManualPreview -> ConfirmedOwnerLocked -> Working -> Reinforced

Collapse:
Ineligible -> EligibleAfterOneDay -> Scheduled
Scheduled -> Substituted | Deferred | Collapsed
Collapsed -> ReExcavated -> BuriedItemsRecovered
```

## 7. Input, UI и Presentation

- room marker blocks click-through;
- central menu показывает purpose, active state, cost, delivered/incoming, stage, worker и typed reason;
- overlays имеют color + icon/pattern/label;
- placement preview показывает wall variant, rack/output side и profile reason;
- `U + inventory slot click` имеет приоритет после UI shielding и до world movement/excavation;
- support ghost не authoritative;
- collapse публикует notification и refreshes terrain/colliders/routes/overlays.

## 8. Инварианты

- purpose доступен только completed template room;
- one TemplateInstanceId имеет максимум one RoomInfrastructureState;
- improvement cost и skill grants применяются exactly once;
- purpose switching не повторяет cost/grants;
- Tent всегда имеет два Bed slots;
- logical footprint никогда не занимает solid wall;
- room modifier применяется exactly once;
- packing job отменяется сменой purpose только до начала packing work;
- one reinforcement target commits at most once;
- manual job не расходует иной stack вместо selected exact stack;
- no source означает pending, не free support;
- collapse не происходит раньше одного дня и не выбирает actor-occupied, room, vertical, junction, reinforced или door-protected cell;
- buried items не дублируются и восстанавливаются exactly once;
- save/load/retry не дублирует stock, visuals, grants, jobs или collapse events.

## 9. Save/Load и migration

Сохраняются room state/material ledger/stages/purpose, temporary stock, active jobs, invalid-building packing jobs и started flag, reinforcement targets/coverage/source stack, buried item refs, collapse eligibility/schedule/sequence/substitution state и grant idempotency keys.

Legacy saves не получают automatic purpose. Reinforcement/collapse migration policy остаётся явной и versioned.

## 10. Диагностика

Inspector/HUD показывает:

- room/template/purpose/improvement state;
- required/current/incoming/consumed materials;
- active worker/job/stage/work position;
- effective multiplier/capacity/profile/visual variant;
- incompatible buildings и packing lifecycle;
- reinforcement kind/coverage/range/source stack/worker;
- collapse earliest/due tick, candidates, rejected reason, substitution и buried refs;
- World/Navigation versions после collapse.

## 11. Тестовая матрица

Domain/Application:

- exact costs и `+0.5` per material;
- multiplier composition 1.20/1.15 exactly once;
- Tent counts and two slots per tent;
- wall visual variant without solid logical footprint;
- mirrored rack/output anchors;
- purpose switch packing/cancel-before-start/no-cancel-after-start;
- one support per 10 cells and range 20 Manhattan;
- manual exact resident stack;
- pending source/retry/idempotency;
- actor substitution/defer and buried item recovery;
- deterministic collapse/save-load.

Unity Play Mode:

- room marker/menu/overlay shielding;
- Small/Medium/Large Tent layouts;
- Small Workshop dual-building layout;
- Medium Farm triple-building layout;
- staged improvement and second worker resume;
- observed Alertness/Nutrition/production rate changes;
- left/right wall variants and shelf/output mirroring;
- automatic/manual supports;
- purpose-invalid automatic packing cancellation boundary;
- real collapse, buried item disappearance and re-excavation recovery.

## 12. Acceptance

- completed template room exposes one marker and purpose menu;
- first purpose uses real hauling, exact cost and staged work;
- worker replacement preserves progress;
- Bedroom/KitchenDining/Workshop/Farm apply confirmed bonuses;
- compact profiles never occupy solid wall;
- Tent capacities are 4/8/12 slots for Small/Medium/Large Bedroom;
- purpose switch creates packing jobs and can cancel only not-started jobs by switching back;
- automatic support range is 20 Manhattan and cadence is one per 10 horizontal cells;
- manual mode consumes selected resident inventory stack;
- stone can reinforce junction or ordinary horizontal floor;
- actor cells defer/substitute collapse, items become buried and recover after re-excavation;
- save/load preserves every authoritative stage and exactly-once effect.

## 13. Открытые вопросы

### Room

- **Q-ROOM-002 — cancel upgrade.** Можно ли явно отменить незавершённое improvement? Что происходит с delivered materials, consumed stages и partial finish?
- **Q-ROOM-003 — action membership.** Building bonus определяется binding при placement или проверкой anchors внутри room? Sleep/Eat bonus проверяется на старте action либо на каждом interval?
- **Q-ROOM-007 — remaining layouts.** Tall Bedroom и точный первый каталог Farm/Workshop profiles для Small/Large/Tall.
- **Q-ROOM-009 — occupied room upgrade.** Где создаются temporary stock/work position, если центр занят building/item/resident? Какие blockers запрещают старт?
- **Q-ROOM-010 — purpose before completion.** Можно ли менять requested purpose во время AwaitingMaterials/Improving?

### Tunnel

- **Q-TUNNEL-002 — interval anchoring.** Какая endpoint/ordering определяет каждый десятый target при split, merge и branch horizontal runs?
- **Q-TUNNEL-004A — interrupted manual exact-stack job.** Если owner-resident принудительно прерван до material commit, job остаётся привязанным к нему или selected stack выгружается/передаётся для продолжения другим гномом?
- **Q-TUNNEL-006 — collapse cadence.** Как выбирается deterministic срок после первого дня и может ли повторно выкопанный segment снова обрушиться?
- **Q-TUNNEL-007 — restored terrain.** Collapse всегда создаёт обычную stone rock без output либо восстанавливает исходный terrain provenance без повторного deposit?
- **Q-TUNNEL-008 — cancellation.** Можно ли отменять pending automatic/manual support jobs и когда automatic target создаётся снова?

## 14. Журнал решений

| Дата | Решение | Кто подтвердил | Изменённые разделы/issues |
|---|---|---|---|
| 2026-08-02 | Создана система room purposes/upgrades и tunnel reinforcement/collapse | владелец дизайна | весь документ, #574 |
| 2026-08-02 | Purpose только для template rooms; Tent = 2 slots; bonuses 1.20/1.15; visual-only wall inset; mirrored shelf/output; automatic packing on purpose change | владелец дизайна + delegated balance | 1, 2.4–2.6, 8, 11–13, #574 |
| 2026-08-02 | Automatic range 20 Manhattan; one support per 10 cells; junction intrinsically safe; exact-inventory manual mode; stone floor support; actor defer/substitute; buried items recover | владелец дизайна | 2.7–2.9, 4–13, #574 |
