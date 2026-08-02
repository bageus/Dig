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

Система добавляет persistent infrastructure только для завершённых шаблонных комнат `Small`, `Medium`, `Large` и `Tall`, а также структурные и декоративные элементы горизонтальных тоннелей.

Система охватывает:

- одноразовое материальное улучшение комнаты;
- оперативно переключаемое назначение комнаты;
- room-specific bonuses и explicit placement/visual profiles;
- temporary room stock, hauling и staged construction;
- автоматические деревянные опоры горизонтальных тоннелей;
- автоматическую и ручную каменную декоративную отделку;
- сохранение частичного прогресса room improvement при смене worker;
- deterministic delayed collapse неукреплённых горизонтальных тоннелей;
- декоративное развитие тоннелей как поздний material sink для камня и ножек гриба.

Произвольно выкопанные области не получают room identity и purpose. Existing template aesthetic trim остаётся бесплатной rebuildable Presentation из `ExcavationTemplateInstance` provenance. Платное улучшение создаёт отдельный authoritative functional state и отдельную визуальную отделку.

Каменная отделка обычного пола тоннеля не является структурной опорой, не заменяет деревянную балку и не защищает тоннель от обрушения.

## 2. Подтверждённый пользовательский workflow

### 2.1 Выбор комнаты, кнопка улучшения и тип

1. После completion шаблонной комнаты над ней появляется небольшая world-space точка-кнопка.
2. Click блокирует world click-through, выбирает stable room identity и открывает центральное HUD-меню.
3. В меню есть отдельная кнопка `Улучшить` с order counter:
   - допустимые значения только `0` и `1`;
   - один click создаёт единственный upgrade order;
   - второй count для той же комнаты добавить нельзя;
   - после completion кнопка больше не создаёт повторное улучшение.
4. Ниже кнопки находятся purpose:
   - `Bedroom` / Спальня;
   - `KitchenDining` / Кухня-столовая;
   - `Workshop` / Мастерская;
   - `Farm` / Ферма;
   - `None` / Без типа.
5. Future purpose:
   - `TrainingRoom` — повышает скорость обучения;
   - `EnergyRoom` — увеличивает полезную работу fuel-based engines от одного fuel batch.
6. `RequestedPurpose` можно менять во время доставки материалов и во время improvement work. Смена типа не сбрасывает stock, progress или committed skill grants. При completion активируется последний выбранный purpose.
7. До completion выбранный purpose не даёт bonus и compact placement.
8. Улучшенную комнату можно оставить без типа и позднее переключать purpose без новой оплаты.
9. В режиме типов комнаты с purpose показывают полупрозрачный overlay. Каждый purpose имеет отдельные цвет, icon/pattern и text label.

### 2.2 Первое улучшение, temporary stock и отмена

Первое нажатие `Улучшить` создаёт persistent room-upgrade operation с count `1`.

1. Temporary internal stock создаётся в свободной клетке, ближайшей к геометрическому центру комнаты.
2. Если центр занят building, item или resident, выбирается следующая reachable free room cell по Manhattan distance; tie-break — stable cell coordinate.
3. Если свободной допустимой клетки пока нет, operation остаётся blocked и автоматически продолжает поиск после освобождения комнаты.
4. Обычные hauling jobs доставляют revealed, reachable и unreserved материалы.
5. Improvement work не начинается до полной доставки required set.
6. Пока worker ещё не начал первый improvement work interval, игрок может отменить upgrade order:
   - pending delivery jobs и reservations отменяются;
   - temporary stock перестаёт быть restricted upgrade stock;
   - уже доставленные материалы остаются лежать в комнате на текущих клетках;
   - эти материалы снова доступны обычной автоматической логистике, производству и другим jobs;
   - consumed materials и skill grants отсутствуют, потому что work ещё не начинался;
   - room остаётся `Unimproved`, order count возвращается в `0`.
7. Как только worker начал первый actual improvement work interval, отмена недоступна. Operation должна быть завершена в любом случае.
8. Worker выполняет material stages последовательно.
9. Каждый committed material unit exactly once:
   - расходуется из room stock;
   - обновляет persistent progress;
   - добавляет соответствующую часть отделки;
   - выдаёт skill grant материала.
