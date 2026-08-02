# Размножение Вукеров / пещерных монстров

Статус: `QUESTIONNAIRE`.

Tracking issue: будет создан после публикации draft.

Связанные системы:

- [`ecology-creatures-and-special-drops.md`](ecology-creatures-and-special-drops.md);
- [`enemy-combat-and-cave-encounters.md`](enemy-combat-and-cave-encounters.md);
- [`combat-spatial-execution.md`](combat-spatial-execution.md);
- [`item-interaction-capabilities.md`](item-interaction-capabilities.md);
- [`save-load-and-migrations.md`](save-load-and-migrations.md).

## 1. Назначение и границы

Система должна реализовать полный deterministic lifecycle размножения вида `enemy.vuker`, который в текущем UI называется «Пещерный монстр»: образование допустимой пары, cooldown, рождение детёныша, рост, свободное/похищенное состояние, взросление, приручение, population cap, combat/ecology integration и save/load.

Хищная лиана относится к растениям и не входит в эту систему. Размножение серного Вукера использует отдельный content profile и не включается автоматически без отдельного утверждения числовых значений.

## 2. Подтверждённый пользовательский workflow

Подтверждено parent specification и issue #149:

- fresh demo создаёт пару диких `enemy.vuker`;
- reproduction cooldown равен 7 игровым дням;
- одна пара имеет максимум 3 успешных reproduction cycles;
- детёныш взрослеет за 3 игровых дня;
- свободного Inventory-backed детёныша можно подобрать через общий item-interaction contract;
- похищенный детёныш становится приручённым guard creature поселения;
- приручённый Вукер не размножается;
- spawn/reproduction transaction не превышает data-driven population cap;
- authoritative ecology state, age, cycle counters, cooldown, tame owner и deterministic state сохраняются.

## 3. Владение состоянием

Подтверждено:

- Ecology владеет species identity, pair identity, age/growth, reproduction counters/cooldown и wild/tamed lifecycle;
- Agents/Combat владеют actor position, Health, hostility, intents и attacks взрослого/боеспособного существа;
- Inventory владеет physical free-child item location и pickup transaction;
- Factions владеют hostile/tamed membership;
- Presentation только проецирует `Child`/`Adult`, `Hostile`/`Tamed`, growth и combat state.

Точная атомарная граница преобразования child item в guard actor остаётся открытой.

## 4. Модель данных

Предлагаемые authoritative сущности после утверждения workflow:

- `VukerIndividualId` / существующий `EntityId`;
- `VukerPairId` со stable ordered parent IDs;
- lifecycle `Child | Adult`;
- disposition `Wild | Tamed`;
- birth tick/day, adulthood due tick/day;
- pair successful cycle count `0..3`;
- next reproduction due day;
- optional linked Inventory stack while child physical/free;
- optional tame owner/settlement faction;
- version and deterministic sequence.

## 5. Commands, events и queries

После утверждения:

- commands: register pair, advance ecology day/tick, commit birth, pick up child, convert/tame child, mature child;
- events: pair formed, reproduction blocked/committed, child born/picked/tamed/matured, pair broken/reformed;
- queries: due pairs, population/cap, child lifecycle/location, blocked reason, next due day.

## 6. Состояния и переходы

```text
Wild adult + eligible partner
 -> PairCooldown
 -> BirthDue
 -> Child born | Blocked retry
 -> PairCooldown (до 3 successful cycles)

Wild child
 -> Growing
 -> Wild adult

Wild child + pickup
 -> OPEN: carried child или immediate tamed guard
 -> Tamed guard

Any tamed Vuker
 -> reproduction disabled
```

## 7. Input, UI и Presentation

Подтверждено:

- child/adult используют существующий Vuker rig с разными lifecycle variants;
- hostile/tamed используют разные disposition markers;
- Presentation не создаёт ребёнка и не выполняет taming.

Открыты точный pickup modifier/cursor, carried-state UI и место появления guard actor.

