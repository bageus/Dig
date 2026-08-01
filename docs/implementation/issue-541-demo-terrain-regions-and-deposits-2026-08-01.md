# Issue #541 — demo terrain regions and restored deposits

Status: `IMPLEMENTED` after merge; licensed Unity Play Mode execution remains required for `VERIFIED`.

Authoritative design: [`../design/terrain-resource-output-and-processing.md`](../design/terrain-resource-output-and-processing.md).

Tracking: [#541](https://github.com/bageus/Dig/issues/541), parent [#87](https://github.com/bageus/Dig/issues/87).

## Root cause

The demo world moved from legacy `demo.rock` to typed `terrain.*` materials, but the Unity demo deposit catalog still allowed only `demo.rock` as a host. `TerrainDepositGenerator` therefore had no compatible host cells and produced an empty deposit state.

## Delivered

- demo deposit definitions now allow current stone, metal-bearing, crystalline and lava rock hosts;
- demo composition creates deterministic contiguous X-axis regions for sand, stone, metal-bearing, crystalline and lava terrain without replacing existing open cells;
- a small deterministic unmineable patch is included for rejection/preview testing;
- ordinary terrain output entries are non-guaranteed and capped at `100‰` per independent entry in current fixtures;
- changed ordinary profiles use version 2;
- deposit output remains guaranteed, exclusive and exactly-once through the existing mining-output transaction;
- Domain, source-contract and checked-in Play Mode regressions cover low-chance rolls, all demo terrain regions and non-empty demo deposits.

## Follow-up startup regression — PR #546

The first local Unity execution after the mixed-terrain change exposed an ordering bug. `ApplyDemoTerrainTestRegions` correctly created the intentional `terrain.unmineable` test patch, but `InitializeDemoDeposits` then converted every solid non-protected cell into a `TerrainDepositHostCell`. `TerrainDepositGenerator` correctly rejected the first unmineable candidate, `(1,8,0)`, and demo startup stopped during world creation.

PR #546 filters candidates by the resolved material contract before calling the Domain generator: only `MaterialDefinition.IsSolid && MaterialDefinition.IsMineable` cells are supplied. The unmineable patch remains present for excavation rejection testing, but it can never host a deposit. The checked-in Play Mode regression creates the full demo world, requires non-empty deposits and verifies every resulting deposit host is solid, mineable and not `terrain.unmineable`. A .NET source contract locks the pre-generation filter because licensed Unity execution may be unavailable in CI.

## Balance boundary

The `100‰` cap is the confirmed qualitative implementation boundary for current fixtures, not final tuning. Exact probabilities, quantities, terrain distribution and deposit density remain Q-014 data-driven balance.

## Validation boundary

Local quality and Unity source-contract scripts pass. CI build, .NET tests, smoke and deterministic soaks are recorded on the linked PR. A successful workflow with a skipped licensed Unity Test Runner does not promote the system to `VERIFIED`.