10. После completion temporary stock удаляется, room получает `Improved`, активируется последний `RequestedPurpose`.
11. Direct interruption после начала work освобождает worker/position claims, но сохраняет stock, consumed ledger, partial visual progress и следующий stage. Другой допустимый worker продолжает с первого незавершённого material unit.

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
- internal stock визуализируется небольшим стеллажом с уровнями и ячейками;
- ready output остаётся на полу перед стеллажом;
- preview показывает конечный roof/rack/output variant и проверяет те же authoritative anchors, что confirmation.

### 2.6 Переключение purpose и несовместимые здания

После completion purpose меняется без новой доставки.

1. Новый purpose активируется сразу; bonuses прежнего purpose отключаются и новые placements по прежнему profile запрещаются.
2. Для зданий, которые больше не legal по новому purpose/layout, автоматически создаются packing jobs.
3. Пока packing job ещё не начал фактическую packing work, возврат прежнего purpose отменяет pending packing job и восстанавливает legal profile.
4. После начала packing work обратное переключение purpose не отменяет job; worker завершает упаковку по обычному BuildingBox lifecycle.
5. Для нескольких несовместимых зданий selection packing targets выполняется детерминированно по profile validity, затем stable BuildingId.

### 2.7 Автоматические структурные опоры и каменная отделка

- automatic range равен `20` cells по 3D Manhattan distance до ближайшей occupied cell любого completed building;
- правило одинаково для wooden-support и junction-stone-trim jobs;
- vertical tunnel не укрепляется распорками и не обрушается;
- каждое горизонтальное направление разбивается на deterministic segments;
- начало segment — первая horizontal cell после выхода из template room либо первая horizontal cell после vertical-tunnel junction;
- vertical-tunnel junction внутри горизонтального тоннеля разделяет левую и правую части на отдельные segments; отсчёт в каждой стороне начинается заново от junction;
- structural wooden-support target создаётся на 10-й, 20-й, 30-й и последующих клетках каждого segment;
- automatic job создаётся только для target, который находится не дальше 20 Manhattan cells от completed building;
- каждый десятиклеточный block считается structurally reinforced только если его required target содержит completed wooden support либо door protection;
- automatic horizontal support расходует 1 `material.mushroom_leg`, создаёт vertical wooden beam и выдаёт Woodworking `+0.7` (`70` units);
- vertical/horizontal junction создаёт low-priority decorative stone-trim job стоимостью 1 `material.stone` и Stonework `+0.7`;
- junction безопасен от collapse сразу после excavation независимо от доставки stone; stone trim не создаёт дополнительной structural protection;
- doors считаются structural reinforcement для своего required target;
- no source оставляет automatic job pending/blocked без phantom reservation;
- automatic jobs имеют минимальный ordinary-work priority;
- interruption автоматического job сохраняет target/job и позволяет другому worker продолжить.

### 2.8 Ручной режим `U`

Ручной режим использует material, уже находящийся в inventory выбранного resident.

1. Игрок удерживает `U`, наводит pointer на inventory slot с `material.mushroom_leg` или `material.stone` и нажимает LMB по предмету.
2. Выбранный exact stack резервируется; placement job owner-locked текущему resident.
3. Mushroom leg preview:
   - зелёный только на legal horizontal wooden-support target;
   - final visual — вертикальная деревянная балка перед камерой;
   - completion создаёт structural protection соответствующего десятиклеточного block и Woodworking `+0.7`.
4. Stone preview:
   - зелёный на vertical/horizontal junction;
   - также зелёный на legal horizontal tunnel floor target;
   - junction visual — каменное обрамление стыка;
   - ordinary floor visual — каменное обрамление пола;
   - оба stone variants являются декоративными, не заменяют wooden support, не закрывают required support target и не предотвращают collapse;
   - completion выдаёт Stonework `+0.7`.
5. LMB по valid world target создаёт ghost и job текущему resident.
6. Resident идёт к work position, выкладывает exact material и выполняет один work cycle. Commit расходует item, создаёт visual/infrastructure state и grant exactly once.
7. Если owner-resident принудительно прерван до commit:
   - manual job отменяется;
   - ghost удаляется;
   - exact-stack reservation освобождается;
   - material остаётся в inventory того же resident;
   - другой worker не продолжает это manual job.
8. Invalid target возвращает typed reason и не падает в movement/excavation.
9. RMB отменяет unconfirmed preview без расхода.

### 2.9 Обрушение