## 8. Зависимости и конфликты

- reproduction выполняется после authoritative population/cap check;
- combat/death/taming могут сделать parent неeligible;
- Inventory pickup не должен оставлять одновременно item и actor;
- spawn-cell selection использует Navigation/World и deterministic ordering;
- concurrent due pairs не могут превысить cap или занять одну physical cell.

## 9. Инварианты

- один child identity имеет не более одного authoritative owner/location;
- одна birth transaction создаёт не более одного lifecycle result;
- failed/blocked retry не расходует successful cycle;
- pair successful cycles не превышают 3;
- tamed individual не участвует в pair/reproduction;
- save/load/retry не дублируют child;
- population cap проверяется атомарно.

## 10. Save/Load и migration

Сохраняются individual/pair identity, lifecycle, birth/adulthood timing, pair cycle count, next due time, disposition/tame owner, linked item/actor identity и deterministic sequence. Derived route, visual growth progress, target selection и animation не сохраняются.

Нужна новая save migration и registration в production composition после утверждения atomic item-to-actor transition.

## 11. Диагностика

Inspector должен показывать pair ID/parents, eligibility, completed cycles, next due day, population/cap, child age/adulthood due, item/actor owner, disposition и последний blocked reason.

## 12. Тестовая матрица

- Domain: pair eligibility, cooldown, three-cycle limit, tamed exclusion, deterministic child ID;
- Application: atomic cap + birth + item/actor ownership, death/pickup/taming races;
- deterministic simulation: several pairs due together, blocked spawn retry;
- save/load/migration: active cooldown, growing child, carried/tamed transition;
- Unity Play Mode: birth, visible child growth, pickup/taming, adulthood and no tamed reproduction.

## 13. Acceptance

После ответов система должна пройти полный observable workflow от due pair до рождения, роста и повторного цикла, включая blocked retry, death/taming interruption, population cap, save/load и Play Mode evidence.

## 14. Открытые вопросы

1. **Формирование пары.** Пара закрепляется навсегда при fresh spawn, или любой свободный взрослый дикий Вукер может детерминированно образовать новую пару? Могут ли повзрослевшие дети создавать пары между собой/с другими взрослыми?
2. **Результат одного цикла.** Один успешный цикл создаёт ровно одного детёныша?
3. **Первый cooldown.** Первая пара рождает через 7 дней после fresh spawn, а затем ещё через 7 дней после каждого успешного рождения?
4. **Population cap.** Какой cap установить для обычного `enemy.vuker` в одной connected cave region/мире?
5. **Место рождения.** Детёныш появляется в ближайшей legal свободной клетке к какому parent: stable-lowest parent, матери/выбранному reproductive owner или между обоими? Что делать, если клетки нет: оставлять birth due и повторять позже без расхода цикла?
6. **Поведение ребёнка.** Дикий детёныш до взросления патрулирует, убегает/не сражается или сразу использует обычный hostile combat profile с уменьшенным visual scale?
7. **Pickup и taming.** Сохраняем актуальное правило ordinary LMB или старое правило `Alt+LMB` из раннего комментария #149? После pickup ребёнок сразу превращается в guard actor рядом с resident/поселением либо остаётся physical carried item до отдельного release/use?
8. **Разрыв пары.** Если один parent погиб, приручён или удалён, surviving parent навсегда теряет эту пару либо может сформировать новую; сохраняется ли уже использованный pair cycle count?
9. **Рост вне мира.** Продолжаются ли 3 дня взросления, пока ребёнок находится в personal/building inventory, или growth идёт только в свободном/guard actor состоянии?

## 15. Журнал решений

| Дата | Решение | Кто подтвердил | Изменённые разделы/issues |
|---|---|---|---|
| 2026-08-02 | Создан focused questionnaire; подтверждённые parent rules отделены от отсутствующих observable решений | ChatGPT по запросу пользователя | draft, #149 |
