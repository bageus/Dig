# Resident work tool visual projection — implementation

Дата: 2026-08-03.  
Статус: `IN PROGRESS`.  
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
- checked-in Play Mode sequence `club -> pickaxe -> axe -> hammer -> club -> empty` с проверкой отсутствия colliders.

## Validation

Заполняется после финального CI. Фактический Unity EditMode/PlayMode результат будет указан отдельно; workflow-level success при skipped activation не считается runtime evidence.