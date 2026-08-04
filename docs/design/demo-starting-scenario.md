# Стартовый demo-сценарий: готовый костёр и упакованный BuildingBox

Статус: `QUESTIONNAIRE`; exact surface position подтверждена 2026-07-29, depth-layer correction подтверждена 2026-08-01.

Tracking issues: [#389](https://github.com/bageus/Dig/issues/389), [#634](https://github.com/bageus/Dig/issues/634).

Связанные документы:

- [`building-box-placement-and-packing.md`](building-box-placement-and-packing.md);
- [`building-production-and-internal-supply.md`](building-production-and-internal-supply.md);
- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md);
- [`entity-fall-knockback-and-vertical-shafts.md`](entity-fall-knockback-and-vertical-shafts.md);
- [`../implementation/campfire-building-box-content.md`](../implementation/campfire-building-box-content.md);
- [`../implementation/campfire-placement-policy.md`](../implementation/campfire-placement-policy.md).

## 1. Назначение

Demo bootstrap одновременно показывает completed workstation и BuildingBox workflow без ручной подготовки сцены. Demo не обязан показывать падение предметов.

## 2. Подтверждённый состав и расположение

Fresh demo содержит две независимые quantity-one entities и не содержит obsolete `Box Workshop`:

1. ровно один completed campfire на поверхности;
2. ровно один отдельный campfire BuildingBox в world Inventory location нижней пещеры.

`demo.workshop.box`, `demo.building_box.workshop`, completed `Box Workshop` и его roster/visual content запрещены.

Completed campfire имеет deterministic origin:

```text
origin.X = ShaftX - 2
origin.Y = SurfaceY
origin.Z = 1
```

Это две logical cells левее вертикального тоннеля на той же surface platform и на ближайшем разрешённом building layer. `Z0` принадлежит только физическим BuildingBox и их relocation. Unpacked, active и completed buildings разрешены только на `Z1–Z3`, поэтому completed campfire не может использовать demo `ShaftZ = 0`.

Bootstrap валидирует world bounds, open footprint, solid support, building/item overlap и reachable work position на exact `Z1`. Он не выбирает `Z0`, другую поверхность или lower-cave fallback. Invalid fixture завершает initialization typed diagnostic failure.

Packed campfire BuildingBox остаётся в deterministic valid lower-cave world location и не создаётся в vertical tunnel.

## 3. UI и workflow

- completed campfire отображается и выбирается как building;
- selection открывает building functions и production/internal-stock workflow;
- packed box отображается в world и Buildings roster как box row;
- ordinary LMB выбирает box и показывает `Unpack`;
- `Unpack` запускает placement mode;
- `Alt + LMB` создаёт pickup order выбранному resident;
- оба visual используют authoritative exact XYZ.

## 4. Bootstrap invariants

- initialization idempotent в одной session;
- save/load не запускает повторный spawn;
- stable IDs не зависят от display names;
- exactly one completed campfire и one packed campfire box существуют независимо;
- obsolete Box Workshop entity/item/row/visual не создаются;
- completed campfire всегда находится в `ShaftX - 2 / SurfaceY / Z1`;
- ни один active/completed building не существует на `Z0`;
- packed box остаётся в нижней пещере;
- обе locations валидируются до commit.

## 5. Решённые вопросы

- **Q-DEMO-001:** completed campfire находится на поверхности две клетки левее shaft.
- **Q-DEMO-002:** packed campfire box начинается в нижней пещере; demo не является falling test.
- **Q-DEMO-007:** completed campfire использует exact `Z1`; `Z0` разрешён только для BuildingBox relocation, buildings используют `Z1–Z3`.

## 6. Открытые вопросы

- **Q-DEMO-003:** initial campfire operation/fuel state.
- **Q-DEMO-004:** fog of war для нижней пещеры.
- **Q-DEMO-005:** demo-only или standard sandbox scope.
- **Q-DEMO-006:** обязательны ли campfire и box в одном initial camera framing.

## 7. Acceptance

- fresh start содержит exactly one completed campfire и one packed campfire box;
- completed campfire origin равен `ShaftX - 2 / SurfaceY / Z1`;
- completed campfire отсутствует на `Z0`;
- support, work position и overlap валидны на `Z1`;
- packed box находится в допустимой lower-cave cell;
- repeated initialization не создаёт duplicates;
- оба объекта selectable и находятся в правильных UI lists;
- save/load сохраняет identities и exact XYZ;
- Play Mode проверяет exact campfire `Z1`, отсутствие completed building на `Z0`, box cave cell, visuals и selection;
- generalized falling проверяется отдельно в #396.

## 8. Decision log

| Date | Decision | Confirmed by |
|---|---|---|
| 2026-07-25 | Completed campfire and packed box are separate; box starts in the lower cave. | User |
| 2026-07-29 | Completed campfire is on the surface two cells left of the shaft. | User |
| 2026-08-01 | Completed campfire uses exact `Z1`; `Z0` is only for physical BuildingBox relocation. | User |
| 2026-08-04 | Obsolete Box Workshop полностью удалён; demo содержит только authoritative campfire + packed campfire box. | User |
