# HUD гномов, выбор, контекстная панель и уведомления

Статус: целевая спецификация. Главная задача: #113. Связи: #70, #89, #93, #107, #114–#118, #159, #390.

## Владение состоянием

Agents владеет identity, needs, schedule и active action. Skills владеет 12 характеристиками, capacity и gain/loss reports. Jobs, Inventory, Buildings, World, Society, Combat и Technology сохраняют свои существующие ownership boundaries. Presentation хранит только selection, hover, panel mode, preview, scroll и анимацию ticker.

## Верхний HUD

Справа сверху расположен полупрозрачный resident roster. По центру верхней части находится notification ticker, отдельно от time controls и roster. UI click всегда блокирует world input.

### Compact row

Строка показывает:

- имя и пол;
- mood face;
- Health bar;
- рабочее или свободное время;
- красный idle-at-work marker.

Мужские имена синие, женские розовые. Пол также дублируется icon/label.

Idle marker включён только при Work schedule, если resident способен действовать, но не имеет action, job, order, emergency или объяснимого blocked state.

### Expanded row

ЛКМ выбирает resident и раскрывает одну строку. Порядок:

1. current typed status;
2. Health;
3. Nutrition;
4. Alertness с названием «Бодрость»;
5. Mood;
6. пять наибольших skills.

Top-5 выбирается среди всех 12: value descending, затем stable AgentSkillId.

## Needs

Domain использует `0..10000`, HUD — `0..100`.

- 51–100: зелёный;
- 26–50: оранжевый;
- 0–25: красный.

Mood face:

- 0–25: грусть;
- 26–75: нейтральное;
- 76–100: радость.

Цвет дополняется числом, icon и accessible label.

Для active actions HUD показывает накопленный эффект:

- Eat: desired/consumed dish, matched/fallback, bites, Nutrition/Mood и lost remainder;
- Sleep: накопленные Alertness/Mood и следующий interval;
- Leisure: накопленный Mood и следующий interval.

Interruption не откатывает уже показанный authoritative effect.

## Skills и capacity

- один pool содержит 12 skills;
- base TotalSkillCapacity = 100;
- University max = 200;
- individual max = 100;
- значение 120 не используется;
- шкала: градиент тёмно-синий → зелёный.

При overflow остальные skills уменьшаются пропорционально текущему значению:

```text
Loss_j = Overflow × DonorValue_j / SumDonorValues
```

Все skills, растущие в одном mixed bundle, исключаются из donor pool. Расчёт fixed-point; после floor остаток распределяется largest-remainder method, tie-break — stable AgentSkillId. UI читает готовый Domain report и не пересчитывает формулу.

Inspector показывает requested/applied gains, free capacity, overflow, donor weights/losses, rounding, values before/after и source result.

## Typed statuses

`ResidentActivityDescriptor` содержит kind, IDs цели/места, source action/job/order, progress и block reason. Domain не хранит локализованный текст.

Поддерживаются: свободное время, движение, действие в игровом столкновении, готовка, упаковка/распаковка, копка, создание, подбор, сервис, тренировка/обучение, логистика, еда, сон, досуг, бегство, idle, work и blocked.

## Выбор и камера

- ЛКМ по HUD row или world resident выбирает того же resident;
- выбранный resident подсвечивается в HUD и мире;
- LMB по completed building или BuildingBox открывает building roster и подсвечивает соответствующую строку;
- новый выбор заменяет прежний;
- ПКМ снимает selection;
- двойной ЛКМ по resident row центрирует camera;
- camera focus не меняет logical position.

## Нижняя контекстная панель

Одновременно отображается один режим.

### Нет selection

Показываются tunnel, room templates и eraser. Eraser удаляет designations и незавершённые plan cells/jobs/reservations, но не восстанавливает выкопанный terrain.

### Выбран resident

```text
[ Weapon ] [ Main: 6 slots ] [ Cargo ]
```

Weapon отделён слева, Cargo справа. Текст `Cargo 4/6` не отображается. Все compartments имеют ровно два ряда: Main `3×2`, basket Cargo `2×2`, large-basket Cargo `3×2`, sheath Weapon `1×2`, harness Weapon `2×2`.

Внешняя рамка центральной нижней панели совпадает по высоте и границам с minimap и clock. Контент не расширяет эту рамку: кнопки, ячейки, padding и best-fit text уменьшаются внутри доступной области.

