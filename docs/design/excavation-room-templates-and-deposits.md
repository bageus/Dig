# Копание, шаблоны пещер и ресурсные жилы

> **Status synchronization (2026-07-30):** template catalog и template-instance scope #89/#90 подтверждены пользователем после merge PR #520 и имеют статус `IMPLEMENTED`. Deterministic per-cell deposits scope #91 также реализован в World/Generation/Save contracts; фактический licensed Unity Test Runner result всё ещё требуется для `VERIFIED`. Output/hauling/processing scope #87 остаётся открытым.

## 1. Назначение

Документ описывает целевую систему свободной копки, объёмных шаблонных пещер, грунта, жил, добычи и передачи материалов в Inventory/Hauling.

Система расширяет существующие World, Jobs, Navigation, Inventory и hauling. Она не создаёт второй источник истины.

Связанные документы:

- [`world-3d-depth.md`](world-3d-depth.md);
- [`terrain-resource-output-and-processing.md`](terrain-resource-output-and-processing.md);
- [`material-demand-and-hauling.md`](material-demand-and-hauling.md);
- [`skills-and-progression.md`](skills-and-progression.md);
- [`open-questions.md`](open-questions.md).

## 2. Владение состоянием

- World владеет `X,Y,Z`, terrain, exploration, designations, template provenance и deposits;
- Jobs владеет lifecycle digging/mining jobs и reservations;
- Navigation владеет derived routes/regions;
- Inventory владеет world stacks и quantities;
- Generation владеет deterministic исходным размещением грунта и жил;
- Agents/Skills владеет `skill.stonework` и другими навыками;
- Presentation строит preview, своды, стены и предметы из snapshots.

## 3. Полноценный 3D-мир

- authoritative координата клетки — `X,Y,Z`;
- глубина ограничена четырьмя клетками: `Z = 0..3`;
- гномы, предметы, здания и пути объёмны;
- камера преимущественно боковая, но не ограничивает симуляцию до 2D;
- свободная копка возможна влево/вправо, вверх/вниз и в глубину при валидном маршруте и рабочей позиции.

## 4. Свободная копка

Игрок может назначать отдельные клетки или протяжённый план без шаблона.

Свободная excavation:

- доступна с начала игры;
- использует обычные `Dig` designations и digging jobs;
- не получает гарантированные эстетические своды;
- может образовать любую допустимую форму;
- проверяет bounds, mineability, protected objects, reservations и reachable work positions;
- использует terrain/deposit output resolver после commit.

## 5. Тоннели

- доступны с начала игры;
- могут идти по X, Y и Z;
- не обязаны занимать все четыре клетки глубины;
- preview показывает весь 3D target set;
- тоннель является проходом, а не отдельным зданием;
- выход за `Z=0..3` запрещён.

## 6. Шаблоны пещер

### 6.1 Каталог

| TemplateId | Название | Доступность | Нижнее основание | Верхнее основание | Высота | Глубина |
|---|---|---|---:|---:|---:|---:|
| `excavation.room.small` | Малая пещера | с начала | 5 | 3 | 3 | 3 |
| `excavation.room.medium` | Средняя пещера | `skill.stonework >= 20` | 8 | 6 | 3 | 3 |
| `excavation.room.large` | Большая пещера | `skill.stonework >= 40` | 12 | 8 | 5 | 4 |
| `excavation.room.tall` | Высокая пещера | `skill.stonework >= 60` | 10 | 6 | 7 | 4 |

### 6.2 Динамическая доступность

Для меню вычисляется максимальный `skill.stonework` среди текущих допустимых гномов.

- если хотя бы один гном достигает порога, шаблон доступен для нового размещения;
- если максимальное значение падает ниже порога, шаблон немедленно становится недоступен для новых размещений;
- unlock не является постоянной технологией;
- уже выкопанные пещеры не исчезают и не перестраиваются;
- уже подтверждённый, но незавершённый plan продолжает выполняться;
- после подтверждения plan больше не проходит повторную проверку Stonework;
- падение Stonework не отменяет, не приостанавливает и не удаляет child designations существующего plan.

### 6.3 Объёмная форма

Фронтальный срез является трапецией. Эта же трапеция без изменения проецируется на каждую клетку глубины шаблона.

Следовательно:

- ширина и высота одинаковы на каждом Z-слое;
- помещение не сужается к задней плоскости;
- depth 2/3/4 означает ровно такое количество занятых логических Z-клеток;
- authoritative mask содержит все клетки объёма, а не только фронтальный силуэт;
- Navigation и Building placement видят фактический excavated volume;
- все строки имеют один геометрический центр, вычисленный от нижнего основания;
- если чётность ширины строки отличается от чётности основания, крайние клетки копаются наполовину: у левой граничной клетки удаляется правая половина, у правой — левая половина;
- half-cell boundary использует существующие `ExcavationQuarter` (`UpperLeft/LowerLeft/UpperRight/LowerRight`), поэтому увеличение клетки до восьми частей не требуется;
- partial boundary cell остаётся solid shell-клеткой, не становится navigation cell, не выдаёт terrain output и считается завершённой для template plan после authoritative quarter-mask commit.

