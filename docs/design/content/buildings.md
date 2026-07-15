# Здания и сервисы поселения

Главная задача расширения зданий и сервисов: #74. Универсальные коробки, placement и packing: #118. Металлургические здания и Песчаник: #108. Полное дерево технологий: #126.

## 1. Общая модель

Здания используют существующие `BuildingDefinition`, Construction, Production, Inventory, Jobs и resident places.

Целевая construction policy для всех размещаемых зданий — физический `BuildingBox`:

- стоимость здания находится в рецепте производства его коробки;
- Production создаёт коробку как обычный Inventory item;
- одна коробка представляет одно здание и не stackable;
- placement резервирует конкретную коробку;
- свободный гном переносит её к ghost plan и выполняет сборку;
- completed building можно разобрать обратно в одну коробку;
- display name не используется как ID.

Полная модель: [`../building-box-placement-and-packing.md`](../building-box-placement-and-packing.md).

Существующая реализация прямой доставки материалов на площадку остаётся фактическим кодом до #118. Для одного `BuildingDefinition` запрещено одновременно применять universal box policy и legacy full-material site policy.

Предмет «металл» унифицирован с `material.iron`.

## 2. Общий content contract

```text
BuildingDefinition
- BuildingDefinitionId
- BuildingBoxItemId
- ConstructionPolicy: UniversalBox | LegacyMaterialSite
- Footprint / WorkPositions
- AssemblyWork / PackingWork
- Durability
- FunctionalCapabilities
- Worker / Visitor Places
- Technology prerequisites
```

Каждая коробка имеет stable `ItemId`, production recipe и definition/version reference. Content validation проверяет все ссылки.

## 3. Нижняя контекстная панель здания

При выборе построенного здания на месте resident inventory/excavation palette отображаются только функции выбранного здания:

- производство и recipes;
- исследования;
- storage filters/modes;
- service modes;
- worker и visitor places;
- active orders и progress;
- durability/repair;
- diagnostics.

Справа находится кнопка упаковки, если definition поддерживает packing. UI отправляет command; он не удаляет building snapshot напрямую.

## 4. Сводная таблица

| Здание | Источник коробки | Работник | Вместимость | Назначение |
|---|---|---:|---:|---|
| Пост часового | data-driven production | 1 | 1 | тревога, запас снаряжения 1/2/4 |
| Арсенал | Оружейная кузница | data-driven | storage | 20 основных предметов + 6 защитных |
| Игровая комната — обычная | Мебельная мастерская | нет | 3 | шахматы и дартс |
| Игровая комната — индустриальная | Мебельная мастерская | нет | 4 | два бильярдных стола |
| Игровая комната — люкс | Мебельная мастерская | нет | 4 | четыре корта мини-гольфа |
| Кинотеатр | Кристаллическая кузня | 1 | 4 зрителя | настроение, fertility, услуги |
| Винокурня | Пилорама | production worker | по Production | глипнир, огненная вода |
| Университет | Пилорама | 1 | 2 студента | TotalSkillCapacity 100 → 200 |
| Бар | data-driven | бармен | data-driven | хранение и подача алкоголя |
| Пивоварня | Лесопилка | production worker | по Production | пиво, эль, сидр |
| Костёр | стартовое производство | Q-018 | по Production | кухня tier 1 |
| Средневековая кухня | data-driven | Q-018 | по Production | кухня tier 2 |
| Индустриальная кухня | data-driven | Q-018 | по Production | кухня tier 3 |
| Люксовая кухня | data-driven | Q-018 | по Production | кухня tier 4 |
| Горн | Мастерская каменщика | data-driven | по Production | ранняя плавка железа |
| Литейный цех | data-driven | data-driven | по Production | продвинутая плавка железа и золота на угле |
| Песчаник | data-driven | data-driven | по Production | обработка кристаллической руды |

Каждая строка в целевой модели получает соответствующий `BuildingBoxItemId`, даже если точный production building или recipe ещё остаются balance/content work.

Подтверждённые связи мастерских не должны заменяться старым расположением из scripts. Полный список хранится в [`../technology-tree.md`](../technology-tree.md).

## 5. Пост часового

Связанная задача: #76.

- максимум 4 единицы снаряжения;
- target stock только 1, 2 или 4;
- недостающее снаряжение создаёт hauling demand;
- полка отображает authoritative building inventory;
- пост является protected source;
- назначенный часовой публикует событие тревоги;
- ближайшие допустимые гномы получают защитное намерение;
- после завершения события снаряжение остаётся у гнома.

## 6. Арсенал

Связанная задача: #77.

