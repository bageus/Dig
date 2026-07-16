# Партнёрство, беременность и рождение

Связанные задачи: #11 и #145.

## Пары

Репродуктивная пара создаётся только между мужчиной и женщиной.

- пара эксклюзивна;
- одновременно resident может состоять только в одной active pair;
- пара сохраняется до смерти одного партнёра;
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

Если все eligibility-проверки выполнены и встреча успешно дошла до commit, зачатие гарантировано. Отдельный probability roll не используется.

Прерывание до commit освобождает reservation и не создаёт беременность.

## Беременность

- длительность — 1 игровой день;
- pregnancy принадлежит Lifecycle/Society state беременной resident;
- parent links фиксируются при зачатии;
- рождение создаёт одного ребёнка и одно birth event;
- максимального числа детей у пары нет;
- после рождения начинается двухдневный cooldown.

## Возвращение умершего партнёра

Возвращение жителя сохраняет identity, family links и historical partnership record. Но surviving resident после смерти мог уже создать новую эксклюзивную пару.

До ответа на Q-052 система не должна:

- автоматически создавать две active partnerships;
- безусловно разрывать новую пару;
- безусловно восстанавливать старую пару;
- использовать historical relation как active reproduction pair.

Q-052 должен определить, какая связь становится active после возвращения.

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

Смерть партнёра прекращает active pair link, но historical partnership сохраняется для Lifecycle return и UI истории.

## Владение состоянием

- Society владеет active pair links, historical relations, candidate scoring и family graph;
- Lifecycle владеет pregnancy, birth, age, death и return;
- Agents/Needs предоставляет authoritative eligibility snapshot;
- Reservations владеет meeting reservation;
- Presentation показывает status/reasons, но не создаёт conception.

## Save/Load

Сохраняются active pair, historical partnership, pregnancy start/end tick, parent IDs, meeting/conception idempotency key, cooldown и lifecycle version. Load не повторяет зачатие или рождение.

## Критерии приёмки

- создаются только мужчина-женщина пары;
- active pair эксклюзивна;
- запрещённое родство всегда отклоняется;
- Mood 75, Nutrition 80 или Alertness 80 не проходят;
- Health ниже 100 не проходит;
- eligible meeting гарантирует беременность;
- беременность длится ровно сутки;
- после смерти партнёра surviving resident может создать новую пару;
- return не создаёт вторую active pair до решения Q-052;
- число детей не ограничено;
- Save/Load не дублирует conception/birth.