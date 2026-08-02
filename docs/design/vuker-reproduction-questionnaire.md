# Размножение Вукеров / пещерных монстров

Статус: `APPROVED`.

Tracking issue: [#569](https://github.com/bageus/Dig/issues/569).

Связанные системы:

- [`ecology-creatures-and-special-drops.md`](ecology-creatures-and-special-drops.md);
- [`enemy-combat-and-cave-encounters.md`](enemy-combat-and-cave-encounters.md);
- [`combat-spatial-execution.md`](combat-spatial-execution.md);
- [`contextual-input-cursors-and-selection.md`](contextual-input-cursors-and-selection.md);
- [`save-load-and-migrations.md`](save-load-and-migrations.md).

## 1. Назначение и границы

Система реализует deterministic lifecycle размножения `enemy.vuker`, который отображается как «Пещерный монстр»: динамическое образование пар, cooldown, рождение и рост детёныша, похищение, приручение, population cap, combat/movement integration и save/load.

Хищная лиана является растением и в эту систему не входит. Серный Вукер имеет отдельный content profile и не наследует числовые параметры автоматически.

## 2. Подтверждённый workflow

1. Любые два живых взрослых диких Вукера одной связной cave-region, которые не состоят в активной паре, могут образовать новую пару. Stable-lowest IDs формируют пары детерминированно. Повзрослевшие дети участвуют в формировании пар.
2. У каждой новой пары собственный stable pair ID, `0` successful cycles и первое рождение через `7` игровых дней.
3. Один успешный цикл создаёт ровно одного детёныша. Следующий due-time равен ещё `7` игровым дням после успешного рождения.
4. Одна pair identity выполняет максимум `3` successful cycles.
5. Population cap равен `10` обычных Вукеров на одну связную cave-region. Общего world cap нет.
6. Детёныш рождается в ближайшей legal свободной клетке к stable-lowest parent. При равной дистанции используется stable `CellId`.
7. Если cap достигнут или legal cell отсутствует, birth остаётся due, цикл не расходуется и deterministic retry выполняется позже.
8. Дикий детёныш патрулирует, но не получает combat intent, не атакует и не является допустимым player attack actor до взросления.
9. Взросление занимает `3` игровых дня. Дикий взрослый становится обычным hostile combat actor и может образовывать пары.
10. Похищение запускается только `Alt+ЛКМ` по дикому детёнышу при выбранном живом гноме. Гном строит обычный tunnel route, подходит к детёнышу и завершает один authoritative kidnap commit.
11. После commit детёныш немедленно становится `Tamed`: присоединяется к faction residents, перестаёт размножаться и перестаёт быть hostile. Отдельного persistent Inventory item/carried state после commit нет; это решение заменяет прежнее общее упоминание Inventory-backed child.
12. Приручённый Вукер подчиняется прямым приказам перемещения. Сразу после похищения он автоматически получает route к ближайшему другому живому гному; если другого гнома нет, целью становится похитивший гном. Distance, затем stable resident ID являются tie-break. Новый прямой приказ заменяет этот automatic return route.
13. Если один parent погиб или приручён, active pair разрывается. Surviving wild adult может создать новую pair identity. История и использованные cycles старой pair identity сохраняются; новая пара начинает с `0`.

## 3. Владение состоянием

- `VukerEcologyState`: lifecycle/disposition, pair identities, cycles, due/maturity ticks, kidnap reservation, region identity и blocked reason.
- `AgentState`: authoritative position, alive/Health и движение каждого взрослого, детёныша и приручённого Вукера.
- `FactionState`: hostile/tamed membership.
- Combat: intents/executions/actions только для взрослых hostile/tamed guards согласно hostility.
- Navigation: cave-region connectivity, legal routes и birth-cell candidates.
- Presentation: child/adult scale, disposition marker, patrol/kidnap/movement projection.

## 4. Time and region model

- Ecology использует тот же demo-time contract, что hamster/grub: `24` simulation ticks = один игровой день.
- Reproduction cooldown: `168` ticks.
- Child maturity: `72` ticks.
- Cave-region — connected component open cells, соединённых legal `SupportedWalk`, `VerticalClimb` или `DepthTraverse` edges профиля Вукера. `ShaftGapTraverse` не объединяет regions.

## 5. Commands, events and queries

Commands/use cases:

- synchronize living Vuker actors;
- form deterministic pairs;
- plan/commit due birth;
- mature child;
- reserve/cancel/commit kidnap;
- assign automatic/direct tamed movement.

Events:

- pair formed/broken;
- birth committed/blocked;
- child matured;
- kidnap reserved/cancelled/committed;
- disposition changed.

Queries:

- individual/pair snapshots;
- due pairs and blocked reasons;
- region population/cap;
- child eligibility and current kidnap owner;
- next reproduction/maturity tick.

## 6. State transitions

```text
Wild Adult + eligible Wild Adult
 -> PairedCooldown
 -> BirthDue
 -> Child born | BlockedRetry
 -> PairedCooldown (до 3 cycles)

Wild Child
 -> PatrollingNonCombat
 -> Wild Adult Hostile

Wild Child + Alt+LMB + selected resident
 -> KidnapReserved
 -> ResidentApproach
 -> Tamed Child
 -> AutoReturnToResidents / DirectMovement

Active pair + parent dead/tamed
 -> Broken (history retained)
 -> surviving adult eligible for new pair
```

## 7. Input/UI/Presentation

- `Alt+ЛКМ` на wild child показывает и выполняет kidnap action только при выбранном живом гноме.
- Ordinary LMB на hostile adult сохраняет combat behavior.
- Ordinary LMB на tamed Vuker выбирает creature; следующий LMB на legal tunnel destination создаёт direct movement.
- Child использует Vuker rig с `Child` lifecycle scale и без combat Health bar, пока не участвует в допустимом combat.
- HUD status показывает reserved/approaching/tamed/blocked reason.

## 8. Failure/retry/concurrency

- Один child имеет максимум одну active kidnap reservation.
- Смерть/удаление resident отменяет reservation; child снова патрулирует.
- Смерть/maturity/taming child до commit отменяет stale order без второго result.
- Несколько due pairs обрабатываются по region key/pair ID; cap и выбранные birth cells резервируются внутри одного deterministic pass.
- Failed birth не расходует cycle.
- Repeated input/save/load не дублирует pair, child или kidnap commit.

## 9. Invariants

- максимум `10` живых ordinary Vukers на cave-region;
- одна active pair на individual;
- максимум `3` successful cycles на pair identity;
- один cycle создаёт одного child;
- tamed Vuker не размножается;
- wild child не сражается;
- one kidnap reservation/commit per child;
- pair history сохраняется после break;
- save/load продолжает тот же следующий deterministic result.

## 10. Save/load and migration

Сохраняются individual IDs, lifecycle/disposition, birth/maturity ticks, region key, active pair ID, kidnap owner, pair parents/cycles/next due/active flag, blocked reason, deterministic sequence и version. Actor position/Health сохраняются существующими Agent sections; Faction membership восстанавливается из disposition. Routes и visual interpolation пересчитываются.

Save format получает отдельную Vuker ecology section и migration default для старых saves. Старый save без section восстанавливает только уже существующую fresh pair и формирует её deterministic initial pair state без повторного spawn.

## 11. Diagnostics

Inspector/read model показывает individual lifecycle/disposition/region/pair, birth/maturity ticks, pair cycles/next due/active, kidnap owner, population/cap и last blocked reason.

## 12. Test matrix

- Domain: pairing/re-pairing, 7-day cadence, 3-cycle limit, 10-per-region cap, no-cell retry, maturity, tamed exclusion, kidnap idempotency.
- Application: simultaneous due pairs, actor/faction synchronization, stale kidnap cancellation, auto-return target.
- Deterministic/save: round-trip and same next birth/maturity/pair result.
- Unity source contracts: Alt+LMB route, child non-combat, tamed direct movement and rendering.
- Play Mode: pair -> birth -> child patrol/no combat -> Alt+LMB approach -> tamed auto-return -> direct move; separate maturity-to-hostile path.

## 13. Acceptance

- complete observable workflow above is checked in;
- Quality/build/full .NET/smoke/soaks pass;
- status becomes `IMPLEMENTED` after merge;
- `VERIFIED` requires actual licensed Unity Play Mode evidence.

## 14. Open questions

Нет открытых вопросов для текущего slice.

## 15. Decision log

| Date | Decision | Confirmed by | Files/issues |
|---|---|---|---|
| 2026-08-02 | Dynamic pairs, one child/cycle, 7-day cadence, cap 10 per connected cave, nearest-cell blocked retry, non-combat child, Alt+LMB kidnap, immediate tamed controllable actor, re-pair with retained old pair history | user | #569, #149 |
