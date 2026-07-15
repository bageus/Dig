# Жители, потребности и Utility AI

Этот документ фиксирует автономного жителя, Utility AI и фактические settlement-циклы еды, сна и досуга.

## Границы владения

- `AgentState` — единственный владелец runtime-состояния конкретного жителя.
- `AgentNeedsState` владеет питанием, бодростью, настроением и здоровьем.
- `DailySchedule` является неизменяемым определением распорядка дня.
- `AgentDecisionSystem` только читает snapshot и возвращает решение.
- `ActiveAgentAction` является единственным текущим действием жителя.
- `InventoryState` владеет едой, количеством и reservation порций.
- `BuildingFacilitiesState` владеет кроватями, досуговыми позициями и их занятостью.
- `ResidentSettlementSystem` оркестрирует состояния, но не хранит копии предметов, мест или needs.
- `IAgentDecisionContextProvider` продолжает сообщать доступность работы, путь отхода и угрозу.
- Presentation владеет только локальным выбором гнома, раскрытием HUD-строки и визуальным состоянием.

UI, игровой движок и навигация не могут напрямую изменять потребности или активное действие.

## Порядок settlement-тика

Для каждого живого агента в стабильном порядке по `EntityId` выполняются шаги:

1. пассивно изменяются потребности;
2. истёкший прямой приказ удаляется;
3. проверяется смерть от критического состояния;
4. создаётся один decision `AgentSnapshot`;
5. проверяется сохранность reservation текущей цели;
6. внешняя доступность объединяется с реальным Inventory и Facilities;
7. Utility AI оценивает все намерения;
8. для `Eat`, `Sleep` или `Rest` атомарно резервируется цель;
9. выполняется один шаг действия;
10. при достижении длительности внешний результат подтверждается;
11. только после успешного результата применяется `NeedDelta`;
12. состояния сохраняются, события публикуются через общий `IEventSink`.

