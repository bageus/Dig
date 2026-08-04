# Встроенный рельеф раскрытых залежей

Статус: `IMPLEMENTED`.

Tracking issue: [#617](https://github.com/bageus/Dig/issues/617).

Связанные authoritative specifications:

- [`excavation-room-templates-and-deposits.md`](excavation-room-templates-and-deposits.md);
- [`../implementation/unity-visual-asset-pipeline.md`](../implementation/unity-visual-asset-pipeline.md).

Implementation evidence: [`../implementation/embedded-terrain-deposit-relief-2026-08-04.md`](../implementation/embedded-terrain-deposit-relief-2026-08-04.md).

## 1. Назначение

Документ уточняет observable presentation раскрытых ресурсных залежей. Он не меняет World-owned deposit state, generation, mining, output, navigation, interaction или persistence.

## 2. Подтверждённое визуальное правило

Раскрытая залежь должна быть видна и различима, но выглядеть частью породы.

- deposit silhouette встроен в открытую грань host rock;
- основание и большая часть объёма находятся внутри стены;
- за плоскость стены выходит только небольшой низкий рельеф;
- залежь не выглядит отдельным кружком, значком, пластиной или предметом, положенным поверх стены;
- правило одинаково применяется к wall, floor, ceiling и depth-facing surfaces;
- Iron/Nodule, Gold/Plate, Crystal/Crystal, Coal/Seam и Stone/Pebble сохраняют разные формы;
- различимость не зависит только от цвета;
- соседние same-id deposits могут иметь connector relief, но connector также остаётся встроенным и низким;
- hidden и depleted deposits не имеют видимой геометрии.

## 3. Геометрические границы

Presentation mesh обязан соблюдать следующие инварианты:

- базовая точка cluster смещена внутрь host-rock plane;
- максимальная точка silhouette может выйти наружу только на малую bounded величину;
- relief не перекрывает существенную часть свободного тоннеля и не читается как отдельный объект;
- connector strips располагаются не выше основного разрешённого relief;
- damage может уменьшать relief/scale, но не выдвигать залежь наружу;
- LOD меняет количество деталей, но не нарушает embedded placement.

Точные числовые размеры являются presentation constants и могут корректироваться без изменения gameplay, пока сохраняются перечисленные observable invariants.

## 4. Владение и ограничения

- `WorldState` остаётся authoritative owner deposit lifecycle;
- Presentation получает immutable deposit decoration snapshot;
- Unity mesh projection не создаёт `GameObject`, collider или picking target для залежи;
- geometry не используется для mining target resolution, pathfinding или support;
- save/load не сохраняет relief vertices, offsets или LOD;
- rebuild должен детерминированно восстанавливать тот же embedded relief из snapshot.

## 5. Workflow

1. Hidden deposit не отображается.
2. Reveal публикует stable deposit id и decoration layout.
3. Terrain chunk rebuild добавляет embedded low-relief silhouette на каждую открытую host face.
4. Damage уменьшает визуальный объём в рамках стены.
5. Depletion удаляет deposit relief и authoritative terrain mutation открывает клетку обычным excavation workflow.
6. Save/load восстанавливает reveal/depletion, после чего Presentation пересобирает relief без отдельного сохранённого mesh state.

## 6. Acceptance

- раскрытая залежь читается как включение в породе, а не отдельный marker;
- silhouette base находится внутри rock plane;
- наружный relief мал и bounded для всех пяти shape families;
- wall/floor/ceiling/depth surfaces используют один contract;
- connectors остаются flush/embedded;
- hidden/depleted visuals отсутствуют;
- topology, navigation, mining, picking, output и save/load не изменяются;
- source contract фиксирует embedded offsets и запрещает прежний raised-marker profile;
- Unity Play Mode regression проверяет crossing wall plane только в разрешённом диапазоне;
- licensed executed Unity evidence требуется для статуса `VERIFIED`.

## 7. Открытые вопросы

Нет открытых business rules для текущего scope. Числовая полировка глубины inset/relief является presentation tuning при сохранении этого контракта.

## 8. Журнал подтверждений

- 2026-08-04 — пользователь подтвердил: залежи остаются видимыми, находятся в стене и только немного выступают; отдельные явно наложенные surface markers запрещены.
- 2026-08-04 — implementation и source/test evidence добавлены в PR #618; без executed licensed Unity result статус не повышается до `VERIFIED`.
