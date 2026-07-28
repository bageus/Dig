# Resident inventory actions

Статус реализации: `IMPLEMENTED`, Unity Play Mode verification pending.

Authoritative design: [`../design/runtime-selection-excavation-item-placement-decisions.md`](../design/runtime-selection-excavation-item-placement-decisions.md), [`../design/resident-inventory-expansion.md`](../design/resident-inventory-expansion.md).

Tracking: [#64](https://github.com/bageus/Dig/issues/64), [#67](https://github.com/bageus/Dig/issues/67), [#70](https://github.com/bageus/Dig/issues/70), [#387](https://github.com/bageus/Dig/issues/387), [#390](https://github.com/bageus/Dig/issues/390), [#459](https://github.com/bageus/Dig/issues/459), [PR #479](https://github.com/bageus/Dig/pull/479).

## Input routing

Selected-resident HUD читает authoritative resident inventory layout и маршрутизирует действия через `ContextInputRouter` на поверхности `ResidentInventory`.

- обычный ЛКМ по BuildingBox сохраняет отдельный unpack/building-placement workflow;
- обычный ЛКМ по доступному generic/material/food stack включает world-space item ghost;
- `Alt + ЛКМ` имеет приоритет и отправляет typed use action;
- `D + ЛКМ` по non-BuildingBox stack отправляет immediate `DropInventoryStack` в current logical resident cell;
- double click и RMB больше не выполняют quick drop;
- hover с `D` показывает анимированную стрелку вниз; hover consumable с `Alt` показывает анимированный рот.

## Resident-bound targeted placement

`CreateResidentInventoryPlacementHandler` повторно читает authoritative Inventory и World и создаёт `ResidentInventoryPlacementJobDefinition` только когда:

- exact stack принадлежит выбранному resident;
- вся requested quantity доступна, не held и не reserved;
- item не является spill-aware inventory expansion;
- destination — explored open cell с walkable support и входит в reachable set.

Stack остаётся в исходном slot. Job резервирует exact quantity и destination и содержит stages `TravelToDestination -> DepositItem`. Первый job сразу claim-ится только выбранным resident. Следующие незавершённые placement jobs того же resident получают dependency на предыдущий job и активируются `ResidentInventoryPlacementQueue` строго в creation order.

`CompleteResidentInventoryPlacementHandler` повторно проверяет resident binding, destination и exact reservation, затем атомарно переносит reserved quantity в `ItemLocation.InWorld(destination)` и завершает job. Blocked route/target использует typed blocked/retry path. Cancel/failure освобождает quantity reservation; failed predecessor отменяет dependents с явной причиной.

`ResidentInventoryPlacementJobSaveCodec` сохраняет resident id, stack id, quantity, destination, retry policy и dependency order. Runtime save composition должен регистрировать codec вместе с остальными job codecs при подключении общего save/load UI.

## Pickup и hover presentation

World pickup требует выбранного живого resident. Generic/material/food stack показывает анимированную стрелку вверх и отдельный interaction hover tint только для exact visual stack. BuildingBox получает такой cursor/highlight только при удержании `Alt`; обычный ЛКМ продолжает selection/unpack workflow.

World item collider остаётся raycastable, но не владеет Navigation occupancy. Успешный pickup использует существующий exact-stack pickup job и resident slot capacity guard.

## Food, potions и drinks

Food в inventory делегируется существующему `StartResidentFoodMealHandler`; Presentation не реализует nutrition/effect. Tool use сохраняет прежний held-item slot guard.

Категории `potion`, `drink` и `beverage` используют тот же Alt-hover/Alt+LMB contract. В текущем content нет authoritative potion/drink effect owner, поэтому action возвращает typed `inventory.consumable.effect_owner_unavailable` вместо скрытого consume или выдуманного эффекта.

## Unity compile boundary

Unity runtime использует публичный ownership contract `DropResidentInventoryStackHandler.IsOwnedByResident`; helper не должен становиться `internal`, потому что `Dig.Unity` и `Dig.Application` являются разными assemblies. Inventory validation повторно получает authoritative repository через общий `ResolveWorldItemRepository`, а HUD/session references в consumable command path фиксируются как non-null после initialization guard, чтобы Unity nullable compilation не оставляла `CS8602` warnings.

## Verification

Добавлены .NET regression tests для:

- `D + ЛКМ`, Alt priority, отсутствия double-click/RMB quick drop и BuildingBox priority;
- exact resident/stack/quantity reservation;
- deterministic dependency order;
- deposit без потери/дублирования quantity;
- placement job save codec;
- публичной Unity-доступности resident ownership policy и её location semantics.

Unity Play Mode остаётся обязательным для animated cursor, exact hover tint, ghost visibility/colour, multi-order resident execution, input shielding и повторного world pickup/drop workflow.
