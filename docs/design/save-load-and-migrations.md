# Сохранение, загрузка и миграции

Статус: `IMPLEMENTED`.

Tracking issue: [#13](https://github.com/bageus/Dig/issues/13).

Связанные системы: [World](../architecture/systems-gameplay.md#1-мир), [Inventory](../architecture/systems-gameplay.md#2-инвентарь), [Jobs](../architecture/systems-gameplay.md#3-система-работ), [Unity host](../implementation/unity-presentation-host.md), [verification #15](https://github.com/bageus/Dig/issues/15).

## 1. Назначение и границы

Saving координирует deterministic snapshot, запись в слот, чтение, последовательную миграцию и восстановление authoritative owners. Система не становится владельцем мира, инвентаря, работ, жителей, зданий или production state и не сохраняет Presentation, meshes, routes, navigation maps, hover/selection или иные пересчитываемые данные.

## 2. Подтверждённый пользовательский workflow

- Manual save собирает один согласованный `SaveGameContext`, присваивает slot metadata и атомарно заменяет файл.
- Autosave использует тот же builder/loader и отличается только стабильным slot id `autosave`.
- Повторная запись сохраняет предыдущий файл до успешного перемещения временного файла.
- Load сначала десериализует документ, применяет каждую миграцию до текущей версии, валидирует content IDs и cross-references, затем создаёт новые authoritative states.
- Ошибка чтения, неизвестный тип job/content, повреждённый документ или отсутствующая миграция возвращают контролируемую диагностику; live state не заменяется частично.
- После успешной загрузки runtime обязан заменить authoritative owners одним commit и пересчитать navigation/presentation/read-model caches.
- Следующее сохранение загруженного состояния должно быть deterministic-equivalent исходному документу.

## 3. Владение состоянием

- `SaveGameBuilder` читает authoritative owners через `SaveGameContext`.
- `SaveGameLoader` создаёт восстановленные `WorldState`, `InventoryState`, `JobSystem`, `BuildingsState`, agent snapshots, deposits, mining-output ledger, packable executions, ecology, production, supply и barrels.
- `FileSaveSlotStore` владеет только файлами слотов и temporary/backup protocol.
- `SaveMigrationPipeline` владеет только ordered transformation документа между соседними format versions.
- `SaveGameCompositionRoot` является единственным production composition owner для полного списка job codecs и migrations.

## 4. Модель данных

`SaveGameDocument.FormatVersion` определяет schema version. Все entity references используют стабильные `EntityId`; content references используют стабильные material/item/building/recipe/ecology/world-object IDs. Collections сериализуются в стабильном порядке. Job payload содержит stable codec `TypeId`, base retry/dependency fields и type-specific properties.

## 5. Commands, events и queries

- Commands: manual save, autosave, load slot.
- Queries: list slots и metadata/corruption state.
- Saving не публикует gameplay events и не изменяет Domain state во время build.

## 6. Состояния и переходы

Запись: `Build -> Serialize temporary -> Flush -> Move old to backup -> Move temporary to slot -> Delete backup`. При ошибке replacement предыдущий slot восстанавливается.

Загрузка: `Read -> Deserialize -> Migrate vN..vCurrent -> Validate -> Restore owner graph -> Validate references -> Return LoadedGameState`. Любой failure завершает операцию без partial loaded result.

## 7. Input, UI и Presentation

Конкретная save/load panel не является частью #13. UI вызывает Application service и отображает slot metadata либо typed storage/save error. Cursor, selection, HUD panels и scene objects не сериализуются. После authoritative load Presentation полностью обновляется из owners.

## 8. Зависимости и конфликты

Builder получает согласованный context на одном simulation tick. Save не выполняется параллельно с authoritative commit. Load commit имеет приоритет над simulation tick; simulation возобновляется только после замены owners и rebuild derived caches.

## 9. Инварианты

- один stable job type имеет ровно один codec `TypeId`;
- каждый concrete `JobDefinition` production assembly покрыт explicit `JobDefinitionSaveRegistration`;
- migrations идут только `vN -> vN+1` и не имеют двух owners одного source version;
- inventory locations, held references, reservations, assigned agents и building/production references валидны после load;
- mining output commit ledger не допускает повторную выдачу ресурса;
- сохранённые IDs не меняются при round-trip;
- unknown content/type не подменяется fallback definition.

## 10. Save/Load и migration

Текущая версия: `v9`. Production pipeline регистрирует миграции `v0 -> v9`. Production job registry включает excavation, hauling, buildings, production/supply, world pickup, resident placement, mushrooms, barrels, healing и strategic execution. Добавление нового concrete `JobDefinition` без explicit registration должно ломать coverage regression до merge.

## 11. Диагностика

Slot listing сообщает corrupted state и message. Loader возвращает typed errors для unsupported version, invalid document, unknown job type и unknown content definitions. Migration report содержит ordered IDs применённых шагов.

## 12. Тестовая матрица

- Domain/Application: deterministic builder/loader round-trip, IDs, jobs, reservations, inventories, agents и subsystem sections.
- Infrastructure: atomic replacement, interrupted replacement recovery, corruption and slot metadata.
- Migration: fixtures и полный `v0 -> v9` pipeline.
- Coverage: reflection over every concrete `JobDefinition` against production registration registry.
- Unity Play Mode: production composition round-trip spatial excavation job and authoritative XYZ target/work cells.
- Runtime verification: фактический licensed Unity Test Runner result остаётся evidence owner issue #15; до него статус не повышается до `VERIFIED`.

## 13. Acceptance

- builder не сериализует Presentation/meshes/navigation caches;
- atomic write сохраняет предыдущий slot при failure;
- full production registry покрывает каждый current concrete job type;
- loader восстанавливает identity, inventory, jobs/reservations и subsystem owners с cross-reference validation;
- migration pipeline загружает fixture начиная с v0;
- unknown IDs/types возвращают controlled errors;
- spatial excavation vertical slice round-trips через production composition;
- executable Unity Play Mode regression проверяет тот же production composition root.

## 14. Открытые вопросы

Нет открытых business rules в scope #13. Конкретный UX save/load panel и shortcut mapping должны отслеживаться отдельной Presentation issue, если будут добавлены.

## 15. Журнал решений

| Дата | Решение | Кто подтвердил | Изменённые разделы/issues |
|---|---|---|---|
| 2026-07-13 | Saving хранит только authoritative snapshots и использует versioned migrations. | issue #13 | initial architecture and issue |
| 2026-07-27 | Mining-output commit ledger обязан проходить через normal manual/autosave service path. | issue #13 / PR #417 | save context, builder, loader |
| 2026-07-28 | Agent needs, meal action/progress и food intent входят в v9. | issue #13 / PR #485 | v9 migration and agent runtime |
| 2026-07-29 | Один Infrastructure composition root регистрирует полный registered codec/migration set; missing concrete job codec является blocking regression. | user request to complete #13 | this specification, #13 |
