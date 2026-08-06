# ADR-0003: Authoritative SurfacePose with cell-based route corridors

- Статус: accepted
- Дата: 2026-08-06
- Владельцы: проект Dig
- Tracking: [#669](https://github.com/bageus/Dig/issues/669)

## Контекст

Jobs, excavation and topology use stable voxel cells, but cell-centre actor positions
cannot represent exact work points, continuous floor movement or wall climbing. Keeping
both a cell position and an independently mutable continuous transform would create two
sources of truth. Actor occupancy also cannot become a hard barrier because approved
shared-cell movement permits immediate visual overlap fallback.

## Рассмотренные варианты

1. Keep `CellId` as the only actor position and let Unity invent visual offsets.
2. Replace cell navigation and world topology with a fully continuous graph.
3. Keep cell routes as derived coarse corridors and make `SurfacePose` the single precise
   authoritative actor position.

## Решение

Use deterministic fixed-point `SurfacePose` as the authoritative precise actor position.
Its containing `CellId` remains the index used by World, Navigation, jobs and coarse route
planning. Movement confirms floor and vertical-face micro-steps through Domain/Application;
Unity only interpolates confirmed poses.

Legacy cell movement resolves to the floor centre. Saves persist face and local `U/V`
coordinates and migrate older actor positions to a floor-centre pose. Capability policy
controls which actor kinds may occupy vertical faces.

Actor proximity is not an authoritative occupancy barrier. Directional lanes, separation
and overlap offsets are derived presentation preferences. An occupied target pose does not
delay a confirmed movement step; direct opposite logical-cell swap remains governed by the
existing deterministic tunnel-transition rule.

## Последствия

### Положительные

- one authoritative actor position supports exact work, combat and climbing;
- cell-based topology and existing job contracts remain stable;
- save/load and deterministic simulation do not depend on Unity transforms;
- shared-cell and overlap behavior stays consistent with approved movement rules.

### Отрицательные

- route execution must translate coarse edges into several precise surface phases;
- every action must re-check its exact pose before authoritative mutation;
- presentation avoidance cannot stop simulation movement and may temporarily overlap.

## Verification

Domain, serialization and source-contract coverage may establish `IMPLEMENTED`. Status
`VERIFIED` additionally requires licensed Unity Play Mode evidence for excavation
invalidation, save/load, combat, narrow tunnels, multiple actors and vertical traversal.

## Критерии пересмотра

Revisit this decision if approved gameplay introduces true one-lane geometry, destructible
active traversal links, or a continuous world representation that replaces voxel topology.
