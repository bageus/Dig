# Здания и сервисы поселения

Главные задачи: #74 — здания/сервисы, #118 — universal BuildingBox, #108 — металлургия/Песчаник, #126 — дерево технологий.

## 1. Общая модель

Здания используют `BuildingDefinition`, Construction, Production, Inventory, Jobs и resident places.

Для всех размещаемых зданий целевая policy — физический `BuildingBox`:

- стоимость находится в recipe коробки;
- Production создаёт один non-stackable item;
- placement резервирует конкретную коробку;
- resident доставляет её к ghost plan и собирает building;
- packing возвращает одну коробку;
- display name не является ID.

До #118 существующий legacy material-site path остаётся фактическим кодом. Один BuildingDefinition не может одновременно использовать обе construction policies.

Полная модель: [`../building-box-placement-and-packing.md`](../building-box-placement-and-packing.md).

## 2. BuildingDefinition

```text
BuildingDefinition
- BuildingDefinitionId
- BuildingBoxItemId
- ConstructionPolicy
- Footprint / WorkPositions
- AssemblyWork / PackingWork
- Durability
- FunctionalCapabilities
- Worker / Visitor Places
- Technology prerequisites
- SourceWorkshopId
```

Content validation проверяет Building ↔ Box ↔ Recipe ↔ Workshop ↔ Technology references.

## 3. Контекстная панель

При выборе completed building нижняя панель показывает:

- production recipes/orders;
- research;
- storage filters/modes;
- service modes;
- worker/visitor places;
- progress;
- durability/repair;
- diagnostics;
- кнопку packing справа, если функция разрешена.

UI отправляет Application commands и не изменяет building snapshot напрямую.

## 4. Сводная таблица

| Здание | Источник коробки | Worker policy | Вместимость/назначение |
|---|---|---|---|
| Пост часового | data-driven | 1 assigned guard | тревога, stock 1/2/4 |
| Арсенал | Оружейная кузница | по definition | 20 основных + 6 защитных slots |
| Игровая комната — обычная | Мебельная мастерская | нет постоянного | 3 посетителя |
| Игровая комната — индустриальная | Мебельная мастерская | нет постоянного | 4 посетителя |
| Игровая комната — люкс | Мебельная мастерская | нет постоянного | 4 посетителя |
| Кинотеатр | Кристаллическая кузня | 1 на сеанс | 4 зрителя |
| Винокурня | Пилорама | worker только на order | глипнир/огненная вода |
| Университет | Пилорама | 1 преподаватель при cycle | 2 студента |
| Бар | data-driven | бармен для service order | drinks/service |
| Пивоварня | Лесопилка | worker только на order | пиво/эль/сидр |
| Костёр | стартовое производство | worker только на cooking order | kitchen tier 1 |
| Средневековая кухня | data-driven | worker только на cooking order | tier 2 |
| Индустриальная кухня | data-driven | worker только на cooking order | tier 3 |
| Люксовая кухня | data-driven | worker только на cooking order | tier 4 |
| Горн | Мастерская каменщика | worker только на production order | раннее железо |
| Литейный цех | data-driven | worker только на production order | железо/золото на угле |
| Песчаник | data-driven | worker только на production order | кристаллическая руда |

Подтверждённые источники мастерских не заменяются расположением legacy scripts. Authoritative tree: [`../technology-tree.md`](../technology-tree.md).

## 5. Пост часового

Связанная задача: #76.

- stock target: 1, 2 или 4;
- недостающее снаряжение создаёт demand;
- visual shelf читает building inventory;
- post является protected source;
- guard публикует alarm event;
- после события снаряжение остаётся у resident.

## 6. Арсенал

Связанная задача: #77.

- technology и box находятся в Оружейной кузнице;
- 10 main slots слева;
- 10 main slots справа;
- 6 defensive slots по центру;
- stable visual anchors;
- `CollectVisibleEquipment` использует раскрытые world stacks;
- personal inventories и guard posts не являются автоматическими sources;
- ручное изъятие использует Inventory transfer.

## 7. Игровые комнаты

Связанная задача: #78.

