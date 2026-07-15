# Детство, школа и наследование характеристик

Связанные задачи: #11, #145 и #146.

## Детство

- стадия детства длится 2 игровых дня;
- ребёнок является отдельным resident/lifecycle entity;
- взрослые jobs недоступны до перехода во взрослую стадию;
- взросление публикует один idempotent lifecycle event.

## Наследование навыков

Ребёнку доступны все 12 характеристик.

Для каждого навыка отдельно выполняется deterministic random roll `10..20%`:

```text
ParentAverageSkill_i = (FatherSkill_i + MotherSkill_i) / 2
ChildSkill_i = ParentAverageSkill_i × (1 - RandomPercent_i)
```

Округление выполняется общей fixed-point policy.

## Наследование TotalSkillCapacity

Если родители имеют capacity выше 100, ребёнок также наследует повышенную максимальную шкалу.

```text
ParentAverageCapacity = (FatherCapacity + MotherCapacity) / 2
CalculatedChildCapacity = ParentAverageCapacity × (1 - RandomPercentCapacity)
RandomPercentCapacity = 10..20%
ChildCapacity = max(CalculatedChildCapacity, Sum(ChildSkill_i))
```

`ChildCapacity` ограничен глобальным допустимым диапазоном `0..200`. Последняя операция гарантирует, что ребёнок не рождается с суммой навыков выше собственной capacity.

## Школа

Одна школа имеет:

- 1 teacher place;
- 4 student places.

В curriculum можно выбрать от 1 до 12 характеристик. Порядок задаётся игроком и сохраняется.

Школа работает круглосуточно. Ограничением являются доступность свободного подходящего учителя, ученика, места, пути и отсутствие более приоритетных survival/emergency действий.

## Учебный цикл

- один цикл длится 1 игровой час;
- учитель может обучать навыку только если `TeacherSkill > ChildSkill`;
- successful cycle добавляет ребёнку `+1` выбранного навыка;
- учитель получает `skill.service +0.5`;
- после завершения создаётся новое учебное **задание**, а не новое здание;
- если тот же учитель может продолжать, он сохраняет назначение и запускает следующий цикл;
- если он больше не может обучить выбранным навыкам, он исключается из текущего teacher search и выбирается другой;
- curriculum выполняется по порядку;
- при отсутствии допустимого учителя обучение ставится на паузу.

Skill grant школы использует общую capacity/redistribution policy. При заполненной capacity рост обучаемого навыка может уменьшать другие навыки по Q-029.

## Владение состоянием

- Lifecycle владеет возрастом и стадией;
- Society/FamilyGraph предоставляет родителей;
- Agents/Skills владеет навыками и capacity;
- School building владеет curriculum и places;
- Jobs/Reservations владеет teacher/student cycle;
- Presentation показывает прогресс и причины блокировки.

## Save/Load

Сохраняются age/stage progress, результаты inheritance random rolls, child capacity, curriculum order, current skill index, teacher/student reservations, cycle progress и applied grant IDs.

## Критерии приёмки

- детство длится ровно 2 дня;
- наследование выполняет отдельный roll для каждого навыка;
- повышенная parental capacity может быть унаследована;
- сумма стартовых навыков не превышает child capacity;
- curriculum поддерживает все 12 навыков;
- одна школа одновременно имеет максимум 1 учителя и 4 учеников;
- школа не ограничена Work schedule;
- равные значения teacher/child не позволяют обучение;
- один час выдаёт ровно `+1` и Service `+0.5`;
- новое задание не создаёт новое School building;
- Save/Load не повторяет completed cycle.
