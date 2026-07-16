# Q-054 — жизненный цикл лечения в больнице

Связанные документы:

- [`health-hospital-and-treatment.md`](health-hospital-and-treatment.md);
- issue [#130](https://github.com/bageus/Dig/issues/130).

## Статус

`ANSWERED`

## Подтверждённые решения

- automatic hospital intent: `Health < 80`;
- при `Health < 25` текущая работа немедленно прерывается;
- при `25 <= Health < 80` лечение выполняется только в свободное время;
- один patient place, один doctor place, один active treatment;
- врачом может быть любой взрослый гном; minimum `skill.service` отсутствует;
- врач является временным worker для active treatment;
- очередь: минимальный Health, затем большее время ожидания, затем меньший `ResidentId`;
- один час лечения восстанавливает максимум 25 Health;
- Health восстанавливается постепенно, а не одним результатом в конце часа;
- уже восстановленный Health и progress сохраняются при прерывании;
- потеря врача или энергии ставит treatment на паузу;
- этапы автоматически продолжаются до Health 100;
- гном с Health 1..24 самостоятельно идёт в больницу;
- отдельного incapacitated/carry flow нет;
- вне больницы Health восстанавливается во время еды, сна и отдыха;
- больница требует энергию класса 2;
- дети не лечатся;
- беременные лечатся по обычным правилам взрослых;
- near-death notification действует при `Health < 25`.

Точные rates естественной регенерации, расход энергии и величина Service experience относятся к Q-014.

## Superseded legacy values

- 4 patient places заменены на 1;
- admission threshold 97% заменён на 80%;
- notification threshold 50% заменён на 25%;
- две service-dependent процедуры заменены единой скоростью `25 Health / game hour`.

## Журнал

| Дата | Статус | Решение |
|---|---|---|
| 2026-07-16 | OPEN | script audit и lifecycle questions |
| 2026-07-16 | ANSWERED | admission, capacity, doctor, queue, partial healing, regeneration, energy и patient eligibility утверждены |
