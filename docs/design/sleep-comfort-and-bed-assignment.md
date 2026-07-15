# Качество сна и личные спальные места

Связанные задачи: #142 и #159.

## Область

На текущем этапе система качества применяется только ко сну. Качество кухни, ванной, гостиной и других помещений в этот контракт не входит.

## Уровни сна

```text
0 — Improvised/Floor
1 — Tent
2 — Medieval
3 — Industrial
4 — Luxury
```

Ожидаемый tier resident равен лучшему исследованному sleep tier поселения. Более высокий фактический tier не даёт дополнительный бонус сверх базовой скорости.

## Скорость восстановления бодрости

Начальные коэффициенты относительно базовой скорости места:

| Отставание actual tier от expected tier | Alertness rate multiplier |
|---:|---:|
| 0 или actual выше expected | 1.00 |
| 1 tier ниже | 0.75 |
| 2 tiers ниже | 0.50 |
| 3 и более tiers ниже | 0.25 |

Коэффициенты являются стартовым балансом и хранятся в data-driven `SleepComfortProfile`.

## Ограничения Mood и Alertness

- при использовании любого sleep tier ниже ожидаемого Mood не может вырасти выше 50 за счёт этого сна;
- сон на полу не восстанавливает Mood;
- сон на полу восстанавливает Alertness только до 75;
- partial sleep применяет эффект постепенно и сохраняет уже полученный результат после пробуждения;
- completion не выдаёт повторный полный bonus.

## Личная кровать

Resident выбирает ближайшее свободное допустимое спальное место и закрепляет его как personal bed assignment.

Assignment снимается, если место:

- уничтожено;
- упаковано;
- перестало существовать или быть sleeping place;
- не имеет достижимого пути;
- находится дальше допустимой path distance.

Начальный `PersonalBedMaxPathLength` — 30 клеток по фактическому navigation path. Значение data-driven.

После снятия assignment resident ищет ближайшее свободное место. Если подходящего места нет, разрешён сон на земле с ограничениями Floor tier.

## Пары

Каждая кровать предоставляет два связанных sleeping slots. Пара старается использовать два slots одной кровати.

- оба slots резервируются атомарно;
- если второй slot занят или недоступен, совместный сон не начинается;
- личные assignments партнёров могут ссылаться на одну кровать и разные slot IDs;
- отсутствие совместного места не блокирует emergency sleep на другом месте или на полу.

## Выбор места

Utility AI учитывает:

1. personal assignment;
2. наличие пути и path length;
3. свободные slots;
4. возможность совместного сна с партнёром;
5. фактический и ожидаемый tier;
6. состояние здания;
7. критичность Alertness;
8. расстояние до fallback.

Личное место предпочтительно, пока оно доступно и находится в пределах `PersonalBedMaxPathLength`.

## Владение состоянием

- Technology предоставляет highest researched sleep tier;
- Buildings владеет beds, slots, condition и actual tier;
- Agents владеет personal assignment и active Sleep action;
- Reservations владеет временным занятием slots;
- Needs применяет Alertness/Mood intervals;
- Presentation показывает expected/actual tier, multiplier, caps и reason.

## Save/Load

Сохраняются personal bed/slot ID, active Sleep action, applied intervals и reservation refs. Derived expected tier и multipliers пересчитываются из authoritative content/building state.

## Критерии приёмки

- gap 0/1/2/3+ использует multipliers 1.00/0.75/0.50/0.25;
- любой lower tier ограничивает sleep Mood значением 50;
- Floor не даёт Mood и не поднимает Alertness выше 75;
- unreachable, packed, destroyed или слишком далёкое место снимает assignment;
- поиск выбирает ближайшее свободное место;
- каждая кровать имеет два связанных slots;
- пара резервирует оба slots или ни одного;
- Save/Load не создаёт duplicate effects/reservations.
