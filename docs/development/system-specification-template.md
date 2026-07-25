# <Название системы>

Статус: `DRAFT | QUESTIONNAIRE | APPROVED | IMPLEMENTED | VERIFIED`.

Tracking issue: `<URL>`.

Связанные системы: `<links>`.

## 1. Назначение и границы

Что система делает, чего не делает и какой observable игровой результат обеспечивает.

## 2. Подтверждённый пользовательский workflow

Пошаговые сценарии:

- запуск;
- нормальное выполнение;
- повторное использование;
- отмена;
- blocked/failure/retry;
- взаимодействие с несколькими объектами или residents.

Неутверждённые предположения сюда не включаются.

## 3. Владение состоянием

Для каждого mutable state указать ровно одного authoritative owner. Производные Presentation/Navigation/read-model данные перечислить отдельно.

## 4. Модель данных

Stable IDs, snapshots, definitions, runtime entities, versions и связи.

## 5. Commands, events и queries

- Commands — намерение изменить состояние;
- Events — произошедшие факты;
- Queries — чтение без side effects.

## 6. Состояния и переходы

State machine, guards, terminal states, retries и recovery.

## 7. Input, UI и Presentation

Pointer/keyboard priority, cursor, selection, panels, status, animation и accessibility. Presentation не изменяет authoritative state напрямую.

## 8. Зависимости и конфликты

Какие системы вызываются, как разрешается конкуренция за residents/items/cells/positions и какой порядок приоритетов.

## 9. Инварианты

Проверяемые правила количества, ownership, reservations, identity, occupancy и lifecycle.

## 10. Save/Load и migration

Что сохраняется, что пересчитывается, как восстанавливаются active operations и как обрабатываются старые версии.

## 11. Диагностика

Какие reasons, routes, reservations, stages, selected targets и transitions видны в runtime inspector/logs.

## 12. Тестовая матрица

- Domain unit;
- Application/integration;
- deterministic simulation;
- save/load/migration;
- performance при необходимости;
- Unity Play Mode/end-to-end.

## 13. Acceptance

Полные observable сценарии, а не проверки наличия методов или строк.

## 14. Открытые вопросы

Нумерованный опросник. Для каждого вопроса указать варианты и влияние на соседние правила.

## 15. Журнал решений

| Дата | Решение | Кто подтвердил | Изменённые разделы/issues |
|---|---|---|---|
