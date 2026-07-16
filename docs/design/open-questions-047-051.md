# Открытые вопросы Q-047–Q-051

Продолжение постоянного реестра `open-questions.md`.

## Q-047 — Рабочие головные уборы

**Статус:** `OPEN`

Подтверждено:

- головной убор определяется текущей или последней рабочей ролью;
- команда движения не меняет last work hat;
- перед combat action сначала надевается воинский шлем;
- смена содержит короткую фазу без шапки.

Нужно уточнить:

1. Это только косметика или некоторые уборы дают gameplay effects?
2. Какой предмет остаётся после завершения lifecycle resident: текущая role hat, обычный личный колпак или отдельный identity item?
3. Меняются ли одежда и причёска либо текущая задача касается только головных уборов?
4. Точная длительность transition без шапки.
5. Приоритет роли для действий, подходящих нескольким категориям.

Связи: `resident-role-headwear.md`, #151, #150.

## Q-048 — Могила, возвращение и омоложение

**Статус:** `OPEN`

Подтверждено:

- personal Inventory остаётся в мире;
- identity cap в видимой области создаёт отрицательный Mood effect;
- grave производится с использованием cap и получает имя resident;
- храмовая процедура требует cap;
- returned resident появляется в начальной взрослой стадии;
- rejuvenation переводит resident в child stage и сохраняет характеристики.

Нужно уточнить:

1. Можно ли использовать cap, уже находящийся в grave, и что тогда расходуется?
2. Какие свойства сохраняются при возвращении: skills, capacity, family links, partnership, appearance?
3. Child stage после rejuvenation длится стандартные 2 игровых дня?
4. Допустимы ли повторные rejuvenation/return cycles?
5. Полные recipes grave и temple action.

Связи: `death-graves-resurrection-and-rejuvenation.md`, #150.

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

## Q-050 — Research UI и experience

**Статус:** `OPEN`

Подтверждено:

- orange = locked;
- white = qualified researcher available;
- yellow = qualified researcher currently rests;
- click creates Research job;
- completion creates notification and production entry.

Нужно уточнить:

1. Можно ли нажать yellow icon и заранее поставить job в очередь?
2. Какой state/color используется, если qualified resident находится в Work schedule, но занят другой работой?
3. Как выбирается researcher при нескольких подходящих?
4. Research начисляет skill experience? Какой skill и в какой момент?

Связи: `research-availability-duration-and-ui.md`, #128.

## Q-051 — Лестницы и лифты

**Статус:** `OPEN`

Подтверждено:

- carts и rails исключены;
- ladders отличаются length;
- elevator использует floor requests;
- по пути обслуживаются requests того же направления;
- elevator ждёт boarding ожидающих на одной stop;
- variants отличаются speed и required energy class.

Нужно уточнить:

1. Остаются ли ездовой хомяк и ховерборд?
2. Одна wooden ladder с разной длиной или несколько material tiers?
3. Минимальная и максимальная длина ladder.
4. Capacity каждого elevator variant.
5. Boarding timeout, если один requester не успевает войти.
6. Поведение при отсутствии энергии во время движения.
7. Соответствие variants energy classes 1/2/3.
8. Floors — каждая Y-cell или только установленные stops?
9. Только residents с personal Inventory или отдельный cargo mode?

Связи: `ladders-and-elevators.md`, #137.
