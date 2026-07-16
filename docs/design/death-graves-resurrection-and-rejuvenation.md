# Смерть, могилы, возвращение и омоложение

Связанная задача: #150.

## Смерть и оставшиеся предметы

После смерти гнома:

- предметы личного Inventory переходят в world locations рядом с местом смерти;
- появляется identity-linked предмет `Колпак {ИмяГнома}`;
- колпак связан с `ResidentId` и не зависит от рабочей шапки;
- identity, имя, пол, family graph, historical relations, внешность, skills, capacity и lifecycle record сохраняются;
- jobs/reservations освобождаются без дублирования предметов.

Видимый незахороненный колпак создаёт отрицательный Mood modifier. Его числовые параметры остаются balance data.

## Могила

```text
3 камня + Колпак конкретного гнома -> Могила {ИмяГнома}
```

- производится в Мастерской каменщика;
- cap становится неотделимой частью могилы;
- размещённую могилу нельзя переносить, упаковывать или разобрать;
- из могилы воскресить нельзя;
- имя и связь с ResidentId сохраняются независимо от локализации.

## Возвращение в Храме

```text
Колпак конкретного гнома
+ 1 хомяк
+ 4 золота
+ 2 кристаллические руды
-> возвращение гнома
```

- нужен свободный cap, не использованный могилой;
- возвращается тот же identity молодым взрослым;
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

- взрослый становится ребёнком на стандартные 2 игровых дня;
- adult jobs временно недоступны;
- обучение в школе доступно;
- skills, capacity, identity, family graph, relations и внешность сохраняются;
- inheritance повторно не начисляется;
- повторное омоложение разрешено после нового взросления;
- consumable расходуется один раз.

## Владение состояния

- Lifecycle/Society: death instance, identity, family/relations, age и return/rejuvenation;
- Inventory: dropped items и identity cap;
- Buildings/Production: grave recipe и temple action;
- Needs: Mood modifier;
- Presentation: имя могилы, visuals и notifications.

## Инварианты

- cap находится ровно в одном состоянии: world, reserved, grave-consumed или temple-consumed;
- grave non-packable;
- return не дублирует Inventory;
- return не создаёт вторую active pair;
- rejuvenation не повторяет inheritance;
- Save/Load не повторяет lifecycle commits.

Q-048 и Q-052 закрыты.