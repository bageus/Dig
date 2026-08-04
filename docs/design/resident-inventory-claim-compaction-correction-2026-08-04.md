# Уплотнение resident inventory при незавершённых входящих slot claims

Статус: `APPROVED`.

Parent specification: [`resident-inventory-expansion.md`](resident-inventory-expansion.md).  
Tracking issue: [#68](https://github.com/bageus/Dig/issues/68).

## 1. Подтверждённое observable правило

После каждого успешного pickup/hauling/building-supply ingress, removal, recovery, load и явной normalization физические предметы resident inventory занимают совместимые ячейки строго по возрастанию `SlotIndex`.

Для ordinary item/material и inventory expansion действует порядок:

1. `Main 1..6`;
2. затем совместимый `Cargo 1..N`.

Для weapon/shield действует порядок:

1. активный `Weapon 1..N`;
2. `Main 1..6`;
3. затем совместимый `Cargo 1..N`.

На примере подтверждённого screenshot workflow: если `Main 1`, `Main 2` и `Main 3` заняты, а pickup большой корзины уже завершён, большая корзина обязана находиться в `Main 4`. Наличие других ещё не завершённых pickup jobs не может оставлять визуально пустые `Main 4`/`Main 5` перед завершённым предметом в `Main 6`.

## 2. Slot claim не является неизменяемым физическим индексом

Incoming `ResidentInventorySlotClaimSnapshot` резервирует логическую совместимую capacity для job, но его текущий `Slot` не является вечной позицией.

Authoritative normalization выполняется атомарно в следующем порядке:

1. фиксирует held stacks, которые по существующему правилу остаются pinned до completion/cancel действия;
2. планирует и уплотняет все уже существующие физические stacks по compartment priority и low-index slots;
3. после физического layout детерминированно перепланирует все незавершённые incoming claims в следующие свободные совместимые slots;
4. сохраняет для каждого claim его `JobId`, `ResidentId`, `ItemId` и quantity;
5. публикует typed claim-change facts для старого и нового slot, если claim был перемещён.

Таким образом claim продолжает защищать реальную capacity от второго job, но не защищает устаревший младший индекс от уже завершённого предмета.

## 3. Success path

1. Несколько jobs резервируют свободные resident slots.
2. Более поздний job может завершить ingress раньше более ранних jobs.
3. Его собственный claim освобождается.
4. Входящий physical stack временно находится в зарезервированном slot.
5. В той же authoritative transaction выполняется normalization.
6. Physical stacks занимают непрерывные low-index slots.
7. Оставшиеся active claims сдвигаются следом в следующие совместимые slots.
8. HUD в том же refresh cycle показывает непрерывный layout без пустых младших ячеек перед завершёнными предметами.

## 4. Failure, retry и cancel

- если после физического layout оставшиеся claims невозможно разместить в совместимой capacity, normalization отклоняется до mutation;
- retry сохраняет тот же job/item/quantity claim и использует его перепланированный slot;
- cancel/failure/retry exhaustion освобождают claim независимо от того, менялся ли его slot;
- два claims не получают одну и ту же ячейку;
- reflow не меняет quantity reservations и не создаёт/удаляет items;
- held stack остаётся pinned; это единственное подтверждённое исключение, при котором физический layout может иметь gap до завершения/cancel held action.

## 5. Save/load и migration

Сохранённый slot claim восстанавливается как логическая reservation. После restore выполняется та же normalization/reflow:

- physical items уплотняются первыми;
- claims перепланируются следом;
- `JobId`, `ResidentId`, `ItemId` и quantity сохраняются;
- stale overlap между physical item и claim исправляется без duplication;
- невозможная capacity возвращает typed load/recovery diagnostic, а не молча удаляет claim или item.

## 6. Ownership

- `InventoryState` остаётся единственным владельцем physical stacks, resident locations и incoming slot claims;
- Jobs владеет lifecycle job, но не назначает постоянный UI index;
- Presentation только проецирует resulting physical layout;
- HUD не сортирует и не перемещает предметы самостоятельно.

## 7. Acceptance

- три physical Main items + два outstanding claims + completed large-basket ingress в прежний `Main 6` нормализуются в physical `Main 1..4` и claims `Main 5..6`;
- большая корзина отображается в четвёртой Main cell;
- claim identities и quantities сохраняются;
- claim-change events отражают reflow;
- no physical/claim overlap остаётся после commit;
- cancel одного из сдвинутых claims освобождает его новую ячейку;
- повторная normalization идемпотентна;
- Domain regression, Application/pickup regression и checked-in Unity Play Mode presentation scenario воспроизводят screenshot workflow;
- фактический Unity Test Runner требуется для статуса `VERIFIED`.
