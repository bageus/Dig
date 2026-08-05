# Runtime pickup, combat VFX и Vuker correction

Дата: 2026-08-05.  
Статус: `IMPLEMENTED IN BRANCH`.

Authoritative specification:
[`../design/runtime-pickup-combat-vuker-correction-2026-08-05.md`](../design/runtime-pickup-combat-vuker-correction-2026-08-05.md).  
Tracking issue: [#644](https://github.com/bageus/Dig/issues/644).

## Root causes

- `PrepareResidentsForDirectCommand` не владел cancellation manual tunnel route;
- `DigPooledVfxInstance` интерпретировал world request как `localPosition`, а effect
  location map не включал combat-only enemies;
- Vuker family использовала unbounded scale `1.0`;
- `VukerCaveRegionResolver` индексировал только supported cells.

## Correction

- direct-command composition связывает terrain preparation с authoritative manual
  movement cancellation;
- pickup создаётся после cancellation старого route;
- combat enemy positions добавлены в effect location map;
- pooled effects устанавливают `transform.position`;
- Vuker family получает tunnel-fit scale `0.68`;
- ecology connectivity строится по all open navigation cells, а birth cells остаются
  supported-only.

## Regression coverage

- .NET unit/source contracts для resolver, direct-command binding, world VFX coordinates
  и Vuker scale;
- checked-in Play Mode для manual move replacement, vertical-cell ecology tick,
  translated-parent combat VFX и one-cell Vuker visual bounds.

## Verification boundary

Локально доступны Python quality/source contracts. Exact-head GitHub build/test/smoke/
soak evidence будет записан перед merge. Фактический Unity Test Runner требуется для
`VERIFIED`.
