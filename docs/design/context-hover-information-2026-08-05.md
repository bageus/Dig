# Централизованная hover-информация производства и мировых целей

Status: `APPROVED`

Tracking issue: [#653](https://github.com/bageus/Dig/issues/653)

Related authoritative systems:

- [`runtime-building-ui-health-unpack-pickup-correction-2026-08-04.md`](runtime-building-ui-health-unpack-pickup-correction-2026-08-04.md);
- [`building-production-and-internal-supply.md`](building-production-and-internal-supply.md);
- [`contextual-input-cursors-and-selection.md`](contextual-input-cursors-and-selection.md);
- [`item-interaction-capabilities.md`](item-interaction-capabilities.md);
- [`enemy-combat-and-cave-encounters.md`](enemy-combat-and-cave-encounters.md).

## 1. Решение пользователя от 2026-08-05

Нижняя context-панель имеет один центральный информационный блок. Он показывает сведения о текущем production hover или о точной мировой цели выбранного resident.

Эта спецификация отменяет только правило #634 об отсутствии production hover-информации. Остальные решения #634 сохраняются без изменений: заголовок выбранного строения не возвращается, зелёная платформа не возвращается, health/unpack/pickup corrections остаются authoritative.

## 2. Production hover

При наведении на кнопку производимого предмета или упакованного строения блок расположен по центру над production-кнопками и показывает:

1. `ProductionIconViewModel.DisplayName`;
2. полный список входных материалов из `ProductionIconViewModel.Ingredients`;
3. для каждого материала — catalog display name и `Current/Required`.

Presentation не пересчитывает рецепт и не содержит таблицу имён. Источник истины — `ProductionContentCatalog`, `ItemCatalog` и существующая production view model.

Production hover имеет приоритет над world hover, пока указатель находится над production UI. Наведение на UI продолжает полностью shield-ить world input.

## 3. World target hover

World target hover отображается только при ровно одном выбранном живом resident и только когда тот же authoritative contextual resolver определил доступное observable действие:

- `Pickup`, `DirectUse` или `UseProductionPackage` — имя предмета из `ItemCatalog.DisplayName`, проецируемое в `WorldItemViewModel`;
- attack hostile creature — `EnemyCombatDefinition.DisplayName` для `CreatureVisualSnapshot.SpeciesId`;
- attack/chop специальных мировых объектов — локализованное presentation-имя их authoritative target type.

Отдельный raycast, отдельная таблица item-id и отдельный hover classifier запрещены. Имя обязано следовать exact target, availability и priority того же resolver, который выбирает cursor и committed command.

Примеры acceptance: камень, гриль-гриб, пещерный монстр, дубина.

## 4. Priority и lifecycle

Приоритет информации:

1. production UI hover;
2. доступная world pickup/use/attack цель одного выбранного resident;
3. пустое состояние.

Информация очищается при:

- pointer exit;
- переходе на UI, которое не является production hover;
- потере или изменении selection;
- недоступности, уничтожении или исчезновении exact target;
- закрытии/скрытии context-панели;
- disable/destroy interaction host.

Повторное наведение после очистки обязано восстановить правильное имя и материалы без stale state.

## 5. Commands, state ownership и save/load

Hover-информация является transient presentation state. Она не создаёт domain command/event, не сохраняется и после load вычисляется заново из текущей selection, UI pointer и exact target resolution.

Authoritative gameplay state остаётся в production, inventory, combat и world-object systems. Context HUD владеет только текущей отображаемой строкой; `DigWorldInteraction` передаёт уже разрешённое имя exact target.

## 6. Failure и diagnostics

Недоступная цель не показывает имя как доступную командную цель. Отсутствующая catalog/definition projection считается presentation defect; fallback может вывести стабильный идентификатор только как diagnostic degradation, но не должен создавать другое действие.

## 7. Acceptance evidence

### Unit / source contracts

- production hover форматируется из `DisplayName` и `Ingredients`;
- `InventoryWorldPresenter` проецирует item display name в world item view model;
- enemy display name разрешается из `EnemyCombatDefinition`;
- cursor/action resolution и world hover information используют одну ветку exact resolution;
- production hover priority и clear lifecycle покрыты regression tests.

### Unity Play Mode / equivalent end-to-end

1. открыть production building;
2. навести на output и увидеть имя/материалы над кнопками;
3. уйти с output и убедиться в очистке;
4. выбрать одного resident;
5. последовательно навести на камень, гриль-гриб и дубину и получить их catalog names при доступном pickup/use;
6. навести на пещерного монстра и получить enemy definition name при доступном attack;
7. проверить UI shielding, потерю selection, despawn/invalid target и повторное наведение;
8. повторить сценарий после refresh context panel.
