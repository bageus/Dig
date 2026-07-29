# Стартовый demo-сценарий: готовый костёр и упакованный BuildingBox

Статус: `QUESTIONNAIRE`; расположение completed campfire подтверждено 2026-07-29.

Tracking issue: [#389](https://github.com/bageus/Dig/issues/389).

Связанные документы:

- [`building-box-placement-and-packing.md`](building-box-placement-and-packing.md);
- [`building-production-and-internal-supply.md`](building-production-and-internal-supply.md);
- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md);
- [`entity-fall-knockback-and-vertical-shafts.md`](entity-fall-knockback-and-vertical-shafts.md);
- [`../implementation/campfire-building-box-content.md`](../implementation/campfire-building-box-content.md);
- [`../implementation/campfire-placement-policy.md`](../implementation/campfire-placement-policy.md).

## 1. Назначение

Demo bootstrap одновременно показывает completed workstation workflow и BuildingBox workflow без ручной подготовки сцены. Текущий demo не обязан демонстрировать процесс падения предметов.

## 2. Подтверждённый состав и расположение

После новой demo session существуют две разные сущности и две независимые единицы количества:

1. ровно один completed campfire на поверхности;
2. ровно один отдельный campfire BuildingBox в Inventory world location в нижней пещере.

Completed campfire использует deterministic anchor относительно authoritative `TunnelDemoLayout`:

```text
origin.X = ShaftX - 2
origin.Y = SurfaceY
origin.Z = ShaftZ
```

Это ровно две logical cells левее вертикального тоннеля на той же surface platform/depth. Bootstrap обязан проверить world bounds, открытый anchor, solid support, отсутствие building/item overlap и доступную work position. Он не может молча выбрать другую поверхность или вернуть campfire в нижнюю пещеру. При нарушении fixture invariant initialization завершается typed/diagnostic failure.

Campfire BuildingBox остаётся в нижней пещере на deterministic valid world location. Коробка не создаётся в vertical tunnel и не телепортируется туда при bootstrap.

## 3. Подтверждённое UI-поведение

- completed campfire отображается и выбирается как building;
- его выбор открывает building roster/functions и production/internal-stock workflow;
- BuildingBox отображается в мире и в building roster как box row;
- ordinary LMB выбирает коробку и показывает кнопку «Распаковать»;
- placement начинается только после кнопки «Распаковать»;
- `Alt + ЛКМ` создаёт pickup order выбранному resident;
- оба visual соответствуют authoritative logical cells.

## 4. Связь с gravity

Demo-коробка не является тестом визуального падения. Общая система падения в будущем применяется ко всем свободным items и отдельно к impacted residents/enemies. Это отслеживается в [#396](https://github.com/bageus/Dig/issues/396).

Изменение demo-позиции объектов не заменяет tests generalized fall resolver.

## 5. Bootstrap invariants

- initialization idempotent внутри одной session;
- save/load не запускает повторное создание объектов;
- stable IDs не зависят от display names;
- bootstrap использует Domain/Application APIs или явно обозначенный fixture path;
- demo content не изменяет production rules основной игры;
- exactly one completed campfire и one packed campfire box существуют независимо;
- completed campfire всегда находится в `ShaftX - 2 / SurfaceY / ShaftZ`;
- packed box остаётся в нижней пещере;
- обе locations валидируются до commit.

## 6. Решённые вопросы

- **Q-DEMO-001:** completed campfire имеет фиксированный layout-relative anchor: две клетки левее vertical shaft на поверхности. Packed box продолжает использовать deterministic valid-cell selection в нижней пещере.
- **Q-DEMO-002:** campfire BuildingBox начинается в нижней пещере; current demo не показывает процесс падения.

## 7. Открытые вопросы

- **Q-DEMO-003:** initial campfire operation/fuel state.
- **Q-DEMO-004:** fog of war для нижней пещеры.
- **Q-DEMO-005:** scope — только development demo scene или standard sandbox start.
- **Q-DEMO-006:** должны ли completed campfire и box быть видимы в одном initial camera framing или достаточно navigation/camera movement.

## 8. Acceptance

- fresh start содержит exactly one completed campfire и one packed campfire box;
- completed campfire origin равен `ShaftX - 2 / SurfaceY / ShaftZ`;
- completed campfire имеет valid support, work position и не пересекает workshop/items;
- packed box находится в нижней пещере на допустимой world location;
- повторная initialization не создаёт третью сущность;
- оба объекта видимы, selectable и присутствуют в правильных UI lists;
- world box LMB выбирает её, а «Распаковать» включает placement;
- save/load сохраняет состав и locations;
- Play Mode fixture проверяет logical IDs, exact campfire cell, box cave cell, visuals и UI selection;
- generalized falling проверяется отдельно в #396.

## 9. Decision log

| Date | Decision | Confirmed by |
|---|---|---|
| 2026-07-25 | Completed campfire and packed campfire box are separate entities; box starts in the lower cave. | User |
| 2026-07-29 | Completed campfire moves to the surface exactly two cells left of the vertical tunnel; packed box remains in the lower cave. | User |
