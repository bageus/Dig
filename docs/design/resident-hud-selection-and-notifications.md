# HUD гномов, выбор, статусы и системные уведомления

## 1. Назначение

Документ описывает целевую систему постоянного списка жителей, подробной карточки выбранного гнома, контекстного управления мышью, отображения личного инвентаря и верхней строки системных уведомлений.

Главная feature-задача: [#113](https://github.com/bageus/Dig/issues/113).

Связанные задачи:

- [#114](https://github.com/bageus/Dig/issues/114) — resident HUD/read models/status descriptors;
- [#115](https://github.com/bageus/Dig/issues/115) — выбор, камера и context input;
- [#116](https://github.com/bageus/Dig/issues/116) — notification ticker;
- [#117](https://github.com/bageus/Dig/issues/117) — цветовые шкалы и skill capacity;
- [#70](https://github.com/bageus/Dig/issues/70) — личный инвентарь и item input;
- [#107](https://github.com/bageus/Dig/issues/107) — навыки, UI и Save/Load.

Неоднозначности и конфликты находятся в [`open-questions.md`](open-questions.md), Q-028–Q-033.

## 2. Владение состоянием

- `AgentState` владеет личностью, живым/мёртвым состоянием, потребностями, текущим действием, приказом и навыками гнома.
- `Jobs` владеет работой, стадией, назначенным исполнителем и reservations.
- `InventoryState` владеет предметами, количеством, отсеками и слотами.
- `Society/Lifecycle` владеет полом, возрастной стадией, рождением и социальными условиями размножения.
- `Combat` владеет враждебностью, атакой и местом столкновения.
- `Technology` владеет изобретениями.
- `Presentation` владеет только выбранной строкой, открытием карточки, hover/focus, scroll/virtualization и анимацией ticker.
- Камера не владеет логической позицией гнома.
- Готовая локализованная строка статуса или уведомления не является источником игрового состояния.

UI читает immutable read models и отправляет Application commands. Он не изменяет Domain collections напрямую.

## 3. Общий layout HUD

### 3.1 Верхняя часть

Верхняя зона делится на независимые области:

- управление временем — существующий HUD из #111;
- notification ticker — верхняя центральная область;
- resident roster — правый верхний угол.

Области не должны перекрываться. Клик по любой HUD-области блокирует world input.

### 3.2 Resident roster

Roster представляет собой полупрозрачную вертикальную панель со строками гномов.

Требования:

- стабильный порядок строк;
- scrolling при нехватке высоты;
- virtualization для больших поселений;
- выбранная строка остаётся видимой или автоматически прокручивается в viewport;
- одновременно раскрывается только выбранная строка;
- удаление/смерть жителя обновляет список из authoritative membership snapshot;
- удаление визуального объекта и повторный renderer rebuild не меняют выбор в Domain, потому что выбор является локальным UI state.

## 4. Компактная строка гнома

Каждая закрытая строка показывает:

1. имя;
2. индикатор пола;
3. mood face;
4. небольшую шкалу здоровья;
5. режим расписания/активности;
6. специальный индикатор безделья в рабочее время.

### 4.1 Имя и пол

- мужские имена отображаются синим;
- женские имена отображаются розовым;
- цвет не является единственным признаком пола;
- рядом используется иконка, символ или accessible label;
- имя и пол поступают из identity/lifecycle read model;
- UI не определяет пол по тексту имени.

Имена должны соответствовать полу согласно системе генерации/контента имён, но stable `ResidentId` не зависит от имени.

### 4.2 Mood face

Три состояния:

- грусть — Mood `0..25`;
- нейтральное — средний диапазон;
- радость — высокий диапазон.

Точная принадлежность значения `75` блокирована Q-031.

Радостный face является информационным признаком готовности по настроению, но не заменяет проверки Society:

- взрослый возраст;
- допустимая пара;
- отсутствие близкого родства;
- fertility modifiers;
- состояние здоровья и другие правила.

### 4.3 Health bar

Компактная Health bar использует общую need-color policy из раздела 7.

Она дополнительно имеет icon/tooltip/numeric value, чтобы состояние было понятно без цвета.

### 4.4 Режим активности

Отдельно от детального статуса строка показывает текущий schedule phase:

- `Work` — рабочее время;
- `Rest` — время отдыха;
- `Sleep` — время сна;
- `Free` — свободное время.

В режиме `Free`, если нет более важного активного действия, текст состояния отображается как **«Свободное время»**.

### 4.5 Безделье в рабочее время

Индикатор подсвечивается красным, когда одновременно выполняются условия:

- текущий schedule phase — `Work`;
- гном жив;
- нет активной emergency/survival activity;
- нет active action, claimed/in-progress job или выполняемого player order;
- гном фактически находится в `Idle`/blocked-without-work состоянии.

Отсутствие работы из-за критического голода, сна, бегства или боя не должно ошибочно считаться обычным бездельем.

UI показывает reason/tooltip, например:

- `no_available_job`;
- `path_unavailable`;
- `required_material_missing`;
- `worker_place_unavailable`.

## 5. Раскрытая строка выбранного гнома

При выборе строки она увеличивается по высоте.

Порядок данных:

1. текущий статус действия;
2. здоровье;
3. сытость;
4. бодрость/усталость;
5. настроение;
6. пять самых высоких навыков.

### 5.1 Пять характеристик

Отображаются только пять навыков с максимальным значением.

Стабильная сортировка:

1. уровень по убыванию;
2. при равенстве — stable `AgentSkillId` по возрастанию.

Локализованное название не участвует в tie-break.

Полный список навыков остаётся доступен в отдельном inspector/details view из #107.

## 6. Типизированные статусы деятельности

### 6.1 Общий контракт

Статус строится из immutable descriptor:

```text
ResidentActivityDescriptor
- ResidentId
- Kind
- SourceActionId / JobId / OrderId
- SubjectEntityId?
- SubjectContentId?
- DestinationEntityId?
- DestinationCellId?
- SchedulePhase
- Progress?
- BlockReason?
- LocalizationArguments
```

Domain не хранит строку «Готовит грибной хлеб на индустриальной кухне». Presentation получает IDs и формирует локализованный текст.

### 6.2 Каталог статусов

| Kind | Отображение | Источник |
|---|---|---|
| `FreeTime` | Свободное время | Schedule `Free` + отсутствие более важного действия |
| `Move` | Идёт | прямой player movement order |
| `Attack` | Атакует `{цель}` | Combat order/action |
| `Cook` | Готовит `{блюдо}` на `{место}` | Production cooking job |
| `UnpackBuilding` | Распаковывает `{здание}` | Construction/building-kit action |
| `PackBuilding` | Упаковывает `{здание}` | dismantle/pack action |
| `Dig` | Копает `{цель?}` | digging/mining job |
| `Craft` | Создаёт `{предмет/здание}` в `{здание}` | Production order/job |
| `Pickup` | Подбирает `{предмет}` | direct pickup order |
| `Service` | Обслуживает `{место}` | bar/cinema/theatre/service job |
| `Train` | Тренируется в `{место}` | combat/training facility |
| `Study` | Обучается в `{место}` | university/education activity |
| `Logistics` | Доставка для `{объект}` | hauling job/demand destination |
| `Eat` | Ест `{блюдо}` | active Eat action |
| `Sleep` | Спит | active Sleep action |
| `Rest` | Отдыхает в `{место?}` | active Rest/Leisure action |
| `Flee` | Убегает от опасности | survival intent |
| `IdleAtWork` | Бездействует в рабочее время | Work schedule + no work/action |
| `Blocked` | Не может выполнить действие: `{причина}` | current action/job diagnostics |

Если target/content отсутствует или устарел, UI использует fallback name и reason, не выбрасывает exception и не меняет state.

## 7. Цветовая система потребностей

### 7.1 Нормализация

Авторитетные needs сейчас хранятся в диапазоне `0..10000`:

- `Health`;
- `Nutrition`;
- `Alertness`;
- `Mood`.

UI преобразует их в `0..100` только для отображения.

### 7.2 Цветовые диапазоны

Для полного значения и постепенного снижения:

- `51..100` — зелёный;
- `26..50` — оранжевый;
- `0..25` — красный.

Точки границы:

- 50 — оранжевая;
- 25 — красная.

Цвет дополняется числом, icon и accessible label.

### 7.3 Бодрость против усталости

Текущий Domain использует `Alertness`: высокое значение является хорошим и должно быть зелёным.

Если UI должен называться «Усталость», шкалу придётся инвертировать. Решение блокировано Q-030. До ответа runtime-значение не переименовывается и не инвертируется.

## 8. Цветовая система навыков

Навык отображается полосой от тёмно-синего при низком значении к зелёному при высоком.

Требования:

- gradient вычисляется по нормализованному `current / max`;
- рядом отображается число;
- tooltip показывает skill name, stable diagnostics ID, current/max и источник последнего изменения;
- цвет не является единственным признаком уровня;
- top-5 и полный inspector используют один skill snapshot.

### 8.1 Новое предложение capacity

Новый design предлагает:

- общий базовый лимит профессиональных характеристик — `120`;
- после достижения суммы 120 рост активного навыка сопровождается уменьшением других;
- навык с большим текущим значением сильнее подвержен потере;
- визуализируемые профессиональные навыки: stonework, cooking, woodworking, metallurgy, alchemy, service, logistics.

Это конфликтует с ранее зафиксированным `TotalSkillCapacity = 100`, расширяемым университетом до `200`, и с каталогом из 12 навыков. До ответа Q-028/Q-029 новая схема не заменяет существующую save/runtime-модель.

## 9. Личный инвентарь выбранного гнома

Инвентарь появляется в нижней части экрана, левее центра.

Layout строго разделяет отсеки:

```text
[ Weapon: ножны/разгрузка ] [ Main: 6 ячеек ] [ Cargo: корзина/большая корзина ]
```

- Weapon compartment располагается слева от Main;
- Main располагается в центре панели;
- Cargo располагается справа от Main;
- между группами есть визуальный отступ, рамка и отдельный заголовок/icon;
- inactive expansion не создаёт ghost slots;
- active/inactive tier отображается согласно #65/#70;
- предмет в руках остаётся в исходной ячейке и показывается через `HeldItemReference`;
- пустая корзина может быть скрыта на модели, но её item и доступные слоты видны в UI;
- штраф скорости показывается рядом с Cargo.

## 10. Выбор гнома

### 10.1 Через HUD

- ЛКМ по строке выбирает гнома;
- раскрывается его строка;
- появляется inventory panel;
- модель гнома получает selected highlight.

### 10.2 В мире

- ЛКМ по visual resident выбирает того же resident;
- HUD прокручивается к его строке;
- строка раскрывается;
- предыдущий selection заменяется.

### 10.3 Снятие выбора

ПКМ при активном resident selection:

- снимает selection;
- закрывает expanded row;
- скрывает resident inventory panel;
- очищает resident-context preview;
- не создаёт digging designation этим же click.

Без resident selection существующий right-click digging control может работать в соответствующем input mode.

### 10.4 Фокус камеры

Double-click по строке:

- выбирает resident;
- переносит и центрирует камеру на его текущей позиции;
- не меняет logical position resident;
- использует тот же camera focus contract, что notification click.

## 11. Контекстное управление мышью

### 11.1 Приоритет обработки

После HUD shielding применяется порядок:

1. `Alt + ЛКМ` — use interaction;
2. selected inventory stack + ЛКМ по валидной земле — targeted drop;
3. selected resident + ЛКМ по hostile target — attack;
4. selected resident + ЛКМ по world item — pickup/use policy;
5. selected resident + ЛКМ по свободной reachable ground — move;
6. world/designation controls без resident selection;
7. ПКМ при resident selection — clear selection.

Один physical click не может создать две команды.

### 11.2 Движение

ЛКМ по свободному месту при выбранном гноме создаёт typed player move order.

- путь проверяется Navigation;
- недостижимая точка возвращает reason;
- UI может показать preview route;
- визуальное перемещение не коммитит logical position без `MoveAgentCommand`.

### 11.3 Атака

ЛКМ по attackable hostile target создаёт attack order:

- target имеет stable entity ID;
- обычная ground movement команда не создаётся;
- stale/dead target безопасно отклоняется;
- статус становится «Атакует {target}» после authoritative acceptance.

### 11.4 Подбор

Прямой приказ подобрать предмет создаёт status `Pickup` и резервирует доступное quantity.

Поведение обычного и `Alt` click по pickup-only/pickup-and-use item блокировано Q-032.

### 11.5 Использование

`Alt + ЛКМ` в inventory отправляет существующий `UseInventoryItem`.

Для мира целевой design допускает цепочку:

```text
reserve world item -> travel -> pickup -> use -> complete
```

Но применимость к pickup-only предметам и обычный click требуют ответа Q-032.

## 12. Верхняя строка уведомлений

### 12.1 Модель

```text
GameNotification
- NotificationId
- Kind
- SourceEventId
- Tick
- Priority
- LocalizationKey
- TypedArguments
- NavigationTarget
- DeduplicationKey
```

`NavigationTarget`:

- Resident;
- Entity;
- Cell;
- Building;
- Job/Task;
- Technology;
- None.

### 12.2 Каталог уведомлений

| Kind | Текст | Click action |
|---|---|---|
| `UnderAttack` | На гномов напали | фокус места боя |
| `ResidentBorn` | Родился гном `{имя}` | выбрать и сфокусировать новорождённого |
| `ResidentHungry` | `{имя}` голоден | выбрать и сфокусировать гнома |
| `ResidentOld` | `{имя}` стар | выбрать и сфокусировать гнома |
| `ResidentVeryUnhappy` | `{имя}` недоволен | выбрать и сфокусировать гнома |
| `ResidentDied` | `{имя}` умер | фокус последней позиции/карточка погибшего |
| `TechnologyDiscovered` | `{изобретение}` изобретено | открыть название и описание технологии |
| `TaskCompleted` | `{задание}` выполнено | фокус места/объекта и сведения о результате |

### 12.3 Источники событий

- нападение — Combat/alarm domain event;
- рождение — Society/Lifecycle event;
- голод — Needs threshold-crossing event;
- старость — LifeStageChanged to Old;
- недовольство — вход в Mood `< 5`;
- смерть — Agent/Lifecycle death event;
- изобретение — Technology unlock event;
- завершение задания — Jobs/Production/Construction completion event.

UI не должен опрашивать состояние каждый frame и создавать одно уведомление на каждый tick.

### 12.4 Очередь

- отображается одна активная бегущая строка;
- следующие записи находятся в bounded queue/history;
- порядок: priority, затем tick, затем stable NotificationId;
- critical attack/death может обгонять информационное completion;
- click блокирует world input;
- stale target возвращает понятную причину;
- точный cooldown, повтор и hunger threshold блокированы Q-033.

## 13. Accessibility

- пол не определяется только цветом;
- needs не определяются только цветом;
- навыки показывают число, а не только gradient;
- selected state имеет outline/icon, а не только оттенок;
- mood face имеет текстовый label;
- ticker доступен паузе/чтению и не требует успеть заметить только движение текста;
- clickable rows имеют keyboard/gamepad focus path в будущей input abstraction.

## 14. Save/Load

Не сохраняются как authoritative simulation state:

- выбранный resident;
- раскрытая строка;
- hover;
- scroll position;
- camera focus;
- текущая анимационная позиция ticker.

Сохраняются владельцами систем:

- resident identity/lifecycle;
- needs;
- skills/capacity;
- active actions/orders/jobs;
- inventory layout;
- technology state;
- lifecycle/combat/task state, из которого происходят новые события.

Если потребуется persistent notification history, она получает отдельную versioned Presentation/Profile schema, а не встраивается в AgentState.

## 15. Диагностика и тесты

### 15.1 Диагностика строки

- resident ID/name/sex/life stage;
- current schedule phase;
- active intent/action/job/order;
- descriptor kind and source;
- idle-at-work reason;
- normalized and raw needs;
- top-5 sort keys;
- selection/focus state.

### 15.2 Input diagnostics

- pointer target category;
- active modifier;
- selected resident/item/mode;
- chosen priority branch;
- command id/result/reason;
- UI shielding result.

### 15.3 Notification diagnostics

- source event;
- dedup key;
- priority/order;
- navigation target;
- stale target reason;
- queue/history size.

### 15.4 Обязательные тесты

- thresholds `25/26/50/51`;
- mood face exact boundaries после Q-031;
- top-5 deterministic sorting;
- Work+Idle red marker и исключения emergency/rest/free;
- все status kinds;
- HUD/world selection synchronization;
- double-click focus;
- full click-priority matrix;
- no click-through HUD;
- inventory Weapon/Main/Cargo layout;
- every notification kind;
- duplicate/stale event handling;
- 64+ resident virtualization and allocation budget;
- renderer rebuild without simulation mutation.

## 16. Открытые вопросы

- Q-028 — `TotalSkillCapacity 120` против существующих `100 -> 200`; какие навыки входят в pool.
- Q-029 — точный deterministic алгоритм уменьшения других навыков.
- Q-030 — показывать `Alertness/Бодрость` или инвертированную `Усталость`.
- Q-031 — состояние Mood ровно `75`.
- Q-032 — поведение ЛКМ и `Alt+ЛКМ` для world items разных use-категорий.
- Q-033 — threshold/cooldown/dedup notification событий голода и повторяющихся состояний.
