# Vuker reproduction implementation — 2026-08-02

Status: `IMPLEMENTED IN BRANCH`; merge and licensed Unity evidence pending.

Tracking: [#569](https://github.com/bageus/Dig/issues/569), parent [#149](https://github.com/bageus/Dig/issues/149).

Authoritative design: [`../design/vuker-reproduction-questionnaire.md`](../design/vuker-reproduction-questionnaire.md).

## Реализация

- `VukerEcologyState` владеет deterministic individuals/pairs, 7-day cadence, 3-cycle budget, 3-day growth, cap 10 per connected region, blocked retry, kidnap reservation и tamed state.
- `VukerCaveRegionResolver` строит components по supported walk, vertical climb и depth traversal; world cap отсутствует.
- Unity session synchronizes actor state, creates due children, projects Child/Tamed lifecycle, prevents child/tamed combat, runs kidnap approach, direct movement and automatic resident-dislocation return.
- Save format v14 stores Vuker ecology and migrates v13 with an empty section.
- Checked-in Play Mode executes birth → non-combat child → kidnap/tame → common tunnel movement → maturity.

## Verification boundary

Domain/Application/save/source tests, Release build, smoke and deterministic soaks must pass on the final PR head. Actual Unity Test Runner evidence is required before status `VERIFIED`; a skipped license path is not runtime evidence.
