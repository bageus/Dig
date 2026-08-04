# Unity Play Mode object/nullability compile correction

Дата: 2026-08-04.  
Статус: `IMPLEMENTED IN BRANCH`.

Authoritative specifications:

- [`../design/resident-work-tool-visual-projection.md`](../design/resident-work-tool-visual-projection.md);
- [`../design/full-depth-tunnel-eating-and-tent-presentation-correction.md`](../design/full-depth-tunnel-eating-and-tent-presentation-correction.md).

Tracking issues: [#602](https://github.com/bageus/Dig/issues/602), [#626](https://github.com/bageus/Dig/issues/626).

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

## Verification boundary

Repository Quality, Release build, .NET suite, smoke и deterministic soaks должны пройти на final branch head. Фактическое исчезновение Unity Console ошибок подтверждается только повторной локальной компиляцией либо выполненным лицензированным Unity Test Runner; source contracts сами по себе не дают статус `VERIFIED`.
