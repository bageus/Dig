# Issue 574 — восстановление tunnel visual projection в mainline

Статус: `IMPLEMENTED IN BRANCH`.

Authoritative specifications:

- [`../design/service-markers-and-tunnel-overwrite-correction.md`](../design/service-markers-and-tunnel-overwrite-correction.md);
- [`../design/room-purposes-upgrades-and-tunnel-reinforcement.md`](../design/room-purposes-upgrades-and-tunnel-reinforcement.md).

Tracking issue: [#574](https://github.com/bageus/Dig/issues/574).  
Correction PR: [#632](https://github.com/bageus/Dig/pull/632).

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

## Automated validation

На code head `9e3485251aeae8349d1a00e0f7b2071af97c3d78`:

- Quality run `30933802177` — success;
- architecture, file-size, C# compatibility и dependency gates — success;
- все Unity source/presentation/runtime contracts — success;
- Release restore/build — success;
- .NET suite — `1505/1505`;
- headless smoke — success;
- standard deterministic soak — success;
- large-settlement deterministic soak — success;
- Export Stage 2 v2 `30933803188` — success;
- Export Stage 2 v3 `30933802440` — success.

Unity workflow `30933802855` выполнил только blocked-evidence path: actual EditMode/PlayMode Test Runner и executed-runtime-evidence validation были skipped из-за недоступной licensed activation.

## Verification boundary

Реальный Unity compile/EditMode/PlayMode считается подтверждённым только после фактического запуска лицензированного Unity Test Runner либо локальной повторной компиляции проекта без Console errors. Автоматические source-contract и .NET checks не повышают runtime status до `VERIFIED`.
