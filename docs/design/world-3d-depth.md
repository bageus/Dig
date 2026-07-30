# Полноценный 3D-мир с ограниченной глубиной

## Статус решения

- **Design:** `APPROVED`.
- **Implementation:** `IMPLEMENTED` после merge closure PR для #88.
- **Runtime verification:** лицензированный Unity Play Mode result всё ещё требуется для `VERIFIED`.
- **Дата решения:** 2026-07-15.
- **Последняя синхронизация:** 2026-07-30.
- **Tracking:** [#88](https://github.com/bageus/Dig/issues/88).
- **Связанные подтверждённые системы:** [#89](https://github.com/bageus/Dig/issues/89), [#90](https://github.com/bageus/Dig/issues/90).

Этот документ фиксирует единственный authoritative coordinate contract. Он имеет приоритет над прежними 2D/2.5D формулировками и compatibility overloads, которые могут только явно переводить legacy-данные в `Z=0`, но не создавать отдельный production state owner.

## Основное правило

Игровой мир является полноценной трёхмерной логической сеткой.

- каждая клетка имеет `CellId(X,Y,Z)`;
- допустима ровно глубина `Z=0..3`;
- одинаковые `X,Y` с разным `Z` обозначают разные клетки;
- жители, предметы, здания, functional places, jobs, reservations, deposits и routes сохраняют exact Z;
- камера преимущественно боковая, но не определяет топологию;
- Unity `Transform`, mesh, collider, overlay и route line являются только rebuildable Presentation.

```text
Z = 0 — передний слой
Z = 1 — второй слой
Z = 2 — третий слой
Z = 3 — задний слой
```

Координаты вне этого диапазона недопустимы для authoritative world entities. Относительные offset values могут быть отрицательными только внутри алгоритма до проверки итоговой клетки через `WorldSize.Contains`.

## Свободная копка и шаблонные комнаты

Игрок может назначать копание по всем трём осям в пределах мира. Число `4` означает максимальную глубину мира, а не обязательную толщину каждого тоннеля.

Подтверждённые глубины cave templates:

- малая — 3 клетки;
- средняя — 3 клетки;
- большая — 4 клетки;
- высокая — 4 клетки.

#89/#90 подтверждены пользователем 2026-07-30. Экструзия, centered rows, half-cell required-quarter masks, left/right entrances, отсутствие mirror и lifecycle template instance определяются [`excavation-room-templates-and-deposits.md`](excavation-room-templates-and-deposits.md), а не этим coordinate contract.

## Владение координатами

Exact `CellId` используют:

- `WorldState`, cells, chunks, snapshots и invalidation;
- agent position и occupancy;
- `ItemLocation` и world stacks;
- building origin, footprint, volume, work/visitor/storage places;
- digging, production, hauling и packing targets;
- Position/Designation reservations;
- Navigation nodes, route cells, stale-route validation и traffic coordination;
- deposits и excavation template provenance;
- deterministic generation, overlays и state fingerprint;
- Save/Load, migration и replay.

Ни одна Presentation-система не может напрямую изменить логическую позицию. Изменение положения проходит через authoritative command/state owner, после чего Unity projection перестраивается из immutable snapshot/read model.

## Генерация и deterministic identity

Generated world materializes `Width * Height * 4` cell states. Базовая cavern layout может оставлять authored rooms/corridors на `Z=0`, но каждый глубокий cell существует как полноценная solid cell и доступен для последующей копки.

Generation fingerprint включает:

- world seed, generator/profile version;
- width, height, depth и chunk size;
- exact XYZ order каждой клетки;
- material, designation, exploration, damage, temperature;
- completed excavation quarters, cut pattern и source material provenance.

Generation overlay сравнивает и восстанавливает все четыре Z-слоя. Изменение deep cell обязано менять fingerprint и попадать в overlay.

## Навигация и перемещение

Сетка имеет шесть ортогональных соседей. Само наличие соседней клетки не гарантирует traversal: profile дополнительно проверяет поддержку, проход, gap/shaft/climbing rules, occupancy, building volume, опасность и accessibility.

- opposite XY swap и opposite depth traversal являются разными случаями;
- traffic anti-swap rule применяется только к переходу по X при одинаковых Y и Z;
- route read model и renderer сохраняют exact route-cell Z;
- stale-route validation сравнивает world/navigation versions, включая deep chunks.

## Чанки и invalidation

`ChunkId(X,Y,Z)` обозначает один chunk-layer. `ChunkLayout` и world snapshot перечисляют chunk layers в стабильном XYZ порядке.

Изменение клетки инвалидирует owning chunk-layer и только соседние boundaries, которые участвуют в mesh/navigation sampling. Overlay diagnostics и chunk-version caches используют полный `(X,Y,Z)` key; слои с одинаковыми X/Y не перезаписывают друг друга.

## Здания, предметы и overlays

- building origin, work position и каждый footprint cell находятся на одном подтверждённом depth layer либо в явно заданном volume;
- packing/production execution требует exact target cell, а не только совпадение X/Y;
- предмет и stockpile projection используют authoritative `Cell.Z`;
- navigation lines, world overlays, selection, fog, deposits, storage и diagnostics преобразуют Z через общий `DigTunnelProjection`;
- read-model compatibility overload без Z допустим только для legacy fixture, но production producer обязан передавать Z явно.

## Save/Load и миграция 2D → XYZ

Save v5 является границей authoritative XYZ. Миграция v4→v5 детерминированно устанавливает `Z=0` для legacy:

1. world chunks и cells;
2. agent positions;
3. world item locations;
4. building origins и work positions;
5. terrain deposits;
6. job coordinate property groups `*.x/*.y` через добавление `*.z=0`;
7. Position/Designation reservation keys `x,y` через нормализацию в `x,y,0`.

Property/reservation output сортируется и форматируется invariant-culture. Миграция не распределяет объекты по глубине случайно, не меняет stable IDs и не создаёт второй coordinate format. После load Navigation и Presentation пересчитываются из restored owners.

## Acceptance

- существует одна authoritative XYZ-модель;
- world depth всегда равна четырём, valid Z — `0..3`;
- same X/Y different Z различаются в cells, chunks, reservations, navigation, hashes и saves;
- generated world materializes все depth cells;
- deep mutation меняет fingerprint, overlay и нужный chunk-layer;
- agents/items/buildings/jobs/deposits round-trip exact Z;
- legacy v4 coordinates мигрируют в explicit `Z=0`;
- packing/traffic/runtime read-model producers не отбрасывают Z;
- route, stockpile, fog, selection и diagnostics projection визуально различают глубину;
- source/unit/integration/property/migration tests проходят;
- checked-in Unity Play Mode regression проверяет same-XY/different-Z route projection;
- статус повышается до `VERIFIED` только после фактического licensed Unity Test Runner result.

## Открытые вопросы

В scope #88 открытых business rules нет. Balance deposits/terrain и mixed-skill policy остаются в #87/Q-014/Q-019 и не меняют coordinate contract.

## Журнал решений

| Дата | Решение | Источник |
|---|---|---|
| 2026-07-15 | Мир использует `X,Y,Z`, глубина ровно `0..3`, legacy `X,Y` мигрирует в `Z=0`. | #88, Q-001/Q-002 |
| 2026-07-30 | #89/#90 подтверждены; Small depth исправлена на 3, geometry принадлежит excavation specification. | пользователь |
| 2026-07-30 | Generated buffer/hash/overlay, v4→v5 migration и Unity runtime projections обязаны сохранять exact Z; compatibility 2D overload не является production owner. | closure implementation #88 |
