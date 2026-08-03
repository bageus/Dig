# Food package unit materialization

Дата: 2026-08-04.  
Статус: `IMPLEMENTED`.  
Authoritative specification: [`../design/building-production-and-internal-supply.md`](../design/building-production-and-internal-supply.md).  
Tracking issue: [#609](https://github.com/bageus/Dig/issues/609).  
Implementation PR: [#611](https://github.com/bageus/Dig/pull/611).

## Изменение

- `ProductionPackageMaterialization` вычисляет authoritative число выходных world stacks.
- Для `ProductionOutputPackageKind.Food` число stack IDs равно сумме quantities manifest.
- Каждая food quantity создаёт отдельный `ItemStackCreation` с quantity `1`.
- Для `Weapon` и `Tool` сохраняется прежняя семантика: один stack на manifest entry с исходной quantity.
- `CompleteProductionPackageUseHandler` валидирует expanded output-ID count до atomic replacement.
- Unity package-use runtime запрашивает stable ID на каждую materialized food unit.
- Package removal, former world cell и exactly-once/stale retry semantics не изменены.

## Regression coverage

- `food.grilled_mushroom x2` создаёт два distinct quantity-one world stacks в бывшей package cell;
- package metadata и package inventory stack удаляются exactly once;
- повторный stale completion не создаёт дополнительный output;
- Unity source contract требует authoritative expanded output count.

## Validation

PR #611 Quality run `30860544464`:

- architecture, file-size, C# compatibility и source-contract gates: passed;
- Release build: `0` warnings, `0` errors;
- full .NET suite: `1473/1473` passed;
- headless smoke: completed at tick `20`;
- standard deterministic soak: replay matched, hash `B26EA859F3F9668DF85CA1BA2842D8C733B09C51B596F4300549AEE7465D5292`;
- large deterministic soak: replay matched, hash `7FD411B4725F7DADC5D355FEC5FB5159D59314CB25921394D9D8B27669EC51C9`;
- Stage 2 v2/v3 source exports: passed.

Unity workflow `30860544539` не выполнил фактические EditMode/PlayMode tests из-за недоступной activation: runtime steps были skipped, а workflow записал blocked evidence. Поэтому изменение имеет статус `IMPLEMENTED`, но не `VERIFIED`.
