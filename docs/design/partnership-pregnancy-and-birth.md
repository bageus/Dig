# Партнёрство, беременность и рождение

Связанные задачи: #11 и #145.

## Пары

Репродуктивная пара создаётся только между мужчиной и женщиной.

- пара эксклюзивна;
- одновременно resident может состоять только в одной паре;
- пара сохраняется постоянно;
- запрещённое родство исключает кандидата;
- главным положительным фактором формирования пары является совпадение расписаний;
- выбор должен быть взаимным;
- при равных результатах используется stable `ResidentId` tie-break;
- после смерти одного партнёра surviving resident освобождается и может создать новую пару.

## Eligibility размножения

Оба партнёра должны одновременно:

- иметь Mood `>75`;
- Nutrition `>80`;
- Alertness `>80`;
- Health `=100`;
- находиться в свободном времени;
- быть живыми и нестарыми;
- иметь путь друг к другу;
- не быть занятыми несовместимым срочным действием.

Беременная женщина не может начать новое зачатие. После родов у неё действует cooldown 2 игровых дня.

## Встреча и зачатие

Специальное здание или кровать не требуются. Система выбирает свободную достижимую клетку, атомарно резервирует встречу обоих партнёров и создаёт совместное действие.

Если все eligibility-проверки выполнены и встреча успешно дошла до commit, зачатие **гарантировано**. Отдельный probability roll не используется.

Прерывание до commit освобождает reservation и не создаёт беременность.

## Беременность

- длительность — 1 игровой день;
- pregnancy принадлежит Lifecycle/Society state беременной resident;
- parent links фиксируются при зачатии;
- рождение создаёт одного ребёнка и одно birth event;
- максимального числа детей у пары нет;
- после рождения начинается двухдневный cooldown.

## State machine

```text
Unpaired
  -> CandidateScoring
  -> ExclusivePair
  -> ReproductionEligibility
  -> MeetingReservation
  -> GuaranteedConception
  -> Pregnancy(1 day)
  -> Birth
  -> PostpartumCooldown(2 days)
  -> ReproductionEligibility
```

Смерть партнёра прекращает pair link. Смерть беременной обрабатывается Lifecycle policy и не должна оставлять dangling pregnancy/parent refs.

## Владение состоянием

- Society владеет pair links, candidate scoring и family graph;
- Lifecycle владеет pregnancy, birth, age и death;
- Agents/Needs предоставляет authoritative eligibility snapshot;
- Reservations владеет meeting reservation;
- Presentation показывает status/reasons, но не создаёт conception.

## Save/Load

Сохраняются pair links, pregnancy start/end tick, parent IDs, meeting/conception idempotency key, cooldown и lifecycle version. Load не повторяет зачатие или рождение.

## Критерии приёмки

- создаются только мужчина-женщина пары;
- пара эксклюзивна;
- запрещённое родство всегда отклоняется;
- Mood 75, Nutrition 80 или Alertness 80 не проходят;
- Health ниже 100 не проходит;
- при выполнении условий commit встречи гарантирует беременность;
- беременность длится ровно сутки;
- после смерти партнёра surviving resident может создать новую пару;
- число детей не ограничено;
- Save/Load не дублирует conception/birth.
