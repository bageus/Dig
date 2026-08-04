# Unity Play Mode object/nullability compile correction

Дата: 2026-08-04.  
Статус: `IMPLEMENTED IN BRANCH`.

Authoritative specifications:

- [`../design/resident-work-tool-visual-projection.md`](../design/resident-work-tool-visual-projection.md);
- [`../design/full-depth-tunnel-eating-and-tent-presentation-correction.md`](../design/full-depth-tunnel-eating-and-tent-presentation-correction.md).

Tracking issues: [#602](https://github.com/bageus/Dig/issues/602), [#626](https://github.com/bageus/Dig/issues/626).  
Correction PR: [#633](https://github.com/bageus/Dig/pull/633).

## Runtime report

После восстановления Unity compilation boundary локальная компиляция показала:

- `CS0104` в `ResidentWorkToolVisualsPlayModeTests.cs` на двух вызовах `Object.DestroyImmediate`;
- `CS8602` в `DigAgentVisual.Eating.cs` при чтении `Model.ActionProgress`.

## Root causes

Play Mode test импортировал одновременно `System` и `UnityEngine`, поэтому краткое имя `Object` было неоднозначным между `System.Object` и `UnityEngine.Object`.

Eating visual сначала проверял nullable-свойство `Model`, а затем повторно читал то же свойство. Nullable flow analysis не гарантирует, что повторное чтение свойства вернёт тот же non-null объект, поэтому Unity compiler выдавал warning на dereference.

## Correction

- teardown теста явно вызывает `UnityEngine.Object.DestroyImmediate` для GameObject и Material;
- eating visual один раз сохраняет `Model` в локальный nullable snapshot;
- guard проверяет этот snapshot перед использованием;
- action progress, resident id и version читаются из того же snapshot;
- gameplay state, Eat cadence, work-tool mapping, Inventory и Presentation behavior не изменяются.

## Regression coverage

- `ResidentWorkToolUnityRuntimeContractTests` требует полностью квалифицированные Unity teardown calls и запрещает неквалифицированный вызов;
- `FullDepthEatingTentSourceContractTests` требует локальный `AgentViewModel` snapshot, явный null guard и запрещает прямое повторное чтение `Model.ActionProgress`.

## Automated validation

На code head `c282cd9db9a016b4afa0b31787aa21867cbe7e97`:

- Quality run `30936651537` — success;
- architecture, file-size, C# compatibility и dependency gates — success;
- все Unity source/presentation/runtime contracts — success;
- Release build — `0` warnings, `0` errors;
- .NET suite — `1505/1505`;
- headless smoke — success;
- standard deterministic soak — success;
- large-settlement deterministic soak — success;
- Export Stage 2 v2 run `30936649949` — success;
- Export Stage 2 v3 run `30936651807` — success.

Unity workflow `30936651600` выполнил только blocked-evidence path: actual EditMode/PlayMode Test Runner и executed-runtime-evidence validation были skipped из-за недоступной licensed activation.

## Verification boundary

Фактическое исчезновение Unity Console ошибок подтверждается только повторной локальной компиляцией либо выполненным лицензированным Unity Test Runner; source contracts и .NET checks сами по себе не дают статус `VERIFIED`.