### 6.4 Ориентация и проходность

- зеркальное разворачивание шаблона запрещено;
- шаблонные пещеры проходные;
- каждая имеет два входа/выхода;
- вся нижняя строка шириной `BaseWidth` на `Z=0` до подтверждения уже должна быть непрерывным открытым горизонтальным тоннелем;
- левая и правая крайние клетки этой строки являются входом и выходом;
- ни одна клетка открытого нижнего тоннеля не создаёт Dig designation повторно;
- входы всегда расположены слева и справа по оси `X` на фронтальном срезе;
- входы на передней или задней границе по `Z` не используются;
- произвольное вращение, создающее зеркальную форму или меняющее ось прохода, запрещено;
- stable orientation и entrance positions входят в TemplateInstance и save data.

### 6.5 План и выполнение

`ExcavationTemplateInstance` содержит:

- stable instance id;
- template id/version;
- anchor/orientation;
- ordered 3D target mask;
- левый и правый entrance descriptors по оси X;
- child designation/job state;
- progress;
- aesthetic provenance;
- unlock snapshot/diagnostics на момент подтверждения.

Поток:

1. UI строит preview без мутации Domain. Pointer может находиться на породе внутри фронтального силуэта: runtime ищет ближайшую подходящую нижнюю строку тоннеля под pointer в пределах высоты preset.
2. Application атомарно валидирует весь объём и текущий Stonework threshold:
   - нижняя строка `Z=0` целиком открыта и образует сквозной тоннель;
   - каждая остальная target cell является solid, mineable и не protected;
   - открытая, protected или unmineable клетка выше основания делает plan invalid.
3. Invalid preview сохраняет полный силуэт, но красным отмечает конкретные клетки нарушения; при отсутствии тоннеля красной является соответствующая часть нижней строки. Invalid ЛКМ не меняет World/Jobs и оставляет diagnostics видимыми.
4. Создаются plan и обычные cell designations только для `ExcavationCells`; уже открытые `BaseTunnelCells` остаются частью provenance/volume, но не входят в commit designation batch. Для parity-mismatch строк plan дополнительно хранит required quarter mask крайних half-cell targets.
5. Reconciliation создаёт child digging jobs. Full-cell target завершается при `4/4`; half-cell target завершается при своём required `2/4`, снимает designation/job, но оставляет противоположную половину rock authoritative.
6. Несколько гномов работают по независимым targets.
7. Последующее падение Stonework не меняет существующий plan.
8. Progress считается по фактически выкопанным клеткам.
9. Своды восстанавливаются из provenance.
9. Отмена по команде игрока не восстанавливает уже выкопанный terrain.

## 7. Эстетические своды

Только template provenance даёт обработанный вид:

- плавный трапециевидный потолок;
- согласованные боковые стены;
- визуально чистый проход;
- отсутствие случайных выступов внутри target mask;
- одинаковый профиль на каждом глубинном слое;
- один и тот же centered row profile используется preview, designation overlay, terrain quarter cuts, completed trim и back wall;
- completed trim строится в world space и не наследует повторно side-view root rotation; над основной сценой не создаётся дублирующая или смещённая проекция;
- rebuild после save/load без хранения mesh как authoritative state.

Свободно выкопанная пещера не получает своды автоматически, даже если форма совпала.

## 8. Грунт

Обычная клетка использует TerrainOutputProfile. Поддерживаются:

- песчаный грунт — outputs отсутствуют;
- каменная порода — может выпадать только камень;
- рудная порода — камень, железная руда, очень редкая золотая руда, уголь;
- кристаллическая порода — немного камня, железная руда, кристаллическая руда, редкая золотая руда;
- лавовая порода — золотая руда, немного камня/кристаллической руды, железная руда, уголь;
- недобываемая порода — designation запрещён.

Outputs случайны, но детерминированы по seed/version/cell. Точные вероятности и quantities остаются data-driven.

## 9. Жилы

### 9.1 Модель

Жила заменяет обычную клетку породы.

- она не является overlay над грунтом;
- при разработке не выдаётся обычный terrain loot;
- output определяется DepositDefinition;
- работа занимает больше времени, чем обычная excavation;
- одна depleted cell не может выдать output повторно.

### 9.2 Типы

- железная руда;
- золотая руда;
- кристаллическая руда;
- уголь;
- камень.

Цепочки:

- `ore.iron -> material.iron`;
- `ore.gold -> material.gold`;
- `ore.crystal -> material.crystal`.

### 9.3 Соседние клетки

