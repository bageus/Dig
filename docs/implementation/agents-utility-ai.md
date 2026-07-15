# Жители, потребности и Utility AI

Этот документ разделяет **фактическое текущее состояние кода** и **утверждённую целевую модель** еды, сна, досуга и выбора блюд.

Authoritative design:

- [`../design/content/food.md`](../design/content/food.md);
- [`../design/needs-continuous-actions.md`](../design/needs-continuous-actions.md);
- [`../design/open-questions.md`](../design/open-questions.md).

Связанные задачи: #28, #96–#101, #144, #159.

## Границы владения

- `AgentState` — runtime-состояние resident;
- `AgentNeedsState` — Nutrition, Alertness, Mood и Health;
- `DailySchedule` — неизменяемое определение распорядка;
- `AgentDecisionSystem` — чистое решение по snapshot;
- `ActiveAgentAction` — единственное текущее действие;
- `InventoryState` — food items, quantities и reservations;
- `BuildingFacilitiesState` — Bed/Leisure places и reservations;
- `Technology` — исследованные блюда;
- `World/Exploration` — видимость еды;
- `ResidentSettlementSystem` — orchestration без копий state;
- Presentation — selection, HUD и animation view.

UI, Unity renderer и Navigation не изменяют needs напрямую.

## Текущее состояние реализации

В существующем settlement tick для targeted action используется complete-only схема:

```text
reserve target
-> progress duration
-> external commit
-> apply full NeedDelta
-> completed
```

Фактически сейчас:

- Eat резервирует food stack;
- по завершении вызывает `InventoryState.ConsumeReserved`;
- затем применяет полный Nutrition effect;
- Sleep и Rest применяют полный effect только после завершения;
- interruption до completion освобождает reservation и не даёт частичного effect.

Эта схема остаётся описанием текущего кода, но **не является целевой design-моделью** для Eat/Sleep/Leisure.

## Целевая модель

- #97 заменяет Eat на три bites;
- #99 добавляет desired dish, fallback и history из 10 трапез;
- #159 заменяет complete-only Sleep/Leisure на постепенные effects;
- #101 расширяет Save/Load.

Одновременно legacy и target path для одного action type работать не должны.

## Порядок settlement tick

Для каждого живого resident в stable `EntityId` order:

1. пассивно изменяются needs;
2. удаляется истёкший player order;
3. проверяется death condition;
4. создаётся decision snapshot;
5. проверяется текущая target reservation;
6. внешняя доступность объединяется с Inventory/Facilities/Technology/Visibility;
7. Utility AI оценивает intents;
8. target резервируется;
9. action продвигается на один simulation step;
10. подтверждённые bite/interval/result events применяются idempotently;
11. terminal action освобождает reservations;
12. state сохраняется и публикует typed events.

В target-модели NeedDelta может применяться на шагах действия, а не только в terminal completion.

## Потребности и scale

Authoritative needs хранятся целыми числами `0..10000`:

- `Nutrition`;
- `Alertness`;
- `Mood`;
- `Health`.

HUD показывает `0..100`.

Для еды подтверждён единый converter:

```text
DomainNutrition = DesignNutrition × 100
```

Пример: `15 -> 1500`, по `500` за один из трёх bites.

Clamp выполняется после каждого применённого effect.

`Alertness` отображается как положительная шкала **«Бодрость»**: большое значение — хорошее состояние.

## Расписание

`DailySchedule` содержит непрерывные неперекрывающиеся segments:

- `Work`;
- `Rest`;
- `Sleep`;
- `Free`.

Schedule влияет на utility score, но critical survival/emergency может прервать обычную работу.

HUD:

- `Free` без другого action → «Свободное время»;
- `Work` без job/action/order/emergency → idle-at-work marker;
- Eat/Sleep/Flee/Combat/Blocked не считаются обычным бездельем.

## Utility intents

Базовый stable tie-break:

