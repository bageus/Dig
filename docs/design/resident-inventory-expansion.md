# Личный инвентарь гномов и его расширения

Статус: `APPROVED`. Реализация отслеживается в #64–#71. Контекстная панель и BuildingBox integration: #113/#115/#118.

## 1. Владение состоянием

`InventoryState` остаётся единственным владельцем ItemStack, quantity, reservation и ItemLocation. Storage владеет filters/capacity, Jobs — lifecycle работ, Presentation — только selection/preview.

## 2. Базовый layout

Каждый взрослый гном имеет 6 универсальных Main slots.

```text
ResidentInventoryLocation(
    ResidentId,
    Compartment: Main | Cargo | Weapon,
    SlotIndex
)
```

Правила:

- один slot содержит ровно одну физическую unit stack либо пуст;
- ordinary item/material в resident inventory всегда имеет quantity `1`;
- compatible stacks в resident inventory не объединяются;
- expansions разрешены только в Main;
- nested containers запрещены;
- BuildingBox является обычным неstackable item, а не вложенным зданием/контейнером.

## 3. Cargo expansions

### Корзина

- занимает 1 Main slot;
- добавляет 4 Cargo slots;
- при непустом Cargo скорость = 75%;
- пустой Cargo не штрафует скорость;
- basket-backpack attachment на заднем Cargo socket скрыт, пока Cargo пуст.

### Большая корзина

- занимает 1 Main slot;
- добавляет 6 Cargo slots;
- при непустом Cargo скорость = 65%;
- имеет приоритет над корзиной.

Обе корзины могут занимать Main slots, но capacity/speed не суммируются. В demo одна корзина и одна большая корзина находятся отдельными world items на поверхности рядом со стартовым resident; стартовый resident не получает Cargo expansion до pickup. Активная Cargo expansion принимает ordinary/general/raw items, а также weapon/shield overflow только после исчерпания Weapon и Main; наличие корзины не меняет приоритет Weapon compartment.

## 4. Weapon expansions

### Ножны

- занимают 1 Main slot;
- добавляют 2 Weapon slots;
- принимают оружие и щиты;
- не меняют скорость;
- рецепт: 2 железа, 2 хомяка в кузнице.

### Разгрузка

- занимает 1 Main slot;
- добавляет 4 Weapon slots;
- принимают оружие и щиты;
- имеет приоритет над ножнами;
- не меняет скорость;
- рецепт: 3 железа, 2 хомяка, 1 золото, 2 ножки в арсенале.

Cargo и Weapon groups работают одновременно.

### Runtime demo и проверка Weapon placement

- стартовый resident не получает ножны или разгрузку автоматически;
- `inventory.sheath`, `inventory.weapon_harness` и `weapon.club` находятся рядом с ним как отдельные selectable/pickable world item entities на поддерживаемой поверхности;
- ножны/разгрузка подбираются в свободный Main slot через обычный world-item pickup workflow;
- `weapon.club` является non-stackable Weapon item; при активном Weapon expansion его pickup резервирует и занимает первый свободный Weapon slot раньше Main/Cargo;
- ножны дают сетку `1×2`, разгрузка — `2×2`; разгрузка имеет tier priority, уже занятые low-index Weapon slots сохраняются;
- world/carry visuals ножен, разгрузки и дубины должны быть различимыми и не использовать generic magenta cube;
- отмена/ошибка pickup освобождает quantity и slot claims, не меняя layout;
- выкладывание active Weapon expansion использует общий quantity-safe spill и только затем активирует lower tier.

## 5. Визуальный порядок

При выбранном resident нижняя context panel показывает:

```text
[ Weapon ] [ Main: 6 ] [ Cargo ]
```

- Weapon слева;
- Main по центру;
- Cargo справа;
- группы разделены рамкой и spacing; текстовые заголовки `Weapon` и `Cargo 4/6` не отображаются;
- каждый compartment строится строго в два горизонтальных ряда: если существует верхняя ячейка колонки, существует и нижняя;
- slot indices проецируются по колонкам: `1` сверху и `2` снизу, затем `3` сверху и `4` снизу следующей колонки, затем `5`/`6`;
- Main всегда имеет сетку `3×2`;
- Cargo показывает `2×2` для корзины или `3×2` для большой корзины, пока active expansion находится в Main;
- Weapon показывает `1×2` для ножен или `2×2` для разгрузки;
- при отсутствии resident эта область показывает ExcavationPalette;
- building selection и BuildingPlacement заменяют Inventory panel, а не накладываются поверх неё.

