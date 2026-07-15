# HUD гномов, выбор, контекстная панель и системные уведомления

Статус: целевая спецификация. Главная feature-задача: #113.

Связанные задачи: #114–#118, #70, #89, #93 и #107.

## 1. Назначение

Система объединяет:

- постоянный список гномов в правом верхнем углу;
- раскрытую строку выбранного гнома;
- выбор гнома через HUD или мир;
- контекстные команды мышью;
- взаимозаменяемую нижнюю панель копки, инвентаря, здания и placement mode;
- верхнюю очередь системных уведомлений.

Presentation читает immutable snapshots и отправляет Application commands. Готовый текст, цвет, cursor и подсветка не являются источником игрового состояния.

## 2. Владение состоянием

- Agents владеет identity, needs, schedule, player order, active action и skills.
- Jobs владеет работами, стадиями, исполнителями и reservations.
- Inventory владеет предметами, количеством, locations и личными slots.
- Buildings владеет plans, completed buildings, functions и durability.
- World владеет terrain и dig designations.
- Society/Lifecycle владеет полом, возрастом, рождением, смертью и условиями размножения.
- Combat владеет attack state и местом столкновения.
- Technology владеет открытиями.
- Presentation владеет selection, hover, scroll, раскрытием строки, placement preview и анимацией ticker.

## 3. Верхний HUD

Верхняя часть делится на неперекрывающиеся зоны:

- существующее управление временем;
- notification ticker по центру;
- resident roster справа.

Любой UI click блокирует world input.

## 4. Resident roster

Roster — полупрозрачная вертикальная панель в правом верхнем углу.

Требования:

- одна строка на каждого текущего гнома;
- stable ordering;
- scrolling и virtualization для больших поселений;
- выбранная строка автоматически остаётся в viewport;
- одновременно раскрывается только одна строка;
- HUD и world view используют один `SelectedResidentId`;
- renderer rebuild не изменяет Domain и не создаёт второго selected state.

### 4.1 Компактная строка

Показывает:

1. имя;
2. пол;
3. mood face;
4. компактную Health bar;
5. рабочее/свободное состояние;
6. красный индикатор безделья в рабочее время.

Мужские имена отображаются синим, женские — розовым. Пол также обозначается иконкой или accessible label; UI не определяет пол из текста имени.

### 4.2 Рабочий индикатор

Строка подсвечивается как безделье, если одновременно:

- текущий schedule segment — `Work`;
- гном жив и способен действовать;
- нет active action;
- нет claimed/in-progress job;
- нет emergency intent;
- нет ожидаемой внешней reservation/blocked операции, которая объясняет простой.

Во время `Free` status отображается «Свободное время», а красный idle-at-work marker не включается.

## 5. Раскрытая строка

ЛКМ по строке выбирает гнома и раскрывает её по высоте.

Порядок данных:

1. текущий статус;
2. Health;
3. Nutrition/Сытость;
4. Alertness с названием **«Бодрость»**;
5. Mood/Настроение;
6. пять навыков с наибольшим текущим значением.

Top-5 выбирается из всех 12 навыков: семи рабочих и пяти боевых. Сортировка — level descending, затем stable `AgentSkillId` ascending.

## 6. Шкалы состояния

Domain хранит Health, Nutrition, Alertness и Mood в диапазоне `0..10000`. UI нормализует их в `0..100`.

Цветовые диапазоны:

- `51..100` — зелёный;
- `26..50` — оранжевый;
- `0..25` — красный.

Каждая шкала также показывает icon, число или tooltip; цвет не является единственным носителем информации.

### 6.1 Mood face

- грусть: `0..25`;
- нейтральное: `26..75`;
- радость: `76..100`.

Радость сообщает, что mood-условие размножения выполнено, но Society всё равно проверяет возраст, партнёра, родство, здоровье и modifiers.

## 7. Навыки

Один общий pool включает 12 навыков.

