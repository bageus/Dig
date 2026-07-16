# Проектные вопросы Q-047–Q-052

Продолжение постоянного реестра `open-questions.md`.

## Q-047 — Рабочие головные уборы

**Статус:** `ANSWERED`

- все рабочие шапки только визуальные;
- после смерти создаётся отдельный identity-linked обычный колпак конкретного гнома;
- одежда постоянна;
- при старости волосы седеют, у мужчин появляется лысина;
- любая работа начинается только после проверки или смены нужной шапки;
- доставка имеет приоритет Logistics и использует фуражку, даже при переноске металла;
- точная длительность переодевания остаётся balance data Q-014.

Связи: `resident-role-headwear.md`, #151, #150.

## Q-048 — Могила, возвращение и омоложение

**Статус:** `ANSWERED`

- могила использует `3 камня + identity cap`;
- cap становится частью могилы;
- могилу нельзя переносить или упаковывать;
- воскресить из могилы нельзя;
- возвращение сохраняет skills, capacity, family, partnership и appearance, сбрасывая возраст до young adult;
- омоложение длится стандартные 2 дня детства;
- омоложённый child не выполняет adult jobs, но может учиться;
- repeated rejuvenation и repeated death/return разрешены;
- temple recipe: cap + 1 hamster + 4 gold + 2 crystal ore;
- rejuvenation potion: 1 hamster + 1 crystal + 1 iron ore + 2 gold.

Отдельный конфликт партнёрства после возвращения вынесен в Q-052.

Связи: `death-graves-resurrection-and-rejuvenation.md`, `content/products.md`, #150.

## Q-049 — Energy allocation

**Статус:** `OPEN`

Подтверждено:

- четыре sources и три classes;
- radii `20/20/60/120`;
- outputs `10/100/400/600`;
- refill threshold `<15%`;
- enable/disable;
- Production сохраняет progress при отсутствии питания.

Нужно уточнить:

1. Перекрывающиеся zones образуют общий pool или consumer выбирает один source?
2. Может ли class 2/3 питать consumers младшего класса?
3. Output хранится как stock или производится постепенно?
4. Что происходит с незавершённой часовой итерацией ручного source?
5. Официальные display names ручного и class-3 sources.
6. Lifecycle хомяка при выключении, упаковке и демонтаже.

Связи: `energy-generation-and-production-pausing.md`, #127.

## Q-050 — Research UI

**Статус:** `OPEN`

Подтверждено:

- orange и yellow icon можно нажать заранее;
- orange job ждёт появления qualified resident;
- любой свободный qualified resident может получить claim;
- research не начисляет experience;
- материалы не расходуются и только определяют duration;
- все руды имеют weight 2;
- completed technology не блокируется после последующего падения skills.

Открыто:

1. Какой color/state используется для qualified resident, который находится в Work schedule, но занят другой работой?
2. Какой research-minute weight имеет уголь?
3. Что происходит при потере requirements во время уже начатого progress — pause и ожидание, продолжение текущим worker или cancel?
4. Сколько одновременных research slots имеет одно здание и как обрабатывается recipe без ingredients?

Связи: `research-availability-duration-and-ui.md`, #128, Q-034, Q-036.

## Q-051 — Лестницы и лифты

**Статус:** `PARTIALLY_ANSWERED`

- ездовой хомяк и ховерборд остаются;
- лестницы имеют фиксированные длины: wooden 12, metal 16, crystal 24;
- capacity каждого лифта = 4;
- лифт ждёт всех подошедших в радиус 1 при наличии мест;
- timeout отсутствует;
- при потере энергии кабина останавливается, пассажиры выбираются по стенам;
- elevator classes: ordinary 1, steam 2, crystal 3;
- manual control допускает любую высоту шахты;
- automatic routing использует только этажи с примыкаемой доступной площадкой;
- отдельного cargo mode нет.

Открыто:

1. К какой площадке ползут пассажиры после power loss: ближайшей, исходной или целевой?
2. Нужны отдельные design-параметры скорости, технологии и управления ездового хомяка и ховерборда.

Связи: `ladders-and-elevators.md`, #137.

## Q-052 — Партнёрство после возвращения жителя

**Статус:** `OPEN`

При return сохраняется прежнее partnership record. Однако по Q-042 переживший партнёр после смерти мог создать новую эксклюзивную пару.

Нужно определить:

1. Если прежний партнёр уже состоит в новой паре, восстанавливается ли старая пара?
2. Какая пара прекращается, если одновременно нельзя сохранить обе?
3. Или прежняя связь остаётся только family/history relation, а active partnership восстанавливается лишь когда оба свободны?

Связи: `death-graves-resurrection-and-rejuvenation.md`, `partnership-pregnancy-and-birth.md`, #145, #150.

## Журнал

| Дата | Вопрос | Решение |
|---|---|---|
| 2026-07-16 | Q-047 | cosmetic role hats, identity cap, aging hair, work waits for headwear |
| 2026-07-16 | Q-048 | grave/temple/potion recipes and repeat lifecycle policy |
| 2026-07-16 | Q-050 | queued orange/yellow research, no XP or material consumption, ore weight 2 |
| 2026-07-16 | Q-051 | ladder lengths, elevator capacity/classes/boarding and emergency climb |
