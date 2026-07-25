# Каталог систем Dig

## 1. Назначение

Этот файл показывает верхнеуровневую карту систем и не должен дублировать подробные спецификации.

Обязательный индекс заголовков, aliases, статусов, tracking issues и ссылок на авторитетные документы:

- [`../systems/README.md`](../systems/README.md).

Любой поиск требований начинается с этого индекса.

## 2. Основные документы

- [`../systems/README.md`](../systems/README.md) — единый индекс всех систем и точка входа для feature/bug work.
- [`overview.md`](overview.md) — верхнеуровневая архитектура и направления зависимостей.
- [`module-structure.md`](module-structure.md) — низкоуровневая структура исходного кода и публичных контрактов.
- [`systems-core.md`](systems-core.md) — runtime, мир, навигация, жители, работы, резервирования, предметы и логистика.
- [`systems-gameplay.md`](systems-gameplay.md) — строительство, производство, прогрессия, общество, бой, сохранения, presentation и качество.
- [`../development-rules.md`](../development-rules.md) — обязательные правила разработки.
- [`../development/system-specification-workflow.md`](../development/system-specification-workflow.md) — сверка запроса, документации, issues, кода и полного workflow.
- [`../development/system-specification-template.md`](../development/system-specification-template.md) — шаблон новой системы и опросника.
- [`../development/chatgpt-project-instructions.md`](../development/chatgpt-project-instructions.md) — готовая Project Instructions для ChatGPT.
- [`../ROADMAP.md`](../ROADMAP.md) — этапы реализации и критерии завершения.

## 3. Карта систем

```text
Runtime Foundation
├── Simulation Loop
├── Entity Identity
└── Commands / Events / Queries

World Simulation
├── Cell World
├── Chunk Management
├── Excavation / Depth / Rooms
├── Terrain / Deposits / Outputs
├── Exploration
└── Procedural Generation

Navigation
├── Traversability
├── Region Graph
├── Pathfinding
├── Resident Occupancy
└── Vertical Traversal

Agent Simulation
├── Agent State
├── Needs
├── Schedule
├── Utility Decisions
├── Player Overrides
└── Action Execution

Work Management
├── Work Orders
├── Job Lifecycle
├── Reservation System
├── Job Matching
└── Multi-cell Continuation

Colony Economy
├── Item Catalog
├── Inventory
├── World Item Gravity / Pickup
├── Storage
├── Hauling
├── BuildingBox Lifecycle
├── Construction
├── Production
└── Energy

Progression and Society
├── Skills
├── Technology / Research
├── Relationships
├── Family and Reproduction
├── Lifecycle
├── Health
└── Leisure

Conflict and Ecology
├── Creatures
├── Threat Detection
├── Combat
├── Factions and Diplomacy
└── Strategic AI

Runtime Platform
├── Save / Load / Migration
├── Content Validation
├── Context Input / Selection / Cursors
├── Presentation
├── Diagnostics
├── Testing
├── Performance
└── Demo Bootstrap
```

## 4. Приоритеты

```text
P0: runtime, world, chunks, navigation, agents, jobs, reservations
P1: inventory, hauling, construction, needs, save/load, diagnostics
P2: production, technology, generation, society, combat
P3: factions, strategic AI, advanced environment, content tools
```

## 5. Правило детализации

Каждый системный заголовок регистрируется в `docs/systems/README.md` и имеет ровно один authoritative specification. Файл спецификации может находиться в `docs/design/`, `docs/architecture/` или другом явно указанном разделе; копирование одного контракта в несколько файлов запрещено.

Спецификация должна содержать:

- статус `DRAFT`, `QUESTIONNAIRE`, `APPROVED`, `IMPLEMENTED` или `VERIFIED`;
- tracking issue и обратную ссылку issue → document;
- полный пользовательский workflow;
- владельца состояния;
- модель данных;
- команды, события и запросы;
- состояния и переходы;
- input/UI/presentation contract;
- зависимости и приоритеты конфликтов;
- инварианты;
- отмену, ошибки, retries и recovery;
- сохранение и migration;
- диагностику;
- unit/integration/deterministic/Play Mode сценарии;
- открытые вопросы, если система неполна.

Если система отсутствует или её observable behavior неполон, создаются draft-файл и issue-опросник до реализации спорной логики.
