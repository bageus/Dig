# Реестр проектных вопросов Dig

Вопросы не удаляются: после ответа меняется статус и сохраняется решение. Детали Q-047–Q-052: [`open-questions-047-051.md`](open-questions-047-051.md).

Статусы: `OPEN`, `ANSWERED`, `PARTIALLY_ANSWERED`, `SUPERSEDED`, `BALANCE_TBD`.

## Q-001–Q-012 — мир, копание и ресурсы

| Q | Статус | Решение |
|---|---|---|
| Q-001 | ANSWERED | Мир `X,Y,Z`, глубина четыре клетки |
| Q-002 | ANSWERED | `Z=0..3` |
| Q-003 | ANSWERED | Камень = `skill.stonework`, thresholds 20/40/60 |
| Q-004 | ANSWERED | Потеря skill блокирует новые plans, но не текущий |
| Q-005 | ANSWERED | Трапеция повторяется по Z, без mirror, входы по X |
| Q-006 | ANSWERED | Deposit заменяет terrain cell |
| Q-007 | ANSWERED | Gold ore и gold — разные IDs |
| Q-008 | ANSWERED | Metal = iron ingot |
| Q-009 | ANSWERED | Terrain output определяется profile |
| Q-010 | ANSWERED | Соседние deposit cells имеют отдельный depletion |
| Q-011 | ANSWERED | Hauling требует demand/filter и visibility |
| Q-012 | ANSWERED | Deposit depletion меняет geometry как excavation |

## Q-013–Q-014 — контент и баланс

- **Q-013 — ANSWERED:** один ItemId `material.iron`.
- **Q-014 — BALANCE_TBD:** непредоставленные коэффициенты хранятся в definitions.

## Q-015–Q-018 — еда и Needs

- **Q-015 — ANSWERED:** Nutrition UI `0..100`, Domain `0..10000`, `×100`.
- **Q-016 — ANSWERED:** desired dish, fallback и история 10 трапез.
- **Q-017 — ANSWERED:** прерывание сохраняет уже применённые интервалы.
- **Q-018 — ANSWERED:** worker кухни нужен только для active order.

## Q-019–Q-022 — навыки

- **Q-019 — ANSWERED:** mixed result может начислять несколько skills.
- **Q-020 — ANSWERED:** weapon и shield grants возможны в одном combat cycle.
- **Q-021 — ANSWERED:** Production per unit, Combat per event, Jobs on completion.
- **Q-022 — ANSWERED:** 12 skills, individual max 100, capacity 100→200.

## Q-023–Q-027 — термины

- **Q-023 — ANSWERED:** «Песчаник».
- **Q-024 — ANSWERED:** «Рудная порода».
- **Q-025 — ANSWERED:** существующий cave plan продолжается.
- **Q-026 — ANSWERED:** building parameters берутся из definitions.
- **Q-027 — ANSWERED:** входы пещеры слева/справа по X.

## Q-028–Q-033 — HUD и формулы

- **Q-028 — ANSWERED:** значение 120 было ошибкой; используется 100→200.
- **Q-029 — ANSWERED:** proportional donor loss с deterministic rounding.
- **Q-030 — ANSWERED:** UI показывает «Бодрость».
- **Q-031 — ANSWERED:** Mood 75 нейтрален, радость с 76.
- **Q-032 — ANSWERED:** unsupported Alt action обрабатывается как ground command.
- **Q-033 — ANSWERED:** hunger event при Nutrition `<15`.

## Q-034–Q-039 — исследования и производство

### Q-034 — Research duration/cost
- **Статус:** `OPEN`
- **Подтверждено:** материалы не расходуются; duration = сумма количества ingredients × minutes per item.
- **Weights:** шляпка, ножка, хомяк, камень, личинка = 1; все руды и iron = 2; gold = 4; crystal = 5 игровых минут.
- **Открыто:** вес угля, recipes без materials, число slots/queue policy.

### Q-035 — Qualified researcher
- **Статус:** `ANSWERED`
- **Решение:** job лично выполняет гном, удовлетворяющий всем requirements, в Work schedule.

### Q-036 — Requirements lost during research
- **Статус:** `OPEN`
- **Подтверждено:** до старта queued job становится оранжевой и ждёт нового qualified resident; после completion падение skills ничего не отменяет.
- **Открыто:** active progress pause/continue/cancel при потере requirements, смерти или уходе worker.

### Q-037 — Farm actions
- **Статус:** `OPEN`
- **Вопрос:** recipes, growth orders или Ecology actions.

- **Q-038 — ANSWERED:** подтверждены workshops, legacy names и exclusions.
- **Q-039 — ANSWERED:** Cooking влияет только на скорость.

## Q-040–Q-046 — Needs, Society, Ecology, Doors

- **Q-040 — ANSWERED:** matched food и diversity могут поднимать Mood выше 50 до MoodMaximum.
- **Q-041 — ANSWERED:** 5 повторений в предыдущих 10 → Mood multiplier 0.5.
- **Q-042 — ANSWERED:** exclusive male/female pair, guaranteed conception, pregnancy 1 day, cooldown 2 days.
- **Q-043 — ANSWERED:** 12-school curriculum, per-skill inheritance roll, 1 teacher/4 students, 24/7.
- **Q-044 — ANSWERED:** sleep multipliers `1/.75/.5/.25`, personal beds, 2 slots.
- **Q-045 — ANSWERED:** ecology special items/caps policy утверждена, числовой balance TBD.
- **Q-046 — ANSWERED:** wooden/metal/crystal doors, 2-tick auto close, no liquids/switches.

## Q-047–Q-052 — Appearance, Lifecycle, Energy, Research, Transport

Подробный текст находится в [`open-questions-047-051.md`](open-questions-047-051.md).

- **Q-047 — ANSWERED:** cosmetic role hats, identity cap, aging hair и обязательная смена перед работой.
- **Q-048 — ANSWERED:** grave/return/rejuvenation rules и recipes подтверждены.
- **Q-049 — OPEN:** Energy allocation, class compatibility и source lifecycle.
- **Q-050 — OPEN:** цвет занятого qualified researcher, coal weight и связанные research edge cases.
- **Q-051 — PARTIALLY_ANSWERED:** ladder/elevator policy утверждена, emergency climb destination остаётся открытым.
- **Q-052 — OPEN:** active partnership conflict после возвращения умершего партнёра.

## Журнал

| Дата | Вопросы | Решение |
|---|---|---|
| 2026-07-15 | Q-001–Q-033, Q-038 | базовые world/content/needs/skills/HUD решения |
| 2026-07-16 | Q-039–Q-046 | cooking, food Mood, leisure, society, school, sleep, ecology, doors |
| 2026-07-16 | Q-035 | qualified resident лично выполняет Research job |
| 2026-07-16 | Q-047–Q-048 | role appearance, grave, return, rejuvenation и recipes |
| 2026-07-16 | Q-050–Q-051 | queued research, ore weights, no XP/material cost, ladders/elevators |
