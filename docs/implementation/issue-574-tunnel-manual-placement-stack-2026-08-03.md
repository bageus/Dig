# Issue 574 — owner-locked manual tunnel placement

Статус: `IMPLEMENTED` в PR [#600](https://github.com/bageus/Dig/pull/600).

Authoritative specification: [room-purposes-upgrades-and-tunnel-reinforcement.md](../design/room-purposes-upgrades-and-tunnel-reinforcement.md)

Tracking issue: [#574](https://github.com/bageus/Dig/issues/574)

## Реализованный scope

- ручная установка тоннельной инфраструктуры закреплена за выбранным resident и точным stack из его inventory slot;
- деревянная опора может быть поставлена в любой валидной клетке горизонтального сегмента;
- completed wooden support становится новой rolling structural anchor и отменяет устаревшую automatic-support job с освобождением reservation;
- stone floor trim и junction stone trim остаются декоративными и не дают structural protection;
- отмена или прямое прерывание manual job сохраняет тот же source stack у владельца и освобождает reservation;
- успешное завершение потребляет ровно одну зарезервированную единицу и выдаёт Woodworking или Stonework progression;
- manual job definition, tunnel state и decorative trim проходят save/load через формат v17 и migration v16→v17;
- Unity runtime подключает owner-locked preview, confirmation, input shielding, execution и presentation refresh без второго источника истины.

## Исправленная первопричина

`CompleteTunnelManualWorkHandler` больше не проверяет зарезервированную этим же job единицу как свободно доступную. На finalize handler подтверждает точный stack, resident ownership, resident slot и reservation текущего job, после чего вызывает `ConsumeReserved`. Это устраняет ложный `tunnel.manual.source_unavailable` при корректном полном workflow.

## Regression coverage

- support at cell 5 shifts the next automatic target to cell 15 and cancels the obsolete automatic job;
- direct interruption keeps the exact resident stack and releases its reservation;
- stone floor trim is decorative and grants Stonework;
- invalid targets create neither a job nor a reservation;
- legacy migration fixtures include and verify the ordered, idempotent v16→v17 step.

## Выполненные проверки

- Release build: `0` warnings, `0` errors;
- .NET tests: `1467/1467` passed;
- headless smoke completed at tick `20`;
- standard deterministic soak replay hash: `84DF20CCAE6B6CD42CB9B3B07415D468D45E117F8F3B6A1A675DA0A329CB3479`;
- large deterministic soak replay hash: `28CF96B7C7F7FC12CD859AB20E837FAC091FA3FF7B6F20E1B693AA340A303F0C`;
- architecture, file-size, C# compatibility, Unity source-contract and runtime-evidence tooling checks passed.

Unity workflow recorded blocked runtime evidence because activation was unavailable. Actual Unity EditMode and PlayMode tests were skipped, therefore this slice is not marked `VERIFIED` and no runtime-pass claim is made.

## Открытые решения

PR #600 не меняет и не закрывает questionnaire blockers `Q-ROOM-003`, `Q-ROOM-007`, `Q-TUNNEL-006A` и `Q-TUNNEL-008`; они остаются в authoritative specification и issue #574.
