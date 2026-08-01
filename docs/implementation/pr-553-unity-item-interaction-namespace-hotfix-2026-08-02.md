# PR #553 Unity item interaction namespace hotfix — 2026-08-02

Статус: `IMPLEMENTED` на ветке `fix/unity-playmode-item-interaction-namespace`; licensed Unity runtime evidence остаётся у [#511](https://github.com/bageus/Dig/issues/511).

Authoritative design: [`../design/item-interaction-capabilities.md`](../design/item-interaction-capabilities.md).

Связано с merged [PR #553](https://github.com/bageus/Dig/pull/553), merged runtime hotfix [PR #560](https://github.com/bageus/Dig/pull/560), tracking issues [#387](https://github.com/bageus/Dig/issues/387), [#390](https://github.com/bageus/Dig/issues/390) и runtime evidence [#511](https://github.com/bageus/Dig/issues/511).

## Наблюдаемый дефект

После merge PR #553 Unity не входил в Play Mode из-за namespace-resolution `CS0103`.

Сначала ошибки находились в runtime partial-файлах:

- `DigWorldInteraction.CanvasHud.cs` не разрешал `ItemInventoryInteractionAction`;
- `DigWorldInteraction.ResidentInventory.cs` не разрешал `ItemInventoryInteractionAction`.

После runtime hotfix Unity продолжил импорт и обнаружил тот же класс дефекта в PlayMode assembly:

- `BuildingBoxPlacementCursorPlayModeTests.cs` не разрешал `ItemInteractionProfiles`;
- `MushroomChoppingPlayModeTests.cs` не разрешал `ItemWorldInteractionAction`.

## Первопричина

Authoritative item-interaction types определены в `Dig.Domain.Inventory`, но изменённые Unity source-файлы использовали их без `using Dig.Domain.Inventory;`.

Обычный Release build и .NET tests не компилируют Unity assemblies, а licensed Unity Test Runner был недоступен. Первый regression contract дополнительно сканировал только верхний уровень `Runtime` и только два action enum, поэтому PlayMode tests и `ItemInteractionProfiles` остались вне проверки.

## Исправление

- runtime partial-файлы импортируют `Dig.Domain.Inventory` через PR #560;
- оба затронутых PlayMode test-файла импортируют тот же authoritative namespace;
- routing, modifier priority и interaction profiles не изменялись;
- repository regression contract рекурсивно сканирует весь `Assets/Dig.Unity` — Runtime, Editor и Tests;
- contract проверяет все публичные item-interaction types, используемые Unity source, а не только два action enum;
- полная квалификация `Dig.Domain.Inventory.*` остаётся допустимой.

## Проверка

Quality должен выполнить расширенный source-contract до Release build/tests. Headless smoke, deterministic soaks и Stage 2 exports проверяют отсутствие engine-independent regressions.

Фактический Unity compile/Play Mode считается подтверждённым только после запуска licensed Unity Test Runner либо ручного открытия обновлённого проекта без compiler errors. До сохранения такого evidence статус системы остаётся `IMPLEMENTED`, не `VERIFIED`.
