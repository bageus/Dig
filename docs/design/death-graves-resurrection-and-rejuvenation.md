# Смерть, могилы, возвращение и омоложение

Статус: `APPROVED`.  
Tracking issue: [#150](https://github.com/bageus/Dig/issues/150).  
Zombie-mode extension: [`zombie-mode-resident-death-questionnaire.md`](zombie-mode-resident-death-questionnaire.md), [#586](https://github.com/bageus/Dig/issues/586).

Эта спецификация определяет обычный режим. Zombie mode использует отдельный death outcome и не может молча наследовать правила колпака/могилы.

## Смерть и оставшиеся предметы

После смерти гнома:

- умерший гном немедленно покидает active resident roster и больше не выбирается как живой resident в мире;
- все stacks личного Inventory переходят в одну world cell — логическую клетку смерти;
- в этой же клетке может находиться любое количество разных предметов/stacks;
- временный death container не создаётся;
- появляется отдельный identity-linked world item `Колпак {ИмяГнома}`, который можно выбрать и поднять обычным item workflow;
- колпак связан с `ResidentId` и `DeathInstanceId` и не зависит от рабочей шапки;
- identity, имя, пол, family graph, historical relations, внешность, skills, capacity и lifecycle record сохраняются;
- jobs/reservations освобождаются без дублирования предметов.

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

- нужен свободный cap, не использованный завершённым `GraveBox`;
- действие требует работника Храма;
- храмовая работа использует два навыка: `skill.alchemy` и `skill.service`;
- eligibility и начисление опыта используют общий mixed-skill contract; точные requirements/grants задаются data-driven;
- после завершения возвращается тот же identity молодым взрослым;
- гном появляется в ближайшей свободной допустимой клетке возле Храма;
- если свободной клетки нет, completion ждёт появления клетки и не расходует ингредиенты повторно;
- сохраняются skills, TotalSkillCapacity, family graph, historical partnership records и внешность;
- прежний Inventory не восстанавливается повторно;
- одна death instance применяется один раз;
- повторные циклы death/return разрешены через новые `DeathInstanceId`.

### Active partnership после return

Если прежний партнёр уже создал новую active pair, новая пара сохраняется. Старая связь остаётся только historical relation. Return не разрывает новую пару и не восстанавливает старую автоматически. Бывшие партнёры могут снова образовать пару только через обычный matching, если оба позднее свободны.

## Зелье омоложения

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

## Владение состояния

- Lifecycle/Society: death instance, identity, family/relations, age, pregnancy cancellation, return и rejuvenation;
- Inventory: dropped stacks и identity cap;
- Buildings/Production: `GraveBox`, permanent grave и temple action;
- Skills: mixed Alchemy/Service work contract;
- Needs: global non-stacking Mood modifier;
- Presentation: имя могилы, visuals и notifications.

## Инварианты

- все предметы умершего могут находиться в одной world cell без потери количества;
- cap находится ровно в одном состоянии: world, carried, reserved, grave-consumed или temple-consumed;
- completed `GraveBox` необратимо consumes cap;
- grave non-packable;
- глобальный cap penalty применяется не более одного раза независимо от количества источников;
- return не дублирует Inventory;
- return не создаёт вторую active pair;
- rejuvenation не применяется к Child и не повторяет inheritance;
- rejuvenation беременной атомарно отменяет pregnancy;
- Save/Load не повторяет production, lifecycle или consumable commits.

Q-048 и Q-052 закрыты. Design #150 полностью определён.

## Журнал решений

| Дата | Решение | Кто подтвердил | Изменённые разделы/issues |
|---|---|---|---|
| 2026-08-03 | В обычном режиме мёртвый resident исчезает из active roster/selection; вместо него остаётся поднимаемый именной колпак. Мастерская каменщика производит персональное надгробие. Resurrection остаётся будущим building slice. Zombie mode не создаёт колпак и вынесен в отдельный questionnaire. | Пользователь в проектном чате | Death workflow, grave naming, #150, #586 |
