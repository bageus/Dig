# Смерть врага, выпадение содержимого и растворение трупа

Статус: `QUESTIONNAIRE` — основной workflow подтверждён, открыт только визуальный тайминг трупа.

Tracking issue: [#559](https://github.com/bageus/Dig/issues/559).

Parent authoritative specifications:

- [`enemy-combat-and-cave-encounters.md`](enemy-combat-and-cave-encounters.md);
- [`ecology-creatures-and-special-drops.md`](ecology-creatures-and-special-drops.md);
- [`combat-spatial-execution.md`](combat-spatial-execution.md);
- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md).

## 1. Подтверждённое правило — 2026-08-04

Когда authoritative Health врага достигает `0`:

1. враг умирает ровно один раз;
2. cell смерти фиксируется из authoritative `AgentState.Position` в момент lethal commit;
3. враг немедленно перестаёт быть допустимой целью для новых attack commands, patrol, movement, work и других живых actor workflows;
4. его active combat intent/execution завершаются через typed death cleanup;
5. visual врага падает и остаётся лежащим трупом в cell смерти;
6. всё содержимое, которое уже принадлежало этому врагу, атомарно оказывается на земле в той же exact XYZ cell;
7. после corpse lifecycle труп исчезает через эффект растворения;
8. выпавшие предметы не растворяются вместе с трупом и продолжают существовать как обычные world items.

## 2. Что считается содержимым врага

В death-release входят все физические Inventory entities, authoritative location которых принадлежит умершему enemy actor:

- carried/equipped/held предметы;
- обычные enemy inventory stacks;
- предмет с location `InsideCreature(EnemyId)`, включая проглоченный Живоглотом предмет;
- другие будущие enemy-owned compartments, если их authoritative owner — тот же enemy id.

Каждый предмет сохраняет:

- `ItemId`;
- stack/unit identity;
- quantity;
- item-specific state;
- deterministic provenance.

Held/equipped reference освобождается до world-location commit. Предмет не копируется и не создаётся заново, если физическая entity уже существовала.

## 3. Exact death-cell placement

Все released stacks получают `ItemLocation.InWorld(DeathCell)`.

- nearest-cell fallback запрещён;
- занятость cell другим world item не переносит loot в соседнюю cell;
- несколько разных stacks/entities могут лежать в одной logical XYZ cell;
- дальнейшая gravity/settlement обработка выполняется обычной world-item системой уже после death commit;
- presentation offset может визуально разнести предметы внутри cell, но authoritative location остаётся одной и той же death cell.

Это правило заменяет прежний fallback из `ecology-creatures-and-special-drops.md`, по которому проглоченный предмет мог выпадать рядом или в ближайшую допустимую клетку.

## 4. Species drop tables

Data-driven species drops остаются отдельным источником loot и не заменяют уже существовавшее содержимое врага.

Если definition вида создаёт дополнительные drops:

- они materialize ровно один раз;
- получают stable derived identities;
- создаются в той же exact death cell;
- validation всего output выполняется до commit;
- replay lethal/death event не создаёт второй набор drops.

Вид без drop table и без owned contents не создаёт предметы только из-за факта смерти.

## 5. Authoritative owners и transaction boundary

- `AgentState` владеет Health, alive/dead state и death position.
- `CombatState` владеет прекращением intent/execution и target-loss cleanup.
- `InventoryState` владеет всеми item identities, quantities, reservations, held references и world locations.
- enemy death lifecycle/Application handler координирует exactly-once переход `Alive -> Dead -> LootReleased -> CorpseExpired`.
- Presentation только проецирует падение, лежащий труп и dissolve progress; animation callback не убивает actor и не создаёт loot.

Lethal damage, death registration и Inventory release должны иметь один idempotent death identity. Если Inventory release временно не может завершиться, enemy остаётся мёртвым, corpse не удаляется, handler повторяет release с typed diagnostic и не дублирует уже committed items.

## 6. Observable workflow

### Success path

1. Confirmed combat action уменьшает Health до `0`.
2. Actor публикует typed death fact с enemy id, death cell и death tick.
3. Combat прекращает active intent/execution и уведомляет actors, которые держали dead target.
4. Inventory освобождает enemy-held references/reservations и переносит все owned contents в `InWorld(DeathCell)`.
5. Data-driven species drops, если они определены, materialize в той же cell.
6. Creature projection показывает fallen corpse без Health bar, hover highlight и attack interaction.
7. Corpse dissolve progress доходит до конца.
8. Creature visual удаляется; world loot остаётся.

### Replay и concurrency

- повторная обработка того же death identity не меняет Inventory и не продлевает corpse lifecycle;
- два врага, умершие в одной cell и tick, сохраняют независимые death identities и loot commits;
- несколько stacks одного врага не сливаются автоматически, если обычные Inventory rules не разрешают merge;
- actors, атаковавшие умершего врага, получают typed target-death completion и не наносят дополнительный hit.

### Cancel/undo

После authoritative lethal commit смерть не отменяется. Player cancel может отменить только ещё не resolved combat intent/action.

## 7. Save/load и migration

Save должен сохранять минимум:

- enemy alive/dead state;
- death cell и death tick;
- stable death identity;
- loot-release committed flag и resulting Inventory locations;
- corpse lifecycle phase/progress;
- species-drop commit identity.

Load не должен:

- повторно создавать loot;
- возвращать released item внутрь corpse;
- оживлять enemy;
- заново запускать corpse timer с начала, если часть lifecycle уже прошла.

Legacy dead enemies без death-lifecycle section требуют deterministic migration: существующие enemy-owned contents освобождаются один раз в saved enemy position, затем создаётся migrated corpse phase.

## 8. Presentation и input

- при переходе в dead state creature rig принимает fallen pose;
- corpse collider не участвует в hostile hover/attack routing;
- Health bar скрывается сразу после смерти;
- dissolve изменяет только visual opacity/geometry и не мутирует Domain/Application state;
- после полного dissolve renderer удаляет/pools visual root и очищает selection/highlight identity;
- loot renderer и interaction остаются независимыми от corpse visual.

## 9. Diagnostics

Диагностика показывает:

- enemy id/species;
- death identity, cell и tick;
- corpse phase и dissolve progress;
- количество owned stacks до release;
- количество/identity released stacks;
- species-drop output identities;
- retry/block reason;
- replay/idempotency result.

## 10. Acceptance после ответа на тайминг

- Domain/Application: Health `0` создаёт один death identity и exact death cell;
- Inventory integration: carried, held и `InsideCreature` items сохраняют identity/quantity и оказываются в exact death cell;
- conflict regression: занятая cell не вызывает nearest-cell fallback;
- combat integration: intents/executions завершаются, dead target нельзя атаковать повторно;
- deterministic/replay: повтор death event не дублирует loot или drops;
- save/load: mid-corpse lifecycle и released loot round-trip без reset/duplication;
- Presentation source contract: dissolve не мутирует authoritative state;
- Unity Play Mode: visible lethal hit -> enemy falls -> loot visible in same place -> corpse dissolves -> loot remains interactive.

## 11. Открытый вопрос

### Q-ENEMY-DEATH-001 — corpse timing

Нужно подтвердить полный тайминг от lethal hit до удаления visual:

- длительность падения;
- должен ли труп лежать без растворения и сколько;
- длительность самого растворения.

До ответа runtime не должен придумывать эти значения. Подтверждённые Domain/Application/Inventory правила выше остаются authoritative независимо от visual timing.