- template room volume, vertical tunnel и junction никогда не обрушаются;
- десятиклеточный horizontal block с completed wooden support или door protection не обрушается;
- decorative stone floor/junction trim не защищает ordinary horizontal block от collapse;
- после excavation completion eligible unreinforced horizontal segment получает deterministic due delay `1`, `2` или `3` game days;
- раньше одного полного game day collapse невозможен;
- event выбирает 1–2 locations, каждое размером 1–3 consecutive cells;
- occupied actor cell не обрушается: resolver сначала ищет соседнюю eligible cell того же segment; если замены нет, event откладывается на отдельный deterministic retry;
- world items/material stacks в collapsed cell становятся buried, невидимыми и недоступными для hauling/use;
- re-excavation восстанавливает те же buried stack identities и quantities exactly once;
- collapse создаёт обычную `terrain.sand` без deposit и без terrain mining outputs;
- buried items восстанавливаются отдельно от terrain output и не дублируются;
- ladder находится в vertical tunnel и этим contract не затрагивается;
- collapse обновляет Navigation/Jobs/Presentation, после чего cell можно выкопать снова;
- после повторной excavation неукреплённый segment снова получает новый deterministic delay `1..3` days и может обрушиться повторно;
- due time, candidate order, substitution, selected cells и random sequence сохраняются.

Точный delay retry после полного actor-blocked defer остаётся открытым в Q-TUNNEL-006A.

## 3. Владение состоянием

- World владеет room/template cells, tunnel topology, terrain solidity, excavation tick, buried-item attachment и collapse mutation.
- `RoomInfrastructureState` владеет RoomInfrastructureId, TemplateInstanceId, order count, improvement lifecycle, material ledger, requested/active purpose и profile refs.
- `TunnelInfrastructureState` владеет segment origins, wooden-support targets, decorative stone targets, structural protection и collapse schedule; terrain owner остаётся World.
- Inventory владеет stack identity, quantity, locations/reservations, room stock и buried stack identity.
- Jobs владеет delivery/improvement/automatic-support/manual-decoration/packing lifecycles и claims.
- Buildings владеет identity, footprint, functions, stocks и packing.
- Skills владеет grants/capacity/idempotency.
- Needs/Production предоставляют authoritative rates; room system предоставляет typed multiplier context.
- Presentation владеет buttons, count, menus, previews, overlays и rebuildable visuals.

## 4. Модель данных

```text
RoomInfrastructureState
- RoomInfrastructureId
- TemplateInstanceId
- UpgradeOrderCount: 0 | 1
- ImprovementStatus
- CancellationLocked
- RequestedPurpose
- ActivePurpose
- TemporaryStockCell
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

HorizontalTunnelSegment
- SegmentId
- OriginKind: RoomExit | VerticalJunction
- OriginCell
- OrderedHorizontalCells[]
- RequiredSupportTargets[] at distance 10*n
- AutomaticRangeDistances
- Version

TunnelInfrastructureTarget
- TargetId
- Kind: WoodenStructuralSupport | StoneFloorTrim | JunctionStoneTrim | DoorProtection
- Cell/CoveredBlock
- IsStructural
- Status
- ExactSourceStackId for manual job
- Version

TunnelCollapseState
- SegmentId
- ExcavatedAtTick
- DueDelayDays: 1..3
- ScheduledTick
- CandidateOrder
- RetryState
- Sequence
- BuriedItemRefs[]
- Version
```

## 5. Commands, events и queries

Commands:

- increment room upgrade count from 0 to 1;
- cancel room upgrade before work start;
- change requested room purpose;
- synchronize material demand;
- commit room material unit;
- switch improved room purpose;
- synchronize automatic wooden-support and junction-trim targets;
- request manual infrastructure placement from exact resident stack;
- cancel interrupted owner-locked manual job;
- commit wooden support or stone trim;
- create/cancel purpose-invalid packing jobs;
- evaluate/commit/defer tunnel collapse;
- recover buried items after excavation.

Events:

- upgrade ordered/cancelled/work-started/stock-filled/material-committed/completed;
- requested purpose changed/active purpose changed;
- packing required/cancelled/started;
- wooden support required/blocked/completed;
- stone trim required/completed;
- manual placement cancelled by interruption;
- collapse scheduled/substituted/deferred/committed;
- item buried/recovered;
- typed skill grant result.

Queries expose room count/cost/progress/purpose/bonuses/profile, target structural/decorative kind, source/coverage/range и collapse diagnostics.

## 6. Состояния и переходы

