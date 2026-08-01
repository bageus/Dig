# Единые capabilities подбора, использования и размещения предметов

Статус: `IMPLEMENTED`.

Tracking issues: [#67](https://github.com/bageus/Dig/issues/67), [#70](https://github.com/bageus/Dig/issues/70), [#387](https://github.com/bageus/Dig/issues/387), [#390](https://github.com/bageus/Dig/issues/390), [#459](https://github.com/bageus/Dig/issues/459).

Связанные authoritative specifications:

- [`runtime-selection-excavation-item-placement-decisions.md`](runtime-selection-excavation-item-placement-decisions.md);
- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md);
- [`resident-inventory-expansion.md`](resident-inventory-expansion.md);
- [`campfire-cooking-and-food-use.md`](campfire-cooking-and-food-use.md);
- [`building-box-placement-and-packing.md`](building-box-placement-and-packing.md).

При расхождении по классификации предмета, modifier gating, hover/click parity или автоматическому подключению новых `ItemDefinition` этот документ имеет приоритет как последнее подтверждённое решение пользователя от 2026-08-01.

## 1. Назначение

Система задаёт один data-driven contract для всех Inventory-owned предметов. Добавление нового материала, оружия, инструмента, еды или другого переносимого предмета не требует новой ветки Unity input-кода: world hover, world click, inventory hover и inventory click читают один immutable interaction profile из authoritative `ItemDefinition`.

Presentation не выводит поведение из `ItemId`, префикса строки, имени prefab или отдельного списка исключений.

## 2. Authoritative owner и модель данных

`ItemDefinition` владеет:

- `ItemInteractionProfile`;
- optional `ItemFoodUseDefinition` для еды;
- categories, включая canonical `building.box`;
- tool/weapon facts и inventory expansion facts.

`ItemInteractionProfile` определяет:

- primary world action;
- `Alt` world action;
- primary inventory action;
- `Alt` inventory action;
- допустимость `C` quick drop;
- вид direct-use feedback (`Eat` или `Use`).

Inventory остаётся владельцем stack identity, quantity, location, reservation и held state. Jobs/Agents владеют travel/action execution. Presentation отображает только resolved action и отправляет typed intent.

## 3. Автоматическая классификация новых предметов

Если content не задаёт explicit profile, используется единый deterministic default resolver:

1. category `building.box` → `BuildingBox`;
2. наличие `ItemFoodUseDefinition` → `Food`;
3. `IsTool == true`, включая weapon/tool content → `Tool`;
4. всё остальное → `Generic`.

Следовательно:

- новый material/generic item автоматически получает обычный pickup, placement и quick drop;
- новая food definition с `ItemFoodUseDefinition` автоматически получает обычный pickup и `Alt` direct use;
- новый tool/weapon автоматически получает pickup, placement, `Alt` use и quick drop;
- новая BuildingBox автоматически получает box selection/placement contract только через category, без Unity override по `ItemId`.

Explicit profile разрешён для специальных предметов, например production package, но задаётся рядом с content definition, а не в Presentation.

## 4. World workflow

Для любой команды pickup/use требуется один выбранный живой resident. Без него action cursor не показывается, а click возвращает typed reason без скрытого movement/excavation fallback.

### 4.1 Обычный предмет, материал, оружие или инструмент

1. Hover доступного stack показывает слегка анимированную стрелку вверх и highlight только этого stack.
2. Один обычный LMB создаёт pickup job для exact `StackId`.
3. Resident идёт к authoritative source cell и подбирает предмет через Inventory transaction.

### 4.2 BuildingBox

- обычный LMB выбирает exact box, открывает BuildingBox menu и не создаёт pickup;
- только `Alt + LMB` показывает pickup feedback и создаёт pickup job;
- обычный LMB никогда не запускает unpacking или ground movement тем же event.

### 4.3 Предмет с direct use

Для еды:

- обычный LMB использует обычный pickup contract;
- при удержании `Alt` hover показывает animated mouth;
- `Alt + LMB` создаёт exact pickup-then-use job;
- конкретный эффект и количество укусов берутся из `ItemFoodUseDefinition`, не из `ItemId`.

### 4.4 Production package

- unfinished package неинтерактивна;
- closed `food`/`weapon`/`tool` package использует explicit package-use action;
- package не попадает в generic pickup и не зависит от строкового prefix.

## 5. Resident inventory workflow

Для любого доступного stack primary LMB читает тот же profile:

- `PlaceItem` → generic world-space placement mode;
- `PlaceBuilding` → BuildingBox placement/assembly mode;
- `None` → typed unavailable reason.

`Alt + LMB` имеет приоритет и выполняет `DirectUse`, когда profile это разрешает.

