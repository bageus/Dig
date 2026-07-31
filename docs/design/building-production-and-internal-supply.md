# Производство в зданиях и внутреннее снабжение

Статус: `QUESTIONNAIRE`; internal-stock identity, continuous refill и segmented progress подтверждены, staged output package требует решений по cancel/load/pickup.

Tracking issue: [#433](https://github.com/bageus/Dig/issues/433).

Связанные документы:

- [`campfire-cooking-and-food-use.md`](campfire-cooking-and-food-use.md);
- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md);
- [`resident-inventory-expansion.md`](resident-inventory-expansion.md).

## 1. Назначение

Completed workstation производит предметы и BuildingBox через data-driven recipes, автоматически пополняет защищённый внутренний запас и показывает spatial workflow в мире. Новые здания, stock rules и recipes добавляются content definitions, а не building-specific runtime branches.

## 2. Владение состоянием

- `ProductionContentCatalog` владеет immutable recipes и workstation definitions.
- `ProductionState` владеет очередями, active order, material-step progress и consumed-input ledger.
- `InventoryState` владеет физическими item entities, quantity, reservations, `ItemLocation.InBuilding` и world outputs.
- `BuildingSupplyState` владеет delivery toggles, incoming quantities и active supply request.
- `JobSystem` владеет production/supply lifecycle, worker claims и reservations.
- `BuildingsState` владеет footprint, orientation и work position.
- `Agents` владеет authoritative resident position и skills.
- `Presentation` только проецирует icons, counters, zones, items, hover и post-work pose.

## 3. Основной content

Campfire использует stable IDs `building.campfire`, `building_box.campfire`, `food.grilled_mushroom` и `food.roasted_hamster`. Внутренний запас содержит mushroom cap, mushroom leg, stone и hamster по data-driven capacity/toggle rules. Одна food recipe может производить несколько единиц в одном world stack; resident ingress затем применяет отдельное правило unit-per-slot из resident inventory specification.

## 4. UI и очередь

- LMB по product icon добавляет один order.
- RMB по тому же icon отменяет один order; при нуле это consumed no-op.
- Отдельная minus-кнопка запрещена.
- Counter равен числу non-terminal orders и уменьшается после `Completed`/`Cancelled`.
- Shortage tint не блокирует enqueue; icon зелёный, когда полный input set уже доступен во внутреннем stock.
- Для active order поверх product icon показывается segmented progress: одно деление на каждый material step, заполненное после обработки и помещения материала в output package.
- Internal-stock icon показывает current/incoming/capacity и delivery toggle.

## 5. Пространственные зоны

Каждое completed workstation показывает две зоны в world-X порядке независимо от orientation:

1. слева — внутренний input storage;
2. справа — finished output.

Обе зоны derived from footprint и не сохраняются как entities. Обе визуально являются только плоским tray/base. Rear rail, спинка или задняя стенка запрещены.

### 5.1 Внутренний склад

- Физическое состояние остаётся `ItemLocation.InBuilding(buildingId)`.
- Каждый видимый unit проецирует настоящий `StackId` и использует тот же item visual profile, art, reservation tint, hover и pickup cursor, что обычный world item; декоративный proxy без identity запрещён.
- Различие world/internal определяется только authoritative `ItemLocation` и принадлежностью зоне действия building, а не видом или отдельным interaction contract.
- Hit colliders являются triggers и не блокируют Navigation.
- Выбранный resident может обычным LMB/pickup cursor забрать одну конкретную available/unreserved единицу; click и hover разрешают один и тот же `StackId`.
- `ItemLocation.InBuilding` никогда не является источником automatic delivery/building supply.
- После ручного pickup delivery может создать replacement demand.

### 5.2 Готовая продукция

- После назначения production worker в finished-output zone создаётся authoritative output package/box для конкретного order; presentation-only placeholder запрещён.
- Box для food/building/item использует единый нейтральный package visual и не раскрывает категорию продукта внешним видом.
- Каждый обработанный material step помещается в этот package и заполняет одно деление progress overlay.
- После последнего step worker закрывает package; только закрытие создаёт готовый output item/BuildingBox и уменьшает counter на один.
- Output cell должен быть explored, open, supported, вне footprint и без другого world item.
- Candidate order идёт только вправо: `right edge + 1`, затем `+2` и далее; side/rear fallback запрещён.
- Policy занятой output-zone для ещё не созданного package остаётся Q-PROD-024; после закрытия готовый output использует обычный world-item selection/pickup/hauling workflow.
- `WorldItemViewModel.IsInteractive` является authoritative для interaction collider. Art/profile collider metadata не может отключить pickup-capable entity.

## 6. Supply lifecycle