1. `Flee`;
2. `Eat`;
3. `Sleep`;
4. `PlayerOrder`;
5. `Work`;
6. `Rest/Leisure`;
7. `Idle`.

Scores учитывают needs deficits, schedule, threat, skill, Mood, order priority и target availability.

Critical Hunger/Alertness имеет survival bonus. Отсутствующая еда/кровать/место создаёт rejected candidate с typed reason.

## Еда — текущее состояние

Текущий код:

1. находит food category stack;
2. резервирует quantity;
3. сохраняет target;
4. ждёт duration;
5. consumes reserved quantity;
6. применяет полный Nutrition.

Одна quantity не может быть зарезервирована двумя residents. Исчезновение stack/reservation блокирует action без effect.

## Еда — целевая модель

### Desired dish

При начале Eat intent:

1. из всех исследованных блюд выбирается desired FoodId;
2. используется отдельный deterministic random stream;
3. desired choice сохраняется в action и Save/Load;
4. сначала ищется видимая незарезервированная порция desired dish;
5. если её нет, ищется любая другая видимая еда;
6. если еды нет, возвращается `food_unavailable`.

Fallback разрешён при critical Hunger.

### Start meal

- до commit food quantity только reserved;
- interruption до commit освобождает reservation;
- commit списывает quantity из Inventory;
- meal payload хранится внутри active action;
- payload не является вторым ItemStack;
- action хранит desired/consumed IDs и `Matched`.

### Bite progress

- `TotalBites = 3`;
- каждый committed bite применяет `NutritionTotal / 3`;
- matched meal применяет data-driven positive food Mood per bite;
- fallback meal Nutrition применяет, positive food Mood не применяет;
- third bite завершает action без второго full effect;
- animation callback effect не начисляет.

### Interruption

После commit начала:

- completed bite effects сохраняются;
- remaining portion уничтожается;
- Inventory quantity не возвращается;
- no-bite meal не создаёт history entry;
- после первого bite entry создаётся один раз;
- death/remove не оставляет orphan meal state.

## История рациона

Agents хранит ring buffer максимум `10` entries:

- desired FoodVarietyId;
- consumed FoodVarietyId;
- Matched;
- first committed bite tick;
- source meal id.

Текущая entry добавляется до расчёта.

```text
U = число исследованных блюд

history count < 10       -> delta 0
Mismatches >= 6          -> delta -U
Matches >= 6 и Matched   -> delta +U
иначе                    -> delta 0
```

Следствия:

- `5/5 -> 0`;
- current fallback не создаёт positive delta;
- bad window остаётся negative до улучшения;
- Society читает итоговый Mood и не хранит diet copy.

## Сон — целевая модель

Пока `Sleep` active:

- повторяющиеся simulation intervals повышают Alertness;
- те же или отдельные intervals повышают Mood;
- amounts/frequency задаёт `SleepEffectProfile`;
- interruption прекращает новые intervals;
- уже полученный effect не откатывается;
- completion не применяет второй полный bonus;
- Bed reservation освобождается terminal policy.

Current implementation complete-only должна быть мигрирована в #159.

## Досуг — целевая модель

Пока `Leisure` active:

- simulation intervals повышают Mood;
- rate задаёт activity/place definition;
- interruption сохраняет accumulated Mood;
- target loss прекращает action и освобождает place;
- animation не начисляет Mood;
- completion не дублирует intervals.

## Active action contract

Целевой `AgentActionSnapshot` содержит:

- action kind;
- target/reservation id;
- start tick;
- completed interval/bite count;
- accumulated Nutrition/Alertness/Mood;
- next interval progress;
- effect profile/version;
- interruption/block reason;
- meal payload для Eat.

У resident не более одного `ActiveAgentAction`.

## Hysteresis и interruption

Текущее intent получает hysteresis bonus. Cooldown предотвращает осцилляцию, но не блокирует emergency, survival или explicit order.

