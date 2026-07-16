# Здоровье, лечение и больница

Связанная задача: [#130](https://github.com/bageus/Dig/issues/130).

## 1. Статус

Authoritative design закрыт ответом Q-054. Issue #130 остаётся открытой до реализации.

Подтверждено владельцем дизайна:

- отдельных травм, ранений и severity-каталога у гнома нет;
- authoritative medical state — только числовой `Health`;
- автоматическое обращение в больницу создаётся при `Health < 80`;
- при `Health < 25` гном немедленно прерывает текущую работу и идёт лечиться;
- при `25 <= Health < 80` больница посещается только в свободное время;
- лечение требует второго взрослого гнома, временно выполняющего работу врача;
- минимальный `skill.service` для допуска врача не требуется;
- лечение не требует и не резервирует материалы или лекарства;
- врач получает опыт `skill.service` за лечебную работу;
- одна больница имеет одно место пациента, одно место врача и один active treatment;
- очередь сортируется по минимальному Health, затем по большему времени ожидания, затем по меньшему `ResidentId`;
- лечение восстанавливает Health постепенно со скоростью до 25 за один игровой час;
- уже восстановленная часть Health сохраняется при прерывании;
- лечение автоматически продолжается этапами до Health 100;
- при `Health < 25` создаётся одно уведомление о том, что гном при смерти;
- любой живой взрослый гном с `Health > 0` способен самостоятельно дойти до больницы;
- вне больницы Health постепенно восстанавливается во время еды, сна и отдыха;
- больница требует источник энергии класса 2;
- детей в больнице не лечат;
- беременные лечатся по обычным правилам взрослых.

Точные коэффициенты естественной регенерации, расход энергии и Service experience являются data-driven balance values и относятся к Q-014.

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

Больница открывается от `Dreherei` — поздней Токарной/металлообрабатывающей мастерской.

### 2.2. Что superseded

Scripts содержали четыре patient places, automatic intent при Health ниже 97%, уведомление ниже 50%, две лечебные процедуры и service-dependent duration. Для Dig эти значения superseded решениями Q-054:

- один patient place;
- admission threshold 80;
- near-death threshold 25;
- единая скорость лечения 25 Health за игровой час;
- Service не изменяет скорость лечения;
- один active treatment.

## 3. Target domain model

```text
ResidentHealthState
- ResidentId
- Health                         // 0..100
- IsAlive
- HospitalIntentState?           // None, NonCritical, Critical
- ActiveHospitalVisitId?
- Version

HospitalTreatment
- TreatmentId
- HospitalId
- PatientResidentId
- DoctorResidentId?
- RestoredHealth                 // уже применённая часть текущего этапа
- StageProgress                  // 0..1
- State                          // Waiting, Active, Paused, Completed, Cancelled
- BlockReason?
- Version
```

Отдельные `InjuryId`, wound slots, severity и medical status stacks не создаются.

## 4. Admission и schedule policy

```text
Health >= 80:
  automatic hospital intent отсутствует

25 <= Health < 80:
  NonCritical intent
  лечение выполняется только в FreeTime schedule

0 < Health < 25:
  Critical intent
  текущая работа немедленно прерывается
  hospital intent имеет emergency priority

Health <= 0:
  death lifecycle
```

Игрок может назначить лечение вручную, пока resident является допустимым пациентом. Автоматический и ручной intents используют одну очередь и не создают дубликаты.

Дети не являются допустимыми пациентами. Беременность не меняет admission, priority или treatment rate.

## 5. Places, worker и concurrency

```text
DoctorPlaceCount = 1
PatientPlaceCount = 1
ActiveTreatmentSlots = 1
RequiredEnergyClass = 2
```

- единственное patient place принадлежит выбранному пациенту;
- остальные кандидаты остаются в логической очереди и не резервируют физическое место;
- один resident не может одновременно ожидать в нескольких больницах;
- врачом может быть любой живой взрослый гном, кроме самого пациента;
- минимальный Service threshold отсутствует;
- врач выбирается временно для active treatment и освобождается при паузе, завершении или отмене;
- постоянной должности врача у больницы нет;
- упаковка здания блокируется при patient/doctor/treatment reservation.

## 6. Deterministic queue

Кандидаты сортируются по ключу:

```text
1. Health ascending
2. WaitingSince ascending
3. ResidentId ascending
```

То есть сначала лечится гном с самым низким Health; при равном Health — ожидающий дольше; при полном равенстве — с меньшим stable `ResidentId`.

Critical intent участвует в той же сортировке и естественно получает преимущество благодаря более низкому Health. При освобождении patient place очередь пересчитывается детерминированно.

## 7. Treatment flow

1. Выбирается первый допустимый кандидат очереди.
2. Он резервирует единственное patient place и самостоятельно идёт в больницу.
3. Больница ожидает временного взрослого врача и источник энергии класса 2.
4. После резервирования врача начинается continuous treatment.
5. За один полный игровой час может быть восстановлено максимум 25 Health.
6. Health применяется постепенно в течение часа, а не одним callback в конце.
7. При достижении Health 100 visit завершается и все reservations освобождаются.
8. Если после часа Health ниже 100, следующий этап начинается автоматически без выхода пациента из больницы.

Формула накопления:

```text
rate = 25 Health / 1 game hour
appliedDelta = min(rate * activeDeltaTime, 100 - Health)
```

Начисление должно быть interval/idempotency-safe. Presentation animation не является владельцем Health result.

## 8. Interruption и pause

Treatment переходит в `Paused`, когда:

- врач ушёл, умер или стал недоступен;
- пропала энергия подходящего класса;
- active job был прерван;
- больница временно перестала быть функциональной.

При паузе:

- уже применённый Health не откатывается;
- `StageProgress` и `RestoredHealth` сохраняются;
- patient place остаётся за пациентом, пока visit не отменён;
- doctor reservation освобождается, если врач больше не выполняет работу;
- после появления нового врача и энергии лечение продолжается с сохранённого прогресса.

Смерть пациента отменяет visit и освобождает все reservations. Разрушение или принудительное удаление больницы отменяет visit без отката уже восстановленного Health.

## 9. Natural Health regeneration

Вне Hospital и зелий Health постепенно восстанавливается только во время:

- еды;
- сна;
- отдыха.

Работа, перемещение, бой и обычное бездействие сами по себе Health не восстанавливают. Каждое подходящее действие применяет data-driven regeneration rate через continuous Needs/Activity owner. Уже применённая регенерация сохраняется при прерывании действия.

Natural regeneration может изменить hospital priority. При достижении Health 100 visit/intention завершается; точные rates относятся к Q-014.

## 10. Notification

```text
Health < 25 -> one active near-death notification for this resident
Health >= 25 -> notification cleared
```

Уведомление также удаляется при смерти/удалении resident и не создаётся повторно, пока существует активная notification с тем же stable key.

## 11. Energy

- `RequiredEnergyClass = 2`;
- источник выбирается по общей single-source energy policy;
- без подходящего источника лечение не начинается;
- потеря энергии ставит active treatment на паузу;
- уже восстановленный Health сохраняется;
- точное потребление энергии является data-driven balance value Q-014.

## 12. Save/Load

Сохраняются:

- Health;
- hospital intent и `WaitingSince`;
- hospital visit и patient place;
- doctor reservation;
- stage progress и уже применённый `RestoredHealth`;
- treatment state/block reason;
- interval/idempotency state;
- near-death notification logical state;
- definition versions.

Load не должен повторно применять уже начисленное Health или Service experience.

## 13. UI and diagnostics

Resident HUD показывает:

- текущий Health;
- `NeedsHospital`, `WaitingForHospital`, `WaitingForDoctor`, `WaitingForEnergy`, `BeingTreated`, `TreatmentPaused`;
- critical/non-critical admission mode;
- progress текущего часа;
- уже восстановленный Health и следующий ожидаемый Health;
- block reason.

Hospital inspector показывает:

- одно patient place;
- active patient;
- current temporary doctor;
- очередь и priority key;
- stage progress;
- required energy class 2 и energy block reason;
- отсутствие medical item requirements.

## 14. Definition of Done

- один authoritative Health owner без injury catalog;
- admission thresholds 80/25 и schedule policy реализованы;
- Hospital имеет 1 patient place, 1 temporary doctor и 1 active treatment;
- очередь использует `Health -> WaitingSince -> ResidentId`;
- лечение непрерывно восстанавливает максимум 25 Health за игровой час;
- partial Health и progress сохраняются при interruption/SaveLoad;
- этапы автоматически повторяются до Health 100;
- взрослые и беременные допустимы, дети исключены;
- больница требует энергию класса 2;
- natural regeneration работает только при еде, сне и отдыхе;
- near-death notification использует threshold 25;
- cancellation/death/building removal освобождают reservations;
- Domain, integration и Play Mode tests покрывают основной workflow и edge cases.
