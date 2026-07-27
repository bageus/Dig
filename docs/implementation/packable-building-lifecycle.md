# Packable building lifecycle

Статус реализации: `IMPLEMENTED`, Unity Play Mode verification pending.

Authoritative design: [`../design/building-box-placement-and-packing.md`](../design/building-box-placement-and-packing.md).

Tracking: [#118](https://github.com/bageus/Dig/issues/118), [#398](https://github.com/bageus/Dig/issues/398), [#15](https://github.com/bageus/Dig/issues/15).

## Scope

BuildingBox lifecycle связывает одну quantity-one physical box entity с двумя layer-derived placement intents:

- Z0: relocation той же коробки;
- Z1–Z3: delivery/unpack/assembly конечного здания.

World selection, resident inventory action, moving preview, Jobs, Inventory reservation, carry, commit, cancel и save/load используют один `StackId`.

## Authoritative owners

- `InventoryState` владеет box entity, item id, quantity, location и item reservation.
- `BuildingsState` владеет assembly plan, footprint, progress и completed building.
- `JobSystem` владеет relocation/assembly stage, worker claim и position reservations.
- `BuildingBoxPlacementPresenter` только проектирует preview и layer-derived `BuildingBoxPlacementKind`.
- Unity Presentation владеет system cursor visibility, moving ghost, tint и selected ids.

Preview никогда не является второй коробкой, building plan или reservation.

## Runtime entry points

### World source

Обычный LMB выбирает world box и открывает Buildings menu. `Unpack` вызывает `BeginBuildingBoxPlacement`.

### Resident inventory source

LMB по BuildingBox slot вызывает тот же `BeginBuildingBoxPlacement` без отдельного menu action.

Оба entry point сохраняют authoritative source location до valid confirmation.

## Moving placement cursor

`DigWorldInteraction.BuildingBoxes`:

- скрывает system cursor на входе;
- восстанавливает предыдущую visibility при cancel/confirmation;
- обновляет preview по terrain hit;
- если actor/item/открытое пространство не дают terrain hit, проецирует pointer на текущий depth layer;
- не обновляет renderer повторно, пока origin не изменился;
- использует IgnoreRaycast/collider-disabled ghost geometry.

`DigBuildingBoxGhostRenderer` получает `BuildingBoxPlacementKind`:

- `RelocateBox` разрешает BuildingBox visual profile;
- `AssembleBuilding` разрешает Completed visual profile;
- valid tint зелёный, invalid tint красный.

## Z0 relocation

`BuildingBoxPickupJobDefinition` остаётся единым typed job definition для direct pickup и relocation:

- world relocation stages: `TravelToTarget -> AcquireItem -> TravelToDestination -> DepositItem`;
- inventory-held relocation stages: `TravelToDestination -> DepositItem`;
- item reservation принадлежит одному job;
- source/destination position reservations создаются по фактическому path;
- save codec сохраняет optional destination и `starts_held`, сохраняя backward compatibility с direct pickup v1.

`CreateBuildingBoxRelocationHandler` повторно валидирует Z0/open/explored/reachable/no-building-overlap target и quantity-one source.

- world source остаётся `Available` и получает обычный nearest candidate matching;
- inventory source немедленно claim-ится authoritative holder resident;
- pickup использует `MoveFullyReservedPreservingReservation`;
- carried box сохраняет reservation и отображается синей в resident inventory;
- deposit переносит ту же entity в target world cell, сохраняет quantity one и завершает job.

Relocation не создаёт `BuildingsState` plan и не расходует box.

## Z1–Z3 assembly

Существующий `ConfirmBuildingBoxPlacementHandler` создаёт `BuildingBoxAssemblyJobDefinition` и `BuildingsState.PlaceBoxPlan`.

Execution сохраняет прежние гарантии:

- source in world забирает назначенный worker;
- source in AgentInventory допускает только holder candidate;
- box reservation сохраняется во время carry;
- box расходуется только при успешном final assembly completion;
- cancel/retry сохраняют quantity и не создают duplicate entity.

## Placement blockers

Validation получает только `BuildingsState.GetOccupiedCells()` как dynamic occupancy blocker. Resident, creature и loose world item не передаются в building footprint occupancy.

Terrain/world blockers остаются authoritative:

- out of bounds;
- solid;
- unexplored;
- building/plan overlap;
- unreachable target/work position;
- missing/reserved/mismatched source.

## Inventory projection

`DigGameHudCanvas.Inventory` окрашивает reserved BuildingBox slot в синий фон и светло-синий text. Это применяется и к box, которая уже находилась у holder, и к box, которую worker поднял с земли с сохранением reservation.

## Save/load

Assembly продолжает использовать existing building/job/inventory snapshots.

Relocation использует backward-compatible `BuildingBoxPickupJobSaveCodec`:

- старые direct pickup snapshots без destination декодируются прежним образом;
- relocation дополнительно сохраняет destination XYZ и `starts_held`;
- Job stage, assignment, Inventory location и reservation восстанавливаются через общий production save path.

Interactive cursor/preview не сохраняются. Confirmed job projection восстанавливается из authoritative Job/Inventory state.

## Regression coverage

- presenter: Z0 single-cell relocation versus Z1–Z3 assembly footprint;
- confirmation draft сохраняет placement kind;
- world relocation creates available reserved job;
- inventory relocation claims holder resident;
- world pickup/carry/deposit сохраняет StackId и quantity;
- relocation save codec round-trip;
- source contracts: hidden/restored cursor, pointer projection fallback, layer-specific visual, relocation dispatch, blue inventory state;
- Unity Play Mode source: ghost moves between cells/layers and every child remains IgnoreRaycast/collider-disabled.

## Remaining verification

GitHub Actions не выполняет Unity Test Runner. До статуса `VERIFIED` требуется локальный Play Mode workflow:

1. world box selection -> `Unpack`;
2. moving green/red cursor across Z0 and Z1–Z3;
3. Z0 relocation from world and holder inventory;
4. Z1–Z3 assembly from world and holder inventory;
5. resident/loose item under pointer does not freeze or invalidate ghost;
6. cancel, route retry and save/load at each confirmed stage.
