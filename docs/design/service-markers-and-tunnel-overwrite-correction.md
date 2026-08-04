# Служебные маркеры и изменение усиленной геометрии тоннеля

Статус: `APPROVED`.

Tracking issues: [#390](https://github.com/bageus/Dig/issues/390), [#574](https://github.com/bageus/Dig/issues/574).

Связанные authoritative specifications:

- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md);
- [`contextual-input-cursors-and-selection.md`](contextual-input-cursors-and-selection.md);
- [`presentation-input-ui-and-diagnostics.md`](presentation-input-ui-and-diagnostics.md);
- [`room-purposes-upgrades-and-tunnel-reinforcement.md`](room-purposes-upgrades-and-tunnel-reinforcement.md).

Этот документ фиксирует последнее подтверждённое пользователем observable behavior для correction scope и имеет приоритет над противоречащими старыми строками связанных specifications и issue acceptance.

## 1. Предметы и служебные interaction volumes

- обычный world item отображается только своей item geometry;
- interaction collider остаётся невидимым и не получает cylinder, circle, pedestal или другой service mesh;
- Jobs/debug overlay не является interaction target предмета;
- normal gameplay запускается со скрытым Jobs overlay; `F3` остаётся явным диагностическим toggle;
- включение или отключение overlay не меняет pickup, use, reservations или authoritative item state.

## 2. Выбранное строение

- completed building selection сохраняет model tint/outline, roster-row highlight и context panel;
- отдельный круг/цилиндр над моделью либо над каждой footprint-cell не отображается;
- удаление overhead marker не меняет selected BuildingId, input priority или panel synchronization.

## 3. Автоматическое усиление тоннеля

- automatic tunnel subsystem создаёт оба утверждённых вида ordinary jobs:
  - `WoodenSupport` для горизонтального тоннеля;
  - `JunctionStoneTrim` для стыка вертикального и горизонтального тоннелей;
- vertical/horizontal junction создаёт low-priority automatic `JunctionStoneTrim` job, резервирует 1 `material.stone`, после completion расходует камень и выдаёт Stonework `+0.7`;
- horizontal rolling chain автоматически создаёт `WoodenSupport` target через 10 horizontal cells от последнего structural anchor, резервирует 1 `material.mushroom_leg`, после completion расходует материал и выдаёт Woodworking `+0.7`;
- automatic range для обоих типов равен `30` cells по 3D Manhattan distance до ближайшей occupied cell любого completed building; расстояние измеряется до footprint boundary, а не до origin здания;
- target за пределами range не создаёт/не сохраняет active automatic job; после появления completed building в range synchronization может создать job;
- manual `U` placement остаётся дополнительным способом установить допустимую деревянную опору или каменную отделку и не отключает automatic jobs;
- completed wooden support становится structural anchor; completed junction stone trim остаётся decorative и не заменяет деревянную опору;
- automatic support/trim jobs участвуют в обычном assignment, retry, interruption, reservation и save/load lifecycle.

## 4. Невидимая Presentation автоматических jobs

- automatic `WoodenSupport` и `JunctionStoneTrim` jobs не создают world cylinder, point, circle, pedestal или отдельный selectable object;
- отсутствие маркера не отменяет job, material reservation, worker assignment, progress или completion;
- даже при ручном включении Jobs diagnostics tunnel-infrastructure jobs не создают world markers;
- job остаётся доступным в текстовой диагностике и job read model;
- completed wooden support и stone trim становятся частью tunnel geometry без отдельного collider, selection target или HUD entity.

## 5. Planning overlays комнат

Room marker и room-purpose overlay видны только в режиме планирования копки тоннелей. Выбор resident, building, BuildingBox или job и переход в placement mode скрывают planning visuals. Persistent физическая отделка комнаты остаётся видимой.

## 6. Разрушение усиленной геометрии

Когда excavation/topology overwrite превращает reinforced tunnel cell в room/cave volume либо копка вниз удаляет усиленный пол:

- completed wooden support или stone trim provenance удаляется вместе с прежней геометрией;
- ранее израсходованный `material.mushroom_leg` или `material.stone` уничтожается;
- world stack не создаётся;
- материал не переносится в inventory копающего resident-а, building stock или storage;
- повторное создание допустимого reinforcement требует новый материал и новый job;
- операция должна быть deterministic и idempotent: повторная synchronization не создаёт возврат материала и не удаляет дополнительный material unit.

## 7. Acceptance

- обычные world items не имеют видимого interaction cylinder;
- Jobs overlay скрыт при обычном запуске и включается только явно;
- выбранное completed building не имеет overhead footprint circles;
- tunnel-infrastructure job никогда не создаёт world marker;
- vertical/horizontal junction создаёт automatic `JunctionStoneTrim` job без точки/цилиндра;
- horizontal rolling chain создаёт automatic `WoodenSupport` jobs;
- оба automatic job kinds используют inclusive range `30` до completed-building footprint cell;
- automatic finalization расходует соответствующий material и выдаёт утверждённый skill exactly once;
- topology overwrite удаляет reinforcement и безвозвратно уничтожает consumed material;
- checked-in Unity Play Mode regression покрывает default overlay visibility и отсутствие tunnel-job marker, но runtime status повышается до `VERIFIED` только после фактического выполнения Unity Test Runner.

## 8. Журнал решений

- 2026-08-04 — пользователь запретил видимые служебные цилиндры над предметами, overhead circle выбранного здания и отдельные точки/маркеры tunnel reinforcement; completed reinforcement является частью тоннеля.
- 2026-08-04 — пользователь подтвердил, что материал разрушенного reinforcement уничтожается без возврата.
- 2026-08-04 — пользователь исправил ошибочную трактовку: и junction stone trim, и horizontal wooden support являются automatic jobs; для обоих automatic range равен 30 cells, но world point/cylinder не отображается.
