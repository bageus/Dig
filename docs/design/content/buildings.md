# Здания и сервисы поселения

Главные задачи: здания #74, универсальные коробки #118, металлургия #108, технологии #126.

## Общая модель

Все размещаемые здания используют физический `BuildingBox`:

- стоимость находится в production recipe коробки;
- одна коробка представляет одно non-stackable здание;
- placement резервирует конкретную коробку;
- worker доставляет её к ghost plan и выполняет assembly;
- completed building можно упаковать обратно в одну коробку;
- для одного BuildingDefinition запрещено одновременно применять box policy и legacy full-material site policy.

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
```

## Контекстная панель

При выборе здания нижняя панель показывает его capabilities: production, research, storage/service modes, workers/visitors, orders, progress, durability и diagnostics. Справа находится кнопка упаковки, если definition её поддерживает.

## Сводная таблица

| Здание | Источник коробки | Работник | Вместимость | Назначение |
|---|---|---:|---:|---|
| Пост часового | data-driven | 1 | 1 | тревога и запас снаряжения |
| Арсенал | Оружейная кузница | data-driven | 26 slots | хранение снаряжения |
| Обычная игровая комната | Мебельная мастерская | нет | 3 | шахматы/дартс |
| Индустриальная игровая комната | Мебельная мастерская | нет | 4 | бильярд |
| Люксовая игровая комната | Мебельная мастерская | нет | 4 | мини-гольф |
| Кинотеатр | Кристаллическая кузня | 1 | 4 зрителя | Mood/fertility/service |
| Винокурня | Пилорама | order worker | Production | глипнир/огненная вода |
| Университет | Пилорама | 1 | 2 students | capacity 100→200 |
| Бар | data-driven | бармен | data-driven | хранение/подача алкоголя |
| Пивоварня | Лесопилка | order worker | Production | пиво/эль/сидр |
| Костёр | старт | order worker | Production | кухня tier 1 |
| Средневековая кухня | data-driven | order worker | Production | кухня tier 2 |
| Индустриальная кухня | data-driven | order worker | Production | кухня tier 3 |
| Люксовая кухня | data-driven | order worker | Production | кухня tier 4 |
| Горн | Мастерская каменщика | order worker | Production | ранняя плавка железа |
| Литейный цех | data-driven | order worker | Production | железо/золото на угле |
| Песчаник | data-driven | order worker | Production | обработка кристаллической руды |
| Больница | Токарная мастерская (`Dreherei`) | временный взрослый врач | 1 пациент, 1 active | continuous Health treatment, energy class 2 |

## Пост часового

- максимум 4 единицы снаряжения;
- target stock 1, 2 или 4;
- недостающее снаряжение создаёт hauling demand;
- shelf отображает authoritative inventory;
- пост является protected source;
- тревога создаёт защитное намерение у допустимых гномов;
- после тревоги снаряжение остаётся у resident.

Связь: #76.

## Арсенал

- technology и box производятся Оружейной кузницей;
- 20 main slots и 6 defense slots;
- stable slot indices/anchors;
- собирает только раскрытые доступные equipment stacks;
- personal inventories и посты часового не являются источниками;
- ручной pickup разрешён.

Связь: #77.

## Игровые комнаты

Все три коробки производит Мебельная мастерская. Постоянного worker нет. После открытия игровых комнат generic rest ограничивается data-driven Mood cap; специальные activities позволяют превысить его.

Связи: #78, #143.

## Кинотеатр

- 4 spectator places, 1 worker place;
- без worker session не начинается;
- session повышает Mood и может выдавать fertility modifier;
- worker получает Service experience;
- числовая сила/длительность BALANCE_TBD.

Связь: #79.

## Винокурня

- box производится Пилорамой;
- производит глипнир и огненную воду;
- «Грибной самогон» — legacy display name Огненной воды, не отдельный ItemId;
- cycles развивают Alchemy;
- металл в старых стоимостях означает `material.iron`.

Связь: #80.

## Бар

Хранит пиво, эль, сидр, глипнир и огненную воду. Режимы `Light`, `Glipnir`, `Firewater`, `Mixed`. Недоступный без stock режим блокируется с reason. Inventory является владельцем содержимого.

Связь: #81.

## Университет

- box производится Пилорамой;
- 2 student places, 1 active-cycle worker;
- работает в Work schedule;
- повышает тот же TotalSkillCapacity с 100 до 200;
- один pool включает 12 skills;
- не выдаёт скрытый опыт конкретного skill;
- worker получает Service experience.

Связь: #82.

## Кухни

Tier output одного разрешённого recipe:

- Campfire — 2;
- Medieval — 2;
- Industrial — 3;
- Luxury — 3.

Worker lifecycle:

- постоянного повара нет;
- worker назначается только на active cooking order;
- completion/cancel/terminal block освобождают worker;
- пустая кухня resident не удерживает.

`skill.cooking` влияет **только на скорость приготовления**. Он не изменяет ingredients, output quantity, Nutrition, Mood, bites, quality или extra results. Speed curve data-driven.

Полный каталог: `food.md`, #96, #98.

## Больница

Authoritative specification: [`../health-hospital-and-treatment.md`](../health-hospital-and-treatment.md), issue #130.

### Content

```text
BuildingBox recipe:
3 stone + 3 iron + 3 crystal + 1 gold

