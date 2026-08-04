# Issue 574 — восстановление tunnel visual projection в mainline

Статус: `IMPLEMENTED IN BRANCH`.

Authoritative specifications:

- [`../design/service-markers-and-tunnel-overwrite-correction.md`](../design/service-markers-and-tunnel-overwrite-correction.md);
- [`../design/room-purposes-upgrades-and-tunnel-reinforcement.md`](../design/room-purposes-upgrades-and-tunnel-reinforcement.md).

Tracking issue: [#574](https://github.com/bageus/Dig/issues/574).

## Runtime report

После merge PR #628 и correction PR #631 Unity compilation остановилась в `DigTerrainTunnelInfrastructure.cs` с тремя `CS0246` на:

- `TunnelInfrastructureVisualPresenter`;
- `TunnelInfrastructureVisualVolumeViewModel` в поле sink;
- `TunnelInfrastructureVisualVolumeViewModel` в bind method.

## Root cause

`DigTerrainTunnelInfrastructure.cs` получил ссылки на completed-infrastructure visual projection, но authoritative implementation slice PR #591 был ранее merged только в stacked branch. Его Presentation owner, Unity renderer, WorldRenderer partial и driver sink binding не попали в `main`.

Обычный Release build не компилирует Unity Runtime assembly, а существующий source contract проверял строки в `DigTerrainTunnelInfrastructure.cs`, но не требовал физического наличия всего visual projection slice. Поэтому разрыв прошёл CI и проявился только при локальной Unity compilation.

## Correction

В mainline correction branch перенесён полный необходимый slice:

- `TunnelInfrastructureVisualPresenter` и immutable visual view models;
- `DigTunnelInfrastructureRenderer` с collider-free support/trim geometry;
- `DigWorldRenderer.TunnelInfrastructure` как presentation sink;
- binding `DigTerrainWorkSession -> DigWorldRenderer` при driver initialization;
- deterministic presenter regressions;
- Unity source contract на полный publication workflow;
- checked-in Play Mode regression на creation, XYZ placement, collider absence и removal.

Gameplay rules не меняются: automatic `WoodenSupport` и `JunctionStoneTrim` jobs, range 30, reservations, completion и невидимость job markers остаются как утверждено.

## Verification boundary

Quality, Release build, full .NET suite, headless smoke, deterministic soaks и exports должны пройти на final correction head. Реальный Unity compile/EditMode/PlayMode считается подтверждённым только после фактического запуска лицензированного Unity Test Runner либо локальной повторной компиляции проекта без Console errors.