- базовый `TotalSkillCapacity = 100`;
- университет расширяет его до `200`;
- максимум одного навыка — `100`;
- шкала навыка идёт градиентом от тёмно-синего к зелёному;
- число и максимум показываются явно;
- значение `120` не используется.

Точный алгоритм уменьшения других навыков после заполнения capacity остаётся Q-029. Проверка `scripts` показала только вызовы встроенной `add_expattrib`; сама формула в TCL отсутствует.

## 8. Типизированные статусы

Domain не хранит готовую локализованную строку. Read model передаёт `ResidentActivityDescriptor`:

- kind;
- resident id;
- subject entity/content id;
- destination/building/cell id;
- job/action/order id;
- progress и block reason;
- localization arguments.

Поддерживаемые статусы:

- `FreeTime` — «Свободное время»;
- `Move` — «Идёт»;
- `Attack` — «Атакует {target}»;
- `Cook` — «Готовит {dish} на {place}»;
- `UnpackBuilding` — «Распаковывает {building}»;
- `PackBuilding` — «Упаковывает {building}»;
- `Dig` — «Копает»;
- `Craft` — «Создаёт {product} в {building}»;
- `Pickup` — «Подбирает {item}»;
- `Service` — «Обслуживает {place}»;
- `Train` — «Тренируется в {place}»;
- `Study` — «Обучается в {place}»;
- `Logistics` — «Доставка для {destination}»;
- Eat, Sleep, Rest, Flee, Idle, Work и Blocked используют тот же typed contract.

Missing/stale target не ломает строку: используется безопасный локализованный fallback и diagnostics ID.

## 9. Выбор и камера

- ЛКМ по HUD row выбирает resident.
- ЛКМ по модели гнома выбирает того же resident.
- выбранный гном подсвечивается в HUD и мире;
- новый выбор заменяет прежний;
- ПКМ при выбранном гноме снимает selection;
- двойной ЛКМ по HUD row центрирует камеру на гноме;
- camera focus не меняет logical position;
- умерший/удалённый target обрабатывается безопасно.

## 10. Нижняя контекстная панель

В один момент отображается ровно один режим.

### 10.1 Нет выбранного объекта — ExcavationPalette

На месте инвентаря показывается меню:

- свободный тоннель;
- шаблоны комнат;
- ластик.

Ластик удаляет нарисованные designation и незавершённые части tunnel/room plans. Он отменяет связанные незавершённые jobs и reservations, но не восстанавливает уже выкопанный terrain.

### 10.2 Выбран гном — ResidentInventory

Панель находится в нижней части левее центра:

```text
[ Weapon: ножны/разгрузка ] [ Main: 6 slots ] [ Cargo: корзина ]
```

Weapon визуально отделён слева, Cargo — справа. Подробности: `resident-inventory-expansion.md`, #70.

### 10.3 Выбрано здание — BuildingFunctions

В той же области показываются capabilities выбранного здания:

- производство;
- исследования;
- storage/service modes;
- workers/visitors;
- active orders;
- другие функции из definition/runtime snapshot.

Справа находится кнопка упаковки, если здание поддерживает packing. Она создаёт typed command/job и не удаляет здание напрямую из UI.

### 10.4 Активна коробка — BuildingPlacement

Показывается название здания, orientation, валидность позиции, reason code и отмена preview. Курсор отображает здание, мир — его призрачный footprint.

Полная модель: `building-box-placement-and-packing.md`, #118.

## 11. Контекстный ввод

UI shielding выполняется первым. Затем один pointer event проходит строгий router:

1. активный placement mode + ЛКМ — подтвердить valid ghost plan;
2. `Alt + ЛКМ` по Inventory consumable/tool — использовать предмет;
3. выбранный Inventory stack + ЛКМ по земле — targeted drop;
4. `Alt + ЛКМ` по world BuildingBox — приказ выбранному гному подобрать коробку;
5. обычный ЛКМ по world BuildingBox — включить placement mode;
6. ЛКМ по BuildingBox в resident inventory — включить placement mode;
7. выбранный resident + hostile target — attack order;
8. выбранный resident + свободная reachable ground — move order;
9. без выбранного resident gameplay использует активный excavation tool.

