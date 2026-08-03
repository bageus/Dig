# Смерть гнома, колпак, надгробие, возвращение и омоложение

Статус: `APPROVED`.  
Tracking issue: [#150](https://github.com/bageus/Dig/issues/150).  
Zombie-mode extension: [`zombie-mode-resident-death-questionnaire.md`](zombie-mode-resident-death-questionnaire.md), [#586](https://github.com/bageus/Dig/issues/586).

Эта спецификация определяет обычный режим. Zombie mode использует отдельный death outcome и не может молча наследовать правила колпака/могилы.

Связанная задача: [#150](https://github.com/bageus/Dig/issues/150).
Zombie-mode extension: [`zombie-mode-resident-death-questionnaire.md`](zombie-mode-resident-death-questionnaire.md), [#586](https://github.com/bageus/Dig/issues/586), статус `QUESTIONNAIRE`.

## Граница текущего решения

- умерший гном немедленно покидает active resident roster и больше не выбирается как живой resident в мире;
- все stacks личного Inventory переходят в одну world cell — логическую клетку смерти;
- в этой клетке может находиться любое количество разных предметов/stacks;
- временный death container не создаётся;
- появляется отдельный identity-linked world item `Колпак {ИмяГнома}`, который можно выбрать и поднять обычным item workflow;
- колпак связан с `ResidentId` и `DeathInstanceId` и не зависит от рабочей шапки;
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

## Надгробие и могила

```text
3 камня + Колпак конкретного гнома -> Надгробие {ИмяГнома}
```

- производится в Мастерской каменщика;
- player-facing product называется `Надгробие {ИмяГнома}`; internal pre-placement/assembly payload может использовать stable type `GraveBox`, но локализованное имя не является identity key;
- до завершения производственного цикла отмена возвращает зарезервированные материалы по общей production policy;
- при завершении производства надгробия (`GraveBox`) колпак окончательно consumed и больше не может использоваться Храмом;
- надгробие/`GraveBox` сохраняет `ResidentId` и `DeathInstanceId`;
- после размещения и сборки создаётся `Могила {ИмяГнома}`;
- размещённую могилу нельзя переносить, упаковывать или разобрать;
- из могилы воскресить нельзя;
- имя и связь с resident сохраняются независимо от локализации.

## Возвращение в будущем здании/Храме

Возвращение является утверждённым будущим building/service slice. Текущая реализация колпака и надгробия не должна создавать временную кнопку, Presentation-owned resurrect command или расходовать колпак без authoritative здания.

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

Q-048 и Q-052 закрыты. Design #150 полностью определён.

## Журнал решений

| Дата | Решение | Кто подтвердил | Изменённые разделы/issues |
|---|---|---|---|
| 2026-08-03 | В обычном режиме мёртвый resident исчезает из active roster/selection; вместо него остаётся поднимаемый именной колпак. Мастерская каменщика производит персональное надгробие. Resurrection остаётся будущим building slice. Zombie mode не создаёт колпак и вынесен в отдельный questionnaire. | Пользователь в проектном чате | Death workflow, grave naming, #150, #586 |
