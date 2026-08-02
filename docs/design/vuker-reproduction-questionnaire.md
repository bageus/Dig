# Размножение Вукеров / пещерных монстров

Статус: `APPROVED`.

Tracking issue: [#569](https://github.com/bageus/Dig/issues/569).

Связанные системы:

- [`ecology-creatures-and-special-drops.md`](ecology-creatures-and-special-drops.md);
- [`enemy-combat-and-cave-encounters.md`](enemy-combat-and-cave-encounters.md);
- [`combat-spatial-execution.md`](combat-spatial-execution.md);
- [`item-interaction-capabilities.md`](item-interaction-capabilities.md);
- [`save-load-and-migrations.md`](save-load-and-migrations.md).

## 1. Назначение и границы

Система реализует deterministic lifecycle обычного Вукера `enemy.vuker`, который в UI называется «Пещерный монстр»: образование и разрыв пар, cooldown, рождение, рост детёныша, похищение, приручение, движение guard creature, population cap и save/load.

Хищная лиана является растением и не входит в эту систему. Серный Вукер использует отдельный species/content profile и не наследует этот balance автоматически.

## 2. Подтверждённый workflow

1. Fresh demo создаёт двух взрослых диких Вукеров в одной connected cave region.
2. Любые два свободных живых взрослых диких Вукера одной region детерминированно образуют пару. Повзрослевшие дети также могут участвовать в pairing.
3. Новая pair identity начинает с нуля successful cycles. Первый child становится due через 7 игровых дней.
4. Один successful cycle создаёт ровно одного child. Следующий cooldown отсчитывается 7 дней от successful birth.
5. Одна pair identity имеет максимум 3 successful cycles.
6. Child появляется в ближайшей legal свободной клетке к stable-lowest parent. При отсутствии клетки или достижении cap birth остаётся due, cycle не расходуется и проверка повторяется.
7. Population cap равен 10 живым обычным Вукерам на connected cave region. Общего world cap нет.
8. Wild child патрулирует, но до взросления не создаёт combat intent, не атакует и не отвечает атакой. Через 3 игровых дня он становится взрослым.
9. Похищение запускается выбранным живым гномом через `Alt+ЛКМ` по wild child. Гном получает direct approach route к текущей клетке child.
10. Пока похищение зарезервировано, child не уходит с клетки. При достижении клетки выполняется exactly-once tame commit без persistent carried-item состояния.
11. Tamed child/Adult переходит во фракцию residents, не размножается и не создаёт combat intent. До maturity он остаётся non-combat child.
12. Tamed Vuker принимает прямые команды перемещения и, если прямой команды нет, автоматически возвращается к ближайшему месту дислокации живых гномов.
13. Смерть или приручение parent разрывает active pair. Surviving wild adult может создать новую pair identity. История старой пары сохраняется; новая пара начинает с 0 cycles.
14. После третьего successful cycle живая пара остаётся связанной, но больше не рождает. Она освобождается для re-pair только при смерти/приручении/удалении parent или расхождении regions.

## 3. Владение состоянием

- `VukerEcologyState`: identity, region, lifecycle, disposition, active pair, pair history, cycles, cooldown, growth, kidnap reservation и blocked reason.
- `AgentState`: authoritative actor position, Health и alive state.
- `FactionState`: hostile/resident membership.
- Combat: intents, executions и attacks; Ecology определяет eligibility child/tamed Vuker.
- Navigation/World: connected cave regions, legal routes и birth cells.
- Presentation: lifecycle/disposition/growth/selection projection; не изменяет authoritative state.

## 4. Модель данных

Individual сохраняет `EntityId`, `Child|Adult`, `Wild|Tamed`, region root, position, alive, birth/maturity tick, optional kidnap resident, optional tame resident, active pair и version.

Pair сохраняет stable `VukerPairId`, ordered parent IDs, region, successful cycles `0..3`, next birth tick, active/terminal state, blocked reason и version.

Временная шкала использует authoritative simulation tick: 24 ticks = 1 игровой день, cooldown = 168 ticks, growth = 72 ticks.

## 5. Commands, events и queries

Commands/use cases:

- register/synchronize Vuker actor;
- advance ecology tick and form pairs;
- plan/commit or block birth;
- reserve/cancel/commit kidnapping;
- issue direct movement to tamed Vuker;
- restore ecology snapshot.

Events:

- registered, pair formed/broken;
- child born, birth blocked, child matured;
- kidnapping reserved/cancelled, child tamed.

Queries:

- individuals/pairs ordered by stable identity;
- due pairs, region population/cap;
- lifecycle/disposition/combat eligibility;
- reservation owner and blocked reason.

## 6. State machine

```text
Wild Adult + eligible Adult -> PairCooldown
PairCooldown --7 days--> BirthDue
BirthDue -> Child born + next cooldown | blocked retry
Pair --3 births--> ExhaustedPair

Wild Child -> patrol/no-combat --3 days--> Wild Adult
Wild Child + Alt+LMB -> Reserved -> resident approach -> Tamed Child
Tamed Child --maturity--> Tamed Adult
Tamed -> direct movement | automatic return to resident deployment

Parent dead/tamed/region changed -> pair broken -> survivor may re-pair
```

## 7. Input, UI и Presentation

- `Alt+ЛКМ` требует ровно одного selected resident и wild child under pointer.
- Hover показывает pickup cursor.
- Ordinary `ЛКМ` по tamed Vuker выбирает его для direct movement.
- `ЛКМ` по legal tunnel destination создаёт common manual tunnel route.
- Child использует Vuker visual в `Child` lifecycle и growth progress; tamed использует `Tamed` disposition.
- Diagnostics показывают pair, due tick, cycles, region population, lifecycle, disposition, reservation и blocked reason.

## 8. Приоритеты и конфликты

- Kidnap reservation принадлежит одному resident; второй resident получает conflict.
- Reserved child не патрулирует.
- Direct tamed movement имеет приоритет над automatic return.
- Wild child и tamed Vuker исключены из autonomous/retaliation combat intent.
- Due pairs обрабатываются по region и pair ID; каждый committed child сразу входит в cap и occupied cells следующей транзакции.
- Failed route отменяет reservation. Death resident/child отменяет active kidnapping без tame commit.

## 9. Инварианты

- одна child identity создаётся один раз;
- `living population <= 10` на region после каждой транзакции;
- blocked birth не расходует cycle и не сдвигает due tick;
- pair cycles не превышают 3;
- exhausted pair не сбрасывает budget через немедленное re-pair тех же живых parents;
- child до maturity не создаёт combat intent;
- tamed Vuker не размножается и не создаёт combat intent;
- одна kidnap reservation имеет одного resident owner;
- save/load/retry сохраняет следующий deterministic result.

## 10. Save/Load и migration

Save format v14 добавляет `VukerEcologySaveData`. Сохраняются individuals, pairs, current tick, pair sequence, lifecycle/disposition, region/position, due/maturity, cycles, reservation/tame owner, terminal/blocked reasons и versions.

Migration `v13 -> v14` создаёт пустой Vuker section с world seed и текущим simulation tick. Derived routes, presentation selection и interpolation пересчитываются.

## 11. Failure и retry

- no legal birth cell/cap: birth остаётся due;
- child identity collision: typed blocked diagnostic, no second actor;
- kidnap route unavailable: reservation отменяется;
- resident/child unavailable before arrival: order отменяется;
- no reachable resident deployment for tamed auto-return: position сохраняется, retry выполняется позже;
- invalid save snapshot: load fails typed, без частичного restore.

## 12. Тестовая матрица

- Domain: pairing/re-pairing, cadence, cycle limit, cap, blocked retry, maturity, taming, snapshot determinism;
- Application: connected regions and stable nearest birth cell;
- Save: v14 round-trip and v13 migration;
- source contracts: runtime wiring, Alt input, common route, combat exclusion;
- Unity Play Mode: 7-day birth, visible non-combat child, kidnapping/taming, direct movement, maturity and no tamed combat.

## 13. Acceptance

Feature получает `IMPLEMENTED` после merge green Quality/build/.NET/source-contract/smoke/soak evidence. `VERIFIED` требует фактического licensed Unity Play Mode execution полного workflow.

## 14. Открытые вопросы

Нет открытых вопросов для ordinary `enemy.vuker` vertical slice. Balance серного Вукера остаётся отдельной системой.

## 15. Журнал решений

| Дата | Решение | Кто подтвердил | Изменённые разделы/issues |
|---|---|---|---|
| 2026-08-02 | Dynamic pairs, one child, 7-day cadence, cap 10 per connected region, blocked retry | пользователь | #569, sections 2/6/9 |
| 2026-08-02 | Child patrols without combat; `Alt+ЛКМ` kidnapping creates tamed directly controllable guard and auto-return | пользователь | #569, sections 2/7/8 |
| 2026-08-02 | Death/taming breaks pair; survivor re-pairs and old pair history remains | пользователь | #569, sections 2/6/9 |
