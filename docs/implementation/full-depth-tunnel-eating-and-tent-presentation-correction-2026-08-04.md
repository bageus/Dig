# Full-depth tunnel, eating and tent presentation correction

Дата: 2026-08-04.  
Статус: `IMPLEMENTED`.

Authoritative specification: [`../design/full-depth-tunnel-eating-and-tent-presentation-correction.md`](../design/full-depth-tunnel-eating-and-tent-presentation-correction.md).  
Tracking issue: [#626](https://github.com/bageus/Dig/issues/626).  
Pull request: [#627](https://github.com/bageus/Dig/pull/627).

## Reported runtime problems

- бирюзовые поверхности оставались видны внутри выкопанного тоннеля;
- Z0 выглядел полноценным, а Z1-Z3 — визуально укороченными;
- non-mineable terrain на фронтальном слое не закрывал ту же колонну в глубину;
- resident продолжал стоять во время еды и не показывал еду/укусы;
- палатка была обращена входом от side-view camera и казалась больше укороченной глубинной поверхности.

## Root causes

### Open-cell cyan tiles

`DigWorldRenderer` сохранял отдельный primitive cube для каждой open walk-surface cell. `ApplyCell` масштабировал этот cube как тонкую плитку, `DigCellVisual.Configure` повторно восстанавливал floor scale, а `ResolveColor` возвращал для empty cell бирюзовый `RGB 0.20/0.52/0.66`. Поэтому плитка оставалась видимой независимо от chunked terrain mesh.

### Depth projection

`DigTunnelProjection` использовал разные сокращённые размеры:

- `DepthSpacing = -0.55`;
- `FloorDepth = 0.45`;
- `RockCellHalfExtent = 0.48`;
- terrain mesh отдельно строил Z0 глубиной `0.82`, а Z1-Z3 — по сокращённому deep spacing;
- invisible dig/movement proxies использовали отдельный hardcoded depth `0.50`.

Из-за этого logical Z-cells не имели одного визуального объёма и между слоями могли оставаться видимыми внутренние поверхности.

### Eating presentation

Domain/Application уже публиковали authoritative `ActiveIntent = Eat` и action progress, но resident visual presenter не имел typed `Eat` state. Rig и right-hand visual поэтому оставались в обычном idle/equipment presentation.

### Tent orientation

Direct side-view camera находится на положительной world-Z стороне. Authored entrance flap representative tent profile находился на отрицательной local-Z стороне, а runtime дополнительно применял gameplay orientation к визуалу.

### Unmineable depth

World generation и demo terrain overlay применяли материал к exact XYZ cells независимо. Не существовало column invariant, которое после генерации распространяет solid non-mineable Z0 material на Z1-Z3.

## Implemented correction

### Open cells, terrain and depth

- open/non-solid `DigCellVisual` всегда имеет нулевой scale и disabled renderer;
- `ApplyTunnelCutaway` активирует только solid cell visuals; walk-surface membership больше не создаёт render geometry;
- бирюзовый empty-cell color удалён и заменён `Color.clear` как дополнительная защита;
- `_walkSurfaceCells` сохраняется только как derived interaction/query state;
- invisible dig, movement и cave-room proxies используют общий `InteractionDepth` и не имеют renderer;
- `DepthOrigin = 0.50`, `DepthSpacing = -1.00`;
- каждый Z0-Z3 slice имеет глубину `1.00` и общую boundary plane с соседом;
- Z0 special-case удалён из `ResolveDepthExtents`;
- floor/support depth также равен `1.00`;
- resident/building positions продолжают использовать общий `DigTunnelProjection`.

### Unmineable columns

`WorldGenerator` после material/resource layout проверяет Z0 каждой `(X,Y)` колонки. Если material solid и `IsMineable == false`, тот же material записывается в Z1-Z3 до создания authoritative `WorldState`, deposit generation и fingerprint.

Demo unmineable patch также охватывает весь `world.Size.Depth`.

### Eating

- добавлен typed `ResidentActionVisualState.Eat`;
- presenter проецирует authoritative active Eat и action progress;
- rig опускается на землю, сгибает ноги и циклически подносит правую руку ко рту;
- right hand показывает collider-free synthetic committed meal portion;
- meal visual имеет приоритет только во время Eat и после очистки восстанавливает фактическое equipment visual;
- Presentation не применяет Nutrition и не создаёт Inventory state.

### Tent

- representative и catalog tent profiles помечаются `FacesCamera`;
- authored model развёрнут на `180°`, чтобы flap находился на положительной Z-стороне;
- camera-facing profile не наследует gameplay yaw в `DigBuildingVisual`;
- visual bounds `3×2×2` и authoritative logical footprint не изменены.

## Regression coverage

- `FullDepthEatingTentBehaviorTests`:
  - проверяет complete unmineable depth columns;
  - проверяет typed looping Eat и authoritative progress;
- `FullDepthEatingTentSourceContractTests`:
  - фиксирует full-depth constants и отсутствие Z0 special-case;
  - запрещает cyan empty-cell color, walk-surface render branch и open-cell renderer;
  - требует shared interaction depth, meal pose/mesh, camera-facing tent metadata и all-Z demo patch;
- `FullDepthEatingTentPlayModeTests` проверяет actual private depth extents, disabled renderer/zero scale open cell, meal geometry/colliders, seated rig pose и положительную Z-сторону tent entrance;
- существующие deep seam, terrain, building, resident, item, save/overlay и deterministic tests остаются включены в полный suite.

## Validation evidence

До удаления последнего open-cell render path кодовый head `9e9de051b0c13223208af5d29bdf305e151314c5` уже прошёл:

- architecture, file-size, C# compatibility и все Unity source/presentation contracts;
- Release build с `0` warnings и `0` errors;
- full .NET suite `1494/1494`;
- headless smoke, standard и large deterministic replay;
- Stage 2 v2/v3 exports.

Authoritative final exact-head evidence после open-cell correction фиксируется в PR #627 и issue #626; этот implementation note входит в тот же финальный CI head.

Unity activation остаётся недоступной. Actual EditMode/PlayMode execution и executed-evidence validation не считаются выполненными, поэтому статус остаётся `IMPLEMENTED`, а не `VERIFIED`.

## Remaining evidence

Для `VERIFIED` требуется лицензированный Unity run с выполненными `FullDepthEatingTentPlayModeTests` и существующим runtime suite, XML result и Console/evidence validation.
