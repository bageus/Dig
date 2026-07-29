# Runtime stacking, barrel attack and cave-room recovery decisions

Статус: `APPROVED`.

Tracking issues: [#67](https://github.com/bageus/Dig/issues/67), [#87](https://github.com/bageus/Dig/issues/87), [#443](https://github.com/bageus/Dig/issues/443).

Этот документ фиксирует последние подтверждённые решения пользователя от 2026-07-29 и имеет приоритет над более ранними формулировками в связанных inventory, barrel и cave-room specifications при расхождении по перечисленным ниже пунктам.

## 1. Единичные предметы в resident inventory

- каждая физическая единица ordinary item/material в личном инвентаре resident является отдельным quantity-one stack и занимает отдельную ячейку;
- одинаковые `ItemId` в resident inventory не объединяются ни при pickup, ни при hauling/building-supply ingress, ни при deterministic layout normalization;
- occupied slot никогда не считается дополнительной capacity для такого же `ItemId`; каждая входящая единица требует отдельный свободный совместимый slot;
- Main/Cargo/Weapon destination priority сохраняется, но применяется только к свободным slots;
- reservations и held references относятся к конкретной unit stack и не меняют правило one unit per slot;
- агрегированные quantity stacks могут существовать во world/building storage projections, но при входе в resident inventory каждая переносимая единица получает отдельную unit stack/slot identity.

## 2. Прямая атака бочки

- при выбранном resident доступная бочка подсвечивается красным и показывает анимированный sword cursor;
- attack work position находится на той же высоте рядом с бочкой по `X` или `Z` и обязана иметь полную твёрдую support surface;
- весь route обязан проходить по клеткам с полной опорой;
- допустимы `SupportedWalk` и `DepthTraverse`, если обе стороны перехода поддержаны;
- `VerticalClimb`, `ShaftGapTraverse` и вертикальная соседняя work position для удара запрещены;
- hover и click используют один resolver: если sword cursor показан, тот же resident должен получить валидный attack job.

## 3. Eraser и повторное размещение незавершённой комнаты

- Eraser отменяет оставшиеся designations/jobs незавершённой комнаты, но не восстанавливает уже выкопанный terrain или completed quarter mask;
- exact preset/entrance/volume provenance сохраняется как paused room plan;
- повторное размещение того же preset у того же entrance возобновляет paused plan и создаёт Dig designations только для unfinished full/half-cell targets;
- completed targets не назначаются повторно и не создают повторный output;
- произвольная ранее открытая форма без paused или completed room provenance не считается комнатой и не может быть «доделана» через resume path;
- cancel/reapply обязан освобождать старые reservations/jobs и не оставлять duplicate active plans.

## 4. Medium-room marker и pointer anchoring

- room preview можно наводить на любую rock cell внутри фронтального силуэта, а не только на скрытую центральную колонку;
- runtime перебирает вертикальные уровни и horizontal anchors, row profile которых содержит pointer cell;
- medium preset `8 -> 7 -> 6` является even-width у основания и не имеет единственной центральной клетки, поэтому fixed `entrance.X = pointer.X` запрещён;
- первый валидный deterministic candidate рисует полный marker на породе; при отсутствии valid candidate отображаются per-cell invalid diagnostics лучшего кандидата.

## 5. Acceptance

- несколько одинаковых units в resident inventory занимают столько же отдельных ячеек, сколько существует физических единиц;
- одинаковые и разные материалы остаются отдельными quantity-one stacks;
- pickup/hauling/building-supply отклоняются до commit, если свободных совместимых slots меньше количества переносимых units;
- sword cursor и разрушение бочки работают через supported same-height route, включая supported depth transition;
- air/gap/climb route не создаёт cursor/job;
- partial room -> Eraser -> повторный marker того же preset/entrance -> designations только на остаток -> completion без повторной добычи;
- medium marker видим при наведении на любую клетку его фронтального `8 -> 7 -> 6` silhouette;
- unit/integration/source-contract и Unity Play Mode tests покрывают observable workflow; без фактического Unity Test Runner статус остаётся `IMPLEMENTED`, а не `VERIFIED`.
