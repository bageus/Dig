# Низкоуровневая структура модулей Dig

## 1. Назначение

Документ описывает рекомендуемое разбиение исходного кода, владение данными и публичные контракты. Имена директорий могут быть адаптированы к движку, но направления зависимостей сохраняются.

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

Application не владеет доменным состоянием. Он координирует вызовы владельцев состояния.

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

Presentation получает read models и события представления. Он не хранит авторитетное игровое состояние.

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

## 8. Публичные контракты модулей

Каждый крупный модуль предоставляет минимальный фасад.

Пример:

```text
IWorldService
  GetCell(cellId)
  ValidateDesignation(command)
  ApplyTerrainChange(change)
  CreateSnapshot(region)

IJobService
  CreateWorkOrder(spec)
  FindCandidates(agentId)
  ClaimJob(agentId, jobId)
  AdvanceJob(jobId, result)
  CancelJob(jobId, reason)

IInventoryService
  QueryAvailableItems(filter)
  ReserveItems(request)
  Transfer(transfer)
  ReleaseReservation(id)
```

Внутренние коллекции и изменяемые сущности наружу не выдаются.

## 9. События между системами

Примеры фактов:

```text
CellChanged
ChunkInvalidated
JobCreated
JobClaimed
JobCompleted
ReservationReleased
ItemTransferred
BuildingCompleted
TechnologyUnlocked
AgentNeedBecameCritical
AgentDied
ChildBorn
FactionRelationChanged
```

Событие содержит идентификаторы и минимально необходимый снимок данных. Получатель не должен изменять состояние отправителя через ссылку на объект.

## 10. Структура системы

Каждая система по возможности имеет одинаковый внутренний шаблон:

```text
<System>/
  Model/
  Commands/
  Events/
  Queries/
  Policies/
  Services/
  Serialization/
  Tests/
```

Не нужно создавать пустые папки заранее. Шаблон применяется по мере появления ответственности.

## 11. Правила разбиения файлов

При достижении 250–300 строк следует проверить, не смешаны ли:

- модель и оркестрация;
- валидация и выполнение;
- сериализация и доменная логика;
- выбор стратегии и реализация стратегии;
- публичный контракт и детали движка;
- разные состояния конечного автомата.

Файл более 350 строк считается нарушением, кроме документированных исключений из `docs/development-rules.md`.

## 12. Направления зависимостей

Разрешено:

```text
Presentation -> Application
Application -> Domain
Infrastructure -> Domain interfaces
Infrastructure -> Application interfaces
Content loader -> Content schemas
Diagnostics -> public read-only contracts
```

Запрещено:

```text
Domain -> Presentation
Domain -> Engine API
Domain -> File system
World -> UI
Jobs -> concrete animation controller
Inventory -> scene object
Content definitions -> mutable runtime entity
```

## 13. Минимальный вертикальный срез

Первый сквозной сценарий должен пройти через всю структуру:

1. пользователь назначает одну клетку на копание;
2. Application создаёт команду;
3. World валидирует назначение;
4. Jobs создаёт работу;
5. Agent выбирает и резервирует её;
6. Navigation возвращает маршрут;
7. Agent выполняет действие;
8. World меняет клетку;
9. Inventory создаёт ресурс;
10. Presentation перестраивает затронутый чанк;
11. состояние сохраняется и загружается;
12. сценарий проверяется интеграционным тестом.
