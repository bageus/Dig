# Runtime building/UI, Health bar, unpack and forced-pickup correction

Дата: 2026-08-04.  
Статус: `IMPLEMENTED IN BRANCH`.

Authoritative specification: [`../design/runtime-building-ui-health-unpack-pickup-correction-2026-08-04.md`](../design/runtime-building-ui-health-unpack-pickup-correction-2026-08-04.md).  
Tracking issue: [#634](https://github.com/bageus/Dig/issues/634).  
Correction PR: [#637](https://github.com/bageus/Dig/pull/637).

## Runtime report

Скриншоты и повторное использование runtime показали несколько связанных Presentation/Input regressions:

- demo продолжал создавать obsolete completed `Box Workshop`;
- building/production projection показывала служебные platforms, повторный heading и hover tooltip;
- Health bars наследовали scale владельца и находились слишком низко;
- multi-row production output чередовал footprint rows и оставлял визуальные промежутки;
- видимый valid BuildingBox ghost не всегда подтверждался одним LMB;
- замена текущего действия direct pickup могла оставить предыдущую item reservation.

## Root causes

- demo bootstrap всё ещё вручную добавлял отдельный workshop definition, completed snapshot и item definition, хотя authoritative demo уже содержал только campfire + campfire BuildingBox;
- selection/production renderers создавали derived footprint/bay geometry независимо от реальных моделей и item entities;
- Health-bar transform был child actor-а с постоянным local offset/scale, поэтому визуальный размер зависел от parent scale;
- output candidates сортировались сначала по lateral distance, затем по footprint row;
- placement click повторно разрешал hover target в click-frame вместо маршрутизации уже показанного preview;
- direct-command preparation сохранял stale pre-cancellation Inventory snapshot после специализированного release path.

## Correction

- obsolete workshop definition/item/bootstrap/profile aliases удалены;
- building selection footprint and production tray platforms удалены, реальные stock/output entities сохранены;
- production heading и hover/material tooltip area удалены, RMB cancel pointer для recipe icon сохранён;
- Health bar выравнивается над renderer bounds и компенсирует parent lossy scale;
- output candidates заполняют stable primary row от края наружу;
- LMB routes visible BuildingBox preview через shared `ContextInputRouter`, а authoritative confirmation revalidates тот же origin;
- stale Inventory save удалён; cancellation owner сохраняет released reservation до нового pickup command.

## Regression coverage

- `RuntimeBuildingUiHealthPickupContractTests` закрепляет demo/UI/Health/input/source boundaries;
- `ProductionOutputPlacementTests` проверяет contiguous primary row;
- `CombatHealthBarPresentationPlayModeTests` проверяет одинаковую world width и расположение над actor bounds;
- `ForcedPickupReplacementPlayModeTests` проверяет replacement pickup и освобождение первой reservation;
- existing BuildingBox and production source/Play Mode contracts обновлены под отсутствие workshop/tray/tooltip.

## CI contract alignment

Первый Quality run на production-коде прошёл Release build и все Unity source gates, но нашёл три устаревших test expectations:

- screenshot contract ожидал старую синтаксическую форму `if (model.ShowWorkbench)` вместо эквивалентного guarded conjunction;
- normalized input contract искал строку с пробелом, хотя helper удаляет whitespace перед assertion;
- gameplay contract всё ещё требовал demo-массив `workshop + campfire`.

Assertions синхронизированы с новым authoritative contract: временный active-production workbench сохранён, visible-preview routing проверяется в normalized форме, а fresh demo требует только completed campfire и запрещает `workshop`.

## Automated validation

На code-and-contract head `54aee2b4c3ab508a3c3b05ede15fb7be71631c50`:

- Quality run `30945346327` — success;
- architecture, file-size, C# compatibility и dependency gates — success;
- все Unity source/presentation/runtime contracts — success;
- Release build — success;
- .NET suite — `1510/1510`;
- headless smoke — success;
- standard deterministic soak — success;
- large-settlement deterministic soak — success;
- Export Stage 2 v2 run `30945346163` — success;
- Export Stage 2 v3 run `30945346203` — success.

Unity workflow `30945346270` завершился через blocked-evidence path: actual EditMode/PlayMode Test Runner и executed-runtime-evidence validation были skipped из-за недоступной licensed activation.

## Verification boundary

Фактическое исчезновение runtime-багов требует повторной локальной проверки в Unity либо выполненного лицензированного Test Runner. Repository/source checks и .NET tests подтверждают `IMPLEMENTED IN BRANCH`, но не повышают систему до `VERIFIED`.
