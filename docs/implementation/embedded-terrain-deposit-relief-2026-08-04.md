# Embedded terrain deposit relief

Дата: 2026-08-04.  
Статус: `IN PROGRESS`.

Authoritative specification: [`../design/embedded-terrain-deposit-relief.md`](../design/embedded-terrain-deposit-relief.md).  
Tracking issue: [#617](https://github.com/bageus/Dig/issues/617).

## Reported problem

Раскрытые залежи отображались как отдельные low-poly markers, лежащие поверх стены. Особенно заметными были круглые Nodule/Pebble silhouettes в vertical shaft.

## Confirmed correction

- залежь остаётся видимой и различимой по форме;
- cluster origin смещён внутрь host-rock plane на `0.030` world units;
- наружный relief ограничен максимумом `0.032` world units;
- connector relief ограничен `0.004` world units;
- базовый footprint всех silhouettes уменьшен;
- Plate больше не является отдельным flat quad: он строится как широкий низкий embedded relief;
- Nodule, Crystal, Seam и Pebble сохраняют distinct silhouettes, но большая часть geometry остаётся внутри стены;
- правило одинаково работает для walls, floor, ceiling и depth-facing faces.

## Ownership boundary

Изменение затрагивает только `DigTerrainChunkMeshBuilder`.

Не изменяются:

- `WorldState` и `TerrainDepositState`;
- reveal/depletion lifecycle;
- generation;
- mining targets и work effort;
- terrain topology и Navigation;
- colliders/picking;
- output, Inventory и hauling;
- save/load/migrations;
- LOD state ownership.

## Code

- `DigTerrainChunkMeshBuilder.DepositDecorations.cs` — embedded center, common inset/relief constants и clamp;
- `DigTerrainChunkMeshBuilder.DepositDecorationGeometry.cs` — reduced low-relief profiles для пяти shape families;
- `DigTerrainChunkMeshBuilder.DepositDecorationConnectors.cs` — flush connector placement.

## Regression coverage

- `EmbeddedTerrainDepositReliefSourceContractTests` запрещает прежние raised-marker offsets/heights и требует embedded constants/clamp;
- `EmbeddedTerrainDepositReliefPlayModeTests` вызывает фактические private mesh builders через reflection и проверяет для всех пяти shapes:
  - наличие vertices внутри host plane;
  - наличие небольшого видимого relief;
  - отсутствие выхода за общий relief budget;
  - exact flush connector relief.

## Validation

Заполняется после CI на точном PR head. Licensed Unity EditMode/PlayMode execution учитывается отдельно; skipped activation не является runtime evidence.
