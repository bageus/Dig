# Resident work tool visual projection

Статус: `IMPLEMENTED`.

Tracking issue: [#602](https://github.com/bageus/Dig/issues/602).

Связанные authoritative specifications:

- [`presentation-input-ui-and-diagnostics.md`](presentation-input-ui-and-diagnostics.md);
- [`building-box-placement-and-packing.md`](building-box-placement-and-packing.md);
- [`excavation-command-execution.md`](excavation-command-execution.md);
- [`mushroom-growth-and-chopping.md`](mushroom-growth-and-chopping.md);
- [`enemy-combat-and-cave-encounters.md`](enemy-combat-and-cave-encounters.md).

## 1. Цель

Рука гнома должна визуально отражать текущую authoritative работу, не создавая вторую Inventory-, Job- или save-модель инструмента.

## 2. Владение состоянием

- Inventory и equipment read model остаются единственным источником истины для реальных предметов, включая `weapon.club`.
- JobSystem остаётся владельцем worker, status, stage и target текущей работы.
- Presentation проецирует только временный вид инструмента из immutable job snapshot.
- Кирка, топор и молоток не являются ItemStack, не резервируются, не сохраняются и не участвуют в выборе Job tool.
- Animation и visual lifetime не завершают gameplay command.

## 3. Authoritative mapping

Временный инструмент существует только для назначенного живого resident, когда Job имеет status `InProgress` и stage `PerformWork`.

| Authoritative job | Временный инструмент |
|---|---|
| `DigJobDefinition` | кирка |
| `SpatialDigJobDefinition` | кирка |
| `MushroomChopJobDefinition` | топор |
| `BuildingWorkJobDefinition` с kind `Construction` | молоток |
| `BuildingBoxAssemblyJobDefinition` | молоток |
| `BuildingBoxPackingJobDefinition` | молоток |
| все остальные jobs и stages | отсутствует |

`BuildingWorkKind.Repair`, `BuildingWorkKind.Demolition` и `ProductionWorkJobDefinition` не получают молоток автоматически: их отдельные visual rules пользователь не подтверждал, а производство еды или предметов не является сборкой/упаковкой строения.

## 4. Приоритет правой руки

1. Временный инструмент активной работы.
2. Реальный предмет из текущего equipment read model.
3. Пустая рука.

Временный инструмент скрывает, но не изменяет реальный предмет. После completion, interruption, cancellation, reassignment, stage change или удаления resident реальный предмет немедленно восстанавливается из актуального read model.

Во время атаки synthetic club не создаётся. Если resident реально держит `weapon.club`, отображается именно этот Inventory-owned предмет; без дубины рука остаётся без дубины.

## 5. Presentation contract

- Все варианты используют существующий `RightHand` socket.
- `weapon.club`, кирка, топор и молоток имеют разные collider-free silhouettes.
- Один visual owner переиспользуется при смене предмета; старые children удаляются перед созданием нового вида.
- Несколько residents проецируются независимо по stable resident id.
- Hover/selection, movement, combat, save/load и gameplay reservations не меняются.

## 6. Acceptance

### Unit / integration

- exact job definitions проецируют ожидаемый typed tool kind;
- non-`PerformWork` stages проецируют `None`;
- construction/BuildingBox assembly/packing публикуют authoritative `WorkPosition`;
- transient override не изменяет equipment model и восстанавливает его после очистки;
- club, pickaxe, axe и hammer создают разные geometry markers.

### Unity / Play Mode

Checked-in сценарий проверяет последовательность:

`real club -> pickaxe -> axe -> hammer -> real club -> empty hand`.

Также проверяются отсутствие colliders и очистка transient visual после завершения работы.

Фактический запуск EditMode/PlayMode на лицензированном Unity runner остаётся обязательным для перехода в `VERIFIED`.