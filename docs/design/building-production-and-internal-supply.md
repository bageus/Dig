# Производство в зданиях и внутреннее снабжение

Статус: `IMPLEMENTED`; `VERIFIED` требует фактического licensed Unity EditMode/PlayMode evidence.

Tracking issue: [#433](https://github.com/bageus/Dig/issues/433).

Связанные документы:

- [`campfire-cooking-and-food-use.md`](campfire-cooking-and-food-use.md);
- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md);
- [`resident-inventory-expansion.md`](resident-inventory-expansion.md).

## 1. Назначение

Completed workstation производит предметы и BuildingBox через data-driven recipes, автоматически пополняет защищённый внутренний запас и показывает spatial workflow в мире. Новые здания, stock rules и recipes добавляются content definitions, а не building-specific runtime branches.

## 2. Владение состоянием

- `ProductionContentCatalog` владеет immutable recipes и workstation definitions.
- `ProductionState` владеет очередями, active order, material-step progress и consumed-input ledger.
- `InventoryState` владеет физическими item entities, quantity, reservations, `ItemLocation.InBuilding` и world outputs.
- `BuildingSupplyState` владеет delivery toggles, incoming quantities и active supply request.
- `JobSystem` владеет production/supply lifecycle, worker claims и reservations.
- `BuildingsState` владеет footprint, orientation и work position.
- `Agents` владеет authoritative resident position и skills.
- `Presentation` только проецирует icons, counters, zones, items, hover и post-work pose.

## 3. Основной content

Campfire использует stable IDs `building.campfire`, `building_box.campfire`, `food.grilled_mushroom` и `food.roasted_hamster`. Внутренний запас содержит mushroom cap, mushroom leg, stone и hamster по data-driven capacity/toggle rules. Каждая произведённая единица является отдельной quantity-one item entity. Если recipe output quantity равен двум, Finalize создаёт две независимые world entities в двух разных finished-output cells; production output никогда не объединяется в stack quantity больше одного.

## 4. UI и очередь

- LMB по product icon добавляет один order.
- RMB по тому же icon отменяет один order; при нуле это consumed no-op.
- Отдельная minus-кнопка запрещена.
- Counter равен числу non-terminal orders и уменьшается после `Completed`/`Cancelled`.
- Shortage tint не блокирует enqueue.
- Internal-stock icon показывает current/incoming/capacity и delivery toggle.

### 4.1 Progress overlay в product cell

- Каждый product icon владеет progress overlay только для active order этой recipe.
- Overlay не содержит процентов или текста: вся площадь product cell заполняется визуальным слоем снизу вверх, позволяя приблизительно оценить текущий прогресс.
- Overlay показывает только фактический production work. `Queued`, ожидание supply, добыча/harvest dependency и `InputsReserved` не отображают progress.
- Для `InProgress` fill равен отношению выполненного production work к total resolved work текущего order.
- После перехода в `ReadyToComplete` overlay остаётся полностью заполненным во время движения worker к finished-output zone.
- Overlay исчезает только в одном presentation refresh с успешным output commit, terminal `Completed` order/job и уменьшением counter на один.
- Cancel/Failed немедленно убирают overlay. Повторный order начинает новый overlay с нуля только после фактического старта work.

## 5. Пространственные зоны

Каждое completed workstation показывает две зоны в world-X порядке независимо от orientation:

1. слева — внутренний input storage;
2. справа — finished output.

Обе зоны derived from footprint и не сохраняются как entities. Обе визуально являются только плоским tray/base. Rear rail, спинка или задняя стенка запрещены.

### 5.1 Внутренний склад

- Физическое состояние остаётся `ItemLocation.InBuilding(buildingId)`.
- Presentation показывает доступные единицы слева.
- Hit colliders являются triggers и не блокируют Navigation.
- Выбранный resident может забрать одну available/unreserved единицу.
- `ItemLocation.InBuilding` никогда не является источником automatic delivery/building supply.
- После ручного pickup delivery может создать replacement demand.

### 5.2 Готовая продукция

- Готовая продукция существует только как authoritative quantity-one `ItemStackSnapshot` в `ItemLocation.InWorld(outputCell)`.
- Recipe/queue placeholder в world tray запрещён: если authoritative stack отсутствует, предмет не рисуется.
- Output cell должен быть explored, open, supported, вне footprint и без другого world item.
- Candidate order идёт только вправо: `right edge + 1`, затем `+2` и далее; side/rear fallback запрещён.
- Finalize резервирует достаточное количество разных cells для всех output units. При рецепте с quantity `N` commit требует `N` свободных cells и создаёт `N` quantity-one entities.
- При недостаточном количестве свободных cells order остаётся `ReadyToComplete` и безопасно retry без partial output, duplicate input или повторного skill grant.
- Finished output использует обычный world-item selection/pickup/hauling workflow.
- `WorldItemViewModel.IsInteractive` является authoritative для interaction collider. Art/profile collider metadata не может отключить pickup-capable entity.

