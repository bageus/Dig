# Excavation cadence profiles — 2026-07-29

Status: `IMPLEMENTED`; licensed Unity Play Mode evidence pending.

Authoritative specification: [`../design/excavation-cadence-profiles.md`](../design/excavation-cadence-profiles.md).
Tracking: [#388](https://github.com/bageus/Dig/issues/388).

## Correction

The runtime previously had two cadence owners: regular excavation used `tick % 3`, while spatial excavation used a separate constant. The Domain quarter planner also randomized required swings and high skill could complete multiple quarters from one action. Equipment mining intervals did not participate in authoritative quarter commit, and the full skill profile was granted again only at job finalization.

The implementation now:

- resolves one fixed-tick interval from hardness, Stonework band, equipment interval and posture;
- routes ordinary, direct/manual and spatial work through the same resolver;
- completes exactly one reserved quarter per due action;
- commits terrain and the quarter skill share through `CommitExcavationQuarterCommandHandler`;
- suppresses grants for idempotent no-change commits;
- removes full/partial finalization grants to prevent duplication;
- retains only World-owned quarter progress across cancel/retry/save/load;
- exposes a typed non-authoritative flavor cue, disabled by current balance data.

## Main files

- `src/Dig.Application/Jobs/ExcavationCadence.cs`;
- `src/Dig.Application/Jobs/ExcavationQuarterSkillGrantResolver.cs`;
- `src/Dig.Application/Jobs/CommitExcavationQuarter.cs`;
- `src/Dig.Domain/World/ExcavationQuarterPlanner.cs`;
- `src/Dig.Domain/World/ExcavationWorkCoordinator.cs`;
- `unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigTerrainWorkExcavationQuarters.cs`;
- `unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigTerrainWorkExcavationCadence.cs`;
- `unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigTerrainSpatialExcavation.cs`.

## Regression coverage

Unit tests cover skill bands, hardness/tool/posture composition, fixed due ticks, one-quarter work, concurrent reservations, exact remainder allocation, missing recipient validation, duplicate idempotency and exact four-quarter deposit profiles. Source contracts prevent reintroducing `tick % 3`, a spatial cadence constant or finalization skill grants. `ExcavationCadenceProfilesPlayModeTests` is the executable Unity fixture.

## Verification boundary

Quality build/tests, smoke and deterministic soaks are required before merge. The status remains `IMPLEMENTED`, not `VERIFIED`, until the licensed Unity Test Runner actually executes the Play Mode fixture and publishes results.