## 6. Эффективная скорость

```text
EffectiveMoveSpeed = BaseMoveSpeed * CargoSpeedMultiplier
```

- empty Cargo: 1.00;
- occupied basket: 0.75;
- occupied large basket: 0.65;
- Weapon expansions: 1.00.

Multiplier влияет на simulation movement, ETA, Utility AI и job cost, не только на animation.

## 7. Layout changes и spill

При смене active tier layout пересчитывается атомарно.

- 4→6 Cargo и 2→4 Weapon сохраняют существующие low-index slots;
- при уменьшении capacity лишними считаются highest-index slots;
- лишние items проливаются в cell удаления expansion;
- ручное удаление active expansion проливает весь связанный compartment;
- после успешного drop/spill Cargo/Weapon slots исчезают из следующего layout snapshot, attachment скрывается и movement multiplier пересчитывается в том же refresh cycle;
- lower-tier expansion активируется только после spill;
- автоматический перенос contents в lower tier не выполняется;
- операция полностью rollback при ошибке.

Quantity не теряется и не дублируется.

## 8. Destination priority и автоматическое уплотнение

Authoritative resident layout после каждого успешного ingress, removal, load/recovery и явной normalization уплотняется по возрастанию `SlotIndex`; пустая low-index ячейка не может оставаться перед совместимым предметом в том же или более низкоприоритетном compartment.

Единый deterministic порядок:

1. оружие и щиты занимают свободные slots активного `Weapon` compartment по индексам `1..N`;
2. weapon overflow занимает свободные `Main` slots по индексам `1..6`;
3. остальные ordinary items/materials и inventory expansions занимают `Main` по индексам `1..6`;
4. только после заполнения доступного `Main` weapon overflow и ordinary items используют совместимые `Cargo` slots по индексам `1..N`.

Inventory expansions всегда остаются в `Main`. Каждая переносимая физическая единица требует отдельный свободный совместимый slot. Occupied slot того же `ItemId` не предоставляет дополнительную capacity и не принимает merge.

Если в `Main` освобождается ячейка, совместимый предмет из `Cargo` автоматически возвращается в первый свободный `Main` slot при следующей authoritative normalization. Weapon items аналогично возвращаются из `Main/Cargo` в первый свободный `Weapon` slot. Перемещение сохраняет тот же `StackId`, quantity, reservations и held reference; layout projection обновляется в том же refresh cycle.

Активный held stack остаётся закреплён в исходной ячейке до completion/cancel действия; остальные предметы уплотняются вокруг этой ячейки. Входящие slot claims также считаются занятыми и не могут быть перехвачены rebalancing-ом.

Один и тот же порядок используется world pickup, hauling, building supply, retry и save/load recovery.

## 9. Предмет в руках

Предмет остаётся в исходном slot. Действие использует ссылку:

```text
HeldItemReference(ResidentId, StackId, Quantity = 1, Purpose)
```

- ссылка не создаёт quantity;
- необходимая quantity резервируется;
- UI сохраняет icon в slot;
- Agent View показывает held representation;
- ссылка очищается при completion, cancel, destruction или consumption;
- временный Equipped location для предмета в руках не используется.

## 10. Контекстный input предметов

После UI shielding применяется типизированный приоритет.

### 10.1 BuildingBox в Inventory

Обычный ЛКМ по BuildingBox включает `BuildingPlacement` для соответствующего BuildingDefinition. `C + ЛКМ` является отдельным explicit quick-drop action и немедленно выкладывает exact box stack в current resident cell.

- box остаётся в исходном slot во время preview;
- valid placement atomically создаёт plan и reservation;
- invalid/cancel preview не меняет Inventory;
- после plan creation UI показывает reservation owner/state.

Полная модель: `building-box-placement-and-packing.md`.

