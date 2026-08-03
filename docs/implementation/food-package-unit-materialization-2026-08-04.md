# Food package unit materialization

Дата: 2026-08-04.  
Статус: `IN PROGRESS`.  
Authoritative specification: [`../design/building-production-and-internal-supply.md`](../design/building-production-and-internal-supply.md).  
Tracking issue: [#609](https://github.com/bageus/Dig/issues/609).

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

Заполняется после PR CI. Фактический Unity EditMode/PlayMode результат указывается отдельно; skipped activation не считается runtime evidence.
