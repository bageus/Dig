# PR #553 Unity item interaction namespace hotfix — 2026-08-02

Статус: `IMPLEMENTED` на ветке `fix/unity-item-interaction-namespace`; licensed Unity runtime evidence остаётся у [#511](https://github.com/bageus/Dig/issues/511).

Authoritative design: [`../design/item-interaction-capabilities.md`](../design/item-interaction-capabilities.md).

Связано с merged [PR #553](https://github.com/bageus/Dig/pull/553), tracking issues [#387](https://github.com/bageus/Dig/issues/387), [#390](https://github.com/bageus/Dig/issues/390) и runtime evidence [#511](https://github.com/bageus/Dig/issues/511).

## Наблюдаемый дефект

После merge PR #553 Unity не входил в Play Mode из-за двух `CS0103`:

- `DigWorldInteraction.CanvasHud.cs` не разрешал `ItemInventoryInteractionAction`;
- `DigWorldInteraction.ResidentInventory.cs` не разрешал `ItemInventoryInteractionAction`.

## Первопричина

`ItemInventoryInteractionAction` корректно определён в authoritative namespace `Dig.Domain.Inventory`, но оба Unity partial-файла использовали enum без `using Dig.Domain.Inventory;`.

Обычный Release build и .NET tests не компилируют Unity runtime assembly, а licensed Unity Test Runner был недоступен. Поэтому прежние source-contract gates не обнаружили отсутствующий namespace import.

## Исправление

- оба Unity partial-файла импортируют `Dig.Domain.Inventory`;
- логика routing, modifier priority и interaction profiles не изменялась;
- добавлен repository regression contract, который сканирует Unity runtime-файлы и требует namespace import либо полное имя для `ItemInventoryInteractionAction` и `ItemWorldInteractionAction`.

## Проверка

Repository Quality, Release build/tests, Unity source contracts, headless smoke и deterministic soaks должны пройти на hotfix PR.

Фактический Unity compile/Play Mode считается подтверждённым только после запуска licensed Unity Test Runner. До этого статус системы остаётся `IMPLEMENTED`, не `VERIFIED`.