В пунктах 9–11 описана **текущая реализованная complete-only модель**. Для `Eat` она должна быть заменена поукусной моделью из [#97](https://github.com/bageus/Dig/issues/97); `Sleep` и `Rest` продолжают использовать двухфазное завершение.

Второй snapshot создаётся только на аварийном пути, когда существующая reservation цели исчезла и действие было заблокировано. Жители обрабатываются последовательно, поэтому reservation первого жителя немедленно видна следующему жителю в том же simulation tick.

## Потребности

Все значения представлены целыми числами в диапазоне `0–10000`:

- `Nutrition`;
- `Alertness`;
- `Mood`;
- `Health`.

Операции насыщения используют clamp, поэтому переполнение или выход за диапазон невозможны.

Пассивное изменение и эффекты действий задаются `AgentBehaviorPolicy`. При критическом питании или бодрости здоровье уменьшается, а настроение получает дополнительный штраф. В стабильном состоянии здоровье может медленно восстанавливаться.

Design-значения новых блюд заданы в шкале `15–33`; способ перевода в доменный диапазон `0–10000` открыт в `docs/design/open-questions.md`, Q-015.

### HUD-нормализация

HUD из [`../design/resident-hud-selection-and-notifications.md`](../design/resident-hud-selection-and-notifications.md) нормализует needs в `0..100` только для отображения:

- `51..100` — зелёный;
- `26..50` — оранжевый;
- `0..25` — красный.

Цвет дополняется числом, icon и accessible label.

`Alertness` является прямой положительной шкалой: высокое значение означает высокую бодрость. Термин «Усталость» потребовал бы обратной шкалы `100 - Alertness`. До ответа Q-030 Domain и read models продолжают использовать `Alertness/Бодрость` и не создают второй показатель усталости.

## Расписание

`DailySchedule` состоит из непрерывных неперекрывающихся сегментов. Сегменты обязаны покрывать полный игровой день.

Поддерживаются периоды:

- `Work`;
- `Rest`;
- `Sleep`;
- `Free`.

Расписание влияет на utility score, но не блокирует критические действия. Критический голод или усталость могут прервать обычную работу и прямой приказ.

### HUD-состояния расписания

- `Free` без более важного действия отображается как «Свободное время»;
- `Work` без active action, claimed/in-progress job, player order или emergency intent отображается как безделье в рабочее время;
- красный idle-at-work marker является производным read model и не записывает новое состояние в `AgentState`;
- критический голод, сон, бегство, бой и blocked action не должны ошибочно классифицироваться как обычное безделье;
- причина отсутствия работы берётся из authoritative decision/job diagnostics.

## Намерения и utility

Базовый набор:

1. `Flee`;
2. `Eat`;
3. `Sleep`;
4. `PlayerOrder`;
5. `Work`;
6. `Rest`;
7. `Idle`.

Порядок является стабильным tie-break. Оценки используют целочисленную арифметику: дефициты needs, расписание, угрозу, навык, настроение и приоритет приказа.

Критическая угроза, голод или усталость получают survival bonus. Недоступная еда или кровать остаётся отклонённым вариантом с диагностикой, а житель выбирает следующую допустимую альтернативу.

После реализации #99 выбор между несколькими доступными блюдами дополнительно учитывает разнообразие рациона, но критический голод остаётся важнее предпочтения.

## Реальная еда — текущее состояние

Едой считается item с категорией `food`.

В текущей реализации при выборе `Eat` система:

1. выбирает доступный stack по location и стабильному ID;
2. резервирует ровно одну единицу на `AgentId`;
3. сохраняет stack как `AgentActivityTarget`;
4. продвигает действие до заданной длительности;
5. атомарно вызывает `InventoryState.ConsumeReserved`;
6. применяет полный эффект насыщения только после успешного списания.

Одна порция не может быть зарезервирована двумя жителями. Исчезновение stack или reservation блокирует действие, освобождает остатки и не применяет эффект.

Эта логика остаётся фактическим состоянием кода до реализации #97, но больше не является целевой design-моделью.

## Целевая поукусная модель еды — ещё не реализована

Authoritative design находится в `docs/design/content/food.md`.

### Старт трапезы

- до старта одна quantity только зарезервирована;
- interruption до старта освобождает reservation без потери;
- при commit старта quantity списывается из Inventory;
- meal payload хранится внутри единственного active `Eat` action и не является ItemStack.

### Прогресс

- стандартная порция имеет 3 укуса;
- action хранит `FoodItemId`, total/completed bites и эффект одного укуса;
- каждый завершённый simulation bite применяет одну треть сытости;
- третий укус завершает action без повторного полного `NeedDelta`;
- animation callback не применяет эффект.

### Interruption

- срочное действие может завершить начатую трапезу;
- уже применённые укусы сохраняются;
- оставшиеся укусы теряются;
- порция не возвращается в Inventory и не создаётся на земле;
- полный перечень interruption classes уточняется Q-017.

### Требуемые изменения контрактов

- расширить `AgentActionSnapshot` meal progress;
- добавить атомарный start-meal Application coordinator для Agents + Inventory;
- отделить bite effect от общего action completion effect;
- добавить события meal started/bite completed/meal interrupted/meal completed;
- обновить deterministic hashing, save schema и diagnostics;
- запретить одновременную работу legacy complete-only и bite-based paths для одного блюда.

Связанные issues: #96, #97, #100, #101.

## Сон и досуг

`BuildingFacilitiesState` содержит typed места:

- `Bed` для сна;
- `Leisure` для досуга.

Место имеет стабильный ID, owning building, позицию и не более одной reservation. Один житель также не может одновременно резервировать несколько мест.

После завершения `Sleep` или `Rest` reservation освобождается, затем authoritative needs жителя получают соответствующий эффект. При прерывании, смерти или исчезновении цели reservation снимается без восстановления needs.

## Action execution

У агента существует не более одного `ActiveAgentAction`. Targeted action дополнительно хранит typed `AgentActivityTarget`, длительность, elapsed ticks и readiness к внешнему commit.

Обычные legacy-действия продолжают применять policy effect по существующему циклу. Targeted settlement-действия используют двухфазное завершение:

```text
progress -> ready -> external commit -> need effect -> completed
```

Это предотвращает восстановление голода, бодрости или настроения «из воздуха».

После #97 `Eat` станет специализированным многошаговым действием:

```text
reserve -> commit portion -> bite 1 effect -> bite 2 effect -> bite 3 effect -> completed
                                 \ interruption -> discard remainder
```

## Hysteresis, cooldown и interruption

Текущее намерение получает hysteresis bonus. После переключения действует cooldown против осцилляции. Cooldown не блокирует критическое выживание, бегство, новый приказ или продолжение текущего действия.

При смене targeted intent прежние Inventory/Facility reservations освобождаются до захвата новой цели.

После commit meal простого освобождения reservation недостаточно: food quantity уже списана, а interruption должен завершить meal payload и записать потерянный остаток.

## Allocation-light read path

Settlement не строит полный `InventorySnapshot` и списки facilities для каждого жителя. Авторитетные агрегаты предоставляют узкие read-only операции:

- наличие доступного item category с учётом reservation конкретного жителя;
- стабильный ID первого доступного food stack;
- проверка одной reservation без materialization всех reservations;
- наличие и стабильный ID доступного `Bed` или `Leisure` места;
- проверка ownership одной facility reservation.

Эти операции сканируют authoritative dictionaries и возвращают bool или один ID. Они не создают второго состояния и сохраняют тот же stable tie-break, который раньше обеспечивался сортировкой полных snapshots.

`InMemoryAgentRepository` кэширует immutable порядок ссылок на жителей и обновляет элемент при `Save`, не пересортировывая всех жителей на каждом тике.

В стандартном 2020-тиковом Linux CI soak оптимизация сохранила прежний state hash и снизила `agents.settlement`:

- average allocations с `295927` до `34517` bytes/execution;
- average time с `570.62` до `213.73` microseconds/execution.

CI закрепляет отдельный budget: 500 microseconds average, 50000 average allocated bytes и 100 milliseconds maximum execution.

## Resident HUD read path — целевой design

HUD не должен собирать готовые строки из Domain. Для #114 формируется immutable read model из:

- identity/lifecycle data;
- needs;
- schedule phase;
- current intent/action/player order;
- current claimed/in-progress job;
- top skills;
- block reasons;
- logical/interpolated position reference для camera focus.

`ResidentActivityDescriptor` содержит typed `Kind`, subject/destination IDs, source action/job/order ID, schedule phase, progress и block reason. Presentation локализует:

- движение;
- атаку;
- готовку;
- упаковку/распаковку;
- копание;
- создание;
- подбор;
- сервис;
- тренировку/обучение;
- логистику;
- существующие Eat/Sleep/Rest/Flee/Idle.

Готовый display text не сохраняется и не используется как ключ. Top-5 skills сортируются по значению, затем stable `AgentSkillId`.

Roster на 64+ жителях должен использовать stable order, virtualization и incremental row updates, а не материализовывать полный новый GameObject/list graph каждый simulation tick.

## Разнообразие питания — целевой design

После реализации #99 Agents хранит ограниченную индивидуальную историю `FoodVarietyId`.

Подтверждено:

- при одном доступном виде еды штрафа нет;
- после появления выбора однообразное меню снижает Mood;
- новый вид блюда обновляет историю;
- рождаемость уменьшается косвенно через снижение Mood;
- отдельный food fertility modifier не создаётся.

Окно истории, условие активации и величина штрафа остаются открыты в Q-016 и должны быть data-driven.

## Диагностика

`AgentDecision` содержит выбранное намерение, score, critical flag, reason code, объяснение и все отклонённые варианты.

`SettlementTickReport` дополнительно содержит для каждого жителя:

- выбранное решение;
- фактическую цель;
- признак завершения действия;
- причину блокировки.

`AgentState.LastActionBlockReason` и событие `AgentActionBlocked` сохраняют причины вроде:

- `food_unavailable`;
- `bed_unavailable`;
- `leisure_unavailable`;
- `target_reservation_missing`.

После #97–#101 диагностика еды дополнительно показывает committed state, bite progress, применённую сытость, потерянный остаток, историю рациона и monotony penalty.

После #113/#114 диагностика HUD дополнительно показывает:

- descriptor kind и source IDs;
- schedule/activity classification;
- idle-at-work reason;
- raw и normalized needs;
- top-5 sort keys;
- selection/focus state только на Presentation side.

## Реализованные события

- `AgentPlayerOrderChanged`;
- `AgentActionStarted`;
- `AgentActionInterrupted`;
- `AgentActionCompleted`;
- `AgentActionBlocked`;
- `AgentDied`;
- `ReservedItemConsumed`;
- `BuildingFacilityReservationChanged`.

Meal-specific events пока не реализованы и входят в #97.

Notification ticker из #116 должен использовать подтверждённые events. Для hunger threshold crossing, birth, old-age transition, technology discovery и task completion недостающие typed events добавляются владельцам соответствующих систем; UI не имитирует их polling-ом каждый frame.

## Проверки

Текущие тесты покрывают:

- survival priority, расписания, hysteresis и cooldown;
- одну порцию еды для двух критически голодных жителей;
- одну кровать для двух истощённых жителей;
- эффект досуга только после завершения;
- исчезновение food reservation без ложного восстановления needs;
- стабильный порядок нескольких жителей;
- освобождение facilities после completion;
- атомарное списание еды;
- allocation-light owner queries с сохранением deterministic ordering;
- per-system performance budget override;
- headless-сценарий двух жителей, которые независимо едят и спят без нарушения reservations.

Новые обязательные тесты bite progress, interruption, recipes, variety и save/load перечислены в #97–#101.

HUD/input/notification tests перечислены в #113–#117 и должны покрывать границы needs, Work+Idle classification, typed status mapping, top-5 ordering, event threshold crossing, UI shielding и renderer rebuild без изменения AgentState.
