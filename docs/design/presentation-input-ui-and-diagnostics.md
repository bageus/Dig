# Presentation, ввод, UI и диагностические представления

Статус: `IMPLEMENTED`.

Tracking issue: [#14](https://github.com/bageus/Dig/issues/14).

Связанные системы: [`resident-hud-selection-and-notifications.md`](resident-hud-selection-and-notifications.md), [`contextual-input-cursors-and-selection.md`](contextual-input-cursors-and-selection.md), [`world-3d-depth.md`](world-3d-depth.md), [`../architecture/systems-gameplay.md`](../architecture/systems-gameplay.md#6-presentation), [`../development-rules.md`](../development-rules.md).

## 1. Назначение и границы

Presentation является Unity-host слоем Dig. Он отображает подтверждённое состояние симуляции, преобразует пользовательский ввод в типизированные Application intents и хранит только локальное визуальное состояние.

В scope системы входят:

- bootstrap Unity scene и adapters к engine-independent core;
- world, resident, building, item и job projections;
- side-view camera и ограниченная depth projection;
- selection, hover, focus, previews и mutually exclusive panels;
- resident roster, inspector, inventory и notification ticker;
- debug overlays, typed rejection reasons и runtime diagnostics;
- интерполяция и визуальные эффекты после authoritative commit.

Система не владеет terrain, residents, jobs, reservations, items, buildings, needs, skills, combat, technology или save data. Конкретные игровые workflows уточняются в связанных authoritative specifications и не переопределяются этим документом.

## 2. Подтверждённый пользовательский workflow

### Запуск

1. Unity bootstrap создаёт runtime composition и связывает Application/Domain sessions.
2. Presentation загружает immutable snapshots/read models.
3. World, residents, items, buildings, jobs и HUD создаются из snapshots.
4. До готовности session bindings HUD возвращает безопасные unavailable projections и typed `unity.agent_simulation.not_initialized` для mutation attempts.

### Нормальное выполнение

1. UI shielding проверяется до world routing.
2. Pointer/keyboard input преобразуется в один `ContextInputDecision`.
3. Один pointer event создаёт не более одной Application command intent; local effects selection/focus/preview могут выполняться отдельно.
4. Authoritative owner валидирует и commits command.
5. Подтверждённые snapshots/events обновляют renderers, HUD, ticker и diagnostics.
6. Visual position интерполируется между подтверждёнными logical positions; animation не завершает Domain action.

### Повторное использование

- visual entities, HUD rows и overlays могут быть удалены и пересозданы из snapshots;
- повторный refresh не создаёт игровые сущности, jobs, reservations или events;
- notification projector отклоняет повторный source event/idempotency key;
- roster использует bounded pooled rows вместо создания GameObject на каждого resident каждый tick.

### Отмена

- RMB и UI cancel применяются согласно активному контексту до world fallback;
- uncommitted preview, hover, selection и camera focus отменяются локально;
- отмена committed plan/job выполняется только Application command владельца системы;
- UI click не проходит в world и не создаёт вторую command.

### Blocked, failure и retry

- invalid/stale target возвращает typed reason и не изменяет authoritative state;
- unavailable runtime bindings возвращают empty/unavailable read models, а не exception;
- invalid placement/command сохраняет допустимый preview или selection согласно связанной specification;
- после authoritative change Presentation перечитывает snapshot и может повторить routing на следующем pointer event;
- один failed presenter/visual не должен становиться владельцем simulation lifecycle.

### Несколько residents/items/jobs

- каждый visual/read-model использует stable entity ID;
- selection хранит один active target per selection context и не меняет membership;
- simultaneous jobs/reservations/notifications отображаются как отдельные records;
- renderer rebuild не меняет candidate pools, reservations, actions, item locations или notification sources.

## 3. Владение состоянием

Authoritative owners:

- World — cells, chunks, excavation state и world versions;
- Agents/Society — residents, needs, schedule, actions и lifecycle;
- Jobs/Reservations — job state, claims и release;
- Inventory/Buildings — items, storage, plans и building lifecycle;
- Combat/Technology — соответствующие intents, events и results;
- Application/Infrastructure — command handlers, event journal, save/load и adapters.

Presentation владеет только:

- selected/hovered/focused IDs;
- camera state и interpolation state;
- active panel, scroll и pooled row binding;
- uncommitted previews/cursors;
- visual instances/material instances;
- notification queue animation/dismissed local state, если history не сохраняется профилем;
- enable/disable state debug overlays.

## 4. Модель данных

- immutable world, resident, building, item, job и diagnostic view models;
- stable entity IDs и typed target kinds;
- `ResidentActivityDescriptor` и typed localization arguments;
- `GameNotification` со stable id, source event key, tick, priority и navigation target;
- logical cell/position отдельно от interpolated transform;
- presentation version/cursor для event journal и refresh cycles;
- bounded pooled HUD row/view instances.

Локализованный текст, GameObject name, material color и animation state не являются identity или authoritative data.

## 5. Commands, events и queries

Commands:

- создаются только как типизированные Application intents после input routing;
- проходят валидацию соответствующим authoritative owner;
- не исполняются renderer, animation event или HUD widget напрямую.

Events:

- подтверждённые Domain/Application facts питают notification ticker, effects и refresh;
- один source event key проецируется не более одного раза;
- visual event не используется как скрытая gameplay command.

Queries:

- snapshot/read-model reads не имеют side effects;
- inspector, roster, overlay и cursor reason читают один authoritative projection cycle;
- camera focus, selection highlight и debug toggles не меняют simulation.

## 6. Состояния и переходы

Основной Presentation lifecycle:

```text
Unbound -> Bound -> SnapshotLoaded -> Rendered -> Refreshing -> Rendered
```

Допустимые recovery transitions:

```text
Bound/Rendered -> Unbound (Unity reload or teardown)
Unbound -> Bound (rebind)
Rendered -> Rebuilding -> Rendered
```

Input lifecycle:

```text
PointerReceived -> UIShielded | Routed
Routed -> LocalEffectOnly | CommandIntent | RejectedWithReason
CommandIntent -> Accepted/RejectedByOwner -> Refresh
```

Animation и interpolation могут завершиться только после authoritative transition и не переводят gameplay state самостоятельно.

## 7. Input, UI и Presentation

- UI shielding выполняется до world routing.
- Один pointer event создаёт максимум одну command intent.
- Selected inventory stack drop имеет приоритет над resident move.
- Hostile target имеет приоритет над free-ground movement.
- RMB при выбранном resident снимает selection и не создаёт digging designation тем же click.
- Без resident selection excavation input сохраняется в активном excavation mode.
- HUD/world selection используют тот же stable ID и обновляются вместе.
- Camera focus и highlight являются локальными effects.
- Panel modes взаимоисключающие и вычисляются из typed selection/mode state.
- Цвет всегда дополняется числом, icon, label, pattern или text reason.
- нижняя центральная context panel всегда имеет ту же внешнюю высоту и нижнюю/верхнюю границу, что minimap и clock;
- содержимое context panel не может увеличивать внешний HUD: при большем количестве controls уменьшаются внутренние padding, cell/button size и best-fit font size внутри фиксированной границы;
- inventory compartments используют ровно два ряда, а Cargo не выводит отдельный capacity title.

Детальная priority matrix и BuildingBox/world-item behavior принадлежат связанным input/item specifications.

## 8. Зависимости и конфликты

Presentation зависит от публичных contracts Application/Domain и не импортируется ими.

Конфликты разрешаются до command creation:

1. blocking UI;
2. active modal/preview context;
3. selected item interaction;
4. typed combat/world target;
5. resident movement;
6. excavation/tool action;
7. local selection/focus.

Фактический порядок для конкретного gameplay workflow берётся из более узкой authoritative specification. Physics raycast order и collider overlap не определяют бизнес-приоритет.

## 9. Инварианты

- удаление и пересоздание visual entities не меняет simulation;
- UI не изменяет доменные коллекции напрямую;
- animation event не является единственным источником action completion;
- logical и visual positions разделены;
- renderer rebuild не меняет selection targets в Domain, jobs, reservations, actions, notifications или items;
- один pointer event не создаёт двойные commands;
- status/notification text строится из typed data;
- debug overlays отключаются без изменения поведения;
- stale IDs обрабатываются контролируемо;
- 64+ residents не создают unbounded rows/GameObjects на каждый tick;
- accessibility не зависит только от цвета.

## 10. Save/Load и migration

Не сохраняются как simulation state:

- hover/selection/focus;
- camera interpolation;
- uncommitted preview/cursor;
- HUD scroll/expanded row;
- renderer instances/material instances;
- debug overlay visibility;
- ticker animation.

Сохраняются соответствующими owners:

- authoritative entities и logical positions;
- jobs, reservations, plans и item locations;
- needs, actions, skills и lifecycle data;
- confirmed events/history только если это отдельно определено profile/save contract.

После load Presentation полностью пересоздаётся из restored snapshots. Migration Presentation-local ephemeral state не требуется.

## 11. Диагностика

Минимально доступны:

- resident intention, current job/action, needs, decision reason и alternatives;
- job lifecycle, target и reservation owners;
- routes/navigation regions и dirty chunks;
- typed command rejection/block reasons;
- world/chunk versions и refresh state;
- simulation timing/overlay toggles;
- selected source ID и navigation target notification;
- runtime unavailable/binding reason без повторяющихся exceptions.

## 12. Тестовая матрица

Domain/Application/unit/integration:

- ownership and dependency rules;
- immutable read models and typed descriptors;
- input priority and single-command invariant;
- notification threshold/idempotency/order;
- stable selection and stale-target fallback;
- pooled roster window for 70 residents;
- renderer/source contracts and save-independent reconstruction.

Deterministic/headless:

- fixed-tick simulation remains unchanged by Presentation toggles;
- smoke plus standard/large soak complete without invariant or budget violations.

Unity EditMode/PlayMode and end-to-end scenarios are checked in for representative bootstrap, renderer, selection/input, BuildingBox, excavation, notification and HUD regressions. Their factually executed licensed evidence belongs to [#511](https://github.com/bageus/Dig/issues/511) and is required before status can become `VERIFIED`.

## 13. Acceptance

`IMPLEMENTED` acceptance:

- [x] Unity host composes engine-independent core without reversing dependencies;
- [x] visual entities can rebuild from snapshots without authoritative mutation;
- [x] world/resident/building/item/job projections and side-view camera exist;
- [x] logical movement and visual interpolation are separated;
- [x] resident inspector exposes intention, job/action, needs and decision reason;
- [x] HUD/world selection uses stable IDs;
- [x] input router enforces UI shielding and at-most-one command;
- [x] notification ticker consumes confirmed typed events with idempotency keys;
- [x] debug overlays are optional Presentation state;
- [x] excavation vertical slice is observable through terrain, jobs, routes and output projections;
- [x] HUD uses non-color accessibility signals;
- [x] bounded roster pool covers 64+ residents;
- [x] source-contract, Release build, .NET tests, headless smoke and deterministic soaks have passing evidence.
- [x] bottom context outer bounds align with minimap/clock and adaptive inner controls do not resize the HUD shell.

`VERIFIED` acceptance:

- [ ] licensed Unity Play Mode runner executes the checked-in end-to-end scenarios;
- [ ] Play Mode result XML and logs are retained;
- [ ] representative scene has no Console errors during the acceptance run.

## 14. Открытые вопросы

Внутри Presentation foundation открытых бизнес-решений нет. Открытые observable правила конкретных systems остаются у их owners:

- contextual input/cursors — [#390](https://github.com/bageus/Dig/issues/390);
- world item interactions — [#387](https://github.com/bageus/Dig/issues/387);
- BuildingBox full runtime verification — [#398](https://github.com/bageus/Dig/issues/398);
- resident HUD/skills broader backlog — [#113](https://github.com/bageus/Dig/issues/113), [#117](https://github.com/bageus/Dig/issues/117);
- licensed Unity Test Runner evidence — [#511](https://github.com/bageus/Dig/issues/511).

Эти issues не переопределяют ownership и invariants Presentation foundation.

## 15. Журнал решений

| Дата | Решение | Кто подтвердил | Изменённые разделы/issues |
|---|---|---|---|
| 2026-07-13 | Presentation отображает simulation и отправляет commands, но не владеет gameplay state. | Issue #14 | 1–10 |
| 2026-07-15 | HUD использует typed read models/statuses, event-driven ticker и bounded roster. | Issues #113–#116 | 4, 7, 9, 12 |
| 2026-07-25 | Input priority вынесен из Unity raycast order в deterministic router. | Issues #390/#398 | 5, 7, 8 |
| 2026-07-28 | Unity Play Mode workflow добавлен; без activation credentials licensed test step пропускается и не является evidence. | PR #472 / issue #15 | 12–13 |
| 2026-07-28 | HUD adapter при unavailable sessions возвращает typed fallback projections вместо NullReferenceException. | PR #498 / issue #497 | 2, 6, 11 |
| 2026-07-29 | Umbrella #14 нормализован как Presentation foundation; более узкие gameplay и verification задачи остаются у связанных owners. | Запрос пользователя | Все |
| 2026-07-29 | Repository implementation issue #14 закрывается как `IMPLEMENTED`; remaining licensed EditMode/PlayMode, Console и runtime budget evidence перенесены в approved owner #511 без повышения до `VERIFIED`. | Issue #16 closure path | 12–15 |
