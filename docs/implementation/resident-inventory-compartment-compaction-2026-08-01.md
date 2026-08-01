# Resident inventory compartment compaction — 2026-08-01

Статус: `IMPLEMENTED` в ветке `fix/resident-inventory-compartment-compaction`; tracking issue [#68](https://github.com/bageus/Dig/issues/68). Licensed Unity Play Mode evidence остаётся обязательным для `VERIFIED`.

Authoritative specification: [`../design/resident-inventory-expansion.md`](../design/resident-inventory-expansion.md).

## Наблюдаемое поведение

В runtime `Main` мог содержать пустые ячейки 2 и 6, пока обычный предмет (`Iron ore`) оставался в `Cargo`. Порядок на HUD переставал соответствовать приоритету базового инвентаря и создавал ложное впечатление заполненного Main.

## Первопричина

- `NormalizeResidentInventory` сохранял все уже slotted locations и распределял только unslotted units, поэтому после удаления/расходования предмета Cargo не возвращался в Main;
- production definitions корзины принимали ordinary/raw categories, но не `Weapon`/`Shield`, поэтому weapon overflow не мог использовать Cargo после заполнения Main;
- legacy migration размещала Main раньше Weapon и могла восстанавливать другой порядок, чем pickup/hauling;
- свободные low-index slots и incoming slot claims не рассматривались одной операцией rebalancing.

## Исправление

- resident layout детерминированно пересобирается по low-index slots: совместимое оружие `Weapon -> Main -> Cargo`, остальные предметы `Main -> Cargo`;
- expansions остаются в Main и резервируют необходимую Main capacity;
- active held stack закреплён в исходном slot до completion/cancel; остальные предметы уплотняются вокруг него;
- incoming slot claims считаются занятыми и не перехватываются compaction;
- перенос сохраняет `StackId`, quantity, reservations и held ownership;
- Cargo definitions принимают weapon/shield overflow, не меняя специализированный Weapon-first priority;
- save/load migration использует тот же порядок и low-index policy;
- UI продолжает читать authoritative normalized layout и получает исправленный порядок в том же refresh workflow.

## Regression coverage

- screenshot-like sparse Main + loaded Cargo compacts to contiguous Main cells;
- weapon overflow fills Weapon, then Main, then Cargo;
- incoming claims shield their exact slots;
- held stack stays pinned while neighbouring items compact;
- migration restores Weapon-first layout and held tool identity;
- content validates Cargo acceptance for weapon/shield overflow;
- checked-in Unity Play Mode scenario covers the visible two-row layout and exact compartment placement.

## Evidence boundary

Repository source contracts, .NET tests, build, smoke and deterministic soak can establish `IMPLEMENTED`. Actual selected-resident HUD refresh, repeated pickup/drop and save/load continuation remain `VERIFIED` only after licensed Unity EditMode/PlayMode execution.
