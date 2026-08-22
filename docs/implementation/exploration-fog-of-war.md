# Exploration / fog of war — implementation

Tracking: #165. Authoritative design: `docs/design/exploration-fog-of-war.md`.

## Реализованный контур

- `ExplorationState` является владельцем persistent explored mask, current visible mask и last-known item markers.
- Детерминированный flood-fill использует все 26 соседей `3x3x3` и graph-distance; solid terrain, дополнительные blockers и закрытые двери останавливают распространение только на target cell. Диагональный corner cutting разрешён без synthetic blocker между осями. Отдельный 26-связный boundary pass раскрывает только один слой окружающей породы, включая диагонали, и не пропускает обзор сквозь неё.
- Источники задаются snapshots: resident 4, building 5, damaged building 2, ladder/lift/door/trap 2, grave 5.
- Каждая занятая клетка building footprint является origin; lift также публикует всю шахту.
- `Visible` не сохраняется; save snapshot хранит explored history и markers, после load current mask пересчитывается.
- Unity demo больше не помечает мир разведанным при создании. Residents и действующие buildings публикуют sources при старте и simulation tick.
- Presentation получает `CellVisibility`, затемняет `ExploredNotVisible` и строит отдельную чёрную/полупрозрачную fog mesh для пустых клеток.

## Verification boundary

Unit coverage проверяет radius, стены/потолки, проход, closed door, source removal, damaged building, lift shaft, deterministic save/load history. Основной static quality gate проходит.

Статус не повышается до `VERIFIED`, пока не выполнены лицензированные Unity Play Mode-сценарии, визуальная accessibility-проверка и runtime performance capture dirty chunks.

## Открытая интеграционная граница

Для #740 отдельно остаются: authoritative door lifecycle adapter, сохранение и визуализация remembered item markers в общем save-document, специализированные ladder/lift/trap/grave publishers вне demo building catalog и полноценное dirty-chunk обновление вместо пересборки fog mesh.
