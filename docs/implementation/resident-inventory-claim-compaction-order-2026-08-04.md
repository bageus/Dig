# Resident inventory claim compaction order — 2026-08-04

Status: `IMPLEMENTED IN BRANCH`.

Authoritative correction: [`../design/resident-inventory-claim-compaction-correction-2026-08-04.md`](../design/resident-inventory-claim-compaction-correction-2026-08-04.md).  
Parent specification: [`../design/resident-inventory-expansion.md`](../design/resident-inventory-expansion.md).  
Tracking issue: [#68](https://github.com/bageus/Dig/issues/68).  
Implementation PR: [#615](https://github.com/bageus/Dig/pull/615).

## Reported regression

В screenshot workflow первые три Main cells содержали physical mushroom items, четвёртая и пятая выглядели пустыми, а завершившая pickup большая корзина отображалась в шестой Main cell. По утверждённому low-index contract корзина должна была занять четвёртую ячейку.

## Root cause

`InventoryState.NormalizeResidentInventory` добавлял каждый active incoming `ResidentInventorySlotClaimSnapshot.Slot` в immutable `unavailable` set до планирования physical stacks.

При нескольких параллельных pickup jobs:

1. ранние jobs резервировали `Main 4` и `Main 5`;
2. pickup большой корзины резервировал `Main 6`;
3. корзина завершалась первой и освобождала только собственный claim;
4. normalization продолжал защищать точные indices `Main 4`/`Main 5` для незавершённых jobs;
5. physical basket не мог уплотниться из `Main 6`, хотя UI cells 4/5 ещё не содержали physical items.

HUD проецировал authoritative `SlotIndex` корректно; ошибка находилась в Domain layout/claim ordering, не в `GridLayoutGroup` или label ordering.

## Implementation

- physical resident stacks теперь планируются первыми по утверждённому `Weapon -> Main -> Cargo` / `Main -> Cargo` порядку;
- held stacks сохраняют существующий pinned-slot contract;
- outstanding incoming claims после physical plan детерминированно перепланируются в следующие свободные совместимые slots;
- claim reflow сохраняет `JobId`, `ResidentId`, `ItemId` и quantity;
- changed claims публикуют removal/addition `ResidentInventorySlotClaimChanged` facts;
- весь reflow preflight завершается до mutation physical stacks;
- невозможная совместимая capacity отклоняет normalization до commit;
- повторная normalization идемпотентна.

## Regression coverage

### Domain

`ResidentInventoryClaimCompactionTests` воспроизводит:

- physical Main slots 1–3;
- два outstanding ordinary-item claims;
- large-basket claim в Main 6;
- basket claim completion раньше других jobs;
- physical basket в прежнем Main 6;
- normalization в basket Main 4 + claims Main 5/6;
- preserved claim identities/quantities, typed reflow events и idempotent repeat.

Existing `ResidentInventorySlotClaimTests` также проверяет простой reflow: physical stack получает младший совместимый Main slot, а pending claim переносится следом без смены ownership.

### Unity Play Mode fixture

`ResidentInventoryClaimCompactionPlayModeTests` строит тот же authoritative layout через `ResidentInventoryLayoutPresenter` и требует:

- `Large basket` в четвёртой Main cell;
- пустые physical cells 5/6;
- outstanding claims в indices 5/6.

Fixture checked in; actual licensed Unity Test Runner execution remains required before `VERIFIED`.

## Validation — code/test head `0364b2a767091d2a78ea67a307a28f325deb66d4`

Quality run `30861848761` passed:

- architecture, file-size and C# compatibility checks;
- Unity source contracts and presentation/runtime gates;
- Release build with `0` warnings and `0` errors;
- `1474/1474` .NET tests;
- headless smoke;
- standard deterministic soak;
- large-settlement deterministic soak.

Export Stage 2 v2 run `30861848785` and v3 run `30861848789` passed.

Unity workflow `30861848762` completed through the blocked-evidence path. Actual EditMode/PlayMode execution was skipped because licensed Unity activation was unavailable. Therefore the behavior remains `IMPLEMENTED IN BRANCH`, not `VERIFIED`.

The final documentation-only head must retain the same green repository gates before merge.
