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

## Regression coverage

- engine-independent tests для exact job/stage mapping, target XYZ и отсутствия invented tools;
- Unity source contract для typed mapping, right-hand override, restoration и distinct geometry;
- checked-in Play Mode sequence `club -> pickaxe -> axe -> hammer -> club -> empty` с проверкой отсутствия colliders;
- прежний excavation feedback contract обновлён с runtime `JobToolKind.Mining` на authoritative `ResidentWorkToolVisualKind.Pickaxe` projection.

## Validation

Final Quality run `30852795651` на code head `c0bde8b9f18bc40a21d3e3239ef11e93c76dc654`:

- architecture, file-size, C# compatibility, dependency и Domain-boundary gates passed;
- Unity source contracts и runtime-evidence tooling passed;
- Release build: `0` warnings, `0` errors;
- full .NET suite: `1445/1445` passed;
- headless smoke passed at tick `20`;
- standard deterministic soak replay hash `D27036F09F4EF8CA6C20159F194ED68F07D7C54963CE82C81CB3375AE4DBDFE3`;
- large deterministic soak replay hash `66620BC1E1B9756DF64C0D018450E7EF0E4AD10D67F741079EA894945897701E`;
- both soak runs reported `replay=True` and passed performance budgets.

Unity workflow `30852795650` успешно записал blocked runtime evidence: activation была недоступна, поэтому реальные EditMode/PlayMode tests и executed-runtime-evidence validation были skipped. Реализация имеет статус `IMPLEMENTED`, но не `VERIFIED`.