## 6. Supply lifecycle

Demand создаётся только для completed workstation с enabled delivery, недостающей capacity и без active `InProgress`/`ReadyToComplete` order. Planner сначала читает revealed, reachable, unreserved world stacks. Worker проходит `workstation check -> reserved sources -> workstation deposit`. Cancel/failure/retry освобождает source quantity, incoming capacity и claims атомарно.

Если нужного world material ещё нет, но существует revealed/reachable extraction или harvest target, один planning pass создаёт:

1. authoritative extraction/harvest job;
2. authoritative dependent BuildingSupply job с requested item/quantity и dependency на extraction job.

Dependent supply job существует сразу, но остаётся `Created` и не получает worker/source reservations, пока dependency не завершена. После появления world output обычный BuildingSupply planner в том же job id детерминированно привязывает revealed/reachable/unreserved source, резервирует resident slots и incoming capacity, затем делает job доступным. Отмена/провал dependency завершает dependent supply без phantom incoming reservation. Повторная synchronization не создаёт duplicate dependency/supply pair.

## 7. Production lifecycle

1. Order может ожидать inputs в queue.
2. Полный input set резервируется во внутреннем stock.
3. Один eligible resident получает `ProductionWorkJob`.
4. Work выполняется только на supported side work position.
5. Material steps consume reserved inputs exactly once.
6. Тот же resident остаётся owner через `Finalize`.
7. Finalize разрешает правую output cell и делает её movement target.
8. После достижения первой resolved output cell один Application transaction создаёт все quantity-one world outputs в заранее разрешённых разных cells, переводит order и job в terminal `Completed`, выдаёт skill grants и освобождает reservations.
9. Наблюдаемое состояние `visible/committed output + non-terminal production job` запрещено.
10. Counter уменьшается, resident получает небольшой presentation-only offset и ждёт лицом к камере до следующего authoritative action.

## 8. Повтор, cancel, blocked и concurrency

- Repeat order занимает следующие свободные правые candidate cells по количеству output units.
- Cancel освобождает неиспользованные reservations; уже consumed steps не восстанавливаются.
- Blocked output не создаёт presentation-only продукт и не завершает job частично.
- Каждый building владеет независимой queue/stock/active order.
- Два buildings не могут commit output в одну cell; occupancy перепроверяется в Finalize.

## 9. Save/load и diagnostics

Save включает workstation registration, delivery toggles/incoming, queue/status, material progress, consumed ledger, active job references и supply allocations. World outputs сохраняются обычным `ItemLocation`. Derived zone geometry и wait pose не сохраняются. Load не повторяет committed output/skill grants.

Diagnostics показывают building/recipe/order/job IDs, stock current/incoming/capacity, material progress, assigned worker, output candidates/chosen cell, block reason и terminal completion result.

## 10. Acceptance

Domain/Application:

- protected internal stock не выбирается automatic supply;
- direct pickup забирает одну available unit;
- right candidates deterministic и без fallback;
- world output commit и terminal order/job происходят атомарно;
- quantity `N` создаёт `N` отдельных quantity-one world entities в `N` cells;
- product-cell overlay отражает только active work, остаётся full в Finalize и исчезает вместе с terminal commit/counter decrement;
- extraction dependency и dependent delivery job создаются в одном planning pass, а delivery source binding происходит exactly once после dependency completion;
- duplicate completion не создаёт второй output;
- save/load сохраняет exactly-once semantics.

Unity Play Mode:

- слева и справа видны плоские trays без rear rail;
- internal units видны/clickable и не блокируют navigation;
- worker производит, идёт вправо и commit-ит настоящий world item;
- product icon заполняется progress overlay без текста во время work, остаётся full до placement и очищается при commit/cancel;
- recipe quantity two показывает две отдельные pickup-capable world entities;
- output имеет enabled interaction collider и поднимается обычным pickup workflow;
- после output job/order terminal, counter уменьшается, worker ждёт лицом к камере;
- repeat/blocked/save-load не создают visual-only или duplicate products.

## 11. Журнал решений

| Дата | Решение | Подтвердил |
|---|---|---|
| 2026-07-27 | Generic production, protected internal stock, progressive consumption и deferred replenishment. | User |
| 2026-07-29 | Left internal zone, right output zone, same-worker Finalize, pickup-capable output и camera-facing wait. | User |
| 2026-07-29 | Work position находится сбоку на той же supported Y/Z plane, не над building. | User |
| 2026-07-30 | Оба tray плоские без спинки; output рисуется только из authoritative world stack; interaction collider следует authoritative read model; output и terminal job/order commit атомарны. | User |
| 2026-07-31 | Product-cell progress overlay показывает только work и исчезает при output commit/cancel; каждая output unit создаётся отдельной entity; extraction и dependent delivery job создаются вместе. | User |
