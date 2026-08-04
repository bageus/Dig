# Embedded terrain deposit relief

Дата: 2026-08-04.  
Статус: `IMPLEMENTED`.

Authoritative specification: [`../design/embedded-terrain-deposit-relief.md`](../design/embedded-terrain-deposit-relief.md).  
Tracking issue: [#617](https://github.com/bageus/Dig/issues/617).  
Implementation PR: [#618](https://github.com/bageus/Dig/pull/618).

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

Code head `dd734f7925e4b83de8f970432cc92617cf572d41` прошёл:

- architecture, file-size и C# compatibility checks;
- все Unity source/presentation contracts, включая terrain deposit и accessibility/LOD;
- Release build: `0` warnings, `0` errors;
- полный .NET suite: `1480/1480` passed;
- headless smoke: tick `20`;
- standard deterministic soak: replay `true`, hash `B26EA859F3F9668DF85CA1BA2842D8C733B09C51B596F4300549AEE7465D5292`;
- large deterministic soak: replay `true`, hash `7FD411B4725F7DADC5D355FEC5FB5159D59314CB25921394D9D8B27669EC51C9`;
- Stage 2 v2/v3 exports.

Unity workflow завершился только с blocked evidence: activation unavailable, поэтому `Run Unity EditMode and PlayMode tests` и runtime-evidence validation были skipped. Checked-in geometry regression фактически не исполнялся Unity Test Runner; статус остаётся `IMPLEMENTED`, не `VERIFIED`.

После final evidence commits требуется повторный exact-head CI; результаты фиксируются в PR #618 и issue #617.
