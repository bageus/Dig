# Vuker reproduction implementation — 2026-08-02

Status: `IMPLEMENTED IN BRANCH`; merge and licensed Unity evidence pending.

Tracking: [#569](https://github.com/bageus/Dig/issues/569), parent [#149](https://github.com/bageus/Dig/issues/149), implementation [PR #576](https://github.com/bageus/Dig/pull/576).

Authoritative design: [`../design/vuker-reproduction-questionnaire.md`](../design/vuker-reproduction-questionnaire.md).

## Реализация

- `VukerEcologyState` владеет deterministic individuals/pairs, 7-day cadence, 3-cycle budget, 3-day growth, cap 10 per connected region, blocked retry, kidnap reservation и tamed state.
- `VukerCaveRegionResolver` строит components по supported walk, vertical climb и depth traversal; world cap отсутствует.
- Unity session synchronizes actor state, creates due children, projects Child/Tamed lifecycle, prevents child/tamed combat, runs kidnap approach, direct movement and automatic resident-dislocation return.
- Save format v14 stores Vuker ecology and migrates v13 with an empty section.
- Checked-in Play Mode executes birth → non-combat child → kidnap/tame → common tunnel movement → maturity.

## Проверки ветки

На implementation head `8cff5beec3048bf24967b652590a7fd4f2d1afc4` выполнены:

- architecture, file-size, C# compatibility и Unity source-contract gates — success;
- Release build — success, `0` warnings, `0` errors;
- full .NET suite — `1367/1367` passed;
- headless smoke — success;
- standard deterministic soak — success;
- large-settlement deterministic soak — success;
- Stage 2 v2/v3 source exports — success.

Unity workflow завершился через blocked-evidence path: activation была недоступна, поэтому actual EditMode/PlayMode Test Runner был skipped. Checked-in Play Mode fixture не повышает систему до `VERIFIED` без фактического лицензированного запуска.

## Verification boundary

После merge система получает status `IMPLEMENTED`. Status `VERIFIED` требует actual Unity Test Runner evidence для birth → child patrol/no-combat → Alt+LMB kidnap → tame/direct movement/auto-return → maturity и повторного reproduction lifecycle.
