# Deep terrain seam correction

Дата: 2026-08-04.  
Статус: `IN PROGRESS`.  
Authoritative specifications:

- [`../design/excavation-room-templates-and-deposits.md`](../design/excavation-room-templates-and-deposits.md);
- [`unity-visual-asset-pipeline.md`](unity-visual-asset-pipeline.md).

Tracking issue: [#613](https://github.com/bageus/Dig/issues/613).

## Reported symptom

В открытом горизонтальном тоннеле между соседними глубинными slices видны повторяющиеся коричневые прямоугольные выступы. Они не являются объектами, опорами, navigation markers или частью excavation plan.

Серые low-poly круги на стенке vertical shaft относятся к отдельной существующей системе revealed terrain deposits (`Nodule`/`Pebble`) и этим исправлением не изменяются.

## Root cause

`DigTerrainChunkMeshBuilder` размещает центры глубоких terrain layers с шагом `DigTunnelProjection.DepthSpacing`, но использовал `DepthLayerScale = 0.94f`. Поэтому каждый solid slice занимал только 94% расстояния до следующего центра. Оставшийся зазор показывал internal side faces как прямоугольные fins внутри выкопанного прохода.

## Correction

- deep terrain slice depth равен полному `abs(DepthSpacing)`;
- соседние Z1/Z2 и Z2/Z3 slices имеют одну общую boundary plane;
- X/Y half-cell extents и coplanar roughness из #267 не меняются;
- authoritative World/Navigation topology, interaction proxies, deposits и cave-template trim не меняются.

## Regression coverage

- `DeepTerrainSeamSourceContractTests` запрещает возврат `DepthLayerScale = 0.94f` и требует full scale `1f`;
- `DeepTerrainLayerSeamPlayModeTests` вызывает actual Unity geometry resolver и проверяет общую boundary для Z1/Z2 и Z2/Z3, а также exact slice width.

## Validation

Заполняется после CI на точном PR head. Реальное Unity EditMode/PlayMode execution указывается отдельно; blocked activation не считается runtime evidence.
