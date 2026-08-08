# Exploration / fog of war — implementation

Tracking: #165. Authoritative design: `docs/design/exploration-fog-of-war.md`.

## Реализованный контур

- `ExplorationState` является владельцем persistent explored mask, current visible mask и last-known item markers.
- Детерминированный flood-fill использует шесть соседей и graph-distance; solid terrain, дополнительные blockers и закрытые двери останавливают распространение.
- Источники задаются snapshots: resident 10, building 10, damaged building 2, ladder/lift/door/trap 2, grave 5.
- Несколько origins моделируют границу footprint и всю шахту lift.
- `Visible` не сохраняется; save snapshot хранит explored history и markers, после load current mask пересчитывается.
- Unity demo больше не помечает мир разведанным при создании. Residents и действующие buildings публикуют sources при старте и simulation tick.
- Presentation получает `CellVisibility`, затемняет `ExploredNotVisible` и строит отдельную чёрную/полупрозрачную fog mesh для пустых клеток.

## Verification boundary

Unit coverage проверяет radius, стены/потолки, проход, closed door, source removal, damaged building, lift shaft, deterministic save/load history. Основной static quality gate проходит.

Статус не повышается до `VERIFIED`, пока не выполнены лицензированные Unity Play Mode-сценарии, визуальная accessibility-проверка и runtime performance capture dirty chunks.

## Открытая интеграционная граница

До `IMPLEMENTED` остаются: authoritative door lifecycle adapter, сохранение и визуализация remembered item markers в общем save-document, специализированные ladder/lift/trap/grave publishers вне demo building catalog и полноценное dirty-chunk обновление вместо пересборки fog mesh.
