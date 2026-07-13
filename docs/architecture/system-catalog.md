# Каталог систем Dig

## 1. Назначение

Этот файл является индексом системной документации и не должен дублировать подробные спецификации.

## 2. Основные документы

- [`overview.md`](overview.md) — верхнеуровневая архитектура и направления зависимостей.
- [`module-structure.md`](module-structure.md) — низкоуровневая структура исходного кода и публичных контрактов.
- [`systems-core.md`](systems-core.md) — runtime, мир, навигация, жители, работы, резервирования, предметы и логистика.
- [`systems-gameplay.md`](systems-gameplay.md) — строительство, производство, прогрессия, общество, бой, сохранения, presentation и качество.
- [`../development-rules.md`](../development-rules.md) — обязательные правила разработки.
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
├── Environment
└── Procedural Generation

Navigation
├── Traversability
├── Region Graph
└── Pathfinding

Agent Simulation
├── Agent State
├── Needs
├── Schedule
├── Utility Decisions
└── Action Execution

Work Management
├── Work Orders
├── Job Lifecycle
├── Reservation System
└── Job Matching

Colony Economy
├── Item Catalog
├── Inventory
├── Storage
├── Hauling
├── Building Placement
├── Construction
├── Production
└── Energy

Progression and Society
├── Skills
├── Technology
├── Relationships
├── Family and Reproduction
├── Lifecycle
└── Leisure

Conflict
├── Threat Detection
├── Combat
├── Factions and Diplomacy
└── Strategic AI

Runtime Platform
├── Save / Load / Migration
├── Content Validation
├── Presentation
├── Diagnostics
├── Testing
└── Performance
```

## 4. Приоритеты

```text
P0: runtime, world, chunks, navigation, agents, jobs, reservations
P1: inventory, hauling, construction, needs, save/load, diagnostics
P2: production, technology, generation, society, combat
P3: factions, strategic AI, advanced environment, content tools
```

## 5. Правило детализации

Каждая система получает отдельную техническую спецификацию в `docs/systems/`, когда начинается её реализация.

Спецификация должна содержать:

- владельца состояния;
- модель данных;
- команды;
- события;
- запросы;
- состояния и переходы;
- зависимости;
- инварианты;
- сохранение;
- диагностику;
- тестовые сценарии;
- критерии производительности.

Один и тот же контракт не описывается в нескольких местах. Этот индекс только ссылается на авторитетный документ системы.
