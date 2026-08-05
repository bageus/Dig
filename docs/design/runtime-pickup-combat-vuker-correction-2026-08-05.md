# Runtime pickup, combat VFX и Vuker traversal correction

Статус: `IMPLEMENTED`.

Tracking issue: [#644](https://github.com/bageus/Dig/issues/644).

Связанные authoritative systems:

- [`item-interaction-capabilities.md`](item-interaction-capabilities.md);
- [`contextual-input-cursors-and-selection.md`](contextual-input-cursors-and-selection.md);
- [`enemy-combat-and-cave-encounters.md`](enemy-combat-and-cave-encounters.md);
- [`combat-spatial-execution.md`](combat-spatial-execution.md);
- [`presentation-input-ui-and-diagnostics.md`](presentation-input-ui-and-diagnostics.md).

При расхождении по четырём correction-пунктам ниже этот документ имеет
приоритет как последнее подтверждённое решение пользователя от 2026-08-05.

## 1. Назначение и границы

Correction устраняет четыре runtime-дефекта без изменения gameplay balance:

1. explicit pickup заменяет незавершённый manual move resident;
2. combat impact VFX появляется в фактической XYZ-точке сражения;
3. визуал `enemy.vuker` полностью помещается в горизонтальном тоннеле;
4. ecology принимает legal vertical/depth position Вукера без exception.

Inventory, Combat, Agent position, Navigation и Ecology остаются authoritative.
Presentation не меняет damage, reach, Health, collision или traversal policy.

## 2. Pickup после manual movement

### Success path

1. Resident имеет active manual tunnel route.
2. Пользователь выдаёт explicit pickup exact `StackId`.
3. Common direct-command preparation немедленно отменяет manual route с причиной
   `HigherPriorityAction` до reservation нового pickup job.
4. Старые jobs/meals/combat ownership очищаются существующим typed path.
5. Pickup job резервирует exact quantity и resident slot capacity.
6. Resident строит маршрут от текущей authoritative cell к source и подбирает item.
7. Completion освобождает reservations; item становится resident-owned.

Старый destination больше не выполняется после принятия pickup command.

### Repeat, cancel, failure и retry

- повторный pickup заменяет предыдущий pickup и освобождает его reservations;
- rejected/stale pickup не создаёт reservation;
- cancel/terminal failure освобождает stack reservation и slot claims;
- route retry использует текущую resident position, а не отменённый destination;
- предмет не может остаться permanently non-interactive без active owning job.

Несколько residents и stacks используют exact resident id/job id/stack id.
Save/load хранит только authoritative job/reservations; manual preview не сохраняется.

## 3. Combat impact world position

`CombatAttackResolved` проецируется по authoritative target actor position текущего
presentation refresh. Location map включает residents и combat-only enemy actors.

- X, Y и Z переводятся через общий `DigTunnelProjection`;
- pooled VFX request coordinates являются world coordinates;
- parent transform VFX pool не добавляет смещение;
- surface fallback для известного combat actor запрещён;
- missing/stale actor пропускает effect, но не создаёт effect в `(0,0,0)`.

Повторный event id остаётся idempotent. VFX не применяет damage и не завершает attack.

## 4. Cave-monster tunnel fit

Все visual variants семейства `CreatureVisualFamily.Vuker` получают bounded
presentation scale `0.68`. Масштаб применяется к authored/fallback rig, hostile marker
и anchors, чтобы silhouette не выходил за высоту одной горизонтальной tunnel cell.

Не изменяются:

- logical occupancy и navigation;
- melee range;
- collider gameplay semantics;
- Health, damage, patrol cadence и save data.

Child lifecycle scale умножается на тот же species scale.

## 5. Vuker ecology regions

Region connectivity вычисляется по всем legal open navigation cells, включая
`SupportedWalk`, `VerticalClimb` и `DepthTraverse`. Поэтому actor может временно
находиться в shaft/depth traversal cell и сохранять region identity.

Birth candidates остаются только `SupportedCells`. Unsupported vertical/depth cells:

- входят в actor-to-region lookup;
- не входят в `VukerCaveRegion.Cells` для рождения;
- не выбираются `FindNearestFreeCell`;
- не создают новый isolated region без supported anchor.

Topology rebuild атомарно обновляет navigation, region resolver, birth planner и combat
execution. Следующий ecology tick не выбрасывает exception для legal actor position.

## 6. Input, status и diagnostics

- pickup cursor/selection продолжают использовать exact item resolver;
- accepted pickup немедленно удаляет old movement feedback;
- reserved item показывает только активного owning pickup job;
- combat splash находится у target/engagement cell на текущем depth;
- diagnostics различают missing actor и invalid navigation cell без surface fallback.

## 7. Acceptance

- Play Mode: `manual move -> pickup -> old route absent -> exact item acquired`;
- cancellation/failure не оставляют orphan stack/slot reservations;
- combat VFX под translated parent остаётся в requested world XYZ;
- enemy actor position присутствует в combat effect location map;
- adult hostile Vuker visual bounds не превышают высоту tunnel cell;
- unit test: unsupported vertical cell resolves в тот же region, но не является birth cell;
- Play Mode: Vuker в reachable unsupported vertical cell проходит следующий ecology tick;
- build, full .NET suite, source contracts, smoke и deterministic soaks проходят;
- `VERIFIED` требует фактически выполненный licensed Unity Play Mode workflow.
