# Низкоуровневая структура модулей Dig

## 1. Назначение

Документ описывает рекомендуемое разбиение исходного кода и владение данными. Публичные контракты, события и правила зависимостей вынесены в [`module-contracts.md`](module-contracts.md).

Имена директорий можно адаптировать к выбранному движку, но архитектурные границы сохраняются.

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
    Optional
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
    ExplorationState
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
    NeedDefinitionRef
    NeedModifier
    NeedEvaluator

  Jobs/
    Job
    JobId
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
    FacilityState

  Production/
    ProductionOrder
    RecipeRef
    ProductionQueue
    ProductionProgress
    EnergyDemand

  Technology/
    TechnologyState
    UnlockSet
    ResearchProgress
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

  Events/
    WorldEvents
    AgentEvents
    JobEvents
    EconomyEvents
    SocietyEvents
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
    ChangePriority
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

Application не владеет доменным состоянием. Он преобразует ввод в команды и координирует владельцев состояния.

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

Infrastructure реализует внешние технические возможности через интерфейсы внутренних слоёв.

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

Presentation получает read models и события отображения. Он не хранит авторитетное игровое состояние.

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

Runtime-состояние не хранится в Content. После загрузки определения становятся неизменяемыми.

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

Diagnostics читает только публичные read-only контракты и не изменяет симуляцию.

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
    Save/load size and time
```

## 10. Правила добавления нового модуля

Перед добавлением модуля необходимо определить:

- владельца состояния;
- публичные команды и запросы;
- публикуемые события;
- инварианты;
- направление зависимостей;
- сохранение или способ пересчёта;
- диагностику;
- тестовые сценарии;
- ожидаемый performance budget.

Не следует создавать модуль только ради группировки файлов. Модуль нужен, когда существует самостоятельная ответственность и граница владения.

## 11. Связанные документы

- [`overview.md`](overview.md) — верхнеуровневая архитектура.
- [`module-contracts.md`](module-contracts.md) — контракты, события и зависимости.
- [`system-catalog.md`](system-catalog.md) — индекс систем.
- [`../development-rules.md`](../development-rules.md) — обязательные правила.
- [`../ROADMAP.md`](../ROADMAP.md) — порядок реализации.
