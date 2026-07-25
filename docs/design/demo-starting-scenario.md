# Стартовый demo-сценарий: готовый костёр и упакованный BuildingBox

Статус: `QUESTIONNAIRE`.

Tracking issue: [#389](https://github.com/bageus/Dig/issues/389).

Связанные документы:

- [`building-box-placement-and-packing.md`](building-box-placement-and-packing.md);
- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md);
- [`../implementation/campfire-building-box-content.md`](../implementation/campfire-building-box-content.md);
- [`../implementation/campfire-placement-policy.md`](../implementation/campfire-placement-policy.md).

## 1. Назначение

Demo bootstrap должен одновременно показывать completed building workflow и BuildingBox workflow без ручной подготовки сцены.

## 2. Подтверждённый состав

После новой demo session существуют:

1. ровно один completed campfire в нижней пещере на валидной плоской поверхности;
2. ровно один отдельный campfire BuildingBox в Inventory world location.

Это две разные сущности и две независимые единицы количества. Completed campfire не является visual representation коробки.

## 3. Подтверждённое UI-поведение

- completed campfire отображается и выбирается как building;
- его выбор открывает building roster и highlights row;
- BuildingBox отображается в мире и в building roster как box row;
- box поддерживает placement и `Alt + ЛКМ` pickup согласно общим системам;
- оба visual находятся в игровой зоне и соответствуют logical cells.

## 4. Bootstrap invariants

- initialization idempotent внутри одной session;
- save/load не запускает повторное создание объектов;
- stable IDs не зависят от display names;
- bootstrap использует обычные Domain/Application APIs или явно обозначенный fixture path;
- demo content не изменяет production rules основной игры.

## 5. Открытые вопросы

- **Q-DEMO-001:** фиксированные координаты или deterministic valid-cell selection?
- **Q-DEMO-002:** стартовая позиция коробки: поверхность, нижняя пещера или vertical tunnel для демонстрации gravity?
- **Q-DEMO-003:** initial campfire operation/fuel state.
- **Q-DEMO-004:** fog of war для нижней пещеры.
- **Q-DEMO-005:** scope — только development demo scene или стандартный sandbox start?

## 6. Acceptance

- fresh start содержит exactly one completed campfire и one packed campfire box;
- повторная initialization не создаёт третью сущность;
- оба объекта видимы, selectable и находятся в roster;
- box pickup/placement и building selection проходят end-to-end;
- save/load сохраняет состав;
- Play Mode fixture проверяет logical ids, cells, visuals и UI selection.