- все три boxes производит Мебельная мастерская;
- Пилорама игровые комнаты не производит;
- постоянный worker не нужен;
- места резервируются посетителями;
- после открытия technology обычный отдых ограничивается `MoodWithoutGameRoomCap`;
- успешная game activity позволяет превысить cap;
- Mood восстанавливается постепенно согласно [`../needs-continuous-actions.md`](../needs-continuous-actions.md).

## 8. Кинотеатр

Связанная задача: #79.

- 4 spectator places;
- 1 worker place;
- сеанс не начинается без worker и зрителя;
- successful session повышает Mood;
- Society получает temporary fertility modifier;
- worker получает `skill.service`;
- balance values data-driven.

## 9. Винокурня

Связанная задача: #80.

- box производится Пилорамой;
- recipes: глипнир и огненная вода;
- «Грибной самогон» — legacy name огненной воды, не отдельный ItemId;
- successful production развивает `skill.alchemy`;
- `металл` в старых данных означает `material.iron`.

## 10. Бар

Связанная задача: #81.

Бар хранит пиво, эль, сидр, глипнир и огненную воду. Serving modes:

- `Light`;
- `Glipnir`;
- `Firewater`;
- `Mixed`.

Кнопка недоступна без подходящего authoritative stock. Visual bottles являются rebuildable view.

## 11. Университет

Связанная задача: #82.

- box производится Пилорамой;
- 2 student places;
- 1 worker place при active study cycle;
- работает в Work schedule;
- TotalSkillCapacity повышается 100 → 200;
- один pool содержит 12 skills;
- university не выдаёт скрытый experience конкретного skill;
- worker получает `skill.service`.

## 12. Кухни

Связанная задача: #98.

Tier output:

- Campfire — 2;
- Medieval — 2;
- Industrial — 3;
- Luxury — 3.

Костёр готовит только гриль-гриб из утверждённого набора. Более высокие tiers открывают recipes согласно [`food.md`](food.md).

### Worker lifecycle

Постоянный повар не закрепляется.

- worker нужен только при наличии cooking order;
- Production/Jobs claim допустимого resident;
- no-order kitchen не удерживает worker;
- completion/cancel/terminal block освобождает worker reservation;
- active worker и progress показываются в building panel;
- влияние уровня `skill.cooking` на speed/output/effect остаётся Q-039.

## 13. Металлургия и кристаллы

### Горн

- legacy `Schmelze/Плавильня` = Горн;
- ID `building.furnace`;
- `3 ore.iron + 2 material.mushroom_leg -> 2 material.iron`;
- skill `metallurgy`;
- золото не плавит.

### Литейный цех

- ID `building.foundry`;
- `3 ore.iron + 2 coal -> 2 material.iron`;
- `3 ore.gold + 2 coal -> 2 material.gold`;
- advanced coal smelting;
- skill `metallurgy`.

### Песчаник

- display name «Песчаник»;
- ID `building.crystal_processor`;
- `1 ore.crystal -> 1 material.crystal`;
- skill `alchemy`.

## 14. Технологические исключения

- водолазный колокол исключён;
- `Dojo` трактуется как направление «Кулачный бой»;
- отдельный ItemId «Грибной самогон» не создаётся;
- Лесопилка и Пилорама — разные узлы;
- Театр, Боулинг, Дискотека, Тренажёрный зал и Бордель остаются `TBD_OWNER` candidates.

## 15. Placement и packing

- LMB world/inventory box → placement mode;
- `Alt+LMB` world box → pickup order;
- valid placement создаёт ghost plan и резервирует коробку;
- free resident доставляет коробку и собирает building;
- packing создаёт disassembly job;
- после commit появляется ровно одна box;
- preview/panel принадлежат Presentation.

## 16. Demand и hauling

Production создаёт demand на recipe inputs. Universal box construction требует конкретную коробку, а не повторно полный список materials. Source обязан быть revealed согласно `material-demand-and-hauling.md`.

## 17. Диагностика

Inspector здания показывает:

- definition/box/source workshop IDs;
- construction policy;
- technology/research state;
- functional/block state;
- worker/visitor places;
- input/output inventories;
- demands/reservations;
- active recipe/mode;
- assembly/packing stage;
- progress/durability;
- cancellation/error;
- quantity conservation report.
