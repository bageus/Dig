# Визуальные габариты ранних строений

Статус: `APPROVED`.

Tracking issue: [#620](https://github.com/bageus/Dig/issues/620).  
Parent systems: [`building-box-placement-and-packing.md`](building-box-placement-and-packing.md), [`content/buildings.md`](content/buildings.md), [`presentation-input-ui-and-diagnostics.md`](presentation-input-ui-and-diagnostics.md).  
Implementation pipeline: [`../implementation/unity-building-representative-pack.md`](../implementation/unity-building-representative-pack.md).

## 1. Назначение

Документ задаёт точные визуальные размеры и обязательный силуэт трёх ранних строений. Эти данные принадлежат Presentation и не заменяют authoritative `BuildingDefinition.Footprint`.

Одна Unity world unit по X/Y/Z соответствует одной логической клетке. Размер записывается как:

```text
ширина X × высота Y × глубина Z
```

Измерение выполняется для completed model в authored North orientation по объединённым renderer bounds.

## 2. Подтверждённые размеры

| Stable building id | Название | Visual bounds |
|---|---|---|
| `building.tent` | Палатка | `3.0 × 2.0 × 2.0` |
| `building.stone_mason` | Мастерская каменщика | `3.5 × 2.5 × 2.5` |
| `building.wood_workshop` | Деревянная мастерская | `2.5 × 2.0 × 2.0` |

Допуск автоматической проверки каждого измерения: `±0.02` world unit.

Нижняя граница completed visual находится на floor plane `Y = 0`. Pivot и rotation не могут поднимать модель над опорой либо погружать её ниже пола.

## 3. Силуэты

### Палатка

Палатка не может использовать generic box/furnace/storage silhouette. Она должна иметь:

- двускатный наклонный тент;
- различимый вход или входной клапан на передней стороне;
- низкое основание/настил;
- читаемый North-facing фасад.

### Мастерская каменщика

Мастерская каменщика должна визуально отличаться от деревянной мастерской и generic furnace:

- тяжёлое каменное основание или корпус;
- рабочая поверхность/каменные блоки;
- крыша либо верхняя конструкция в пределах заданных bounds.

### Деревянная мастерская

Деревянная мастерская должна иметь отдельный деревообрабатывающий силуэт:

- деревянный каркас;
- рабочий стол/бревно или пильный prop;
- двускатная либо навесная крыша;
- отсутствие каменного тяжёлого корпуса мастерской каменщика.

## 4. Authoritative ownership

- `BuildingDefinition.Footprint` остаётся единственным владельцем logical occupancy, support validation, placement conflicts и Navigation blocking.
- `VisualBoundsCenter` и `VisualBoundsSize` принадлежат building visual profile.
- Presentation profile не может расширять либо уменьшать logical footprint.
- Selection collider охватывает visual bounds, но не участвует в gameplay occupancy.
- Finished renderer, BuildingBox Z1–Z3 placement ghost и planned-building ghost используют одну completed geometry и одинаковые bounds.
- Z0 BuildingBox остаётся компактной коробкой и не масштабируется до размеров распакованного здания.
- Stable ids и simulation state не меняются; save/load migration не требуется.

## 5. Lifecycle

### Success path

1. Building visual resolver получает stable `BuildingDefinitionId`.
2. Catalog возвращает отдельный representative profile без generic fallback.
3. Completed/placement ghost строятся из одной profile geometry.
4. Selection collider создаётся из `VisualBoundsCenter/Size`.
5. Orientation поворачивает модель вокруг утверждённого pivot без изменения размера по высоте.

### Assembly, damaged и packing

- Assembly использует тот же основной силуэт и существующий progress scaling/scaffold contract.
- Damaged сохраняет узнаваемость здания; существующая damaged transform может временно уменьшать фактические bounds.
- Packing использует компактную BuildingBox geometry.
- Cancel/failure placement не создаёт visual instance вне существующего ghost lifecycle.

### Повторное использование

Повторный resolve одного stable id/state детерминированно возвращает тот же asset key, geometry, bounds, anchors и budget. Representative templates кэшируются существующей library и не создают новый источник истины.

## 6. Diagnostics и budgets

Validation отклоняет profile, если:

- visual bounds отсутствуют, не положительны или не опираются нижней гранью на `Y = 0`;
- stable id дублируется;
- required silhouette parts отсутствуют;
- renderer/triangle budget превышен;
- selection collider не совпадает с declared visual bounds;
- declared logical footprint не совпадает с `BuildingDefinition` при runtime projection.

Профили сохраняют текущие hard limits: не более `16` renderer parts и `512` triangles на representative template.

## 7. Acceptance

- `building.tent`, `building.stone_mason`, `building.wood_workshop` имеют отдельные data-driven profiles;
- completed renderer bounds равны размерам из раздела 2 с допуском `±0.02`;
- нижняя грань всех трёх моделей находится на `Y = 0`;
- палатка имеет две наклонные половины тента и различимый вход;
- каменная и деревянная мастерские различимы по geometry/part names и props;
- selection collider равен declared visual bounds;
- все три profile сохраняют authoritative `1×1` footprint текущих `BuildingDefinition`; изменение footprint является отдельным gameplay-решением;
- final-building placement ghost использует тот же completed profile;
- JSON/source-contract проверки и checked-in Unity Play Mode scenario покрывают размеры, grounding, silhouette, collider и logical-footprint separation;
- фактический licensed Unity Test Runner обязателен для статуса `VERIFIED`.
