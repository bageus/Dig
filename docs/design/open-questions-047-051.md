# Проектные вопросы Q-047–Q-052

Продолжение постоянного реестра `open-questions.md`.

## Q-047 — Рабочие головные уборы

**Статус:** `ANSWERED`

Рабочие шапки только визуальные; death создаёт отдельный identity cap; одежда постоянна; старость меняет волосы; любая работа ждёт проверки/смены role hat; Logistics имеет приоритет для доставки. Точная длительность переодевания относится к Q-014.

## Q-048 — Могила, возвращение и омоложение

**Статус:** `ANSWERED`

- могила: `3 камня + identity cap`;
- grave permanent/non-packable, return из grave невозможен;
- temple return: cap + hamster + 4 gold + 2 crystal ore;
- rejuvenation potion: hamster + crystal + iron ore + 2 gold;
- return сохраняет identity/skills/capacity/family/history/appearance и сбрасывает возраст;
- rejuvenated child учится, но не выполняет adult jobs;
- repeated lifecycle cycles разрешены.

## Q-049 — Распределение энергии

**Статус:** `ANSWERED`

Один consumer использует один source. Приоритет: exact class, затем минимальный более высокий, затем ближайший и stable ID. Higher class питает lower consumer. Fuel batches сразу создают stock `100/400/600`, расходуемый по demand. Ручной генератор работает без stock. Реактор — официальное имя. Хомяк встроен в генератор и упаковывается вместе с ним.

## Q-050 — Research UI и lifecycle

**Статус:** `ANSWERED`

- `EligibleBusy` имеет белый цвет, как `EligibleAvailable`, но отдельный текст/status icon;
- coal research weight = 2 игровые минуты;
- все руды и iron также имеют weight 2;
- active research продолжается и завершается, даже если worker после старта потерял required skill;
- одно здание выполняет одно active research одновременно;
- queued nodes ждут своей очереди;
- technology recipe без inputs считается invalid content, но runtime fallback имеет duration 0 и мгновенный idempotent completion;
- materials не расходуются;
- Research не начисляет XP;
- completed technology не relock.

Связи: `research-availability-duration-and-ui.md`, #128.

## Q-051 — Лестницы, лифты и mobility tools

**Статус:** `ANSWERED`

- лестницы: wooden 12, metal 16, crystal 24;
- лифты: capacity 4, energy classes 1/2/3;
- ожидание пассажиров radius 1, без timeout;
- autonomous stops требуют примыкаемую площадку, manual control допускает любую высоту;
- cargo только в personal Inventory;
- при power loss жители ползут по стенам к **целевой** площадке;
- Reithamster и Hoverboard остаются;
- legacy scripts автоматически используют их при наличии в Inventory;
- обычный дальний путь использует engine `speedtype 3 (autodist)`, forced/repeated fast move — `speedtype 2`;
- Hoverboard имеет приоритет, если оба предмета находятся в Inventory;
- перенос BuildingBox отключает fast mobility;
- TCL не содержит отдельных числовых speed multipliers: оба используют одинаковые speedtype, поэтому конкретный multiplier остаётся data-driven Q-014.

Связи: `ladders-and-elevators.md`, `scripts/classes/zwerg/z_dignwalk.tcl`, #137.

## Q-052 — Партнёрство после возвращения жителя

**Статус:** `ANSWERED`

- если surviving partner создал новую active pair, новая пара сохраняется;
- return не разрывает её и не создаёт вторую pair;
- прежняя связь остаётся historical family/social relation;
- бывшие партнёры могут снова образовать active pair только через normal matching, если оба позднее свободны;
- return сам по себе не восстанавливает старую пару.

Связи: `death-graves-resurrection-and-rejuvenation.md`, `partnership-pregnancy-and-birth.md`, #145, #150.

## Журнал

| Дата | Вопрос | Решение |
|---|---|---|
| 2026-07-16 | Q-047 | cosmetic role hats, identity cap и aging hair |
| 2026-07-16 | Q-048 | grave/return/rejuvenation recipes и lifecycle |
| 2026-07-16 | Q-049 | deterministic single-source energy binding |
| 2026-07-16 | Q-050 | white busy state, coal 2, one slot, active skill-loss continues, zero-input instant fallback |
| 2026-07-16 | Q-051 | target emergency climb и recovered automatic mobility-tool behavior |
| 2026-07-16 | Q-052 | new active pair survives return; old pair remains historical |