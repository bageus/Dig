# Каталог игрового контента Dig

## Назначение

Каталог хранит согласованные design-правила зданий, продукции, оружия, материалов, еды, алкоголя и навыков. Он описывает **что должно происходить в игре**; runtime-реализация остаётся в Domain/Application системах.

Главные feature-задачи:

- здания и сервисы — #74;
- питание — #96;
- навыки — #103;
- копание и ресурсы — #87.

## Файлы

- [`buildings.md`](buildings.md) — здания, сервисы, кухни, горн, литейный цех и Песчаник;
- [`products.md`](products.md) — физические outputs, блюда, алкоголь и переработка руд;
- [`weapons-and-shields.md`](weapons-and-shields.md) — оружие, щиты и специализированное хранение;
- [`materials.md`](materials.md) — материалы, руды, грунт и deposits;
- [`food.md`](food.md) — блюда, укусы и разнообразие рациона;
- [`alcohol.md`](alcohol.md) — напитки, бар и эффекты;
- [`skills.md`](skills.md) — skill IDs и источники опыта;
- [`../skills-and-progression.md`](../skills-and-progression.md) — полная система развития;
- [`../world-3d-depth.md`](../world-3d-depth.md) — authoritative `X,Y,Z`, глубина `0..3`;
- [`../excavation-room-templates-and-deposits.md`](../excavation-room-templates-and-deposits.md) — свободная копка, шаблоны и жилы;
- [`../terrain-resource-output-and-processing.md`](../terrain-resource-output-and-processing.md) — terrain loot tables, руды и переработка;
- [`../material-demand-and-hauling.md`](../material-demand-and-hauling.md) — спрос, фильтры складов и fog-aware hauling;
- [`../open-questions.md`](../open-questions.md) — вопросы, ответы и журнал решений.

## Правила ведения

1. Display name не используется как ссылка; определения получают stable IDs.
2. Явно заданные числа считаются исходным балансом.
3. Непредоставленные числа остаются data-driven `TBD`.
4. World владеет terrain/deposit state и координатами `X,Y,Z`.
5. Inventory владеет предметами, quantities и locations.
6. Buildings владеет runtime-состоянием здания и functional places, но не копирует предметы.
7. Production владеет orders/progress; RecipeDefinition — inputs/outputs.
8. Agents/Skills владеет значениями навыков и capacity.
9. Jobs/Hauling владеет работами и reservations, но не создаёт отдельные предметы.
10. Presentation отображает snapshots и не является источником terrain, предметов, еды, оружия или навыков.
11. `MaterialId`, `DepositDefinitionId`, `ItemId`, `BuildingDefinitionId` и `AgentSkillId` являются разными typed ссылками.
12. Изменение stable ID требует content migration.
13. Конфликты и двойные смыслы фиксируются как `Q-XXX`.
14. Строительные параметры существующих зданий не копируются в новые документы: authoritative источником остаются их `BuildingDefinition` и связанные content data.

## Принятые решения

- мир — полноценная сетка `X,Y,Z`, глубина `Z=0..3`;
- `Камень` — `skill.stonework`;
- доступ к шаблонам пещер динамический по текущему максимальному Stonework;
- падение Stonework блокирует только новые placements, а уже подтверждённый plan продолжает выполняться;
- трапеция пещеры одинаково проецируется на каждый Z-слой и не зеркалируется;
- пещеры проходные, входы всегда находятся слева и справа по оси X;
- жила заменяет клетку породы; depletion открывает пространство;
- соседние жилы не обязаны быть одной runtime cluster-сущностью;
- `ore.iron -> material.iron`;
- `ore.gold -> material.gold`;
- `ore.crystal -> material.crystal`;
- «металл» и «железо» — один предмет `material.iron`;
- официальное имя `terrain.metal_bearing_rock` — «Рудная порода»;
- официальное имя `building.crystal_processor` — «Песчаник»;
- обычная добыча использует terrain-specific random output tables;
- hauling создаётся только по demand/filter и для раскрытого source.

## Открытые решения

Актуальный реестр: [`../open-questions.md`](../open-questions.md).

Основные оставшиеся вопросы:

- Q-015–Q-018 — шкала сытости, разнообразие, interruption и работники кухни;
- Q-019–Q-022 — mixed skills, combat grants, timing и capacity;
- Q-014 — числовой баланс terrain drops и других систем.

Q-023–Q-027 закрыты и сохранены в журнале решений.

## Связанные issues

### Здания и сервисы

- #75 — building kits;
- #76 — пост часового;
- #77 — арсенал;
- #78 — игровые комнаты;
- #79 — кинотеатр;
- #80 — винокурня;
- #81 — бар;
- #82 — университет;
- #108 — горн, литейный цех и Песчаник.

### Еда

- #97 — укусы/interruption;
- #98 — блюда и кухни;
- #99 — разнообразие;
- #100 — UI/анимации;
- #101 — Save/Load.

### Навыки

- #104 — каталог/grants/capacity;
- #105 — опыт работ/производства;
- #106 — боевые навыки;
- #107 — UI/Save.

### Копание и ресурсы

- #88 — 3D coordinates;
- #89 — templates/Stonework;
- #90 — plans/provenance;
- #91 — deposits/generation;
- #92 — outputs/hauling;
- #93 — Presentation;
- #94 — Save/Load;
- #109 — terrain output profiles;
- #110 — demand/fog-aware hauling.