```text
Room:
Unimproved(count=0)
-> UpgradeOrdered(count=1, AwaitingMaterials)
-> ReadyForWork
-> Improving(cancel locked)
-> Improved(RequestedPurpose|None)

AwaitingMaterials | ReadyForWork
-> CancelledBeforeWork
-> Unimproved(count=0, delivered items released)

Improving
-> Improving(worker replaced, progress preserved)

RequestedPurpose may change in AwaitingMaterials, ReadyForWork or Improving.

Improved(A)
-> Improved(B) + create invalid-building packing jobs
Improved(B)
-> Improved(A) + cancel not-started invalid-building packing jobs

Automatic structural support:
Required -> WaitingForMaterial -> Assigned -> Working -> Reinforced

Manual owner-locked placement:
Preview -> ConfirmedOwnerLocked -> Working -> Committed
ConfirmedOwnerLocked | Working(before commit) -> CancelledOnInterruption

Collapse:
Ineligible -> ScheduledAfter1To3Days
Scheduled -> Substituted | Deferred | Collapsed
Collapsed -> ReExcavated -> BuriedItemsRecovered -> ScheduledAfter1To3Days if still unreinforced
```

## 7. Input, UI и Presentation

- room marker blocks click-through;
- central menu показывает `Улучшить`, count `0/1`, purpose list, active/requested state, cost, delivered/incoming, stage, worker и typed reason;
- cancel control виден только до first improvement work interval;
- после work start UI показывает обязательное завершение и не предлагает cancel;
- purpose buttons остаются active во время delivery/work и меняют только RequestedPurpose;
- overlays имеют color + icon/pattern/label;
- placement preview показывает wall variant, rack/output side и profile reason;
- `U + inventory slot click` имеет приоритет после UI shielding и до world movement/excavation;
- preview различает structural wooden beam и decorative stone trim;
- collapse публикует notification и refreshes terrain/colliders/routes/overlays.

## 8. Инварианты

- purpose доступен только completed template room;
- one TemplateInstanceId имеет максимум one RoomInfrastructureState;
- UpgradeOrderCount находится только в `0..1`;
- после первого improvement work interval cancellation навсегда заблокирована до completion;
- отмена до work не уничтожает и не возвращает телепортом доставленные материалы;
- improvement cost и skill grants применяются exactly once;
- purpose switching не повторяет cost/grants;
- Tent всегда имеет два Bed slots;
- logical footprint никогда не занимает solid wall;
- room modifier применяется exactly once;
- packing job отменяется сменой purpose только до начала packing work;
- one structural target commits at most once;
- stone trim никогда не удовлетворяет wooden structural target;
- manual job не расходует иной stack вместо selected exact stack;
- interruption manual job не передаёт material другому worker;
- no source означает pending, не free support;
- collapse не происходит раньше одного дня и не выбирает actor-occupied, room, vertical, junction, structurally reinforced или door-protected cell;
- collapse terrain всегда `terrain.sand` без deposit/output;
- buried items не дублируются и восстанавливаются exactly once;
- unreinforced re-excavated segment снова может получить collapse;
- save/load/retry не дублирует stock, visuals, grants, jobs или collapse events.

## 9. Save/Load и migration

Сохраняются:

- room order count, cancellation lock, requested/active purpose;
- material ledger, temporary stock cell, active jobs and stages;
- invalid-building packing jobs и started flag;
- segment origins/order, required structural targets и decorative targets;
- exact manual source stack and owner resident until commit/cancel;
- buried item refs;
- collapse excavation tick, delay 1..3, due tick, candidate/substitution/retry state and sequence;
- grant idempotency keys.

Legacy saves не получают automatic purpose. Reinforcement/collapse migration policy остаётся явной и versioned.

## 10. Диагностика

Inspector/HUD показывает:

- room/template/order count/requested purpose/active purpose/improvement state;
- cancellation locked reason;
- required/current/incoming/consumed materials and released-on-cancel items;
- temporary stock selection reason and active worker/job/stage/work position;
- effective multiplier/capacity/profile/visual variant;
- incompatible buildings и packing lifecycle;
- segment origin, ordered distance, required support target, structural/decorative kind, coverage, range, source stack and worker;
- collapse earliest/due tick, delay days, candidates, rejected reason, substitution, retry and buried refs;
- World/Navigation versions after collapse.

## 11. Тестовая матрица

Domain/Application:

