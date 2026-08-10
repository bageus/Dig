# Unity runtime verification and release gates

Статус: `APPROVED`.

Tracking issue: [#511](https://github.com/bageus/Dig/issues/511).

Связанные системы: [`presentation-input-ui-and-diagnostics.md`](presentation-input-ui-and-diagnostics.md), [`../implementation/quality-soak-performance.md`](../implementation/quality-soak-performance.md), [`../development-rules.md`](../development-rules.md), [`../ROADMAP.md`](../ROADMAP.md).

## 1. Назначение и границы

Эта система является единственным владельцем фактического Unity runtime evidence. Она не владеет gameplay state и не заменяет acceptance отдельных игровых систем.

В scope входят:

- licensed Unity Editor execution для текущего commit;
- Unity EditMode и PlayMode Test Runner;
- representative `Main.unity` bootstrap без неожиданных Console errors;
- raw XML, runtime logs и machine-readable evidence manifest;
- связь evidence с commit SHA, Unity version и workflow run;
- Unity runtime performance baseline и последующие blocking budgets;
- повышение конкретных систем из `IMPLEMENTED` в `VERIFIED` только после фактического evidence.

Вне scope:

- Domain/Application unit и integration tests;
- headless deterministic soak budgets;
- исправление gameplay bugs, найденных Test Runner;
- владение Unity license credentials;
- автоматическое закрытие child issues без проверки их собственного acceptance.

## 2. Запуск workflow

Поддерживаемые execution paths:

1. GitHub-hosted GameCI с Personal activation (`UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`).
2. GitHub-hosted GameCI с Pro activation (`UNITY_SERIAL`, `UNITY_EMAIL`, `UNITY_PASSWORD`).
3. Approved self-hosted или floating-license runner, который публикует тот же evidence contract.

Без licensed execution workflow создаёт `blocked` manifest. Зелёное завершение activation guard не является verification.

## 3. Основной success path

1. Checkout фиксирует exact commit SHA.
2. Unity `6000.0.71f1` открывает `.`.
3. Test Runner выполняет `EditMode` и `PlayMode`, а не пропускает их.
4. EditMode acceptance подтверждает, что `Assets/Scenes/Main.unity` включена в build settings и содержит ровно один `DigUnityBootstrap`.
5. PlayMode acceptance загружает `Main.unity`, ждёт активные simulation/input adapters и проверяет representative rendered residents/world/HUD.
6. Unexpected Console error/exception делает test failure.
7. Все test cases имеют результат `Passed`.
8. Validator читает XML и structured representative-scene log.
9. Validator создаёт `unity-runtime-evidence.json` со статусом `verified`.
10. Raw results, runtime logs и manifest сохраняются как artifacts.
11. Tracking issue получает run/artifact links и exact commit identity.

## 4. Повторное выполнение

- evidence относится только к указанному commit SHA;
- новый runtime-affecting commit требует нового run;
- повторный run не переиспользует старый manifest как результат;
- cache Unity Library является производным ускорением и не входит в evidence;
- два runs одного commit допустимы, но `VERIFIED` использует последний полный passing run.

## 5. Blocked, failure и retry

`blocked`:

- activation credentials или licensed runner отсутствуют;
- Test Runner не запускался;
- manifest содержит причину и `not-executed` для required modes;
- system/feature statuses не повышаются.

`failed`:

- Unity compilation failed;
- XML отсутствует или не читается;
- suite имеет failed/skipped/inconclusive test;
- required EditMode/PlayMode acceptance test отсутствует;
- representative runtime log отсутствует или не подтверждает clean Console;
- manifest validator возвращает non-zero exit code.

Retry выполняется новым workflow run после исправления prerequisite или root cause. Старый failed/blocked artifact сохраняется как диагностика и не заменяется задним числом.

## 6. Authoritative evidence owner

Authoritative record одного run — immutable artifact set:

- raw Unity Test Runner XML;
- Unity-generated test artifacts;
- `runtime/representative-scene.log`;
- `unity-runtime-evidence.json`;
- GitHub run ID и commit SHA.

Issue comments и docs являются индексом evidence, но не заменяют artifacts. Workflow conclusion без manifest не является источником истины.

## 7. Evidence manifest

Минимальные поля schema version 1:

- `status`: `verified`, `blocked` или `failed`;
- `reason`;
- `unityVersion`;
- `commitSha`;
- `runId`;
- test totals;
- required mode/test results;
- result XML paths;
- runtime log paths.

`verified` допустим только когда:

- найден хотя бы один parsed test case;
- каждый parsed test case имеет `Passed`;
- required EditMode и PlayMode tests выполнены и passed;
- structured runtime log содержит `status=passed`, `scene=Assets/Scenes/Main.unity`, `consoleErrors=0`.

## 8. Input, Presentation и representative scene

Verification не меняет input priority или gameplay behavior. Representative scene использует production `DigUnityBootstrap` и подтверждает только observable composition:

- simulation driver создан и enabled;
- world interaction создан и enabled;
- Main camera и HUD созданы;
- representative residents и world renderer присутствуют;
- неожиданные error/exception logs отсутствуют.

Более узкие cursor, BuildingBox, excavation, food, mushroom, barrel и HUD workflows остаются в своих checked-in tests и tracking issues.

## 9. Performance baseline и budgets

Headless budgets принадлежат `quality-soak-performance.md`. Unity runtime budgets принадлежат этой системе.

Порядок:

1. Первый licensed passing run фиксирует platform, runner, scene, frame/sample window, elapsed time, allocations и memory metrics.
2. Baseline публикуется как artifact и в implementation note.
3. Budgets устанавливаются из измеренного baseline с документированным запасом, а не угадываются.
4. Второй run должен пройти утверждённые budgets.
5. Увеличение budget требует причины и нового evidence.

До первого licensed baseline runtime performance acceptance остаётся открытым в #511.

## 10. Save/load и migration

Verification artifacts не являются save-game state и не загружаются игрой. Manifest schema version изменяется только совместимо либо с отдельной migration/reader policy.

Игровые save/load scenarios выполняются Unity tests соответствующих systems, но authoritative save data остаётся у Saving owners.

## 11. Diagnostics

Каждый run должен явно показывать:

- activation configured/blocked reason;
- выполнялись ли EditMode и PlayMode;
- test totals и non-passing cases;
- required test presence;
- runtime log presence;
- manifest status;
- commit SHA, Unity version и run ID;
- artifact names/retention.

Blocked workflow не маскируется как verified green check.

## 12. Test matrix

Repository/source level:

- validator self-test accepts complete passed fixtures;
- validator rejects failed/missing XML and missing runtime log;
- source-contract test locks workflow, required tests, manifest and build scene;
- normal Quality workflow runs validator self-test.

Unity EditMode:

- Main scene is enabled in build settings;
- exactly one `DigUnityBootstrap` exists.

Unity PlayMode:

- representative Main scene boots without unexpected Console errors;
- complete checked-in runtime suite executes;
- interaction-specific scenarios preserve their own acceptance.

## 13. Acceptance

`APPROVED`/repository-ready:

- [x] one tracking issue owns all remaining licensed Unity evidence;
- [x] workflow declares supported activation paths;
- [x] EditMode and PlayMode are selected together;
- [x] required representative tests are checked in;
- [x] raw results and evidence manifest have retained artifacts;
- [x] validator distinguishes `blocked`, `failed` and `verified`;
- [x] source and self-test regressions protect the gate.

`VERIFIED`:

- [ ] current `main` has an executed licensed Unity run;
- [ ] EditMode and PlayMode both pass without skipped/inconclusive tests;
- [ ] representative scene runtime log confirms zero Console errors;
- [ ] manifest status is `verified` and matches current commit SHA;
- [ ] raw XML/log artifacts are retained;
- [ ] measured Unity runtime baseline is recorded;
- [ ] approved Unity runtime budgets pass on a subsequent run.

## 14. Открытые вопросы

Открытых gameplay или architecture решений нет. External prerequisite — доступ к licensed Unity execution. Runtime budget numbers intentionally remain unset until measured baseline exists.

## 15. Журнал решений

| Дата | Решение | Кто подтвердил | Изменённые sections/issues |
|---|---|---|---|
| 2026-07-28 | Source contracts и зелёный skipped workflow не являются Unity evidence. | Issues #14/#15 | 1–13 |
| 2026-07-29 | Все remaining licensed runtime gates перенесены в отдельного owner #511 без потери acceptance. | Issue #16 approved closure path | Все |
| 2026-07-29 | `verified` определяется manifest validator, required EditMode/PlayMode tests и representative runtime log. | Issue #511 | 3, 6–13 |