`C + LMB` имеет приоритет без `Alt` и немедленно выполняет exact-stack `DropInventoryStack` в current authoritative resident cell для любого profile с `InventoryQuickDropAllowed`, включая BuildingBox. После commit применяется обычная world-item gravity/support policy.

Reserved, held, stale или недоступный stack не размещается, не используется и не выбрасывается. Один click не может одновременно создать placement, use и drop.

## 6. Hover/click parity и input priority

World hover и world click обязаны использовать один resolver над одним отсортированным `RaycastHit[]` и одним `ItemInteractionProfile`:

```text
ItemDefinition + exact stack facts + modifier + selected resident
    -> ItemInteractionDecision
    -> cursor/highlight и typed click command
```

Запрещены:

- отдельный hover classifier и click classifier;
- `ItemId.StartsWith(...)` для gameplay interaction;
- per-item dictionaries в Unity composition;
- late generic-item fallback после movement/excavation;
- преобразование live inventory slot в compatibility model до command selection.

После UI shielding порядок для item target:

1. active placement/modal mode;
2. exact world item/profile action;
3. completed building/resident/movement/excavation fallbacks.

Item target поглощает event даже при typed rejection, поэтому первый LMB не может уйти в ground movement или копание, а следующий случайно сработать как pickup.

## 7. Cancel, failure и retry

- отмена placement preview не меняет stack location/quantity;
- cancel/failure placement job освобождает reservations;
- rejected pickup/use/drop не изменяет Inventory;
- stale target возвращает typed reason и не создаёт другую command;
- blocked destination использует существующий typed retry path;
- repeated click после уже созданного reservation не создаёт второй job;
- BuildingBox selection остаётся Presentation state и не резервирует stack.

## 8. Несколько предметов и residents

- exact `StackId` является identity команды;
- quantity/reservation проверяются в том же snapshot, который формирует action availability;
- несколько stacks одного `ItemId` не объединяются input resolver-ом;
- несколько residents не меняют profile; command всегда связывается с текущим selected resident;
- deterministic sorted hit order и exact visual owner исключают выбор соседнего stack.

## 9. Save/load

Interaction profile является content definition и отдельно не сохраняется. Save хранит:

- ItemId/StackId, quantity, location и reservations;
- pickup/placement jobs и их exact targets;
- held/use state соответствующих owners.

После load Presentation заново получает profile из catalog. Migration не должна записывать Unity-specific interaction flags.

## 10. Cursor, selection, panel и diagnostics

Диагностика item interaction показывает:

- StackId и ItemId;
- resolved profile/action;
- modifier (`None`, `Alt`, `C`);
- selected resident;
- availability/rejection reason;
- source/destination cell;
- reservation/held state.

Cursor/highlight отображаются только для того action, который отправит текущий click. BuildingBox ordinary selection highlight остаётся отличным от pickup hover.

## 11. Acceptance

- generic/material/weapon item без Presentation hardcode автоматически подбирается ordinary LMB;
- category-defined BuildingBox ordinary LMB выбирается, `Alt + LMB` подбирается;
- food definition ordinary LMB подбирается, `Alt + LMB` подбирается и используется;
- любой quick-drop-enabled inventory stack, включая BuildingBox, выбрасывается `C + LMB`;
- ordinary inventory LMB запускает profile-defined placement;
- `weapon.club` проходит generic world pickup и tool inventory placement/use/drop matrix без отдельной Unity ветки;
- hover и click используют один exact target resolver;
- item target имеет приоритет над terrain movement/excavation;
- один event создаёт максимум одну command;
- reserved/held/full-inventory/stale cases дают typed rejection без mutation;
- source-contract tests запрещают ID-prefix и per-item Unity classifiers;
- Domain/Presentation tests покрывают generic, food, tool/weapon, BuildingBox, package и new-item fallback;
- checked-in Play Mode matrix покрывает один-click pickup, direct use, placement и quick drop;
- `VERIFIED` требует фактического licensed Unity run по #511.

## 12. Журнал решений

| Дата | Решение | Кто подтвердил | Tracking |
|---|---|---|---|
| 2026-08-01 | Обычные предметы подбираются ordinary LMB; только world BuildingBox требует `Alt` для pickup; direct-use food использует `Alt`; inventory LMB размещает, `C + LMB` быстро выкладывает любой разрешённый profile. | Пользователь | #67/#70/#387/#390/#459 |
| 2026-08-01 | Новые items подключаются автоматически через authoritative `ItemDefinition`; Unity ID/prefix hardcode и раздельные hover/click classifiers запрещены. | Пользователь | #387/#390 |
