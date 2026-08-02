# Размножение Вукеров / пещерных монстров

Статус: `QUESTIONNAIRE`.

Tracking issue: [#569](https://github.com/bageus/Dig/issues/569).

Связанные системы:

- [`ecology-creatures-and-special-drops.md`](ecology-creatures-and-special-drops.md);
- [`enemy-combat-and-cave-encounters.md`](enemy-combat-and-cave-encounters.md);
- [`combat-spatial-execution.md`](combat-spatial-execution.md);
- [`item-interaction-capabilities.md`](item-interaction-capabilities.md);
- [`save-load-and-migrations.md`](save-load-and-migrations.md).

## 1. Назначение и границы

Система должна реализовать полный deterministic lifecycle размножения вида `enemy.vuker`, который в UI называется «Пещерный монстр»: pair ownership, cooldown, birth, child growth, pickup/taming, adulthood, population cap, combat/ecology integration и save/load.

Хищная лиана относится к растениям и в эту систему не входит. Серный Вукер имеет отдельный species/content profile и не включается автоматически.

## 2. Подтверждённый пользовательский workflow

Подтверждено parent specification и issue #149:

- fresh demo создаёт пару диких `enemy.vuker`;
- reproduction cooldown равен 7 игровым дням;
- одна пара имеет максимум 3 успешных reproduction cycles;
- детёныш взрослеет за 3 игровых дня;
- свободный child является Inventory-backed physical entity и может быть подобран через общий item-interaction contract;
- похищенный child становится приручённым guard creature поселения;
- приручённый Вукер не размножается;
- spawn/reproduction transaction не превышает data-driven population cap;
- lifecycle, cooldown, cycles, tame state и deterministic state сохраняются.

## 3. Владение состоянием

- Ecology владеет individual/pair identity, age/growth, cycles/cooldown и wild/tamed lifecycle.
- Agents/Combat владеют actor position, Health, hostility, intents и attacks.
- Inventory владеет physical free-child item location и pickup transaction.
- Factions владеют hostile/tamed membership.
- Presentation только проецирует `Child`/`Adult`, `Hostile`/`Tamed`, growth и combat state.

Точная атомарная граница item-to-guard transition остаётся открытой.

## 4. Модель данных

После утверждения workflow потребуются stable individual/pair IDs, lifecycle `Child | Adult`, disposition `Wild | Tamed`, birth/adulthood timing, pair cycle count `0..3`, next reproduction due time, optional linked child Inventory stack, optional tame owner, version и deterministic sequence.

## 5. Commands, events и queries

- Commands: register/form pair, advance ecology, commit birth, pick up/tame child, mature child.
- Events: pair formed/broken, reproduction blocked/committed, child born/picked/tamed/matured.
- Queries: due pairs, population/cap, child lifecycle/location, blocked reason, next due time.

## 6. Состояния и переходы

```text
Wild adult + eligible partner
 -> PairCooldown
 -> BirthDue
 -> Child born | Blocked retry
 -> PairCooldown (до 3 successful cycles)

Wild child -> Growing -> Wild adult
Wild child + pickup -> OPEN item-to-guard transition -> Tamed guard
Any tamed Vuker -> reproduction disabled
```

## 7. Input, UI и Presentation

Child/adult используют существующий Vuker rig с разными lifecycle variants; hostile/tamed используют разные markers. Открыты pickup modifier, carried-state UI и место появления guard actor.

## 8. Зависимости и конфликты

- combat death/taming может сделать parent неeligible;
- Inventory pickup не должен оставлять одновременно item и actor;
- spawn использует Navigation/World и deterministic ordering;
- concurrent due pairs не могут превысить cap или занять одну physical cell.

## 9. Инварианты

- один child identity имеет одного authoritative owner/location;
- birth/retry/save-load не создают duplicate;
- failed/blocked attempt не расходует successful cycle;
- pair successful cycles `0..3`;
- tamed individual не участвует в reproduction;
- cap проверяется атомарно.

## 10. Save/Load и migration

Сохраняются individual/pair identity, lifecycle, birth/adulthood timing, pair cycle count, next due time, disposition/tame owner, linked item/actor identity и deterministic sequence. Route, visual progress, target selection и animation пересчитываются. Нужна новая save migration после утверждения atomic item-to-actor transition.

## 11. Диагностика

Inspector показывает pair/parents, eligibility, cycles, next due time, population/cap, child age/adulthood due, item/actor owner, disposition и blocked reason.

## 12. Тестовая матрица

- Domain: pair eligibility, cooldown, three-cycle limit, tamed exclusion, deterministic child ID;
- Application: atomic cap + birth + ownership, death/pickup/taming races;
- deterministic simulation: several due pairs, blocked spawn retry;
- save/load/migration: cooldown, growing child, carried/tamed transition;
- Unity Play Mode: birth, visible growth, pickup/taming, adulthood, no tamed reproduction.

## 13. Acceptance

После ответов должен проходить полный observable workflow от due pair до рождения, роста и повторного цикла, включая blocked retry, death/taming interruption, cap, save/load и фактический Play Mode evidence.

## 14. Открытые вопросы

1. Пара закрепляется навсегда при fresh spawn или взрослые дикие Вукеры могут детерминированно образовывать новые пары? Могут ли взрослые дети участвовать в pairing?
2. Один успешный cycle создаёт ровно одного детёныша?
3. Первый child рождается через 7 дней после fresh spawn, затем cooldown снова отсчитывается от каждого successful birth?
4. Какой population cap установить для `enemy.vuker`: на connected cave region или на весь мир?
5. Детёныш появляется у stable-lowest parent, у отдельного reproductive owner или между parents? При отсутствии клетки birth остаётся due и повторяется без расхода cycle?
6. До взросления child патрулирует, убегает и не сражается либо сразу использует hostile combat profile?
7. Pickup использует актуальный ordinary LMB или старый `Alt+LMB` из комментария #149? После pickup child сразу становится guard actor либо остаётся carried item до отдельного release/use?
8. После смерти/приручения одного parent surviving adult может re-pair? Сохраняется ли cycle count старой пары?
9. Продолжаются ли 3 дня роста внутри personal/building inventory?

## 15. Журнал решений

| Дата | Решение | Кто подтвердил | Изменённые разделы/issues |
|---|---|---|---|
| 2026-08-02 | Создан focused questionnaire; подтверждённые правила отделены от открытых observable решений | ChatGPT по запросу пользователя | #569, #149 |