Demand создаётся для completed workstation с enabled delivery и недостающей capacity независимо от active production order. Planner читает revealed, reachable, unreserved world stacks; reservations текущего production order исключаются через `AvailableQuantity`, поэтому refill не крадёт используемые inputs. Одновременно на building существует не более одного active supply batch. Worker проходит `workstation check -> reserved sources -> workstation deposit`. Пока toggle включён, система повторяет planning после каждого deposit/consumption/pickup до `current + incoming == capacity` либо отсутствия reachable candidates. Cancel/failure/retry освобождает source quantity, incoming capacity и claims атомарно.

## 7. Production lifecycle

Подтверждённый success path:

1. Order может ожидать inputs в queue; icon зелёный при наличии полного input set.
2. Полный input set резервируется во внутреннем stock.
3. Один eligible resident получает `ProductionWorkJob`; segmented overlay получает по одному делению на material step.
4. Worker создаёт order-owned package в finished-output zone.
5. Для каждого material step worker берёт одну конкретную единицу с внутреннего склада в resident inventory, подходит к workstation/campfire и выкладывает материал на derived virtual workbench.
6. После обработки material превращается в transient processed-step state; отдельный processed item в resident inventory не показывается.
7. Worker переносит processed step в package; input расходуется exactly once и соответствующее деление заполняется.
8. После последнего step worker закрывает package. Close atomically создаёт готовый output, terminal-ит order/job, выдаёт skill grants, освобождает reservations и уменьшает counter на один.
9. Тот же worker остаётся owner всего workflow; следующий order начинается только после terminal close/cancel/failure текущего.

## 7.1 Открытые решения staged package

- **Q-PROD-021:** можно ли поднять незакрытый package принудительно; если да, отменяется ли order или переносится вместе с progress.
- **Q-PROD-022:** cancel/failure после одного или нескольких processed steps уничтожает package, оставляет частично заполненный package либо возвращает только ещё не обработанные inputs.
- **Q-PROD-023:** save/load mid-step сохраняет package как отдельную inventory entity или восстанавливает его derived projection из order/material progress.
- **Q-PROD-024:** blocked/occupied finished-output zone не даёт стартовать worker либо package выбирает следующий right-side candidate.

До решения Q-PROD-021..024 код не должен молча придумывать observable поведение незакрытого package.

## 8. Повтор, cancel, blocked и concurrency

- Repeat order занимает следующую свободную правую candidate cell.
- Cancel освобождает неиспользованные reservations; судьба уже processed steps/package остаётся Q-PROD-022.
- Blocked output не создаёт presentation-only package; start/retry policy остаётся Q-PROD-024.
- Каждый building владеет независимой queue/stock/active order.
- Два buildings не могут commit output в одну cell; occupancy перепроверяется в Finalize.

## 9. Save/load и diagnostics

Save включает workstation registration, delivery toggles/incoming, queue/status, material progress, consumed ledger, active job references и supply allocations. Закрытые world outputs сохраняются обычным `ItemLocation`; способ сохранения незакрытого package остаётся Q-PROD-023. Derived zone geometry и wait pose не сохраняются. Load не повторяет committed output/skill grants.

Diagnostics показывают building/recipe/order/job IDs, stock current/incoming/capacity, material progress, assigned worker, output candidates/chosen cell, block reason и terminal completion result.

## 10. Acceptance

Domain/Application:

- protected internal stock не выбирается automatic supply;
- direct pickup забирает одну available unit;
- right candidates deterministic и без fallback;
- package close, готовый output commit и terminal order/job происходят атомарно;
- duplicate completion не создаёт второй output;
- save/load сохраняет exactly-once semantics.

Unity Play Mode:

- слева и справа видны плоские trays без rear rail;
- internal units используют тот же art/hover/pickup cursor, несут exact `StackId`, видны/clickable и не блокируют navigation;
- enabled internal stock продолжает refill одновременно с production до capacity;
- product icon показывает segmented material progress;
- staged package workflow проверяется после решения Q-PROD-021..024;
- output имеет enabled interaction collider и поднимается обычным pickup workflow;
- после output job/order terminal, counter уменьшается, worker ждёт лицом к камере;
- repeat/blocked/save-load не создают visual-only или duplicate products.

## 11. Журнал решений

| Дата | Решение | Подтвердил |
|---|---|---|
| 2026-07-27 | Generic production, protected internal stock, progressive consumption и deferred replenishment. | User |
| 2026-07-29 | Left internal zone, right output zone, same-worker Finalize, pickup-capable output и camera-facing wait. | User |
| 2026-07-29 | Work position находится сбоку на той же supported Y/Z plane, не над building. | User |
| 2026-07-30 | Оба tray плоские без спинки; interaction collider следует authoritative read model. | User |
| 2026-07-31 | Internal stock units идентичны world items по art/hover/pickup и несут exact StackId; enabled refill работает до capacity параллельно production; flat routes предпочтительнее climbing; product icon показывает material segments; production использует staged output package. | User |