Если `Alt + ЛКМ` попал по предмету без поддерживаемого действия или действие невозможно, специальная команда не создаётся: событие трактуется как ground click и выбранный гном идёт к месту клика.

Обычный ЛКМ по generic world item не означает автоматический pickup, если у предмета нет отдельного LMB interaction contract.

ПКМ:

- при resident selection снимает selection и не создаёт dig designation тем же click;
- в placement mode отменяет preview;
- по notification удаляет его;
- в остальных режимах следует текущему tool contract.

## 12. Коробки зданий

Подтверждённый flow:

1. Production создаёт физическую коробку здания.
2. Игрок кликает коробку в мире или inventory и включает placement mode.
3. ЛКМ ставит призрачный plan.
4. Plan резервирует одну конкретную коробку.
5. Свободный гном забирает её, несёт к месту и собирает здание.
6. Выбор построенного здания показывает функции и кнопку упаковки.
7. Упаковка выполняется работой и создаёт одну коробку после commit.

Коробка не существует одновременно в inventory, на площадке и как завершённое здание.

## 13. Notification ticker

Ticker находится в верхней части, показывает очередь отдельных активных сообщений и не перекрывает roster/time controls.

`GameNotification` содержит:

- stable id;
- kind;
- source event/idempotency key;
- simulation tick;
- priority;
- localization key + typed args;
- navigation target;
- active/dismissed state.

### 13.1 Виды

- нападение на гномов;
- рождение гнома;
- голод;
- переход в старость;
- Mood `<5`;
- смерть;
- изобретение технологии;
- выполнение задания.

### 13.2 Голод

- порог: Nutrition `<15` в UI, то есть `<1500` в Domain;
- событие создаётся только при пересечении порога сверху вниз;
- после восстановления до `>=15` новое падение может создать новое уведомление;
- сообщения разных гномов не объединяются.

### 13.3 Жизненный цикл сообщения

- уведомление остаётся активным, пока игрок не обработает его;
- ЛКМ просматривает сообщение и выполняет focus/open action;
- ПКМ удаляет сообщение без перехода;
- отдельная история уведомлений не требуется;
- просмотр/удаление не меняют source Domain event;
- duplicate event id не создаёт второе сообщение.

Death notification хранит последнюю известную позицию только для focus. Technology notification открывает описание без выдуманной world cell.

## 14. Save/Load

Не сохраняются как authoritative simulation state:

- selected row;
- panel expansion;
- hover;
- camera focus;
- placement preview до подтверждения;
- ticker animation.

Сохраняются владельцами:

- needs, skills, actions и schedule;
- items/slots/boxes/reservations;
- confirmed building plans и jobs;
- lifecycle и source events, если они входят в save contract.

Активная notification queue может сохраняться как UI/profile state только отдельным решением; история не требуется.

## 15. Производительность и accessibility

- roster virtualized;
- одна изменившаяся строка не пересоздаёт весь список;
- 64+ residents не создают GameObject/allocations на каждый simulation tick;
- цвет всегда дублируется формой, icon, числом или label;
- click targets имеют доступный размер;
- localization не влияет на identity, sorting или commands.

## 16. Критерии приёмки

- HUD/world selection синхронны;
- collapsed/expanded rows строятся из snapshots;
- Mood ranges и «Бодрость» соблюдаются точно;
- top-5 выбирается из всех 12 навыков;
- без selection отображается tunnel/rooms/eraser menu;
- resident/building/placement modes взаимоисключающие;
- один click создаёт не более одной command;
- BuildingBox flow сохраняет количество;
- notifications используют events, threshold crossing и persistent lifecycle;
- UI click-through отсутствует;
- deterministic/read-model/input/Play Mode tests покрывают status, panel modes, routing и notifications.
