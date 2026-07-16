# Здоровье, лечение и больница

Связанная задача: [#130](https://github.com/bageus/Dig/issues/130).

## 1. Статус

Документ фиксирует подтверждённые владельцем правила и результаты аудита legacy `scripts`.

Подтверждено владельцем дизайна:

- отдельных травм, ранений и severity-каталога у гнома нет;
- authoritative состоянием является только числовой `Health`;
- для лечения в больнице нужен второй гном, выполняющий работу врача;
- лечение не требует и не резервирует материалы или лекарства;
- врач получает опыт `skill.service`;
- при `Health < 25` создаётся уведомление в бегущей строке о том, что гном при смерти;
- лечение выполняется этапами;
- один завершённый этап длится один игровой час и восстанавливает 25 Health.

Оставшиеся решения собраны в Q-054.

## 2. Script audit

Проверены:

- `scripts/misc/techtreetunes.tcl`;
- `scripts/classes/work/krankenhaus.tcl`;
- `scripts/classes/zwerg/z_work_prod.tcl`;
- `scripts/classes/zwerg/z_spare_main.tcl`;
- `scripts/classes/zwerg/z_spare_procs.tcl`;
- `scripts/classes/zwerg/z_events.tcl`.

### 2.1. Technology/content

Legacy Hospital (`Krankenhaus`) имеет:

```text
recipe:
3 stone + 3 iron + 3 crystal + 1 gold

research requirements:
Service 7 + Food 2

construction grants:
Metallurgy 7 + Service 3

service:
_Heilen — pre-invented
```

Больница открывается от `Dreherei` — поздней металлообрабатывающей мастерской.

### 2.2. Functional places

Legacy building создаёт:

- один worker/doctor;
- четыре guest/patient места;
- один `current_patient`;
- один active treatment workflow;
- exclusive production mode.

То есть одновременно ожидать могут до четырёх пациентов, но лечение выполняется только для одного пациента одним врачом.

### 2.3. Legacy patient selection

Legacy пациент:

- самостоятельно ищет построенную больницу;
- рассматривает больницу в пределах range 40;
- требует свободное patient place;
- требует включённый service `_Heilen`;
- начинает искать лечение уже при Health ниже 97%;
- добавляет waiting order каждый цикл ожидания.

Legacy врач выбирает пациента по отношению waiting order к текущему Health. Это сочетает время ожидания и тяжесть состояния, но точный алгоритм не переносится автоматически.

### 2.4. Legacy treatment

В старом коде существовали две presentation-ветви:

- тяжёлое состояние — постепенное восстановление с электрической процедурой;
- более лёгкое состояние — последовательные лечебные анимации и финальный укол.

Продолжительность зависела от недостающего Health и `exp_Service`, а один из путей в конце устанавливал Health сразу в максимум.

Эта механика **не является authoritative для Dig**, потому что владелец уже установил единое правило: один час работы = один committed этап = +25 Health.

### 2.5. Legacy notifications

Legacy newsticker создавал сообщение «почти мёртв» при Health ниже 50% и удалял его после восстановления выше порога.

Для Dig этот порог superseded решением владельца: уведомление создаётся при `Health < 25`.

## 3. Target domain model

```text
ResidentHealthState
- ResidentId
- Health                 // 0..100
- IsAlive
- ActiveHospitalVisitId?
- Version

HospitalTreatment
- TreatmentId
- HospitalId
- PatientResidentId
- DoctorResidentId?
- StageIndex
- StageProgress
- State                  // Waiting, Active, Paused, Completed, Cancelled
- BlockReason?
- Version
```

Отдельные `InjuryId`, wound slots, severity и medical status stacks не создаются.

## 4. Treatment stage

Подтверждённая единица работы:

```text
duration = 1 game hour
committed Health gain = min(25, 100 - current Health)
materials = none
worker = one doctor resident
patient = one living resident
```

Health изменяется только при подтверждённом completion этапа. Animation callback не применяет лечение.

Врач получает `skill.service` только за committed treatment stage. Точное количество опыта относится к Q-014, если владелец не задаст его отдельно.

## 5. Places and reservations

Script-faithful candidate:

- `DoctorPlaceCount = 1`;
- `PatientPlaceCount = 4`;
- `ActiveTreatmentSlots = 1`;
- waiting patient place и active patient place имеют stable indices;
- один patient place не может быть зарезервирован двумя жителями;
- один resident не может одновременно ожидать в двух больницах;
- врач резервируется только для active stage;
- упаковка здания блокируется при doctor/patient/treatment reservations.

Количество мест требует подтверждения в Q-054.

## 6. Job lifecycle

Candidate lifecycle:

1. Пациент получает hospital intent автоматически или по команде игрока.
2. Выбирается доступная больница и резервируется patient place.
3. Пациент самостоятельно идёт в больницу.
4. Treatment ожидает подходящего врача.
5. Врач резервирует doctor place и начинает один час работы.
6. На completion Health увеличивается максимум на 25.
7. Если Health ниже 100, следующий этап может быть поставлен автоматически.
8. При достижении 100 visit завершается и reservations освобождаются.

Переноса недееспособного resident в текущем подтверждённом scope нет: отдельного состояния недееспособности также нет. Окончательное правило движения при критическом Health требует ответа Q-054.

## 7. Interruptions and failures

Обязательные инварианты:

- завершённый этап не применяется повторно после Save/Load;
- отменённый незавершённый этап не выдаёт Health и Service experience;
- смерть пациента отменяет visit и освобождает doctor/patient reservations;
- исчезновение или смерть врача переводит active stage в определённое owner policy состояние;
- разрушение/упаковка больницы не должно оставлять dangling reservations;
- Health не превышает 100;
- лечение не создаёт, не резервирует и не расходует items.

Сохранение или сброс частичного прогресса требует ответа Q-054.

## 8. Notification

Подтверждённое условие:

```text
Health < 25 -> one active near-death notification for this resident
```

Candidate clear policy:

- удалить уведомление при `Health >= 25`;
- удалить при смерти/удалении resident;
- повторно не создавать, пока уже существует активная notification с тем же stable key.

Текст, click-focus и presentation принадлежат HUD/notification layer; условие принадлежит Health owner.

## 9. Save/Load

Сохраняются:

- Health;
- hospital visit и patient place;
- doctor reservation;
- stage index/progress/state;
- committed stage IDs или idempotency key;
- near-death notification logical state;
- definition versions.

Load не должен повторно применять +25 или повторно начислять Service.

## 10. UI and diagnostics

Resident HUD показывает:

- текущий Health;
- состояние `NeedsHospital`, `WaitingForHospital`, `WaitingForDoctor`, `BeingTreated`;
- progress текущего часового этапа;
- следующий ожидаемый Health;
- block reason.

Hospital inspector показывает:

- четыре patient places или подтверждённое другое количество;
- active patient;
- current doctor;
- очередь и priority key;
- stage progress;
- причины ожидания;
- отсутствие medical item requirements.

## 11. Q-054 — оставшиеся решения

1. При каком Health гном автоматически направляется в больницу?
2. Прерывает ли критическое лечение Work schedule немедленно, или больница посещается только в свободное время?
3. Подтвердить `4 patient places / 1 doctor / 1 active treatment`.
4. Кто может быть врачом: любой взрослый гном или требуется минимальный `skill.service`?
5. Врач выбирается как обычный временный worker или назначается больнице постоянно?
6. Как сортируется очередь: lowest Health, longest waiting либо legacy combined priority?
7. После завершения +25 пациент автоматически остаётся на следующий этап до 100 или может уйти после каждого этапа?
8. Что происходит с частичным прогрессом часа при прерывании, смерти/уходе врача или потере питания?
9. Может ли любой живой гном самостоятельно идти при Health 1..24, или появляется состояние недееспособности и перенос другим гномом?
10. Есть ли естественное восстановление Health вне больницы, или только hospital/potions?
11. Требует ли больница энергию; если да, какой класс и расход на этап?
12. Можно ли лечить детей и беременных без дополнительных ограничений?

## 12. Definition of Done

- один authoritative Health owner без отдельного injury catalog;
- Hospital definition и places соответствуют закрытому Q-054;
- treatment stage commit даёт ровно до +25 Health;
- stage длится один игровой час;
- врач обязателен и получает Service grant;
- medical materials отсутствуют;
- очередь и selection deterministic;
- near-death notification использует threshold 25;
- interruption/death/building removal освобождают reservations;
- Save/Load не дублирует stage result;
- Domain, integration и Play Mode tests покрывают основной workflow и edge cases.
