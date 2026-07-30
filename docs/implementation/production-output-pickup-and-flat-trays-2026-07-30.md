# Production output pickup and flat building trays — 2026-07-30

Статус: `IMPLEMENTED` после merge связанного PR; licensed Unity runtime evidence требуется для `VERIFIED`.

Authoritative design: [`../design/building-production-and-internal-supply.md`](../design/building-production-and-internal-supply.md). Tracking: #433.

## Первопричины

`DigBuildingInternalStockBayVisual` создавал один и тот же tray с rear rail для обеих зон. Готовый продукт уже создавался authoritative production transaction, однако `DigWorldItemVisual` дополнительно требовал `DigItemColliderPolicy.InteractiveOnly`. Поэтому art/profile policy могла отключить root collider, даже когда `WorldItemViewModel` явно публиковал pickup interaction.

## Исправление

- общий bay visual создаёт только плоский tray, поэтому спинка исчезает слева и справа;
- root interaction collider включается по authoritative `WorldItemViewModel.IsInteractive`;
- child/art colliders остаются выключенными и не блокируют Navigation;
- finished output виден только через обычную world inventory projection;
- completion transaction атомарно создаёт world stack и переводит order/job в terminal state.

## Regression coverage

- source contract запрещает `Storage back rail` и art-policy veto;
- Play Mode fixture проверяет оба flat trays и pickup collider при `ColliderPolicy.None`;
- integration test проверяет `ItemLocation.InWorld`, terminal order/job, released reservations и replay protection.
