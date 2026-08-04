# Полная глубина тоннелей, визуал еды и ориентация палатки

Статус: `IMPLEMENTED`.

Tracking issue: [#626](https://github.com/bageus/Dig/issues/626).

Implementation evidence: [`../implementation/full-depth-tunnel-eating-and-tent-presentation-correction-2026-08-04.md`](../implementation/full-depth-tunnel-eating-and-tent-presentation-correction-2026-08-04.md).

Связанные authoritative specifications:

- [`world-3d-depth.md`](world-3d-depth.md);
- [`excavation-room-templates-and-deposits.md`](excavation-room-templates-and-deposits.md);
- [`campfire-cooking-and-food-use.md`](campfire-cooking-and-food-use.md);
- [`needs-continuous-actions.md`](needs-continuous-actions.md);
- [`representative-building-visual-dimensions.md`](representative-building-visual-dimensions.md).

Этот документ фиксирует последнее подтверждённое пользователем observable behavior и имеет приоритет для перечисленного correction scope.

## 1. Полноценные ячейки глубины

Логическая клетка на каждом `Z=0..3` визуально занимает один полный world-unit по оси глубины.

- центры соседних Z-слоёв разнесены ровно на `1.0` world unit;
- каждый solid slice имеет глубину `1.0` и соприкасается с соседним по общей boundary plane;
- Z0 не использует отдельную укороченную или расширенную толщину;
- Z1-Z3 не сжимаются относительно Z0;
- walk/support surfaces и interaction proxies используют тот же шаг глубины;
- открытая или выкопанная клетка не оставляет отдельную бирюзовую плитку, floor marker или поверхность логической сетки;
- скрытая grid остаётся только interaction/navigation contract и не рендерится как игровая геометрия.

Изменение относится к Presentation projection. Authoritative координаты, topology, movement adjacency и save data остаются `X,Y,Z`.

## 2. Некопаемая колонна

Если cell `Z0` в колонне `(X,Y)` содержит solid non-mineable material, все клетки той же колонки `Z1..Z3` также получают этот non-mineable material.

- правило применяется детерминированно после terrain material generation/overlay;
- mineable Z0 не переопределяет data-driven материалы глубинных слоёв;
- одна и та же колонна не может выглядеть проходимой в глубину за non-mineable фронтом;
- fingerprints, overlays и save/load сохраняют итоговые exact-Z материалы;
- правило не создаёт отдельный runtime source of truth вне `WorldState`.

## 3. Визуал приёма пищи

Пока authoritative action resident равен `Eat`:

- resident сидит на поверхности земли;
- в правой руке отображается committed meal portion;
- рука периодически подносит еду ко рту, показывая укусы;
- визуальный цикл использует authoritative `Eat` state и action/bite progress, но не применяет Nutrition самостоятельно;
- visual meal не создаёт `ItemStack` и не возвращает уже committed food в Inventory;
- completion, interruption, combat preemption, death или загрузка без active Eat немедленно убирают food visual и seated pose;
- после очистки meal visual восстанавливается фактически экипированный предмет resident.

## 4. Ориентация палатки

`building.tent` сохраняет visual bounds `3.0 × 2.0 × 2.0` и логический footprint из `BuildingDefinition`.

- вход палатки на прямом side-view направлен к основной камере;
- для текущей камеры, расположенной перед миром по положительному world-Z и смотрящей в сторону отрицательного Z, entrance facade находится на положительной Z-стороне модели;
- правило одинаково для completed, assembly и final-building placement ghost;
- BuildingBox/Packing остаются компактными коробками и не обязаны показывать вход;
- orientation не меняет authoritative occupancy, work positions или save data.

## 5. Acceptance

- в открытом тоннеле отсутствуют бирюзовые per-cell tiles;
- Z0-Z3 имеют одинаковую полную глубину и общие boundary planes;
- non-mineable Z0 распространяется на всю глубинную колонну;
- Eat показывает seated pose, food-in-hand и повторяющийся bite motion;
- meal visual очищается на всех terminal/interruption paths;
- палатка обращена входом к камере и остаётся в bounds `3×2×2`;
- source/unit/integration regressions фиксируют первопричины;
- лицензированный Unity Play Mode result требуется для статуса `VERIFIED`.

## 6. Открытые вопросы

Нет открытых business rules для текущего scope. Числовая полировка seated pose и food mesh допускается без изменения перечисленных observable invariants.

## 7. Журнал подтверждений

- 2026-08-04 — пользователь подтвердил полную глубину всех Z-слоёв, распространение non-mineable Z0 на глубину, seated food-in-hand bite presentation и разворот палатки входом к камере.
- 2026-08-04 — implementation и regression evidence добавлены в PR #627; без executed licensed Unity result статус не повышается до `VERIFIED`.