Generation может разместить рядом от одной до четырёх deposit cells. Соседство не создаёт обязательную общую cluster-сущность:

- каждая клетка имеет собственный id/state;
- истощение одной не истощает соседей;
- одинаковый visual может объединять соседние клетки только на уровне Presentation;
- per-cell outputs и reservations остаются независимыми.

### 9.4 Стены и истощение

Разработка жилы меняет геометрию:

- клетка становится пустой;
- боковая/верхняя/нижняя граница открывается;
- разработка задней стены по Z углубляет помещение;
- работа выполняется как mining/excavation command, а не как сбор decoration без изменения World.

### 9.5 Authoritative lifecycle и generation

- `WorldState` является единственным владельцем `TerrainDepositState`; Unity хранит только snapshots/view models;
- генератор использует seed, algorithm version, stable XYZ cell id и независимые named streams для origin, definition, cluster size, instance id и neighbour order;
- порядок входных host cells и definitions не влияет на результат;
- `DepositDefinition.Version`, allowed host `MaterialId` и work-effort multiplier являются data-driven contract;
- новые deposit cells создаются hidden; successful excavation раскрывает только шесть ортогональных XYZ-соседей;
- stale instance id/yield отклоняется до terrain mutation;
- успешная excavation одной deposit cell в той же World mutation делает её depleted, открывает terrain и инвалидирует необходимые chunk/layer snapshots;
- exact density, yields и effort multiplier остаются `BALANCE_TBD` по Q-014; neutral runtime multiplier не считается утверждённым балансом.

## 10. Выдача добычи

После успешного commit:

1. World меняет terrain/deposit state.
2. Output resolver вычисляет terrain или deposit outputs.
3. Inventory создаёт world stacks в клетке или ближайшей валидной пустой позиции.
4. Шахтёр не помещает output в личный inventory.
5. Job завершает и освобождает reservations.
6. Отдельный demand-aware planner может создать hauling job.

Terrain и deposit output взаимоисключающие для одной клетки.

## 11. Hauling и fog of war

Материал на земле не собирается автоматически только потому, что существует.

Задание доставки создаётся, если:

- активная работа/здание требует этот ItemId; или
- склад имеет включённый filter/collection rule для ItemId/category.

Дополнительно source должен быть раскрыт и не скрыт туманом войны в момент создания задания.

Подробности: [`material-demand-and-hauling.md`](material-demand-and-hauling.md).

## 12. Навыки

- обычная копка породы и камня развивает `skill.stonework`;
- специализированная руда и metal processing связаны с `skill.metallurgy`;
- кристаллы/уголь и crystal processing связаны с `skill.alchemy`;
- перенос материалов связан с `skill.logistics`;
- mixed-skill policy открыта в Q-019.

## 13. Save/Load

Сохраняются:

- 3D coordinates и world versions;
- template id/version, mask, левый/правый входы, progress и provenance;
- unlock snapshot на момент placement;
- terrain types и факт output roll;
- deposit instance id, definition id/version, exact XYZ, reveal/depletion и per-cell version;
- jobs/reservations;
- world stacks;
- deposit generator, terrain generator и output profile versions;
- demand/hauling state.

Существующий plan после загрузки продолжает выполняться независимо от текущего Stonework. Старые 2D координаты мигрируют в `Z=0`.

## 14. Диагностика

UI/inspector показывает:

- полный 3D preview;
- Stonework threshold и текущий максимальный источник;
- причину блокировки нового placement;
- сохранённый unlock snapshot существующего plan;
- invalid cells/reasons;
- левую и правую entrance positions;
- terrain/deposit type;
- effort и progress;
- expected output profile без раскрытия скрытого random result, если это запрещено UX;
- world stacks и hauling eligibility;
- fog/demand reason;
- provenance и renderer rebuild state.

## 15. Открытые параметры

- Q-014 — вероятности, quantities, generation frequency и effort balance;
- Q-019 — mixed-skill policy.

## 16. Связанные issues

- #87 — общий feature;
- #88 — 3D coordinates;
- #89 — каталог/skill availability;
- #90 — template plans и своды;
- #91 — deposits/generation;
- #92 — output resolver и hauling;
- #93 — Presentation;
- #94 — Save/Load;
- #108 — processing buildings;
- #109 — terrain outputs;
- #110 — demand/fog-aware hauling.


## 17. Журнал подтверждений

- 2026-07-30 — пользователь подтвердил acceptance #89/#90 после merge PR #520: все четыре presets, thresholds, persistent designations и template instance lifecycle считаются реализованными; runtime status не повышается до `VERIFIED` без executed Unity result artifact.
- 2026-07-30 — #91 реализует versioned deterministic deposit placement, hidden/revealed/depleted per-cell lifecycle, World-owned depletion/reveal, exact-Z persistence v11 и integrity diagnostics. Q-014 остаётся открытым только для числового balance tuning.