### 10.2 Обычный предмет и inventory expansion

ЛКМ по обычному stack, корзине или большой корзине включает item placement mode.

1. UI показывает полупрозрачный ghost на любой explored/reachable open cell непосредственно над твёрдой ровной опорой.
2. ЛКМ по valid cell создаёт resident-bound placement job; stack остаётся зарезервированным в исходной ячейке до deposit.
3. Обычный stack переносится через reserved move transaction.
4. Active basket/large basket переносится через reserved spill-aware transaction: корзина и всё содержимое Cargo оказываются в target cell, Cargo capacity исчезает, attachment скрывается, скорость пересчитывается.
5. Invalid/cancel/failure освобождает reservation и не меняет layout/quantity.

### 10.3 Quick drop

Пока удерживается `C`, hover любого доступного quick-drop-enabled stack, включая BuildingBox, показывает quick-drop indicator. `C + ЛКМ` выбрасывает exact stack в current logical resident cell. Для expansion используется explicit spill-aware drop; double LMB, RMB и `D + ЛКМ` quick drop не выполняют.

### 10.4 Use

`Alt + ЛКМ` по usable inventory item отправляет UseInventoryItem.

- consumable уменьшает quantity;
- tool/weapon создаёт HeldItemReference;
- unavailable/reserved/unusable item возвращает reason;
- box placement использует обычный LMB, не Alt use.

## 11. World interaction fallback

Для world items правила принадлежат context router #115.

- обычный LMB по world BuildingBox выбирает коробку и открывает `Unpack`; placement запускается из меню;
- Alt+LMB по world BuildingBox назначает pickup выбранному resident;
- generic/material/food/tool/weapon world item ordinary LMB подбирается автоматически по definition-owned profile;
- unsupported/невозможный item action возвращает typed reason и не трактуется как ground click;
- full Inventory не уничтожает item и не создаёт скрытый pickup.

## 11.1 Unified item capabilities

Pickup/use/place/drop behavior определяется [`item-interaction-capabilities.md`](item-interaction-capabilities.md). `ItemDefinition` является единственным source of truth; новые предметы не требуют Unity ID/prefix hardcode. Resident inventory slot публикует тот же `ItemInteractionProfile`, который использует world presenter.

## 12. BuildingBox category

BuildingBox:

- имеет MaxStackSize 1;
- допускается в Main/Cargo согласно item category policy, но не в Weapon;
- не является active expansion;
- сохраняет stable BuildingDefinitionId/version reference;
- одна коробка резервируется не более чем одним building plan;
- сама коробка остаётся authoritative Inventory item до site/final commit.

## 13. Hauling integration

- planner учитывает только свободные совместимые slots;
- каждая переносимая unit получает отдельный destination slot/slot claim;
- ordinary resources/boxes не используют Weapon;
- destination slot или slot claim резервируется;
- два jobs не резервируют одну capacity;
- cancel/failure/retry exhaustion освобождают quantity/slot claims;
- layout change не оставляет job со stale slot;
- BuildingBox plan reservation и resident slot reservation согласуются одной Application orchestration.

## 14. Content definitions

```text
InventoryExpansionDefinition
- ExpansionGroup
- Tier
- AddedSlots
- AcceptedCategories
- MoveSpeedMultiplierWhenOccupied
- VisualAttachmentId
- IsMainCompartmentOnly
```

```text
BuildingBoxDefinition
- ItemId
- BuildingDefinitionId
- DefinitionVersion
- PlacementActionId
- MaxStackSize = 1
```

Content validation проверяет IDs, categories, slots, speed, recipes и building references. Basket ItemIds обязаны иметь distinct basket-shaped world/carry presentation policy; generic magenta cube fallback для них запрещён.

## 15. Save/Load

Сохраняются:

- compartment/slot index каждого resident stack;
- active expansion selection data;
- slot/quantity reservations;
- HeldItemReference и active action;
- BuildingBox definition/version и plan reservation;
- external job/storage/building links.

