# Runtime pickup, combat VFX и Vuker correction

Дата: 2026-08-05.  
Статус: `IMPLEMENTED`.

Authoritative specification:
[`../design/runtime-pickup-combat-vuker-correction-2026-08-05.md`](../design/runtime-pickup-combat-vuker-correction-2026-08-05.md).  
Tracking issue: [#644](https://github.com/bageus/Dig/issues/644).  
Pull request: [#645](https://github.com/bageus/Dig/pull/645).

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

## Exact-head evidence

Code, design и system-index head `8a85a2cd0e6609b221472367822f04fa604ce62a`:

- Quality run `30985861905`: success;
- Release build: `0` warnings, `0` errors;
- full .NET suite: `1516/1516` passed;
- headless smoke: success at tick `20`;
- standard deterministic soak: hash
  `B26EA859F3F9668DF85CA1BA2842D8C733B09C51B596F4300549AEE7465D5292`, replay matched;
- large deterministic soak: hash
  `7FD411B4725F7DADC5D355FEC5FB5159D59314CB25921394D9D8B27669EC51C9`, replay matched;
- Stage 2 v2 run `30985862172`: success;
- Stage 2 v3 run `30985861843`: success;
- Unity workflow `30985861886` recorded blocked evidence: actual EditMode/PlayMode
  execution was skipped because licensed activation was unavailable.

The final implementation-note-only commit must pass the same exact-head gates before merge.

## Verification boundary

The checked-in Play Mode regressions were not executed. Status remains `IMPLEMENTED`,
not `VERIFIED`, until a licensed Unity Test Runner run proves the full runtime workflows.