- технология изучается в Оружейной кузнице;
- коробка производится в Оружейной кузнице;
- 10 основных slots слева;
- 10 основных slots справа;
- 6 защитных slots по центру;
- stable slot index и visual anchor;
- `CollectVisibleEquipment` собирает раскрытые world stacks;
- личные inventories и посты часового не являются источниками;
- предметы можно забрать вручную.

## 7. Игровые комнаты

Связанная задача: #78.

Все три building kits производятся в **Мебельной мастерской**. Пилорама не производит игровые комнаты.

Все варианты работают без постоянного работника. После открытия игровых комнат обычный отдых ограничивается `MoodWithoutGameRoomCap`; игровая активность позволяет превысить этот data-driven cap.

## 8. Кинотеатр

Связанная задача: #79.

- 4 spectator places и 1 worker place;
- без работника сеанс не начинается;
- завершённый сеанс повышает настроение;
- Society получает временный fertility modifier;
- работник получает `skill.service` experience;
- сила/длительность остаются `BALANCE_TBD`.

## 9. Винокурня

Связанная задача: #80.

- коробка производится в Пилораме;
- производит глипнир и огненную воду;
- «Грибной самогон» является legacy display name Огненной воды, не отдельным ItemId;
- успешный цикл развивает `skill.alchemy`;
- «металл» в стоимости означает `material.iron`.

## 10. Бар

Связанная задача: #81.

Бар хранит пиво, эль, сидр, глипнир и огненную воду. Режимы:

- `Light`;
- `Glipnir`;
- `Firewater`;
- `Mixed`.

Кнопка режима недоступна без подходящего stock. Inventory владеет содержимым; визуальные бутылки являются rebuildable view.

## 11. Университет

Связанная задача: #82.

- коробка производится в Пилораме;
- 2 student places и 1 worker place;
- работает в рабочие часы;
- базовый `TotalSkillCapacity = 100`;
- обучение увеличивает тот же capacity до `200`;
- один pool включает все 12 навыков;
- университет не выдаёт скрытый опыт конкретного навыка;
- работник получает `skill.service` experience.

## 12. Кухни

Связанная задача: #98.

Tier output:

- Campfire — 2;
- Medieval — 2;
- Industrial — 3;
- Luxury — 3.

Костёр готовит только гриль-гриб; более высокие tiers расширяют каталог согласно `food.md`. Работники и влияние Cooking остаются Q-018.

## 13. Металлургия и кристаллы

### Горн

- старое название узла `Schmelze`/«Плавильня» соответствует Горну;
- ID `building.furnace`;
- `3 ore.iron + 2 material.mushroom_leg -> 2 material.iron`;
- развивает `skill.metallurgy`;
- не плавит золото.

### Литейный цех

- ID `building.foundry`;
- `3 ore.iron + 2 coal -> 2 material.iron`;
- `3 ore.gold + 2 coal -> 2 material.gold`;
- является продвинутой плавкой на угле;
- развивает `skill.metallurgy`.

### Песчаник

- display name **«Песчаник»**;
- ID `building.crystal_processor`;
- `1 ore.crystal -> 1 material.crystal`;
- обрабатывает кристаллическую руду;
- развивает `skill.alchemy`.

## 14. Технологические исключения и переименования

- водолазный колокол не входит в дерево Dig;
- старый узел/название `Dojo` трактуется как направление **Кулачный бой**;
- отдельный ItemId «Грибной самогон» не создаётся;
- Лесопилка и Пилорама являются двумя разными узлами дерева;
- Театр, Боулинг, Дискотека, Тренажёрный зал и Бордель пока остаются будущими content candidates с `TBD_OWNER` параметрами.

## 15. Placement и packing

- обычный ЛКМ по world box включает placement mode;
- `Alt + ЛКМ` по world box назначает pickup выбранному гному;
- ЛКМ по box в resident inventory включает placement mode;
- valid ЛКМ создаёт ghost plan и резервирует коробку;
- свободный гном доставляет коробку и собирает здание;
- кнопка packing создаёт общую работу разборки;
- после commit создаётся одна коробка;
- preview и panel state принадлежат Presentation.

## 16. Demand и доставка

Production создаёт demand на recipe inputs. Для universal box construction площадка требует одну конкретную коробку, а не повторно полный список recipe materials. Source обязан быть раскрыт согласно `material-demand-and-hauling.md`.

## 17. Диагностика

Inspector здания показывает:

- definition и box IDs;
- construction policy;
- technology state и research reason;
- source workshop;
- functional state и block reason;
- worker/visitor places;
- input/output inventories;
- demands/reservations;
- active recipe/mode;
- assembly/packing stage;
- progress и durability;
- последнюю cancellation/error;
- quantity conservation report для box lifecycle.
