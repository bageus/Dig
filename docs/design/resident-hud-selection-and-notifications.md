# HUD гномов, выбор, контекстная панель и уведомления

Статус: целевая спецификация. Главная задача: #113. Связи: #70, #89, #93, #107, #114–#118, #159.

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
- новый выбор заменяет прежний;
- ПКМ снимает selection;
- двойной ЛКМ по row центрирует camera;
- camera focus не меняет logical position.

## Нижняя контекстная панель

Одновременно отображается один режим.

### Нет selection

Показываются tunnel, room templates и eraser. Eraser удаляет designations и незавершённые plan cells/jobs/reservations, но не восстанавливает выкопанный terrain.

### Выбран resident

```text
[ Weapon ] [ Main: 6 slots ] [ Cargo ]
```

Weapon отделён слева, Cargo справа.

### Выбрано completed building

Показываются production, research, storage/service modes, workers/visitors, active orders, progress и diagnostics. Справа — packing button.

### Активна BuildingBox

Показываются building name, orientation, ghost preview, valid state и reason code.

## Input routing

После UI shielding один click создаёт не более одной command:

1. placement LMB;
2. Alt+LMB use item;
3. selected inventory stack drop;
4. Alt+LMB world BuildingBox — pickup order;
5. LMB BuildingBox — placement mode;
6. selected resident + hostile target — соответствующий игровой приказ;
7. selected resident + reachable ground — move;
8. no resident selection — active excavation tool.

Unsupported Alt interaction трактуется как ground click. Generic world item не подбирается обычным LMB без отдельного interaction contract.

ПКМ снимает resident selection, отменяет placement либо dismiss notification согласно текущему контексту.

## BuildingBox flow

Production создаёт физическую коробку. Placement резервирует конкретную коробку. Свободный resident доставляет её и собирает building. Packing job после commit создаёт ровно одну коробку. UI не создаёт и не удаляет quantity.

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
- no click-through/double command;
- BuildingBox quantity сохраняется;
- notifications используют typed events и установленный lifecycle;
- deterministic read-model/input/Play Mode tests проходят.