Миграция старого resident inventory сортирует unit stacks по stable StackId, размещает expansions в `Main`, затем weapon-compatible units в `Weapon`, weapon overflow и ordinary units в `Main`, после заполнения `Main` — в совместимый `Cargo`; внутри каждого compartment используются low-index slots. Остаток выбрасывается в resident cell с report. Legacy resident stacks с quantity больше `1` должны быть разделены migration owner на отдельные unit identities до layout placement.

## 16. Инварианты

- stack имеет одно authoritative location;
- ordinary resident stack имеет quantity `1`;
- slot index валиден;
- expansion находится только в Main;
- containers не вложены;
- active tier один на group;
- speed зависит только от active occupied Cargo;
- HeldItemReference не увеличивает quantity;
- spill сохраняет total quantity;
- BuildingBox preview не резервирует/расходует item;
- одна BuildingBox не принадлежит двум plans;
- BuildingBox ordinary LMB не создаёт одновременно drop и placement; `C + LMB` создаёт только quick drop;
- reserved quantity нельзя использовать/выбросить сверх available.

## 17. Критерии приёмки

- base 6 slots;
- cargo 4/6 и weapon 2/4 работают с tier priority;
- empty/occupied speed rules точны;
- spill quantity-safe и rollback-safe;
- held item остаётся в slot;
- Weapon→Main→Cargo layout;
- ordinary drop/use и BuildingBox placement имеют правильный priority;
- basket/large-basket placement работает на любой допустимой твёрдой ровной поверхности через тот же planning mode и сохраняет quantity при Cargo spill;
- каждая одинаковая или различная ordinary unit в resident inventory занимает отдельный slot;
- pickup/hauling использует Weapon для совместимого оружия, затем Main и только после заполнения Main — Cargo;
- после освобождения Main предметы автоматически уплотняются из Cargo в low-index Main slots, а оружие — в low-index Weapon slots;
- active held stack остаётся в исходной ячейке, а incoming claimed slots не перехватываются уплотнением;
- UI не показывает пустую low-index ячейку перед совместимым предметом в более низкоприоритетном compartment;
- surface demo содержит pickable basket и large basket, а resident начинает без Cargo expansion;
- surface demo содержит pickable sheath, weapon harness и `weapon.club`, а resident начинает без Weapon expansion;
- после pickup ножен/разгрузки `weapon.club` попадает в первый свободный Weapon slot;
- непустой Cargo показывает basket-backpack сзади, пустой Cargo и drop/spill скрывают его;
- HUD не показывает текстовые заголовки `Weapon` и `Cargo 4/6`; inventory compartments используют только парные двухрядные сетки `3×2`, `2×2`/`3×2`, `1×2`/`2×2` и заполняются по колонкам `1/2`, `3/4`, `5/6`;
- hauling учитывает real free slots и не использует merge capacity;
- save/load восстанавливает layout, boxes и reservations;
- unit, integration, migration, soak и Play Mode tests покрывают все правила.


## 18. Журнал решений

| Дата | Решение | Кто подтвердил | Issues |
|---|---|---|---|
| 2026-07-29 | Ordinary pickup/hauling использует Main до Cargo; basket и large basket появляются на поверхности; attachment виден только при непустом Cargo. | пользователь | #68, #69 |
| 2026-07-29 | Cargo title скрыт; все compartments имеют ровно два ряда; basket/large basket размещаются через planning mode на supported surface с reserved quantity-safe spill. | пользователь | #67, #69, #70, #387 |
| 2026-07-29 | Каждая ordinary item/material unit в resident inventory является отдельным quantity-one stack и занимает отдельный slot; merge capacity запрещена. | пользователь | #67, #68, #69 |
| 2026-07-30 | Ножны, разгрузка и `weapon.club` появляются отдельными world items; club служит runtime-проверкой Weapon-slot priority и tier switching. | пользователь | #68, #69, #70 |
| 2026-07-30 | Текстовый заголовок Weapon скрыт; двухрядные inventory grids нумеруются по колонкам: `1/2`, `3/4`, `5/6`. | пользователь | #70 |
| 2026-08-01 | Resident inventory автоматически уплотняется по low-index slots; приоритет оружия `Weapon -> Main -> Cargo`, ordinary items `Main -> Cargo`; held stack закреплён, incoming claims защищены. | пользователь | #68, #69 |