Research:
Service 7 + Food 2

Construction grants:
Metallurgy 7 + Service 3

Service:
_Heilen — pre-invented
```

### Functional definition

```text
PatientPlaceCount = 1
DoctorPlaceCount = 1
ActiveTreatmentSlots = 1
RequiredEnergyClass = 2
DoctorAssignment = Temporary
MinimumDoctorService = none
MedicalMaterials = none
```

### Admission

- automatic intent при `Health < 80`;
- при `Health < 25` resident немедленно прерывает работу;
- при `25 <= Health < 80` лечение выполняется только в свободное время;
- дети исключены;
- беременные используют обычные правила взрослых;
- любой живой взрослый пациент самостоятельно идёт в больницу.

### Treatment

- очередь: `Health asc -> WaitingSince asc -> ResidentId asc`;
- один игровой час восстанавливает максимум 25 Health;
- Health начисляется постепенно;
- partial Health/progress сохраняются при interruption;
- потеря врача или энергии ставит лечение на паузу;
- этапы автоматически повторяются до Health 100;
- врач получает `skill.service`;
- точный energy consumption, natural regeneration rates и Service grant относятся к Q-014.

## Металлургия и кристаллы

### Горн

- ID `building.furnace`;
- legacy «Плавильня» соответствует Горну;
- `3 ore.iron + 2 mushroom_leg -> 2 material.iron`;
- развивает Metallurgy;
- золото не плавит.

### Литейный цех

- ID `building.foundry`;
- `3 ore.iron + 2 coal -> 2 iron`;
- `3 ore.gold + 2 coal -> 2 gold`;
- развивает Metallurgy.

### Песчаник

- ID `building.crystal_processor`;
- `1 ore.crystal -> 1 material.crystal`;
- развивает Alchemy.

## Технологические исключения

- водолазный колокол исключён;
- Dojo трактуется как направление «Кулачный бой»;
- отдельного ItemId «Грибной самогон» нет;
- Лесопилка и Пилорама — разные technology nodes;
- театр, боулинг, дискотека, тренажёрный зал и бордель остаются future content candidates.

## Placement и packing

- LMB по world/inventory BuildingBox включает placement;
- Alt+LMB по world box назначает pickup;
- valid LMB создаёт ghost plan и резервирует коробку;
- worker доставляет box и собирает здание;
- packing выполняется job и создаёт одну box после commit;
- preview принадлежит Presentation.

## Demand и доставка

Production создаёт demand на recipe inputs. Universal construction site требует одну конкретную box. Source обязан быть раскрыт согласно `material-demand-and-hauling.md` и `exploration-fog-of-war.md`.

## Диагностика

Inspector показывает definition/box IDs, construction policy, technology/research state, source workshop, function/block reason, places, inventories, demands/reservations, recipe/mode, worker, effective speed, assembly/packing stage, durability и quantity-conservation report.
