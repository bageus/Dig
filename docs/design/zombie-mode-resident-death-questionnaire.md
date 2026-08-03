# Zombie mode: превращение погибшего resident

Статус: `QUESTIONNAIRE`.

Tracking issue: [#586](https://github.com/bageus/Dig/issues/586).

Связанные системы: [`death-graves-resurrection-and-rejuvenation.md`](death-graves-resurrection-and-rejuvenation.md), [`enemy-combat-and-cave-encounters.md`](enemy-combat-and-cave-encounters.md), [`combat-spatial-execution.md`](combat-spatial-execution.md), [`save-load-and-migrations.md`](save-load-and-migrations.md).

## 1. Назначение и границы

Система определяет альтернативный death outcome только для zombie mode. Она не меняет обычный режим: там умерший resident покидает active roster/selection, его личные stacks переходят в клетку смерти и создаётся поднимаемый identity-linked колпак для надгробия или будущего возвращения.

Zombie mode подтверждённо не создаёт колпак. Погибший resident становится враждебным зомби. Детали game-mode activation, Inventory outcome, identity conversion, combat profile, повторной смерти и migration пока не утверждены.

## 2. Подтверждённый пользовательский workflow

- запуск: игра уже работает в zombie mode; способ выбора режима пока открыт;
- resident умирает от любого authoritative death cause;
- resident удаляется из active resident roster и resident-selection;
- identity cap не создаётся;
- создаётся hostile zombie outcome, связанный с погибшим resident;
- зомби отображается и выбирается только как hostile creature/enemy;
- обычный режим продолжает использовать cap → надгробие и будущий resurrection workflow.

Повторное выполнение, Inventory spill, delay, second death и save migration требуют решений ниже.

## 3. Владение состоянием

Подтверждённая граница:

- Society/Lifecycle владеет фактом смерти и provenance погибшего resident;
- game-mode state владеет выбранным режимом; owner ещё не реализован;
- Combat/Ecology или отдельный enemy lifecycle owner должен владеть zombie actor; точный owner открыт;
- Inventory владеет только фактически выпавшими/переносимыми stacks; outcome Inventory открыт;
- Presentation только проецирует roster removal, hostile visual, hover и notification.

Нельзя одновременно хранить живого resident и отдельного zombie actor как две активные gameplay identity без явного provenance/terminal transition.

## 4. Модель данных

Минимально потребуются stable game-mode id, death outcome, provenance `ResidentId/DeathInstanceId`, zombie entity identity, faction/disposition, combat definition, visual variant и save version. Выбор same-id versus new enemy id открыт.

## 5. Commands, events и queries

Ожидаемые факты, названия не утверждены:

- death event от Agents/Society;
- mode query без side effects;
- Application conversion command/handler;
- one-time zombie-created event;
- query для provenance и Presentation.

Событие смерти не должно быть скрытой командой в Presentation. Conversion координирует Application.

## 6. Состояния и переходы

Подтверждено только:

`LivingResident -> DeceasedResident + HostileZombie`, без identity cap.

Открыты transition delay, Inventory transfer, zombie death, capture/return и retry semantics.

## 7. Input, UI и Presentation

- погибший resident не остаётся в active roster;
- zombie не выбирается как resident и не открывает resident inventory/panel;
- hostile hover/selection должен использовать общий enemy input contract;
- имя/provenance в hover, notification и chronicle открыты.

## 8. Зависимости и конфликты

Conversion выполняется после authoritative death commit и resident job/action/reservation cleanup. Один death instance создаёт ровно один mode-specific outcome. Direct commands, sleep/eat/work и resident selection после death запрещены.

Отношение zombies к другим hostile factions/creatures открыто.

## 9. Инварианты

- ordinary mode: cap outcome; zombie mode: zombie outcome; одновременно оба запрещены;
- один death instance конвертируется не более одного раза;
- dead resident не остаётся active worker/roster member;
- conversion не дублирует Inventory или actor после replay/save-load;
- Presentation не создаёт zombie actor;
- game-mode check читается из одного authoritative owner.

## 10. Save/Load и migration

Должны сохраняться mode id, terminal resident death, zombie provenance/state и exactly-once conversion marker. Политика загрузки старого ordinary save в zombie mode и ретроактивной конвертации открыта.

## 11. Диагностика

Нужны mode id, death instance, chosen outcome, zombie entity id, provenance resident id, faction, current combat intent, conversion event id и blocked reason.

## 12. Тестовая матрица

- Domain: mutually exclusive death outcomes, identity/provenance, second death;
- Application: cleanup → conversion ordering, idempotent replay, Inventory policy;
- deterministic: multiple simultaneous deaths and enemy acquisition;
- save/load/migration: no duplicate zombie/cap;
- Unity Play Mode: resident death → roster removal → zombie visual/hostile selection → attack → reload/second death.

## 13. Acceptance

Acceptance станет executable после ответов на раздел 14. Source-contract или наличие строки `zombie` не считается runtime evidence.

## 14. Открытые вопросы

1. Zombie mode выбирается только при создании новой игры, является отдельным scenario preset или может переключаться в существующем save?
2. Личные вещи погибшего выпадают в клетку смерти, остаются на zombie как loot/equipment или уничтожаются?
3. Conversion происходит на том же simulation tick и в той же клетке либо существует corpse/delay?
4. Zombie использует тот же `EntityId` с новым faction/lifecycle или новый enemy id с immutable `ResidentId/DeathInstanceId` provenance?
5. Какой стартовый data-driven combat/navigation profile используется: health, sight, attack, move cadence и разрешённые climb/depth edges?
6. Zombie атакует только living residents, всех non-zombies или следует общей hostile faction policy вместе с cave enemies?
7. Что происходит после уничтожения zombie: нет drop, обычный loot, corpse/resource; возможен ли когда-либо resurrection этого resident?
8. Показывать ли прежнее имя/историю в hostile hover, death notification и chronicle?
9. При загрузке ordinary save в zombie mode уже умершие residents остаются ordinary deaths или ретроактивно превращаются?

## 15. Журнал решений

| Дата | Решение | Кто подтвердил | Изменённые разделы/issues |
|---|---|---|---|
| 2026-08-03 | В zombie mode погибший resident не создаёт именной колпак и становится враждебным зомби. Ordinary cap/надгробие workflow не меняется. | Пользователь в проектном чате | Sections 1–9, #586 |
