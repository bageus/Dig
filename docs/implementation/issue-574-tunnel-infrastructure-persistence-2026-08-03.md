# Issue 574 — persistence инфраструктуры тоннелей

Статус: `READY FOR REVIEW` в stacked PR #592.

Authoritative specification: [`../design/room-purposes-upgrades-and-tunnel-reinforcement.md`](../design/room-purposes-upgrades-and-tunnel-reinforcement.md).  
Tracking issue: [#574](https://github.com/bageus/Dig/issues/574).  
Implementation plan: [`issue-574-room-infrastructure-implementation-plan-2026-08-03.md`](issue-574-room-infrastructure-implementation-plan-2026-08-03.md).

## Scope

Slice 3 сохраняет уже подтверждённое authoritative состояние `TunnelInfrastructureState` и runtime sequence автоматических работ. Он не добавляет player cancellation и не выбирает ответы для `Q-TUNNEL-008` либо других открытых questionnaire-вопросов.

## Владение состоянием

- `TunnelInfrastructureState` остаётся единственным владельцем segments, anchors, completed trim и derived targets.
- `JobSystem` остаётся владельцем automatic-work jobs и reservations.
- save data хранит snapshot и integrity evidence, но не становится вторым gameplay owner.
- Unity restore пересоздаёт Application handlers вокруг восстановленного Domain aggregate.

## Save contract v15

`TunnelInfrastructureSaveData` сохраняет:

- aggregate version;
- stable segment id, origin kind и origin XYZ;
- exact ordered horizontal cells;
- segment version;
- structural anchor kind, XYZ и distance from origin;
- current automatic-support target identity как integrity evidence;
- completed junction stone-trim XYZ cells;
- pending junction stone-trim target identity как integrity evidence;
- next automatic-work job sequence.

Automatic job definitions продолжают сохраняться существующим codec `job.tunnel_automatic_work.v1` в `JobsSaveData`.

## Load и migration

При load:

1. jobs восстанавливаются существующим `JobSystem` loader;
2. save adapter валидирует sequence против уже восстановленных automatic jobs;
3. `TunnelInfrastructureState.Restore` пересоздаёт authoritative aggregate;
4. Domain повторно выводит automatic-support и pending-trim targets;
5. сохранённые target identities сравниваются с derived state;
6. stale или obsolete target отклоняет документ до публикации runtime state.

Migration `save.v14_to_v15.tunnel_infrastructure`:

- создаёт пустой infrastructure section;
- не выводит anchors или segments из произвольного открытого terrain;
- не создаёт phantom support/trim targets;
- поднимает next sequence выше существующих parseable tunnel automatic-work job ids.

## Runtime composition

`DigTerrainWorkSession`:

- captures `TunnelInfrastructureSnapshot` и `_tunnelAutomaticJobSequence`;
- restores snapshot через Domain validation;
- пересоздаёт topology/support/trim/completion handlers;
- restores sequence без повторного использования id;
- публикует completed infrastructure visuals после restore.

`SaveGameService.Autosave` переносит тот же runtime snapshot, поэтому manual save и autosave используют один контракт.

## Regression coverage

Добавлены проверки:

- aggregate round trip сохраняет anchors, completed/pending trim, versions и sequence;
- obsolete saved support target отклоняется;
- sequence collision с существующим automatic job отклоняется;
- v14 migration не создаёт anchors и поднимает sequence;
- full builder → JSON codec → loader → rebuild сохраняет идентичные bytes;
- Unity capture/restore boundary использует authoritative snapshot и republishes visuals;
- существующие migration chains доходят до v15 и остаются idempotent.

## Validation

Code head: `2ff8fef77749c983e5ae56595ccdc823af3dc4db`.

Quality workflow `30808003569`:

- architecture, file-size, C# 9 compatibility, compiler baseline, dependency и Domain-boundary checks passed;
- Unity source contracts passed;
- Release build: `0` warnings, `0` errors;
- full .NET suite: `1436/1436`;
- headless smoke passed at tick `20`;
- standard deterministic soak replay hash `84DF20CCAE6B6CD42CB9B3B07415D468D45E117F8F3B6A1A675DA0A329CB3479`;
- large deterministic soak with 64 residents replay hash `28CF96B7C7F7FC12CD859AB20E837FAC091FA3FF7B6F20E1B693AA340A303F0C`.

Unity workflow `30808003764` recorded blocked evidence. Actual EditMode/PlayMode execution and executed-runtime-evidence validation were skipped because activation was unavailable. Runtime verification is not claimed.

## Deliberately remaining

- actual licensed Unity Play Mode end-to-end evidence;
- player cancellation until `Q-TUNNEL-008` is answered;
- Slice 4 room-upgrade core.
