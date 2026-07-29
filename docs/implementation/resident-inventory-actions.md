# Resident inventory actions

Статус реализации: `IMPLEMENTED` in PR #501; Play Mode compile boundary and runtime placement verification are corrected in PR #504. The latest `C` quick-drop and blue placement-reservation correction is implemented on the follow-up branch; actual licensed Unity Play Mode verification remains pending.

Authoritative design: [`../design/runtime-selection-excavation-item-placement-decisions.md`](../design/runtime-selection-excavation-item-placement-decisions.md), [`../design/resident-inventory-expansion.md`](../design/resident-inventory-expansion.md).

Tracking: [#64](https://github.com/bageus/Dig/issues/64), [#67](https://github.com/bageus/Dig/issues/67), [#70](https://github.com/bageus/Dig/issues/70), [#387](https://github.com/bageus/Dig/issues/387), [#390](https://github.com/bageus/Dig/issues/390), [#459](https://github.com/bageus/Dig/issues/459), [PR #479](https://github.com/bageus/Dig/pull/479), [PR #480](https://github.com/bageus/Dig/pull/480), [PR #501](https://github.com/bageus/Dig/pull/501), [PR #504](https://github.com/bageus/Dig/pull/504).

## Input routing

Selected-resident HUD читает authoritative resident inventory layout и маршрутизирует действия через `ContextInputRouter` на поверхности `ResidentInventory`.

- обычный ЛКМ по BuildingBox сохраняет отдельный unpack/building-placement workflow;
- обычный ЛКМ по доступному generic/material/food stack включает полноценный world-space item placement mode;
- `Alt + ЛКМ` имеет приоритет и отправляет typed use action;
- `C + ЛКМ` по non-BuildingBox stack отправляет immediate `DropInventoryStack` в current logical resident cell;
- `D` больше не участвует в quick drop и остаётся правым направлением camera pan;
- double click и RMB больше не выполняют quick drop;
- hover с `C` показывает анимированную стрелку вниз; hover consumable с `Alt` показывает анимированный рот.

Camera pan сохраняет обе схемы: `A/D/S/W` и точные directional duplicates `Left/Right/Down/Up`.

## Input wiring regression — 2026-07-29

Application job, validation and ghost code from PR #479 existed, but the active HUD click path did not enter it:

- `DigGameHudCanvas` called `SelectResidentInventoryLayoutSlot` for ordinary LMB;
- that method only stored a selected stack and instructed the player to click the ground;
- the later world click emitted immediate `DropInventoryStack`, bypassing `BeginInventoryItemPlacement`, green/red ghost validation and `ResidentInventoryPlacementJob` creation.

The correction keeps one owner path:

- `SelectResidentInventoryLayoutSlot` delegates to `ActivateResidentInventoryLayoutSlot`;
- `ActivateResidentInventorySlot` invokes `BeginInventoryItemPlacement` for an available non-BuildingBox stack;
- the item mode now follows the BuildingBox presentation lifecycle: system cursor hidden while the transparent ghost is active, continuous world-space hover, green/red validity and RMB cancellation with cursor restoration;
- LMB on a valid world target calls `CreateResidentInventoryPlacement` and creates the resident-bound job;
- immediate movement remains exclusive to `C + ЛКМ`.

The executable Play Mode scenario boots the real demo runtime, inserts a generic material into a resident slot, invokes the actual HUD LMB handler, verifies the active ghost and hidden system cursor, cancels the preview, creates an authoritative placement reservation on a valid flat-supported cell and verifies the resulting blue inventory projection.

## Resident-bound targeted placement

`CreateResidentInventoryPlacementHandler` повторно читает authoritative Inventory и World и создаёт `ResidentInventoryPlacementJobDefinition` только когда:

- exact stack принадлежит выбранному resident;
- вся requested quantity доступна, не held и не reserved;
- item не является spill-aware inventory expansion;
- destination — explored reachable open cell с ровной walkable support surface.

Stack остаётся в исходном slot. Job резервирует exact quantity и destination и содержит stages `TravelToDestination -> DepositItem`. Пока reservation существует, slot остаётся видимым, использует синюю подкраску и сохраняет числовой `R:<quantity>` marker. Первый job сразу claim-ится только выбранным resident. Следующие незавершённые placement jobs того же resident получают dependency на предыдущий job и активируются `ResidentInventoryPlacementQueue` строго в creation order.

`CompleteResidentInventoryPlacementHandler` повторно проверяет resident binding, destination и exact reservation, затем атомарно переносит reserved quantity в `ItemLocation.InWorld(destination)` и завершает job. Blocked route/target использует typed blocked/retry path. Cancel/failure освобождает quantity reservation; последующий HUD refresh убирает синюю подкраску. Failed predecessor отменяет dependents с явной причиной.

`ResidentInventoryPlacementJobSaveCodec` сохраняет resident id, stack id, quantity, destination, retry policy и dependency order. После PR #500 generic `SaveGameCompositionRoot.CreateJobDefinitionRegistry` регистрирует этот codec вместе со всеми concrete job definitions и выполняет coverage validation до создания `SaveGameService`.

## Pickup и hover presentation

World pickup требует выбранного живого resident. Generic/material/food stack показывает анимированную стрелку вверх и отдельный interaction hover tint только для exact visual stack. BuildingBox получает такой cursor/highlight только при удержании `Alt`; обычный ЛКМ продолжает selection/unpack workflow.

World item collider остаётся raycastable, но не владеет Navigation occupancy. Успешный pickup использует существующий exact-stack pickup job и resident slot capacity guard.

## Food, potions и drinks

Food в inventory делегируется существующему `StartResidentFoodMealHandler`; Presentation не реализует nutrition/effect. Tool use сохраняет прежний held-item slot guard.

Категории `potion`, `drink` и `beverage` используют тот же Alt-hover/Alt+LMB contract. В текущем content нет authoritative potion/drink effect owner, поэтому action возвращает typed `inventory.consumable.effect_owner_unavailable` вместо скрытого consume или выдуманного эффекта.

## Unity compile boundary

Unity runtime использует публичный ownership contract `DropResidentInventoryStackHandler.IsOwnedByResident`; helper не должен становиться `internal`, потому что `Dig.Unity` и `Dig.Application` являются разными assemblies. Inventory validation повторно получает authoritative repository через общий `ResolveWorldItemRepository`, а HUD/session references в consumable command path фиксируются как non-null после initialization guard, чтобы Unity nullable compilation не оставляла `CS8602` warnings.

`DigWorldInteraction` хранит HUD как `DigHudOverlay`. Consumable command path обязан использовать тот же adapter type: `DigGameHudCanvas` является внутренним canvas projection overlay и не владеет `SetCommandResult`/`SetJobs` contract напрямую.

PR #501 Play Mode scenarios intentionally reference internal `Dig.Unity` adapters and helpers, but `Dig.Unity.PlayModeTests` was not declared as a friend assembly. Unity therefore emitted the reported `CS0122` errors for `DigProductionIconPointer`, `DigTerrainWorkSession`, `DigInventoryItemGhostRenderer` and `CS1061` for the internal `GetHudModels` helper before any placement scenario could execute. PR #504 adds the explicit `InternalsVisibleTo("Dig.Unity.PlayModeTests")` contract without widening production API visibility.

## Verification

Добавлены regression tests для:

- `C + ЛКМ`, отсутствия `D` quick drop, Alt priority, отсутствия double-click/RMB quick drop и BuildingBox priority;
- hidden/restored system cursor and real item ghost lifecycle;
- flat walkable-support target validation;
- exact resident/stack/quantity reservation;
- blue reserved inventory background/text plus numeric reservation marker;
- deterministic dependency order;
- deposit без потери/дублирования quantity;
- blocked/cancel cleanup without stale reservation tint;
- placement job save codec и composition-root registration coverage;
- публичной Unity-доступности resident ownership policy и её location semantics;
- соответствия world consumable command path фактическому `DigHudOverlay` field contract;
- фактического HUD delegate в local ghost/job placement pipeline;
- friend-assembly identity и arrow-key camera duplicates.

Automated .NET/source-contract CI and the checked-in Unity scenario must pass on the final PR head. Actual animated cursor/ghost, exact hover, input shielding, repeated placement and cleanup remain `VERIFIED` only after execution in a licensed Unity Test Runner.