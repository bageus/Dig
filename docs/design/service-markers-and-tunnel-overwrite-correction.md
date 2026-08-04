# Служебные маркеры и изменение усиленной геометрии тоннеля

Статус: `QUESTIONNAIRE`.

Tracking issues: [#390](https://github.com/bageus/Dig/issues/390), [#574](https://github.com/bageus/Dig/issues/574).

Связанные authoritative specifications:

- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md);
- [`contextual-input-cursors-and-selection.md`](contextual-input-cursors-and-selection.md);
- [`presentation-input-ui-and-diagnostics.md`](presentation-input-ui-and-diagnostics.md);
- [`room-purposes-upgrades-and-tunnel-reinforcement.md`](room-purposes-upgrades-and-tunnel-reinforcement.md).

Этот документ фиксирует последнее подтверждённое пользователем observable behavior для перечисленного correction scope и имеет приоритет над противоречащими старыми строками связанных specifications.

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

## 3. Усиление тоннеля

- automatic tunnel subsystem создаёт только wooden-support jobs;
- vertical/horizontal junction intrinsically safe и не создаёт reinforcement point, selectable cylinder, automatic stone-trim target/job, material reservation или world job overlay;
- junction/floor stone trim запускается только через manual placement mode с exact stone stack resident-а;
- legacy non-terminal automatic junction-trim jobs считаются obsolete, отменяются synchronization и освобождают reservations до material/skill commit;
- automatic completion принимает только wooden support; legacy junction job не может расходовать stone или выдавать Stonework;
- completed wooden support и stone trim являются частью tunnel geometry и не получают отдельный collider, selection target, job cylinder или HUD entity;
- даже при ручном включении Jobs diagnostics tunnel-infrastructure jobs не создают world markers; их состояние остаётся доступным в текстовой диагностике.

## 4. Planning overlays комнат

Room marker и room-purpose overlay видны только в режиме планирования копки тоннелей. Выбор resident, building, BuildingBox или job и переход в placement mode скрывают planning visuals. Persistent физическая отделка комнаты остаётся видимой.

## 5. Открытое правило material recovery

**Q-TUNNEL-009 — topology overwrite recovery.** Когда completed wooden support или stone floor/junction trim удаляется из-за превращения клетки тоннеля в room/cave volume либо из-за excavation вниз через усиленный пол, куда возвращается уже израсходованный материал:

- ordinary world stack в exact cell;
- inventory resident-а, выполняющего excavation;
- либо другой authoritative destination?

До подтверждения нельзя молча выбрать destination или реализовать телепортацию материала. Текущая correction-ветка устраняет служебные маркеры и legacy automatic junction path, но не заявляет исправленной потерю материала при topology overwrite.

## 6. Acceptance

- обычные world items не имеют видимого interaction cylinder;
- Jobs overlay скрыт при обычном запуске и включается только явно;
- выбранное completed building не имеет overhead footprint circles;
- tunnel-infrastructure job никогда не создаёт world marker;
- junction не создаёт automatic stone-trim job или reservation;
- legacy junction job отменяется с освобождением reservation;
- automatic finalization не может расходовать stone для junction trim;
- checked-in Unity Play Mode regression покрывает default overlay visibility и отсутствие tunnel-job marker;
- material recovery при удалении completed reinforcement остаётся blocked только Q-TUNNEL-009.

## 7. Журнал решений

- 2026-08-04 — пользователь запретил видимые служебные цилиндры над предметами, overhead circle выбранного здания и отдельные точки/маркеры tunnel reinforcement; подтвердил, что completed reinforcement является частью тоннеля.
- 2026-08-04 — обнаружена потеря consumed material при topology overwrite; destination возврата не определён и вынесен в Q-TUNNEL-009.
