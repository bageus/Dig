# Единый игровой календарь и cadence действий

Статус: `APPROVED`.

Decision date: 2026-08-03.

Tracking issue: [#601](https://github.com/bageus/Dig/issues/601).

Связанные authoritative systems:

- [`../architecture/systems-core.md`](../architecture/systems-core.md);
- [`runtime-needs-supply-sleep-food-recovery.md`](runtime-needs-supply-sleep-food-recovery.md);
- [`resident-movement-modes.md`](resident-movement-modes.md);
- [`resident-movement-occupancy-and-vertical-traversal.md`](resident-movement-occupancy-and-vertical-traversal.md);
- [`campfire-cooking-and-food-use.md`](campfire-cooking-and-food-use.md);
- [`building-production-and-internal-supply.md`](building-production-and-internal-supply.md);
- [`excavation-cadence-profiles.md`](excavation-cadence-profiles.md);
- [`combat-spatial-execution.md`](combat-spatial-execution.md);
- [`enemy-combat-and-cave-encounters.md`](enemy-combat-and-cave-encounters.md).

## 1. Назначение

Эта спецификация заменяет прежние конфликтующие demo-значения времени и задаёт одну шкалу для календаря, потребностей, перемещения, еды, копки, боя и обработки материалов. Все gameplay commits остаются привязаны к authoritative simulation tick; Unity frame time, animation callbacks и отдельные Presentation timers не могут завершать действия.

## 2. Authoritative time ownership

- `SimulationClock.TickIndex` остаётся единственным порядком simulation commits.
- Normal playback использует `1 simulation tick = 1 real second`.
- Fast и VeryFast являются детерминированными множителями `x2` и `x4` того же clock.
- `150 ticks = 1 игровой час`.
- `3 600 ticks = 24 игровых часа = 1 игровые сутки`.
- Один tick соответствует 24 игровым секундам.
- Pause и single-step не создают отдельный календарь.
- Existing global simulation tick не масштабируется при migration. Calendar projections выводятся из сохранённого tick и текущего cadence version.

## 3. Needs и стартовые residents

- Demo residents начинают с `Nutrition = 10 000` (100%).
- Full Nutrition истощается за два игровых дня: `7 200 ticks`.
- Full Alertness истощается за три игровых дня: `10 800 ticks`.
- Passive decay использует deterministic difference соседних cumulative fractions.
- При непрерывном survival-critical голоде full Health истощается за 12 игровых часов: `1 800 ticks`.
- Starvation Health damage также распределяется cumulative-fraction resolver и не использует фиксированный `-500` каждый tick.
- Existing alertness-critical protection во время подтверждённого Sleep сохраняется. Critical Nutrition продолжает наносить starvation damage.
- Save/load продолжает с той же cumulative phase и не повторяет уже применённую delta.

## 4. Resident movement

Наблюдаемые базовые скорости:

- `Normal`, `ForcedFast` и `Fleeing` на supported/depth route: `1.25 cells/tick`;
- `Tired` и `Carrying`: `1 cell/tick` до применения Inventory cargo multiplier;
- `Climbing`, включая `VerticalClimb` и `ShaftGapTraverse`: `0.5 cells/tick`;
- personal mobility остаётся отдельным data-driven content boundary.

Дробная скорость реализуется deterministic fixed-point movement budget:

- одна cell transition стоит `1 000` movement units;
- run добавляет `1 250` units/tick;
- walk добавляет `1 000` units/tick;
- climb добавляет `500` units/tick.

На прямом supported route run создаёт пять последовательных cell commits за четыре ticks. Tick с двумя переходами выполняет их по одному: каждый промежуточный `CellId`, traversal edge, traffic restriction, route freshness и interruption проверяются отдельно. Teleport или пропуск промежуточной клетки запрещены.

Movement budget является derived tick phase, не хранит отдельную world position и не переносит неиспользованный остаток через blocked/replanned transition. Authoritative resident position по-прежнему изменяется только Movement command.

## 5. Meal cadence

Стандартная порция состоит из трёх bites:

1. первый bite commit;
2. один tick cooldown;
3. второй bite commit;
4. один tick cooldown;
5. третий bite commit и completion.

Если meal стартовала на tick `T`, bites становятся due на `T+1`, `T+3`, `T+5`. Cooldown tick не применяет Nutrition и не продвигает completed-bite count. Interruption сохраняет уже committed bites и уничтожает оставшийся payload по существующему правилу.

Active meal snapshot/save хранит следующий due bite tick. Older save без этого поля получает безопасный следующий due tick после load; existing simulation tick не масштабируется и completed bites не replay.

## 6. Excavation cadence

- Один due mining impact коммитит не более одного reserved quarter.
- Базовый demo pickaxe equipment interval равен `3 ticks`: один impact tick и два recovery ticks.
- Hardness, Stonework band, equipment и posture продолжают вычисляться одним `ExcavationCadenceResolver`.
- Animation swing не является вторым progress owner.
- Cancel/retry/save/load сохраняют только уже committed World quarters; скрытый sub-quarter progress не создаётся.

## 7. Combat cadence

Базовый melee profile для fists, club и cave-monster bite:

- `1 tick` WindUp/authoritative ResolveAttack;
- `3 ticks` Recover;
- следующий ResolveAttack не раньше чем через `4 ticks`.

Damage, statuses и progression коммитятся только одним `CombatActionId` в `ResolveAttack`. Movement/re-range/LoS reevaluation остаются отдельными стадиями и могут увеличить фактический интервал.

## 8. Building production cadence

- Базовая обработка одного ordinary material step: `25 ticks`.
- Cooking material step: `50 ticks`.
- Cooking skill сокращает только processing duration и не может уменьшить её ниже 50% base duration.
- Для cooking base `50`: skill 0/25/50/75/100 даёт `50/38/25/25/25 ticks`.
- Поставить unfinished output package — отдельный committed step длительностью один tick.
- Получить exact reserved material — отдельный committed step длительностью один tick после arrival.
- Workbench stage, package deposit, package close и post-work release остаются отдельными lifecycle transitions.
- Movement между internal stock, workstation и package использует обычный movement cadence и не входит в processing duration.

Один queued grilled-mushroom order потребляет одну mushroom cap, обычно занимает около `60–75 ticks` полного spatial workflow при коротком маршруте и создаёт две отдельные ordinary world entities `food.grilled_mushroom`, quantity `1` каждая. Skill ускоряет processing, но не пропускает spatial/commit stages.

## 9. Input priority, interruption и retry

Эта спецификация не меняет утверждённый input priority.

- Direct resident command использует общий preparation/cleanup boundary.
- Combat, support loss и death могут прервать work/meal согласно их authoritative contracts.
- Already committed movement cells, bites, quarters, attacks и material phases не replay и не откатываются.
- Blocked route или target не расходует будущий action commit.
- Retry пересчитывает derived cadence из current authoritative tick/state.

## 10. Save/load и migration

Сохраняются существующие simulation tick, action/job identities, World quarters, combat cooldown facts, production material progress и active meal state. Добавляется только meal next-bite due tick и cadence/version diagnostics, где это необходимо.

Migration:

- не умножает и не делит existing simulation tick;
- не переписывает stable action/job ids;
- не повторяет completed bites/quarters/attacks/material work;
- для отсутствующего meal due tick назначает первый безопасный due tick после load;
- movement fractional budget пересчитывается из tick и mode, а не сериализуется.

## 11. Diagnostics

Runtime diagnostics показывают:

- authoritative tick duration и playback multiplier;
- game day/hour projection и `ticksPerDay`;
- need depletion/starvation periods и текущую cumulative phase;
- movement mode, speed, due transition count и consumed substeps;
- meal completed bites и next due tick;
- excavation resolved interval и next due impact;
- combat profile cooldown/next resolve tick;
- production base/effective material duration и current phase.

## 12. Acceptance

- normal Unity playback получает tick duration только из active `SimulationClock` и использует 1 real second;
- one day equals 3 600 ticks, one hour equals 150 ticks;
- fresh demo residents begin with full Nutrition;
- Nutrition/Alertness/starvation reach exact endpoints at 7 200/10 800/1 800 ticks without last-tick spike;
- save/load preserves the next proportional need delta;
- straight supported run commits five validated cells in four ticks;
- walk commits four cells in four ticks; climb commits two cells in four ticks;
- no accelerated transition skips an intermediate cell or traffic/traversal validation;
- bites commit at `T+1`, `T+3`, `T+5`; save/load mid-cooldown resumes without replay;
- base demo pickaxe interval is three ticks and one due impact commits one quarter;
- fists, club and cave bite resolve no faster than every four ticks;
- ordinary material step is 25 ticks; cooking step is 50 ticks with 50% minimum duration;
- one cap still produces two separate grilled-mushroom units after the complete spatial lifecycle;
- Domain, Application, deterministic, save/load and Unity Play Mode regressions cover success, repeat, interruption and retry.

## 13. Verification boundary

Source contracts, build and deterministic tests may raise implementation status to `IMPLEMENTED`. `VERIFIED` requires an actual licensed Unity Play Mode run of movement, eating, excavation, combat, needs and campfire workflows, including the next repeated action and interruption/retry.
