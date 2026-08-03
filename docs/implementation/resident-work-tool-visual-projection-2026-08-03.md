# Resident work tool visual projection — implementation

Дата: 2026-08-03.  
Статус: `IMPLEMENTED`.  
Authoritative specification: [`../design/resident-work-tool-visual-projection.md`](../design/resident-work-tool-visual-projection.md).  
Tracking issue: [#602](https://github.com/bageus/Dig/issues/602).

## Реализация

- `JobOverlayPresenter` проецирует typed `ResidentWorkToolVisualKind` только для assigned `InProgress/PerformWork` jobs.
- Обычная и spatial excavation проецируют кирку.
- Mushroom chopping проецирует топор.
- `BuildingWorkKind.Construction`, BuildingBox assembly/unpacking и packing проецируют молоток.
- Production, repair и demolition не получают неподтверждённый инструмент.
- Building work, assembly и packing используют authoritative `WorkPosition` как target presentation.
- `DigAgentRenderer.WorkFacing` передаёт tool kind по stable resident id вместе с существующей facing/action pose.
- `DigAgentVisual` хранит последний equipment read model, но временно показывает synthetic tool в том же `RightHand` socket.
- После очистки work tool вызывается rebuild из актуального equipment model; Inventory и Job state не меняются.
- `DigAgentEquipmentVisual` создаёт разные collider-free procedural silhouettes для `weapon.club`, кирки, топора и молотка.
- `docs/systems/README.md`, specification и tracking issue синхронизированы со статусом `IMPLEMENTED`.

## Regression coverage

- engine-independent tests для exact job/stage mapping, target XYZ и отсутствия invented tools;
- Unity source contract для typed mapping, right-hand override, restoration и distinct geometry;
- checked-in Play Mode sequence `club -> pickaxe -> axe -> hammer -> club -> empty` с проверкой отсутствия colliders;
- прежний excavation feedback contract обновлён с runtime `JobToolKind.Mining` на authoritative `ResidentWorkToolVisualKind.Pickaxe` projection.

## Validation

- architecture, file-size, C# compatibility, dependency и Domain-boundary gates passed;
- Unity source contracts и runtime-evidence tooling passed;
- Release build: `0` warnings, `0` errors;
- full .NET suite: `1445/1445` passed;
- headless smoke passed at tick `20`;
- standard deterministic soak replay hash `D27036F09F4EF8CA6C20159F194ED68F07D7C54963CE82C81CB3375AE4DBDFE3`;
- large deterministic soak replay hash `66620BC1E1B9756DF64C0D018450E7EF0E4AD10D67F741079EA894945897701E`;
- both soak runs reported `replay=True` and passed performance budgets;
- docs/status commits repeated the full Quality pipeline and both Stage 2 source exports without changing production behavior.

Unity workflow recorded blocked runtime evidence: activation was unavailable, so actual EditMode/PlayMode tests and executed-runtime-evidence validation were skipped. The system is `IMPLEMENTED`, not `VERIFIED`.