- order count accepts only 0/1;
- cancellation during delivery releases materials and disables restricted stock;
- cancellation rejected after work starts;
- purpose changes during delivery/work without resetting progress;
- nearest-free stock cell selection and blocked retry;
- exact costs и `+0.5` per material;
- multiplier composition 1.20/1.15 exactly once;
- Tent counts and two slots per tent;
- wall visual variant without solid logical footprint;
- mirrored rack/output anchors;
- purpose switch packing/cancel-before-start/no-cancel-after-start;
- segment origins at room exits and vertical junctions;
- one wooden support target per 10 cells and range 20 Manhattan;
- stone trim never satisfies structural target;
- manual exact resident stack and cancel-on-interruption;
- actor substitution/defer, sand restoration and buried item recovery;
- deterministic 1..3 day collapse, repeat after re-excavation and save/load.

Unity Play Mode:

- room marker/menu/count/purpose/overlay shielding;
- cancel delivery leaves visible usable materials in room;
- work-start cancellation unavailable and second worker resumes;
- Small/Medium/Large Tent layouts;
- Small Workshop dual-building layout;
- Medium Farm triple-building layout;
- observed Alertness/Nutrition/production rate changes;
- left/right wall variants and shelf/output mirroring;
- automatic wooden support and decorative stone trim visuals;
- manual interruption removes ghost/job and leaves item in owner inventory;
- purpose-invalid automatic packing cancellation boundary;
- real repeated collapse, sand cell, buried item disappearance and re-excavation recovery.

## 12. Acceptance

- completed template room exposes one marker, one-use `Улучшить` count and purpose menu;
- first upgrade uses real hauling, exact cost and staged work;
- player may cancel only before actual improvement work starts;
- pre-work cancellation leaves delivered materials in the room and makes them ordinarily usable;
- once work starts, operation is guaranteed to finish and another worker resumes after interruption;
- requested purpose can change at any pre-completion stage without resetting upgrade;
- Bedroom/KitchenDining/Workshop/Farm apply confirmed bonuses;
- compact profiles never occupy solid wall;
- Tent capacities are 4/8/12 slots for Small/Medium/Large Bedroom;
- purpose switch creates packing jobs and can cancel only not-started jobs by switching back;
- automatic structural range is 20 Manhattan and target cadence is one wooden support per 10 horizontal cells from each room-exit/vertical-junction origin;
- manual wooden support consumes selected resident inventory leg and protects its block;
- manual/automatic stone trim consumes stone, grants Stonework and remains decorative only;
- interrupted manual job is cancelled and material stays in owner inventory;
- actor cells defer/substitute collapse, items become buried and recover after re-excavation;
- collapse creates output-free `terrain.sand`, occurs after deterministic 1..3 days and can repeat after re-excavation until structural support exists;
- save/load preserves every authoritative stage and exactly-once effect.

## 13. Открытые вопросы

### Room

- **Q-ROOM-003 — action membership.** Building bonus определяется binding при placement или проверкой anchors внутри room? Sleep/Eat bonus проверяется на старте action либо на каждом interval?
- **Q-ROOM-007 — remaining layouts.** Tall Bedroom и точный первый каталог Farm/Workshop profiles для Small/Large/Tall.

### Tunnel

- **Q-TUNNEL-006A — deferred retry.** Через какой deterministic interval повторяется collapse event, если все выбранные и соседние допустимые cells временно заняты actors?
- **Q-TUNNEL-008 — automatic cancellation.** Может ли игрок отменить pending automatic wooden-support/junction-trim job; если да, когда target снова создаётся synchronization?

## 14. Журнал решений

| Дата | Решение | Кто подтвердил | Изменённые разделы/issues |
|---|---|---|---|
| 2026-08-02 | Создана система room purposes/upgrades и tunnel infrastructure/collapse | владелец дизайна | весь документ, #574 |
| 2026-08-02 | Purpose только для template rooms; Tent = 2 slots; bonuses 1.20/1.15; visual-only wall inset; mirrored shelf/output; automatic packing on purpose change | владелец дизайна + delegated balance | 1, 2.4–2.6, 8, 11–13, #574 |
| 2026-08-02 | Automatic range 20 Manhattan; one support per 10 cells; junction intrinsically safe; exact-inventory manual mode; actor defer/substitute; buried items recover | владелец дизайна | 2.7–2.9, 4–13, #574 |
| 2026-08-02 | Stone floor/junction trim is decorative only; room cancel allowed only before work; requested type may change during work; nearest-free stock; segment origins at room/vertical junction; manual interruption cancels job; collapse after 1..3 days repeats and restores output-free sand | владелец дизайна | 1–14, #574 |
