# Личный инвентарь гномов и его расширения

Статус: проектная спецификация. Реализация отслеживается в #64–#71. Контекстная панель и BuildingBox integration: #113/#115/#118.

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

- один slot содержит не более одного stack;
- stack quantity ограничен MaxStackSize;
- compatible stacks объединяются;
- expansions разрешены только в Main;
- nested containers запрещены;
- BuildingBox является обычным неstackable item, а не вложенным зданием/контейнером.

## 3. Cargo expansions

### Корзина

- занимает 1 Main slot;
- добавляет 4 Cargo slots;
- при непустом Cargo скорость = 75%;
- пустой Cargo не штрафует скорость;
- model attachment скрыт, пока Cargo пуст.

### Большая корзина

- занимает 1 Main slot;
- добавляет 6 Cargo slots;
- при непустом Cargo скорость = 65%;
- имеет приоритет над корзиной.

Обе корзины могут занимать Main slots, но capacity/speed не суммируются.

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
- принимает оружие и щиты;
- имеет приоритет над ножнами;
- не меняет скорость;
- рецепт: 3 железа, 2 хомяка, 1 золото, 2 ножки в арсенале.

Cargo и Weapon groups работают одновременно.

## 5. Визуальный порядок

При выбранном resident нижняя context panel показывает:

```text
[ Weapon ] [ Main: 6 ] [ Cargo ]
```

- Weapon слева;
- Main по центру;
- Cargo справа;
- группы разделены рамкой, spacing и icon/title;
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
- lower-tier expansion активируется только после spill;
- автоматический перенос contents в lower tier не выполняется;
- операция полностью rollback при ошибке.

Quantity не теряется и не дублируется.

## 8. Предмет в руках

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

## 9. Контекстный input предметов

После UI shielding применяется типизированный приоритет.

### 9.1 BuildingBox в Inventory

ЛКМ по BuildingBox **не** выбирает stack для drop. Он включает `BuildingPlacement` для соответствующего BuildingDefinition.

- box остаётся в исходном slot во время preview;
- valid placement atomically создаёт plan и reservation;
- invalid/cancel preview не меняет Inventory;
- после plan creation UI показывает reservation owner/state.

Полная модель: `building-box-placement-and-packing.md`.

### 9.2 Обычный предмет

ЛКМ по обычному stack выбирает его для targeted drop.

1. UI подсвечивает valid open ground cells.
2. ЛКМ по valid cell отправляет drop command.
3. Domain меняется только после успешной команды.

### 9.3 Quick drop

Double LMB по обычному stack выбрасывает его в current logical resident cell. Для expansion выполняется spill.

BuildingBox double click не должен обходить placement priority; quick drop доступен только через явно выбранное drop action/context menu либо после отдельного stack-selection режима.

### 9.4 Use

`Alt + ЛКМ` по usable inventory item отправляет UseInventoryItem.

- consumable уменьшает quantity;
- tool/weapon создаёт HeldItemReference;
- unavailable/reserved/unusable item возвращает reason;
- box placement использует обычный LMB, не Alt use.

## 10. World interaction fallback

Для world items правила принадлежат context router #115.

- обычный LMB по world BuildingBox включает placement mode;
- Alt+LMB по world BuildingBox назначает pickup выбранному resident;
- generic world item без LMB interaction не подбирается автоматически;
- unsupported/невозможный Alt interaction трактуется как ground click: выбранный resident идёт к позиции;
- full Inventory не уничтожает item и не создаёт скрытый pickup.

## 11. BuildingBox category

BuildingBox:

- имеет MaxStackSize 1;
- допускается в Main/Cargo согласно item category policy, но не в Weapon;
- не является active expansion;
- сохраняет stable BuildingDefinitionId/version reference;
- одна коробка резервируется не более чем одним building plan;
- сама коробка остаётся authoritative Inventory item до site/final commit.

## 12. Hauling integration

- planner учитывает merge и free slots;
- ordinary resources/boxes не используют Weapon;
- destination slot или slot claim резервируется;
- два jobs не резервируют одну capacity;
- cancel/failure/retry exhaustion освобождают quantity/slot claims;
- layout change не оставляет job со stale slot;
- BuildingBox plan reservation и resident slot reservation согласуются одной Application orchestration.

## 13. Content definitions

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

Content validation проверяет IDs, categories, slots, speed, recipes и building references.

## 14. Save/Load

Сохраняются:

- compartment/slot index каждого resident stack;
- active expansion selection data;
- slot/quantity reservations;
- HeldItemReference и active action;
- BuildingBox definition/version и plan reservation;
- external job/storage/building links.

Миграция старого resident inventory сортирует stacks по stable StackId, заполняет Main, активирует expansions, затем Cargo/Weapon; остаток выбрасывается в resident cell с report.

## 15. Инварианты

- stack имеет одно authoritative location;
- slot index валиден;
- expansion находится только в Main;
- containers не вложены;
- active tier один на group;
- speed зависит только от active occupied Cargo;
- HeldItemReference не увеличивает quantity;
- spill сохраняет total quantity;
- BuildingBox preview не резервирует/расходует item;
- одна BuildingBox не принадлежит двум plans;
- BuildingBox LMB не создаёт одновременно drop и placement;
- reserved quantity нельзя использовать/выбросить сверх available.

## 16. Критерии приёмки

- base 6 slots;
- cargo 4/6 и weapon 2/4 работают с tier priority;
- empty/occupied speed rules точны;
- spill quantity-safe и rollback-safe;
- held item остаётся в slot;
- Weapon→Main→Cargo layout;
- ordinary drop/use и BuildingBox placement имеют правильный priority;
- hauling учитывает real slots;
- save/load восстанавливает layout, boxes и reservations;
- unit, integration, migration, soak и Play Mode tests покрывают все правила.
