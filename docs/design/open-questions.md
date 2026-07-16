# Реестр проектных вопросов Dig

Вопросы не удаляются: после ответа меняется статус и сохраняется решение.

Подробности:

- Q-047–Q-052: [`open-questions-047-051.md`](open-questions-047-051.md);
- Q-054: [`open-questions-054-hospital.md`](open-questions-054-hospital.md).

Статусы: `OPEN`, `ANSWERED`, `SUPERSEDED`, `BALANCE_TBD`.

## Q-001–Q-012 — мир, копание и ресурсы

| Q | Статус | Решение |
|---|---|---|
| Q-001 | ANSWERED | Мир `X,Y,Z`, глубина четыре клетки |
| Q-002 | ANSWERED | `Z=0..3` |
| Q-003 | ANSWERED | Камень = `skill.stonework`, thresholds 20/40/60 |
| Q-004 | ANSWERED | Потеря skill блокирует новые plans, но не текущий |
| Q-005 | ANSWERED | Трапеция повторяется по Z, входы по X |
| Q-006 | ANSWERED | Deposit заменяет terrain cell |
| Q-007 | ANSWERED | Gold ore и gold — разные IDs |
| Q-008 | ANSWERED | Metal = iron ingot |
| Q-009 | ANSWERED | Terrain output определяется profile |
| Q-010 | ANSWERED | Deposit cells имеют отдельный depletion |
| Q-011 | ANSWERED | Hauling требует demand/filter/visibility |
| Q-012 | ANSWERED | Deposit depletion меняет geometry |

## Q-013–Q-014 — контент и баланс

- **Q-013 — ANSWERED:** один ItemId `material.iron`.
- **Q-014 — BALANCE_TBD:** непредоставленные коэффициенты хранятся в definitions; включая combat coefficients и numeric personal-mobility multiplier.

## Q-015–Q-018 — еда и Needs

- **Q-015 — ANSWERED:** Nutrition UI `0..100`, Domain `0..10000`, `×100`.
- **Q-016 — ANSWERED:** desired dish, fallback и history 10.
- **Q-017 — ANSWERED:** interruption сохраняет уже применённые intervals.
- **Q-018 — ANSWERED:** worker кухни нужен только active order.

## Q-019–Q-022 — навыки

- **Q-019 — ANSWERED:** mixed result начисляет несколько skills.
- **Q-020 — ANSWERED:** weapon и shield grants возможны вместе.
- **Q-021 — ANSWERED:** Production per unit, Combat per event, Jobs on completion.
- **Q-022 — ANSWERED:** 12 skills, individual max 100, capacity 100→200.

## Q-023–Q-027 — термины

Q-023–Q-027 закрыты: «Песчаник», «Рудная порода», продолжение cave plan, authoritative building definitions и входы по X.

## Q-028–Q-033 — HUD и формулы

Q-028–Q-033 закрыты: capacity 100→200, proportional donor loss, «Бодрость», Mood 75 neutral, unsupported Alt как ground command, hunger `<15`.

## Q-034–Q-039 — исследования и производство

- **Q-034 — ANSWERED:** research materials не расходуются; duration по весам materials; один active slot; zero-input fallback мгновенный.
- **Q-035 — ANSWERED:** research лично выполняет qualified resident в Work schedule.
- **Q-036 — ANSWERED:** начатый research завершается после снижения required skill; completed technology не relock.
- **Q-037 — OPEN:** определить runtime model действий фермы.
- **Q-038 — ANSWERED:** workshops, legacy names и exclusions.
- **Q-039 — ANSWERED:** Cooking влияет только на скорость.

## Q-040–Q-046 — Needs, Society, Ecology, Doors

Q-040–Q-046 закрыты и описаны в соответствующих design-файлах.

## Q-047–Q-052 — Appearance, Lifecycle, Energy, Research, Transport

- **Q-047 — ANSWERED:** cosmetic role hats, identity cap, aging hair.
- **Q-048 — ANSWERED:** grave/return/rejuvenation rules и recipes.
- **Q-049 — ANSWERED:** deterministic single-source energy binding и generator lifecycle.
- **Q-050 — ANSWERED:** busy white, coal weight 2, one slot, active skill loss continues, instant zero-input fallback.
- **Q-051 — ANSWERED:** emergency elevator exit идёт к target; mobility tools используют legacy speedtypes `3/2`.
- **Q-052 — ANSWERED:** новая active pair сохраняется; старая остаётся historical.

## Q-053 — Combat equipment migration

- **Статус:** `ANSWERED`.
- Основной colony catalog ограничен десятью производимыми предметами.
- Skill mapping, mixed-building thresholds, shield compatibility, no-ammo и no-wear policies утверждены.
- 32 дополнительные fantasy/creature classes перенесены в backlog #177 и отключены в текущем runtime.
- Modern special-mode content и multiplayer-only object исключены из colony mode.
- Подробности: `content/weapons-and-shields.md`, `content/legacy-combat-equipment-appendix.md`.

## Q-054 — Hospital treatment lifecycle

- **Статус:** `OPEN`.
- Подтверждено:
  - отдельных травм и severity нет;
  - Health — единственное medical state;
  - лечение требует врача, но не материалов;
  - врач получает `skill.service`;
  - один этап длится один игровой час и восстанавливает до 25 Health;
  - near-death notification создаётся при `Health < 25`.
- Открыто: admission threshold, schedule interruption, capacity, doctor eligibility, queue priority, stage repetition, partial progress, critical-patient mobility, natural regeneration, energy и лечение детей/беременных.
- Подробности: `health-hospital-and-treatment.md`, `open-questions-054-hospital.md`, issue #130.

## Журнал

| Дата | Вопросы | Решение |
|---|---|---|
| 2026-07-15 | Q-001–Q-033, Q-038 | world/content/needs/skills/HUD |
| 2026-07-16 | Q-035, Q-039–Q-049 | research worker, cooking, food, leisure, society, ecology, doors, appearance, lifecycle, energy |
| 2026-07-16 | Q-034, Q-036, Q-050 | research weights, slots, busy state и active skill-loss policy |
| 2026-07-16 | Q-051 | elevator target emergency exit и legacy mobility behavior |
| 2026-07-16 | Q-052 | active pair after return |
| 2026-07-16 | Q-053 | core equipment approved; fantasy deferred; special-mode excluded |
| 2026-07-16 | Q-054 | hospital script audit; core rules confirmed, lifecycle decisions open |
