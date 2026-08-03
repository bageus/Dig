# Смерть гнома, колпак, надгробие, возвращение и омоложение

Статус ordinary death/grave slice: `APPROVED`.

Связанная задача: [#150](https://github.com/bageus/Dig/issues/150).
Zombie-mode extension: [`zombie-mode-resident-death-questionnaire.md`](zombie-mode-resident-death-questionnaire.md), [#586](https://github.com/bageus/Dig/issues/586), статус `QUESTIONNAIRE`.

## Граница текущего решения

Эта спецификация authoritative для обычного исхода смерти resident, именного колпака, исключения из active roster, уведомления и изготовления персонального надгробия.

Возвращение умершего гнома и омоложение остаются последующими building/service slices. Их правила могут быть реализованы только через authoritative Domain/Application contract; временная кнопка или локальная логика Presentation не создаются.

## Смерть в обычном режиме

Любая terminal death cause создаёт ровно один `ResidentDied`/death-instance outcome.

В том же authoritative commit:

- гном становится мёртвым и немедленно исключается из active resident roster;
- мёртвый гном больше не является допустимой целью resident world selection, приказов, Utility AI, jobs, отдыха, еды, обучения или боя как живой resident;
- текущие actions/jobs отменяются, а worker, item, position, designation и другие reservations освобождаются без дублирования;
- все stacks личного Inventory переходят в одну world cell — логическую клетку смерти;
- в этой клетке может находиться любое количество разных предметов/stacks;
- временный death container не создаётся;
- создаётся отдельный поднимаемый identity-linked world item `Колпак {ИмяГнома}`;
- identity, имя, пол, family graph, historical relations, внешность, skills, capacity и lifecycle record сохраняются;
- Presentation создаёт одно уведомление с локализованным шаблоном `Гном {ИмяГнома} умер`.

Повторная обработка одного и того же death event не должна повторно удалять resident, сбрасывать Inventory, создавать колпак, уведомление или освобождать reservations.

## Именной колпак

- колпак связан с `ResidentId` и `DeathInstanceId`;
- колпак не является рабочей шапкой и не зависит от текущего role headwear;
- колпак является обычным физическим world item с поддержкой общего pickup/carry/reserve flow;
- имя отображается как `Колпак {ИмяГнома}`, но локализованный текст не является identity key;
- один death instance создаёт не более одного колпака;
- колпак может находиться ровно в одном состоянии: world, carried, reserved, grave-consumed или future-service-consumed.

## Глобальный штраф от незахороненного колпака

Активным источником считается хотя бы один именной колпак, который:

- лежит в world cell, видимой колонии в текущий момент; или
- находится в личном Inventory живого гнома.

При наличии хотя бы одного такого колпака одинаковый отрицательный Mood modifier получают все живые гномы колонии.

- штраф глобальный, а не только для гнома, который лично видит колпак;
- несколько колпаков не складываются и не усиливают штраф;
- перенос колпака в Inventory не убирает штраф;
- точная величина modifier остаётся data-driven balance value;
- после исчезновения последнего активного источника modifier снимается.

## Надгробие

```text
3 камня + Колпак конкретного гнома -> Надгробие {ИмяГнома}
```

- производится в Мастерской каменщика;
- player-facing результат называется `Надгробие {ИмяГнома}`;
- internal pre-placement payload может называться `GraveBox`, но обязан хранить `ResidentId` и `DeathInstanceId`;
- до завершения производственного цикла отмена возвращает зарезервированные материалы по общей production policy;
- при завершении production колпак окончательно consumed и больше не может использоваться future resurrection service;
- после размещения и сборки создаётся персональное надгробие/могила с именем умершего;
- размещённое надгробие нельзя переносить, упаковывать или разобрать;
- из завершённого надгробия воскресить гнома нельзя;
- имя и связь с resident сохраняются независимо от локализации.

## Будущее возвращение гнома

В последующем строении/service можно будет вернуть умершего гнома, пока существует свободный именной колпак, не consumed завершённым надгробием.

Текущий approved future contract:

```text
Колпак конкретного гнома
+ 1 хомяк
+ 4 золота
+ 2 кристаллические руды
-> возвращение гнома
```

- действие требует работника соответствующего строения;
- работа использует `skill.alchemy` и `skill.service` через общий mixed-skill contract;
- возвращается тот же identity молодым взрослым;
- гном появляется в ближайшей свободной допустимой клетке возле строения;
- если свободной клетки нет, completion ждёт появления клетки и не расходует ингредиенты повторно;
- сохраняются skills, TotalSkillCapacity, family graph, historical partnership records и внешность;
- прежний Inventory не восстанавливается повторно;
- одна death instance применяется один раз;
- повторные циклы death/return разрешены через новые `DeathInstanceId`;
- никакая временная Presentation-кнопка не заменяет этот будущий building/service flow.

### Active partnership после return

Если прежний партнёр уже создал новую active pair, новая пара сохраняется. Старая связь остаётся только historical relation. Return не разрывает новую пару и не восстанавливает старую автоматически. Бывшие партнёры могут снова образовать пару только через обычный matching, если оба позднее свободны.

## Зелье омоложения — будущий service slice

```text
1 хомяк
+ 1 кристалл
+ 1 железная руда
+ 2 золота
-> Зелье омоложения
```

- применимо к любому живому resident, кроме текущей стадии `Child`;
- Adult и Old могут использовать зелье;
- resident становится ребёнком на стандартные 2 игровых дня;
- adult jobs временно недоступны;
- обучение в школе доступно;
- skills, capacity, identity, family graph, relations и внешность сохраняются;
- inheritance повторно не начисляется;
- если target беременна, pregnancy state атомарно очищается при commit употребления;
- отменённая таким образом беременность не создаёт ребёнка и не запускает postpartum cooldown, поскольку родов не было;
- после нового взросления обычные reproduction rules снова применяются;
- повторное омоложение разрешено после нового взросления;
- consumable расходуется один раз.

## Zombie mode

Zombie mode использует отдельный death outcome:

- именной колпак не создаётся;
- погибший гном становится враждебным зомби;
- ordinary cap/grave outcome и zombie conversion взаимоисключающие.

Game-mode activation, Inventory outcome, conversion timing, identity/entity ownership, combat profile, targeting, second death, UI history и save/migration пока не определены. Эти решения authoritative ведутся в [`zombie-mode-resident-death-questionnaire.md`](zombie-mode-resident-death-questionnaire.md) и #586. До их утверждения zombie conversion не реализуется предположениями.

## Владение состояния

- Lifecycle/Society: death instance, terminal resident state, identity, family/relations, age, pregnancy cancellation, return и rejuvenation;
- Agents/Application: прекращение active actor/action и атомарная orchestration cleanup;
- Jobs/Reservations: отмена назначенной работы и освобождение claims;
- Inventory: dropped stacks, identity cap и cap location state;
- Buildings/Production: персональное надгробие, permanent grave и future return service;
- Skills: mixed Alchemy/Service work contract;
- Needs: global non-stacking Mood modifier;
- Game mode/Combat: выбранный death outcome и future hostile zombie state;
- Presentation: active roster projection, world selection eligibility, имя надгробия, visuals и notification `Гном {ИмяГнома} умер`.

## Инварианты

- один death event создаёт ровно один death instance и один mode-specific outcome;
- ordinary death создаёт не более одного именного колпака; zombie-mode death не создаёт колпак;
- мёртвый resident отсутствует в active roster и resident selection target set;
- death notification создаётся один раз из typed lifecycle event;
- все предметы умершего могут находиться в одной world cell без потери количества;
- cap находится ровно в одном состоянии: world, carried, reserved, grave-consumed или future-service-consumed;
- completed `GraveBox`/надгробие необратимо consumes cap;
- grave non-packable;
- глобальный cap penalty применяется не более одного раза независимо от количества источников;
- return не дублирует Inventory;
- return не создаёт вторую active pair;
- rejuvenation не применяется к Child и не повторяет inheritance;
- rejuvenation беременной атомарно отменяет pregnancy;
- Save/Load не повторяет death drops, cap creation, notification, production, lifecycle или consumable commits.

## Acceptance текущего ordinary slice

- death от любой причины удаляет гнома из active roster и resident world selection в том же workflow;
- появляется одно уведомление `Гном {ИмяГнома} умер`;
- личные stacks и один именной колпак появляются в логической клетке смерти;
- колпак можно поднять общим item interaction flow;
- Мастерская каменщика принимает конкретный колпак и создаёт персональное `Надгробие {ИмяГнома}`;
- повторная обработка/reload не дублирует drops, cap, notification или cleanup;
- ordinary и zombie outcomes никогда не выполняются одновременно;
- Domain, Application, deterministic и Unity Play Mode покрывают death → roster removal → notification → drops/cap → pickup → gravestone production.

Q-048 и Q-052 закрыты. Ordinary design #150 полностью определён. Zombie-mode extension остаётся `QUESTIONNAIRE` в #586.
