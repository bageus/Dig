# Стартовый demo-сценарий: готовый костёр и упакованный BuildingBox

Статус: `QUESTIONNAIRE`.

Tracking issue: [#389](https://github.com/bageus/Dig/issues/389).

Связанные документы:

- [`building-box-placement-and-packing.md`](building-box-placement-and-packing.md);
- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md);
- [`entity-fall-knockback-and-vertical-shafts.md`](entity-fall-knockback-and-vertical-shafts.md);
- [`../implementation/campfire-building-box-content.md`](../implementation/campfire-building-box-content.md);
- [`../implementation/campfire-placement-policy.md`](../implementation/campfire-placement-policy.md).

## 1. Назначение

Demo bootstrap должен одновременно показывать completed building workflow и BuildingBox workflow без ручной подготовки сцены. На текущем этапе demo не обязана демонстрировать сам процесс падения предметов.

## 2. Подтверждённый состав

После новой demo session существуют:

1. ровно один completed campfire в нижней пещере на валидной плоской поверхности;
2. ровно один отдельный campfire BuildingBox в Inventory world location в нижней пещере.

Это две разные сущности и две независимые единицы количества. Completed campfire не является visual representation коробки.

Коробка начинается уже внизу. Она не создаётся в vertical tunnel и не должна мгновенно телепортироваться туда при bootstrap.

## 3. Подтверждённое UI-поведение

- completed campfire отображается и выбирается как building;
- его выбор открывает building roster и highlights row;
- BuildingBox отображается в мире и в building roster как box row;
- обычный LMB выбирает коробку и показывает кнопку «Распаковать»;
- placement начинается только после кнопки «Распаковать»;
- `Alt + ЛКМ` создаёт pickup order выбранному resident;
- оба visual находятся в игровой зоне и соответствуют logical cells.

## 4. Связь с будущей gravity системой

Demo-коробка не является тестом визуального падения. Общая система падения в будущем должна применяться ко всем предметам, а также к residents/enemies, которых можно сбить в vertical shaft. Это отдельная система и tracking issue [#396](https://github.com/bageus/Dig/issues/396).

Изменение demo-позиции коробки не должно использоваться как замена тестам generalized fall resolver.

## 5. Bootstrap invariants

- initialization idempotent внутри одной session;
- save/load не запускает повторное создание объектов;
- stable IDs не зависят от display names;
- bootstrap использует обычные Domain/Application APIs или явно обозначенный fixture path;
- demo content не изменяет production rules основной игры;
- exactly one completed campfire и one packed campfire box существуют независимо.

## 6. Решённые вопросы

- **Q-DEMO-002:** campfire BuildingBox начинается в нижней пещере; current demo не показывает процесс падения.

## 7. Открытые вопросы

- **Q-DEMO-001:** фиксированные координаты или deterministic valid-cell selection внутри нижней пещеры?
- **Q-DEMO-003:** initial campfire operation/fuel state.
- **Q-DEMO-004:** fog of war для нижней пещеры.
- **Q-DEMO-005:** scope — только development demo scene или стандартный sandbox start?
- **Q-DEMO-006:** должны ли completed campfire и box быть видимы в одном начальном camera framing или достаточно доступности через navigation/camera movement?

## 8. Acceptance

- fresh start содержит exactly one completed campfire и one packed campfire box;
- оба объекта находятся в нижней пещере на допустимых world locations;
- повторная initialization не создаёт третью сущность;
- оба объекта видимы, selectable и находятся в roster;
- world box LMB выбирает её, а «Распаковать» включает placement;
- save/load сохраняет состав;
- Play Mode fixture проверяет logical ids, cells, visuals и UI selection;
- generalized falling проверяется отдельно в #396, а не через bootstrap teleport.
