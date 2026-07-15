# Спрос на материалы, доставка и туман войны

## 1. Назначение

Документ фиксирует, когда лежащий в мире материал становится источником hauling job. Сам факт существования world stack не означает автоматический сбор.

## 2. Источники спроса

Hauling job может быть создан только при наличии одного из двух источников спроса.

### 2.1 Спрос здания или работы

Здание/production/construction job требует конкретный `ItemId` и количество.

Примеры:

- производственный рецепт ожидает руду или топливо;
- строительство ожидает материал;
- бар ожидает разрешённый напиток;
- пост часового ожидает оружие до target stock;
- другая building inventory имеет явную входную reservation.

### 2.2 Сбор на склад

Склад имеет включённый collection/filter rule для конкретного `ItemId` или категории. Без разрешённого фильтра склад не создаёт общий запрос на этот материал.

## 3. Условие тумана войны

При планировании/выдаче hauling job source stack должен:

- находиться в раскрытой игроком клетке;
- не быть скрыт туманом войны в момент создания задания;
- иметь доступное незарезервированное количество;
- иметь достижимую pickup position;
- соответствовать ItemId/category спроса.

Проверка видимости выполняется при создании задания, как явно задано design. После claim задание не отменяется только из-за последующего визуального скрытия, если source и путь остаются валидными. Исчезновение source, потеря reservation или физическая недоступность используют обычный blocked/retry flow.

## 4. Приоритеты и поиск источника

Planner:

1. читает активные demand records;
2. исключает закрытые туманом, зарезервированные и недоступные stacks;
3. выбирает source детерминированно по priority, path cost и stable ID;
4. резервирует quantity и destination capacity атомарно;
5. создаёт hauling job;
6. назначает свободного гнома через общий JobSystem.

Нельзя создавать отдельный «магический» перенос непосредственно из World в здание.

## 5. Добыча и hauling

После mining/excavation:

- добытые items падают на землю как authoritative world stacks;
- шахтёр не кладёт их автоматически в личный inventory;
- новый stack не создаёт hauling job без спроса;
- если здание уже требует ItemId и source раскрыт, planner может создать delivery job;
- если склад разрешает этот ItemId и source раскрыт, planner может создать storage hauling job;
- один stack может обслужить несколько demands только через независимые quantity reservations без превышения quantity.

## 6. Здания и склады

### 6.1 Building demand

Demand record содержит:

- owning building/job/order id;
- required ItemId/category;
- requested, reserved, delivered quantity;
- destination inventory/place;
- priority;
- срок/состояние актуальности;
- разрешённость альтернативных ItemId, если рецепт это допускает.

Если order отменён, его неудовлетворённые demands и reservations освобождаются.

### 6.2 Storage filter

Storage policy содержит:

- разрешённые ItemId/categories;
- приоритет;
- target/max capacity;
- paused/active state;
- исключения protected sources.

Посты часового, личные inventories и другие protected inventories не становятся source для обычного складского сбора.

## 7. Fog-of-war ownership

- World/Exploration владеет explored/revealed state;
- Visibility/Presentation отображает fog, но не принимает решение о hauling eligibility;
- planner получает immutable visibility query;
- Unity renderer не может сделать item доступным, просто показав его модель;
- debug visual не раскрывает source для симуляции.

## 8. Reservations и отказоустойчивость

Атомарно резервируются:

- source quantity;
- destination capacity;
- destination demand quantity;
- worker и pickup/dropoff positions при claim.

При block/cancel/failure все reservations освобождаются. Retry заново проверяет source, demand, visibility и destination capacity.

## 9. Диагностика

Inspector/report показывает:

- почему stack eligible/ineligible;
- demand source;
- visibility at planning tick;
- source quantity/reservations;
- destination capacity/reservations;
- selected worker/path;
- причины `hidden_by_fog`, `no_active_demand`, `storage_filter_disabled`, `source_reserved`, `destination_full`, `path_unavailable`.

## 10. Тесты

Обязательные сценарии:

- добытый раскрытый материал требуется зданию;
- тот же материал не требуется никому и остаётся на земле;
- склад с включённым/выключенным фильтром;
- source скрыт туманом при planning;
- source становится визуально скрыт после claim;
- demand отменяется до pickup и после pickup;
- несколько demands конкурируют за один stack;
- protected source не используется;
- save/load active demand, hauling job и reservations;
- deterministic selection одинакового source.

## 11. Связанные issues

- #7/#14 — Inventory и hauling foundation;
- #68 — personal inventory/hauling integration;
- #76/#77 — protected weapon storage;
- #84 — Unity hauling/storage cycle;
- #92 — mining outputs;
- #110 — demand/fog-aware planner.
