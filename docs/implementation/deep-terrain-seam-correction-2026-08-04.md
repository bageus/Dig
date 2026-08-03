# Deep terrain seam correction

Дата: 2026-08-04.  
Статус: `IMPLEMENTED`.  
Authoritative specifications:

- [`../design/excavation-room-templates-and-deposits.md`](../design/excavation-room-templates-and-deposits.md);
- [`unity-visual-asset-pipeline.md`](unity-visual-asset-pipeline.md).

Tracking issue: [#613](https://github.com/bageus/Dig/issues/613).  
Implementation PR: [#614](https://github.com/bageus/Dig/pull/614).

## Reported symptom

В открытом горизонтальном тоннеле между соседними глубинными slices были видны повторяющиеся коричневые прямоугольные выступы. Они не являлись объектами, опорами, navigation markers или частью excavation plan.

Серые low-poly круги на стенке vertical shaft относятся к отдельной существующей системе revealed terrain deposits (`Nodule`/`Pebble`) и этим исправлением не изменяются.

## Root cause

`DigTerrainChunkMeshBuilder` размещал центры глубоких terrain layers с шагом `DigTunnelProjection.DepthSpacing`, но использовал `DepthLayerScale = 0.94f`. Поэтому каждый solid slice занимал только 94% расстояния до следующего центра. Оставшийся зазор показывал internal side faces как прямоугольные fins внутри выкопанного прохода.

## Correction

- deep terrain slice depth равен полному `abs(DepthSpacing)`;
- соседние Z1/Z2 и Z2/Z3 slices имеют одну общую boundary plane;
- X/Y half-cell extents и coplanar roughness из #267 не меняются;
- authoritative World/Navigation topology, interaction proxies, deposits и cave-template trim не меняются.

## Regression coverage

- `DeepTerrainSeamSourceContractTests` запрещает возврат `DepthLayerScale = 0.94f` и требует full scale `1f`;
- `DeepTerrainLayerSeamPlayModeTests` вызывает actual Unity geometry resolver и проверяет общую boundary для Z1/Z2 и Z2/Z3, а также exact slice width.

## Validation

CI на code head `594a7d723fd10d280239f1a3b19752a450c82793`:

- architecture, file-size, C# compatibility и все Unity source/presentation contracts: passed;
- Release build: `0` warnings, `0` errors;
- full .NET suite: `1474/1474` passed;
- headless smoke: completed at tick `20`;
- standard deterministic soak: replay matched, hash `B26EA859F3F9668DF85CA1BA2842D8C733B09C51B596F4300549AEE7465D5292`;
- large deterministic soak: replay matched, hash `7FD411B4725F7DADC5D355FEC5FB5159D59314CB25921394D9D8B27669EC51C9`;
- Stage 2 v2/v3 source exports: passed.

Unity workflow записал blocked runtime evidence: `Run Unity EditMode and PlayMode tests` и validation executed evidence были skipped из-за недоступной licensed activation. Поэтому correction имеет статус `IMPLEMENTED`, но не `VERIFIED`.
