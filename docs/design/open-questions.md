# Реестр проектных вопросов Dig

Вопросы не удаляются: после ответа меняется статус и сохраняется решение. Детали Q-047–Q-052: [`open-questions-047-051.md`](open-questions-047-051.md).

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
- **Q-014 — BALANCE_TBD:** непредоставленные коэффициенты хранятся в definitions; сюда относятся combat damage/range/cooldown/block coefficients и числовой multiplier personal mobility.

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

### Q-034 — Research duration/cost

- **Статус:** `ANSWERED`
- materials не расходуются;
- duration = сумма quantity × minutes;
- шляпка, ножка, хомяк, камень, личинка = 1;
- уголь, все руды и iron = 2;
- gold = 4, crystal = 5;
- одно здание имеет один active research slot;
- zero-input recipe invalid, fallback duration 0/instant completion.

### Q-035 — Qualified researcher

- **Статус:** `ANSWERED`
- job лично выполняет гном, удовлетворяющий requirements, в Work schedule.

### Q-036 — Requirements lost during research

- **Статус:** `ANSWERED`
- до старта job ждёт нового qualified resident;
- после начала снижение required skill не прерывает работу: текущий worker завершает research;
- completed technology не relock.

### Q-037 — Farm actions

- **Статус:** `OPEN`
- Нужно определить: recipes, growth orders или Ecology actions.

- **Q-038 — ANSWERED:** workshops, legacy names и exclusions.
- **Q-039 — ANSWERED:** Cooking влияет только на скорость.

## Q-040–Q-046 — Needs, Society, Ecology, Doors

Q-040–Q-046 закрыты и описаны в соответствующих design-файлах.

## Q-047–Q-052 — Appearance, Lifecycle, Energy, Research, Transport

- **Q-047 — ANSWERED:** cosmetic role hats, identity cap, aging hair.
- **Q-048 — ANSWERED:** grave/return/rejuvenation rules и recipes.
- **Q-049 — ANSWERED:** deterministic single-source energy binding и generator lifecycle.
- **Q-050 — ANSWERED:** busy white, coal 2, one slot, active skill loss continues, instant zero-input fallback.
- **Q-051 — ANSWERED:** emergency climb идёт к target; Reithamster/Hoverboard автоматически используют одинаковые legacy speedtypes `3/2`, numeric multiplier относится к Q-014.
- **Q-052 — ANSWERED:** новая pair сохраняется; старая остаётся historical и может возникнуть снова только через normal matching, если оба свободны.

## Q-053 — Combat equipment migration

- **Статус:** `ANSWERED`.
- Текущий colony catalog содержит 10 производимых предметов: Рогатка, Дубина, Простой щит, Боевой топор, Меч, Лук, Металлический щит, Ружьё, Световой меч, Кристаллический щит.
- Skill mapping:
  - Меч/«Палаш» и Боевой топор → `skill.two_handed_combat`;
  - Рогатка, Лук и Ружьё → `skill.ranged_combat`;
  - Металлический и Кристаллический щиты → `skill.defense`;
  - Дубина и Световой меч → `skill.one_handed_combat`;
  - Простой щит использует `skill.defense` для confirmed defense grants.
- Для Оружейной кузницы, Оружейной фабрики и Dojo generic Combat threshold выполнен, если хотя бы один из пяти combat skills достиг threshold.
- `AllowsShield=true` у Меча, Светового меча и Рогатки.
- Дубина, Боевой топор, Лук и Ружьё блокируют `Shield` slot.
- Рогатка, Лук и Ружьё не расходуют ammo items.
- Все десять предметов имеют `DurabilityPolicy = NoWear`.
- 32 fantasy/creature класса не применяются сейчас и перенесены в backlog [#177](https://github.com/bageus/Dig/issues/177); production, loot и runtime registration выключены.
- `AK47`, `MP5`, `M4`, `Para`, `M3_super_90`, `Duals`, `Awp`, `Deagle` и `Bombe` окончательно исключены из colony mode.
- Название «Палаш» пока трактуется как ссылка на `weapon.sword` / legacy `Schwert`; stable ItemId не меняется.
- Точные damage/range/cooldown/block values относятся к Q-014.

Спецификация: `content/weapons-and-shields.md`; legacy policies: `content/legacy-combat-equipment-appendix.md`.

## Журнал

| Дата | Вопросы | Решение |
|---|---|---|
| 2026-07-15 | Q-001–Q-033, Q-038 | world/content/needs/skills/HUD |
| 2026-07-16 | Q-035, Q-039–Q-049 | research worker, cooking, food, leisure, society, ecology, doors, appearance, lifecycle, energy |
| 2026-07-16 | Q-034, Q-036, Q-050 | research weights, slots, busy state и active skill-loss policy |
| 2026-07-16 | Q-051 | target emergency climb и legacy mobility-tool behavior |
| 2026-07-16 | Q-052 | active pair после return |
| 2026-07-16 | Q-053 | основной equipment catalog утверждён; fantasy deferred to #177; modern special-mode excluded |