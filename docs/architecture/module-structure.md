# Низкоуровневая структура модулей Dig

## 1. Назначение

Документ описывает рекомендуемое разбиение исходного кода и владение данными. Контракты, события и зависимости вынесены в [`module-contracts.md`](module-contracts.md).

Имена директорий можно адаптировать к движку, но архитектурные границы сохраняются.

## 2. Корневая структура

```text
src/
  Domain/
  Application/
  Infrastructure/
  Presentation/
  Content/
  Diagnostics/

tests/
  Unit/
  Integration/
  Simulation/
  Performance/

docs/
  architecture/
  adr/
  systems/

tools/
  ContentValidation/
  SaveInspection/
  Profiling/
```

## 3. Domain

```text
Domain/
  Core/
    EntityId
    Result
    DomainEvent
    RandomStream
    VersionStamp

  Time/
    SimulationClock
    TickScheduler
    Calendar
    Schedule

  World/
    WorldState
    Cell
    Chunk
    MaterialState
    EnvironmentState
    DesignationState

  Navigation/
    NavigationSnapshot
    RegionGraph
    PathRequest
    PathResult
    TraversalProfile

  Agents/
    Agent
    AgentState
    Health
    SkillSet
    TraitSet
    Lifecycle
    AgentIntent

  Needs/
    NeedSet
    NeedModifier
    NeedEvaluator

  Jobs/
    Job
    JobType
    JobStage
    JobPriority
    JobClaim
    Reservation
    WorkOrder

  Inventory/
    ItemStack
    Inventory
    StoragePolicy
    ItemLocation
    TransferRequest

  Buildings/
    Building
    ConstructionSite
    BuildingPlacement
    Room

  Production/
    ProductionOrder
    RecipeRef
    ProductionQueue
    EnergyDemand

  Technology/
    TechnologyState
    UnlockSet
    PrerequisiteGraph

  Society/
    Relationship
    Family
    Partnership
    Pregnancy
    ChildState
    SocialMemory

  Combat/
    CombatIntent
    AttackProfile
    DamageEvent
    StatusEffect
    ThreatState

  Factions/
    Faction
    DiplomacyState
    StrategicGoal
    TerritoryState
```

Domain содержит игровые правила и не зависит от движка, UI, файловой системы или сцен.

## 4. Application

```text
Application/
  Commands/
    World/
    Agents/
    Jobs/
    Buildings/
    Production/
    Technology/

  Queries/
    Colony/
    Agents/
    World/
    Economy/

  UseCases/
    DesignateDigging
    PlaceBuilding
    IssueDirectOrder
    CreateProductionOrder
    ChangeSchedule
    SaveGame
    LoadGame

  Services/
    SimulationRunner
    CommandBus
    QueryBus
    EventDispatcher
    GameModeCoordinator

  ReadModels/
    ColonySummary
    AgentInspector
    JobInspector
    ResourceSummary
    TechnologyView
```

Application преобразует ввод в команды и координирует владельцев состояния, но не хранит доменные данные.

## 5. Infrastructure

```text
Infrastructure/
  Persistence/
    SaveRepository
    SaveSerializer
    SaveMigration
    SnapshotBuilder

  ContentLoading/
    DefinitionLoader
    DefinitionValidator
    CatalogBuilder

  Concurrency/
    BackgroundTaskScheduler
    VersionedResultQueue

  Logging/
    LoggerAdapter
    EventJournal

  Platform/
    FileSystemAdapter
    ClockAdapter
    ThreadAdapter

  Engine/
    EngineBootstrap
    SceneAdapter
    ResourceAdapter
```

Infrastructure реализует внешние возможности через интерфейсы внутренних слоёв.

## 6. Presentation

```text
Presentation/
  Camera/
  Input/
  UI/
    HUD/
    Windows/
    Inspectors/
    Overlays/

  WorldView/
    ChunkRenderer
    TerrainEffects
    BuildingView

  AgentView/
    AgentPresenter
    AnimationController
    EquipmentView

  Audio/
  VFX/
  Selection/
  DebugView/
```

Presentation отображает read models и события. Он не хранит авторитетное игровое состояние.

## 7. Content

```text
Content/
  Definitions/
    Materials/
    Items/
    Buildings/
    Recipes/
    Technologies/
    Needs/
    Skills/
    Jobs/
    Factions/

  Localization/
  Balance/
  Schemas/
  ValidationRules/
```

Runtime-состояние не хранится в Content. После загрузки определения неизменяемы.

## 8. Diagnostics

```text
Diagnostics/
  AgentInspector/
  JobInspector/
  ReservationView/
  NavigationView/
  ChunkView/
  PerformanceView/
  EventTrace/
```

Diagnostics использует только read-only контракты.

## 9. Tests

```text
Tests/
  Unit/
    Domain rules
    Policies
    State transitions

  Integration/
    Cross-system scenarios
    Save/load
    Content loading

  Simulation/
    Deterministic scenarios
    Long-running colonies
    Regression fixtures

  Performance/
    Pathfinding
    Terrain updates
    Large populations
    Save/load
```

## 10. Добавление нового модуля

Перед добавлением модуля определяются:

- владелец состояния;
- команды, запросы и события;
- инварианты;
- направление зависимостей;
- способ сохранения или пересчёта;
- диагностика;
- тестовые сценарии;
- performance budget.

Модуль создаётся только при наличии самостоятельной ответственности и границы владения.

## 11. Связанные документы

- [`overview.md`](overview.md) — верхнеуровневая архитектура;
- [`module-contracts.md`](module-contracts.md) — контракты и зависимости;
- [`system-catalog.md`](system-catalog.md) — индекс систем;
- [`../development-rules.md`](../development-rules.md) — обязательные правила;
- [`../ROADMAP.md`](../ROADMAP.md) — порядок реализации.