### Выбрано completed building

Показываются production, research, storage/service modes, workers/visitors, active orders, progress и diagnostics. Справа — packing button.

### Выбрана BuildingBox

Обычный LMB выбирает коробку и показывает:

- building name и box state;
- authoritative world/inventory location;
- доступность pickup/placement;
- кнопку «Распаковать»;
- diagnostics/reason code при недоступности.

Selection BuildingBox сам по себе не показывает активный ghost и не включает placement mode.

### Активен placement mode

После кнопки «Распаковать» системный 2D cursor скрывается, а в мире отображаются 3D ghost и footprint. Панель показывает building definition, orientation, validity, reason code и отмену. ПКМ отменяет preview и снимает building/box selection.

## Input routing

После UI shielding один click создаёт не более одной command. Для Inventory-owned item точное действие определяется [`item-interaction-capabilities.md`](item-interaction-capabilities.md) и выполняется до ground fallback:

1. active placement mode: LMB подтверждает preview, RMB отменяет preview/selection;
2. exact world item profile: generic/material/tool/weapon/food ordinary LMB — pickup; food `Alt + LMB` — pickup-then-use; BuildingBox ordinary LMB — selection/menu, `Alt + LMB` — pickup;
3. UI button «Распаковать» — вход в BuildingBox placement mode;
4. inventory exact-slot profile: `Alt` use, затем `C` quick drop, иначе primary placement;
5. selected resident + hostile target — соответствующий игровой приказ;
6. selected resident + reachable ground — move;
7. active excavation tool — terrain designation/command.

Hover и click читают один exact stack/profile resolver. Недоступное item action возвращает typed reason и поглощает pointer: оно не может превратиться в скрытый move, excavation или другой object action.

ПКМ снимает selection, отменяет placement либо dismiss notification согласно текущему контексту. Для BuildingBox/placement ПКМ не расходует и не перемещает коробку.

## BuildingBox flow

Production создаёт физическую коробку. Обычный LMB выбирает её. Кнопка «Распаковать» создаёт только uncommitted placement preview. Подтверждённый valid placement резервирует конкретную коробку и создаёт plan. Свободный resident доставляет её и собирает building. Packing job после commit создаёт ровно одну коробку. UI не создаёт и не удаляет quantity.

Точное поведение LMB на Z0 и запуск unpacking из resident inventory остаются в открытых вопросах `building-box-placement-and-packing.md`.

## Notifications

`GameNotification` содержит stable id, kind, source event key, tick, priority, localization args, navigation target и active state.

Виды:

- нападение на residents;
- рождение;
- голод;
- старость;
- Mood ниже 5;
- смерть;
- изобретение;
- завершение задания.

Голод создаёт сообщение при переходе Nutrition ниже 15 UI / 1500 Domain. После восстановления и нового пересечения сообщение может повториться. Одновременные сообщения не объединяются.

ЛКМ просматривает уведомление и открывает/focus source. ПКМ удаляет его. Сообщение активно до обработки, отдельная история не требуется. Duplicate source event не создаёт второе сообщение.

## Save/Load

Selection, hover, camera focus, uncommitted preview и ticker animation не являются simulation state. Owners сохраняют needs/actions, skill values/capacity, items/reservations, confirmed plans/jobs и lifecycle data. Continuous action progress и skill fixed-point values восстанавливаются без повторного effect/grant.

## Производительность и accessibility

Roster virtualized; изменение одной row не пересоздаёт весь список. Цвет не является единственным сигналом. Localization не влияет на identity, sorting и commands. Formula рассчитывается в Domain, а не каждый UI frame.

## Acceptance

- HUD/world selection синхронны;
- exact needs/Mood boundaries;
- top-5 включает боевые skills;
- UI gain/loss report совпадает с Domain;
- continuous effects отображаются без double application;
- panel modes взаимоисключающие;
- world BuildingBox LMB выбирает box и не включает placement;
- «Распаковать» включает только 3D ghost/footprint;
- no click-through/double command;
- center context HUD aligns with minimap/clock while inner controls compact without changing outer bounds;
- inventory slot grids remain paired two-row layouts and Cargo capacity title is absent;
- BuildingBox quantity сохраняется;
- notifications используют typed events и установленный lifecycle;
- deterministic read-model/input/Play Mode tests проходят.
