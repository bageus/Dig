# Аудит полноты реализованных систем и соответствия runtime

Дата: 2026-07-26  
Проверенный репозиторий: `bageus/Dig`  
Проверенный baseline: `main` после PR #402 (`80af4efd7b64b73ce8a8b559eac3eac74e09aa65`)  
Tracking issue: [#403](https://github.com/bageus/Dig/issues/403)  
Связанный аудит документации: [#393](https://github.com/bageus/Dig/issues/393)

## 1. Цель и метод

Аудит сопоставляет для каждой индексированной системы:

1. `docs/systems/README.md` и заявленный статус;
2. authoritative design;
3. acceptance criteria tracking issue;
4. implementation notes;
5. фактический Domain/Application/Unity code;
6. unit/integration/deterministic/Play Mode evidence;
7. полный observable workflow, включая repeat, cancel, blocked/failure/retry и save/load.

Это статический repository audit. Он не заменяет ручной Unity Play Mode проход. Где runtime evidence отсутствует, система не считается `VERIFIED`, даже если build и source-contract checks зелёные.

## 2. Сводка

На момент аудита индекс содержит 66 системных строк:

- `IMPLEMENTED`: 20;
- `APPROVED`: 26;
- `QUESTIONNAIRE`: 18;
- `DRAFT`: 2;
- `VERIFIED`: 0.

Индекс ссылается на 35 уникальных authoritative файлов. По обязательному шаблону `docs/development/system-specification-template.md`:

- только 10 из 35 файлов имеют явное поле `Статус`;
- только 7 из 35 имеют прямое поле `Tracking issue`/`Tracking issues`;
- 18 из 35 не используют нумерованную структуру полного системного workflow;
- ни одна runtime-система не имеет зафиксированного CI evidence запуска Unity Play Mode Test Runner.

Главный вывод: архитектурный и доменный фундамент значительно шире, чем утверждает старый `docs/implementation-status.md`, но несколько статусов `IMPLEMENTED` завышены, а самые часто используемые Unity workflows расходятся с уже подтверждёнными правилами.

## 3. Критические расхождения design ↔ code

### AUD-001 — direct excavation создаёт второй manual owner и исключает jobs из общего matching pool

Severity: `P0`  
Owner issue: [#388](https://github.com/bageus/Dig/issues/388)

Authoritative rule в `docs/design/excavation-command-execution.md`:

- direct order использует ordinary excavation jobs;
- не создаёт второго manual job owner;
- не закрепляет resident за зоной;
- не удаляет jobs из общего списка;
- новые связанные клетки динамически входят в zone;
- другие свободные residents могут подключаться.

Фактическая реализация:

- `DigTerrainWorkManualExcavation.cs` и `DigTerrainWorkManualExcavation.MultiWorker.cs` выбирают кластер с жёстким `radius: 4`;
- `ManualExcavationGroup` хранит фиксированный список `JobIds` и `TargetCells` в Unity session;
- `RegisterManualExcavationGroup` и single-worker path вызывают `_candidateProvider.SetCandidates(jobId, NoCandidates)`;
- `RefreshManualExcavationGroupJobs` проверяет только `group.TargetCells`, зафиксированные при direct click;
- `_manualGroups` и `_manualGroupByJob` являются вторым runtime owner и не входят в SaveGame snapshot.

Observable consequences:

- direct-started jobs исключаются из общего automatic matching;
- новые связанные клетки за пределами первоначального radius/target list не присоединяются;
- другой свободный resident не может корректно взять job, пока он принадлежит manual group;
- save/load теряет manual continuation state;
- правило «10+ cells и добавление cells во время копки» не подтверждено.

Текущий `DirectExcavationOrderPlayModeTests` не покрывает 10+ cells, dynamic join, common pool, failure/retry или save/load. Более того, тест всё ещё ожидает `LoadManualQuarterAssignment`, хотя текущий multi-worker path отменяет manual-quarter assignment и использует другую схему.

Необходимое исправление: удалить `ManualExcavationGroup` как owner zone membership; direct command должен только немедленно попытаться назначить ordinary job/priority marker, а connected zone каждый раз вычисляться из authoritative designations.

### AUD-002 — BuildingBox обычным ЛКМ сразу запускает placement, а Z0 создаёт assembly workflow

Severity: `P0`  
Owner issue: [#398](https://github.com/bageus/Dig/issues/398)

Authoritative rule в `docs/design/building-box-placement-and-packing.md`:

- обычный LMB только выбирает BuildingBox;
- открывает building roster и подсвечивает строку;
- placement запускается только кнопкой `Распаковать`;
- Z0 click создаёт BuildingBox placement plan;
- после delivery в target cell остаётся та же world BuildingBox;
- Z0 plan не является assembly конечного здания.

Фактическая реализация:

- `ContextInputRouter.World.cs` возвращает `PresentationInputEffect.StartBuildingPlacement` для обычного LMB по `BuildingBox`;
- `DigWorldInteraction.BuildingBoxes.StartBuildingPlacement` немедленно вызывает `BeginBuildingPlacement`;
- `DigBuildingBoxGhostRenderer.Representatives.cs` на `Origin.Z == 0` специально выбирает `BuildingVisualState.BuildingBox`, хотя утверждённый preview должен показывать конечное здание;
- `ConfirmBuildingBoxPlacementHandler` создаёт `BuildingBoxAssemblyJobDefinition` и `BuildingsState.PlaceBoxPlan`;
- `DigBuildingBoxAssemblyExecution` затем выполняет acquire, commit-to-site, assembly work и `CompleteBuildingBoxAssembly`, то есть строит конечное здание.

Observable consequences:

- selection-only workflow отсутствует;
- кнопка `Распаковать` не является единственной точкой входа;
- Z0 preview показывает коробку вместо final-building ghost;
- Z0 confirmation не создаёт отдельный delivery plan коробки и не оставляет коробку в target cell;
- authoritative design и issue acceptance выполняются противоположным образом.

### AUD-003 — directional lanes, chain spacing и stationary avoidance не реализованы

Severity: `P1`  
Owner issue: [#386](https://github.com/bageus/Dig/issues/386)

Authoritative rule:

- движение вправо использует правую lane, влево — левую;
- встречные horizontal residents проходят рядом;
- same-direction residents идут цепочкой;
- stationary resident обходится по свободной стороне;
- vertical opposite climbers могут проходить сквозь друг друга.

Фактическая реализация:

- `TunnelTrafficCoordinator` только запрещает reverse edge swap в один tick;
- `ApplyCrowdingOffsets` группирует residents по logical cell, сортирует по `Id` и задаёт симметричный X-offset;
- offset не зависит от направления движения;
- нет chain predecessor, desired spacing, slowing/waiting policy;
- нет stationary-side avoidance;
- нет отдельного horizontal/vertical traffic mode в coordinator.

Текущий визуальный offset уменьшает полное наложение в одной клетке, но не реализует утверждённую traffic system.

### AUD-004 — production SaveGameService не сохраняет mining-output commit ledger

Severity: `P1`  
Owner issues: [#13](https://github.com/bageus/Dig/issues/13), [#94](https://github.com/bageus/Dig/issues/94)

Фактическая реализация:

- `DigTerrainWorkSession` владеет `_miningOutputCommits` и использует его для validate/record exactly-once mining output;
- `SaveGameBuilder.Build(context, MiningOutputCommitState)` и `SaveGameLoader.LoadWithMiningOutput(...)` существуют как отдельные overloads;
- production `SaveGameService.Save` вызывает только `_builder.Build(context)`;
- все production `SaveGameService.Load` overloads вызывают только `_loader.Load(...)`;
- `SaveGameContext` не содержит `MiningOutputCommitState`;
- Unity terrain session не предоставляет capture/restore wiring в SaveGameService.

Следствие: обычное сохранение не содержит authoritative ledger, а после загрузки exactly-once validation mining output начинает с пустого состояния. Специальные unit tests проверяют overload, который production service не использует.

Дополнительный documentation drift: `docs/implementation/save-load-migrations.md` сообщает current format version `4`, тогда как `SaveFormat.CurrentVersion == 5`.

### AUD-005 — `Unit item entities` отмечена IMPLEMENTED, хотя migration явно не завершена

Severity: `P1`  
Owner issue: [#347](https://github.com/bageus/Dig/issues/347)

`docs/implementation/unit-item-entities.md` прямо перечисляет незавершённые шаги:

- production/demo creation ещё использует legacy stack API;
- pickup/drop/hauling ещё не гарантируют one unit per job;
- legacy save quantities ещё не split в stable unit IDs;
- quantity > 1 в World/AgentInventory ещё не запрещена;
- quantity badges ещё не удалены полностью.

Код подтверждает partial state:

- `InventoryState.AddStack` остаётся production API;
- restore path вызывает `AddStack`;
- в `src`, `unity` и tests остаётся более 100 вызовов `AddStack`;
- существуют сценарии с aggregate world/resident quantities.

Статус `IMPLEMENTED` не соответствует ни implementation note, ни open issue #347. Система должна оставаться `DRAFT` до завершения migration и conservation/save/load acceptance.

### AUD-006 — quality docs заявляют CI soak/Play Mode, но workflow их не запускает

Severity: `P1`  
Owner issue: [#15](https://github.com/bageus/Dig/issues/15)

`docs/implementation/quality-soak-performance.md` утверждает, что GitHub Actions запускает headless smoke и оба deterministic soak profiles и загружает reports.

Фактический `.github/workflows/quality.yml` запускает:

- Python architecture/source-contract checks;
- `dotnet restore`;
- `dotnet build`;
- `dotnet test`.

Workflow не запускает:

- `Dig.Headless --soak --profile standard`;
- `Dig.Headless --soak --profile large`;
- Unity Editor/Test Runner;
- Play Mode tests через `-runTests -testPlatform PlayMode` или эквивалент.

В репозитории есть Play Mode test assembly и семь test files, но CI их не исполняет. Поэтому source-contract/build/unit success нельзя использовать как evidence полного Unity interaction workflow.

### AUD-007 — status и audit documents противоречат текущему репозиторию

Severity: `P2`  
Owner issues: [#393](https://github.com/bageus/Dig/issues/393), [#403](https://github.com/bageus/Dig/issues/403)

- `docs/implementation-status.md` утверждает, что simulation loop, world, residents, jobs, navigation и Unity adapter отсутствуют. Это историческое состояние первого этапа и прямое противоречие текущему codebase.
- `docs/implementation/open-issues-code-audit-and-roadmap.md` датирован 2026-07-21 и содержит выводы до появления текущих authoritative specs и последних runtime regressions.
- индекс присваивает `IMPLEMENTED` системам, чьи authoritative files не имеют полного workflow, tracking link или актуального evidence.

Старые отчёты должны быть явно помечены как historical/superseded и не использоваться как источник истины.

## 4. Аудит систем, отмеченных `IMPLEMENTED`

Легенда:

- `SUPPORTED` — в статическом аудите не найдено прямого противоречия, есть code/tests; runtime всё равно не `VERIFIED`;
- `PARTIAL` — ядро существует, но authoritative spec/evidence неполны;
- `CONFLICT` — найдено подтверждённое расхождение design/issue/code или отсутствует обязательный production wiring.

| Система | Результат | Основной gap |
|---|---|---|
| Simulation loop и fixed ticks | `PARTIAL` | Реализован fixed-tick runtime, но полная fault-isolation policy вынесена в DRAFT и Unity E2E не запускается. |
| Entity identity | `SUPPORTED` | Stable ID и tests есть; отдельная полная specification/tracking metadata отсутствует. |
| Commands, events и queries | `SUPPORTED` | Архитектурные границы проверяются, но индекс ссылается на общие development rules вместо полной system spec. |
| Procedural generation | `SUPPORTED` | Deterministic code/tests присутствуют; нет `VERIFIED` runtime evidence. |
| Traversability, regions и pathfinding | `SUPPORTED` | Pathfinder/tests есть; movement occupancy является отдельной конфликтующей системой. |
| Agent state, needs и Utility AI | `PARTIAL` | Большой implementation slice есть, но authoritative workflow распределён между architecture/design notes. |
| Automatic planning toggle | `PARTIAL` | Реализация и contract tests есть, но нет отдельного authoritative lifecycle/cancel/save spec. |
| Job lifecycle и matching | `SUPPORTED` | Domain/Application tests есть; direct excavation обходится вторым manual owner. |
| Reservations | `SUPPORTED` | Core invariants покрыты; новые Unity workflows не всегда включены в save/recovery evidence. |
| Item catalog и Inventory | `PARTIAL` | Core inventory работает, но physical unit invariant ещё не завершён. |
| Unit item entities | `CONFLICT` | Migration незавершена, status завышен; #347 открыт. |
| Construction | `PARTIAL` | Core construction существует; BuildingBox Z0 lifecycle противоречит отдельной authoritative spec. |
| Production | `SUPPORTED` | Core recipe/queue tests есть; runtime presentation не `VERIFIED`. |
| Settlement management | `PARTIAL` | UI code/PlayMode tests существуют, но Play Mode CI отсутствует. |
| World/agent/building/item visuals | `PARTIAL` | Source contracts не подтверждают реальный renderer/collider/input workflow в Unity. |
| Side-view camera и depth projection | `PARTIAL` | Unit/source tests есть; runtime depth interaction не исполняется в CI. |
| Save/load/migrations | `CONFLICT` | Mining-output ledger и manual direct state не проходят production save wiring; docs version устарела. |
| Content validation | `SUPPORTED` | Validation tests есть; authoritative content files не все имеют status/tracking metadata. |
| Diagnostics, performance и soak | `CONFLICT` | Soak implementation существует, но текущий CI его не запускает; doc утверждает обратное. |
| Unity presentation host | `PARTIAL` | Bootstrap/source checks есть; нет Unity batch/Play Mode acceptance evidence. |

После этого аудита статусы `Unit item entities`, `Save/load/migrations` и `Diagnostics, performance и soak` должны быть понижены до `DRAFT` до устранения конкретных gaps.

## 5. Полнота authoritative specifications

### 5.1 Структурная проблема

Текущий обязательный шаблон требует 15 разделов: workflow, owner, model, CQRS, state transitions, UI/input, conflicts, invariants, save/load, diagnostics, test matrix, acceptance, questionnaire и decision log.

Большинство старых design/architecture files создавались до шаблона. Они могут содержать полезные правила, но не подтверждают полный observable lifecycle. Наиболее неполные группы:

- `docs/architecture/systems-core.md` и `systems-gameplay.md` используются как authoritative source сразу для нескольких систем, но являются обзором, а не полной спецификацией каждой системы;
- `docs/design/content/buildings.md`, `technology-tree.md`, `ladders-and-elevators.md`, `doors-access-and-lifecycle.md` и большая часть society/health docs не имеют tracking metadata и полного failure/retry/save/diagnostics workflow;
- несколько `APPROVED` документов не отделяют подтверждённые правила от открытых вопросов;
- implementation notes иногда описывают больше observable behavior, чем authoritative design, что создаёт риск второго источника истины.

### 5.2 Требуемая нормализация

Для каждой индексированной системы необходимо:

1. создать отдельный authoritative file либо явный section contract;
2. добавить status и tracking issue;
3. перенести подтверждённые observable rules из implementation notes в design;
4. перечислить cancel/failure/retry, concurrency, save/load и UI/status;
5. связать acceptance с конкретными automated и Play Mode tests;
6. использовать `VERIFIED` только при сохранённом E2E evidence.

Это остаётся scope issue #393 и не должно смешиваться с исправлением runtime defects из #388/#398/#386/#347/#94/#15.

## 6. Test evidence audit

### 6.1 Что реально выполняется

Текущий Quality workflow подтверждает:

- architecture/file-size/C# compatibility;
- Unity source contracts и package consistency;
- .NET build;
- .NET unit/integration tests.

### 6.2 Что не выполняется

Не подтверждены CI:

- Unity scene bootstrap;
- actual pointer routing и UI shielding;
- renderer visibility/collider/raycast parity;
- cursor animation/ghost under pointer;
- 10+ cell excavation in Play Mode;
- selection ↔ roster synchronization;
- BuildingBox selection → Unpack → preview → confirm/cancel;
- horizontal lane/chain movement;
- vertical climb/crossing;
- save/load through the actual Unity composition root;
- deterministic soak profiles described in implementation docs.

### 6.3 Required quality gate

Нужен отдельный Unity batch workflow с минимумом:

- EditMode tests;
- PlayMode tests;
- representative scene bootstrap;
- test results XML и logs as artifacts;
- failure on Console errors;
- long workflow tests для excavation, BuildingBox, selection/input и save/load.

Headless soak standard/large также должен быть возвращён в CI либо документация должна перестать утверждать, что он является blocking gate.

## 7. Приоритет исправлений

1. **#398 BuildingBox** — восстановить selection-only LMB, Unpack-only activation, final-building ghost и отдельный Z0 BuildingBox placement plan.
2. **#388 Excavation** — удалить manual zone ownership/`NoCandidates`, перейти на shared ordinary jobs и dynamic connectivity.
3. **#15 Quality** — запустить Unity Play Mode и soak в CI; исправить stale PlayMode contracts.
4. **#94/#13 Save** — встроить mining-output capture/restore в production SaveGameService/Unity composition и добавить round-trip regression.
5. **#386 Movement** — реализовать direction-based lanes, same-direction chain spacing и stationary avoidance.
6. **#347 Unit items** — завершить migration physical unit entities.
7. **#393 Documentation** — нормализовать 35 authoritative files по шаблону и evidence.

## 8. Проверки аудита

Выполнено:

- прочитан индекс систем и 35 уникальных authoritative files;
- сопоставлены связанные implementation notes и tracking issues;
- проанализированы 956 C# files в `src`, `unity` и `tests` поиском owners, workflows и save wiring;
- проверены current GitHub Quality workflow и Unity PlayMode test assembly;
- проверены последние CI runs PR #402: Quality/build/.NET tests и Stage 2 exports прошли;
- найденный lock drift `com.unity.test-framework` уже исправлен в PR #402.

Не выполнено:

- Unity Editor/Play Mode запуск в текущем окружении;
- ручной gameplay walkthrough;
- profiler capture на реальной сцене.

Поэтому ни одна система не повышена до `VERIFIED`.