При target switch прежняя reservation освобождается до захвата новой.

После meal commit простого release недостаточно: payload должен перейти в interrupted/completed terminal state с записанным lost remainder.

Для Sleep/Leisure interruption прекращает interval generation и сохраняет accumulated effect.

## Allocation-light queries

Settlement не материализует полный Inventory/Facilities graph для каждого resident. Узкие owner queries предоставляют:

- доступный item/category с учётом reservation;
- доступный desired FoodId;
- fallback food query;
- visibility eligibility;
- проверку одной reservation;
- available Bed/Leisure place;
- ownership facility reservation;
- researched food catalog/count.

Stable selection/tie-break не зависит от enumeration order словаря.

`InMemoryAgentRepository` кэширует stable resident order и обновляет элементы без полной сортировки на каждом tick.

Существующий Linux soak baseline для `agents.settlement`:

- average allocations: `34517` bytes/execution;
- average time: `213.73` microseconds/execution;
- budget: 500 microseconds average;
- budget: 50000 average allocated bytes;
- maximum execution: 100 milliseconds.

Новые desired/history/interval queries должны сохранять allocation-light подход.

## Resident HUD read path

HUD read model объединяет:

- identity/lifecycle;
- needs;
- schedule;
- intent/action/order;
- claimed/in-progress job;
- top skills;
- block reasons;
- logical/interpolated position reference.

`ResidentActivityDescriptor` хранит typed kind/subject/destination/source/progress/reason. Готовая локализованная строка не хранится в Domain.

Для Eat HUD показывает desired/consumed/match/bites/effects/history. Для Sleep/Leisure — accumulated effects и next interval.

Roster 64+ residents использует stable order, virtualization и incremental row updates.

## Diagnostics

`AgentDecision` содержит:

- выбранный intent;
- score/critical flag;
- reason code;
- rejected candidates.

`SettlementTickReport` содержит:

- actual target;
- action transition;
- committed bite/interval/result;
- applied effects;
- block/interruption reason.

Food diagnostics:

- desired candidates/roll;
- visibility/reservation;
- desired/fallback result;
- meal commit/bite progress;
- Nutrition/Mood effects;
- lost remainder;
- last 10 history;
- Matches/Mismatches/U/formula branch.

Sleep/Leisure diagnostics:

- place reservation;
- completed intervals;
- accumulated effects;
- next interval;
- interruption reason.

## Events

Текущие общие events:

- `AgentPlayerOrderChanged`;
- `AgentActionStarted`;
- `AgentActionInterrupted`;
- `AgentActionCompleted`;
- `AgentActionBlocked`;
- `AgentDied`;
- `ReservedItemConsumed`;
- `BuildingFacilityReservationChanged`.

Target additions:

- meal desired selected;
- meal committed;
- bite completed;
- diet entry added;
- diversity Mood applied;
- need interval applied;
- continuous action interrupted/completed.

Все events имеют stable idempotency keys.

## Save/Load

Сохраняются:

- active action и target reservation;
- desired/consumed FoodId;
- committed payload;
- bite/interval progress;
- accumulated effects;
- diet ring buffer;
- deterministic selection state/source id;
- effect/content versions.

Load не reroll desired dish и не повторяет уже committed bite/interval/history/delta.

## Проверки

Существующие tests покрывают:

- survival priority;
- schedule/hysteresis/cooldown;
- competing food/bed reservations;
- complete-only legacy actions;
- stable resident order;
- allocation-light owner queries;
- headless settlement smoke.

Новые обязательные tests:

- Nutrition conversion `×100`;
- 0/1/2/3 bites;
- desired/fallback/visibility;
- history 10, 5/5, 6/4, 4/6;
- no duplicate history/delta;
- partial Sleep/Leisure;
- interruption/death/target loss;
- Save/Load mid-action;
- deterministic replay;
- performance budgets after new queries.
