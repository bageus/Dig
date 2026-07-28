# Barrel orientation, mushroom execution and internal-stock visibility regression — 2026-07-28

Статус: `IMPLEMENTED` в PR #478. Фактический licensed Unity Test Runner run остаётся обязательным для `VERIFIED`.

Связанные authoritative systems и tracking:

- destructible barrels: [`../design/destructible-barrels.md`](../design/destructible-barrels.md), issue [#443](https://github.com/bageus/Dig/issues/443);
- mushroom growth/chopping: [`../design/mushroom-growth-and-chopping.md`](../design/mushroom-growth-and-chopping.md), issue [#423](https://github.com/bageus/Dig/issues/423);
- building production/internal supply: [`../design/building-production-and-internal-supply.md`](../design/building-production-and-internal-supply.md), issue [#433](https://github.com/bageus/Dig/issues/433);
- excavation execution/fault isolation: [`../design/excavation-command-execution.md`](../design/excavation-command-execution.md), issue [#388](https://github.com/bageus/Dig/issues/388).

## Runtime symptoms

Screenshot и Unity Console подтвердили три observable defects:

1. Barrel primitives наследовали поворот side-view bootstrap root и визуально лежали на земле; пользователь также уменьшил утверждённый presentation-размер на 30%.
2. Spatial excavation достигал quarter commit без действующей World designation и выбрасывал `world.excavation.quarter_requires_designation`. Исключение прекращало simulation tick до mushroom chopping, поэтому рубка грибов выглядела полностью сломанной.
3. Building supply успешно коммитил `ItemLocation.InBuilding`, но материал проецировался около origin/внутри geometry здания без видимой зоны приёмки, поэтому игрок не видел ни место складирования, ни сами units.

## Root causes

### Barrel presentation

`DigBarrelRenderer` создавал root через `SetParent(..., worldPositionStays: false)`. Общий bootstrap transform повёрнут для side-view presentation, поэтому локальный identity barrel root наследовал этот поворот. Геометрия была корректна относительно local-up, но local-up уже не совпадал с world-up.

### Mushroom execution stall

Mushroom Domain/Application workflow не был первопричиной. `DesignateTunnelDepth` создавал spatial excavation job без предварительной authoritative `CellDesignation.Dig`. Дополнительно erase/stale paths не всегда отменяли spatial job и не очищали quarter coordinator. Поздний quarter commit поэтому мог попасть в solid undesignated cell и выбросить исключение внутри общего simulation loop до mushroom advancement.

### Internal stock visibility

`DigBuildingInternalStockRenderer` вычислял piles от building origin и использовал negative front depth offset. Для фактического building visual это помещало маленькие units внутри или за geometry. Presentation не имела отдельной видимой deposit bay, хотя authoritative stock и supply transactions были корректны.

## Corrections

### Barrels

- renderer root сохраняет world orientation через `worldPositionStays: true`;
- каждый tracked barrel reset-ится в `Quaternion.identity`;
- все прежние presentation dimensions умножены на `PresentationScale = 0.70`;
- итоговая visual/collider height равна `0.49` world unit;
- основание остаётся на walk surface, authoritative cell/depth/lifecycle не меняются.

### Excavation and mushroom continuity

- `DesignateTunnelDepth` сначала устанавливает World `Dig` designation, затем создаёт spatial job;
- failure создания job откатывает designation;
- erase batch отменяет ordinary и spatial excavation jobs;
- terrain-session erase cleanup удаляет route/output/spatial maps и quarter coordinator state;
- stale solid-undesignated spatial work отменяется обычным typed job cancellation вместо exception;
- один excavation failure больше не останавливает следующий mushroom/runtime subsystem в tick.

Mushroom growth, chop ownership, swing bands, drops и Woodworking grant не изменены.

### Building internal stock

- stock units и bay проецируются у authoritative building `WorkPosition`, то есть в фактической точке supply check/deposit;
- добавлен постоянный non-blocking `DigBuildingInternalStockBayVisual` с tray и back rail;
- material units увеличены для читаемости и остаются trigger-only pickup targets;
- используется положительный `VisibleDepthOffset = 0.12f`, остающийся внутри logical depth slab;
- разные stock definitions и units сохраняют отдельные deterministic anchors.

Authoritative inventory location, capacities, incoming ledger, direct pickup и production reservations не изменены.

## Regression coverage

- `RuntimeScreenshotRegressionTests` закрепляет designation ownership/rollback, stale cancellation, world-upright barrel scale и visible stock bay;
- `EraseExcavationBatchTests` проверяет cancellation spatial job и освобождение reservations;
- `BarrelUnityRuntimeContractTests` требует `PresentationScale = 0.70`, height `0.49`, world-position parent и identity rotation;
- `BuildingProductionUnityRuntimeContractTests` требует WorkPosition-based bay, positive in-slab depth offset и non-blocking storage geometry;
- `BarrelDestructionPlayModeTests` запускается под rotated parent и проверяет world-up, height `0.49`, depth slab, highlight, destruction и safe landing;
- `BuildingProductionPlayModeTests` проверяет видимую bay, отдельные trigger units и размещение piles у WorkPosition.

## Validation

Behavioral head `47205ee1336eae9b3d865e36264cd55d3bff9605`:

- Quality run `30378034458` / run 6296: success — architecture/file-size/C# compatibility, all Unity source-contract gates, Release restore/build, full `.NET` test suite, headless smoke, standard deterministic soak и large-settlement soak;
- Export Stage 2 v2 run `30378034174` / run 573: success;
- Export Stage 2 v3 run `30378034162` / run 578: success;
- Unity Play Mode workflow `30378034438` / run 60: workflow success, но licensed `Run Play Mode tests` step skipped из-за отсутствующих activation credentials.

Поэтому затронутые системы остаются `IMPLEMENTED`, не `VERIFIED`.
