# Производство в зданиях и внутреннее снабжение

Статус: `QUESTIONNAIRE`; closed-package categories и interaction подтверждены, но unfinished staged package всё ещё требует решений по cancel/load/pickup и blocked output.

Tracking issue: [#433](https://github.com/bageus/Dig/issues/433).

Связанные документы:

- [`campfire-cooking-and-food-use.md`](campfire-cooking-and-food-use.md);
- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md);
- [`resident-inventory-expansion.md`](resident-inventory-expansion.md).

## 1. Назначение

Completed workstation производит предметы и BuildingBox через data-driven recipes, автоматически пополняет защищённый внутренний запас и показывает spatial workflow в мире. Новые здания, stock rules и recipes добавляются content definitions, а не building-specific runtime branches.

## 2. Владение состоянием

- `ProductionContentCatalog` владеет immutable recipes и workstation definitions.
- `ProductionState` владеет очередями, active order, material-step progress, consumed-input ledger и manifest закрытой non-building output box.
- `InventoryState` владеет физическими item entities, quantity, reservations, `ItemLocation.InBuilding`, закрытыми package stacks и materialized world outputs.
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
- Все unfinished/closed package используют единый нейтральный package visual; категория определяется authoritative package kind, а не отличающейся геометрией.
- Каждый обработанный material step помещается в этот package и заполняет одно деление progress overlay.
- После последнего step worker закрывает package, terminal-ит production order/job и уменьшает counter на один.
- Если recipe производит здание, закрытый результат является обычной `BuildingBox` и полностью следует утверждённому lifecycle из `building-box-placement-and-packing.md`.
- Если recipe производит еду, закрытая коробка имеет package kind/name `food`; если оружие — `weapon`; остальные производимые предметы — `tool`.
- `food`, `weapon` и `tool` являются quantity-one world package entities с сохранённым manifest произведённых stack IDs, item IDs и quantities. Они не являются готовым содержимым и не подбираются обычным pickup.
- Hover по доступной `food`/`weapon`/`tool` при выбранном resident показывает слегка анимированный cursor использования; LMB создаёт один direct use/break command для той же package identity/version.
- Resident подходит к допустимой соседней work position, одним committed действием ломает коробку, удаляет её interaction target и exactly once материализует весь manifest в прежней world cell. Повторный/stale commit не создаёт duplicate contents.
- Выпавшие food/weapon/tool contents становятся обычными world item entities и далее используют свои существующие selection/pickup/use/equipment rules.
- Output cell должен быть explored, open, supported, вне footprint и без другого world item.
- Candidate order идёт только вправо: `right edge + 1`, затем `+2` и далее; side/rear fallback запрещён.
- Policy занятой output-zone для ещё не созданного package остаётся Q-PROD-024.
- `WorldItemViewModel.IsInteractive` является authoritative для interaction collider. Package collider включён для `Use`, но `CanPickup = false`; BuildingBox сохраняет собственный interaction contract.

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
8. После последнего step worker закрывает package. Close atomically создаёт BuildingBox либо закрытую `food`/`weapon`/`tool` package entity с manifest, terminal-ит order/job, выдаёт skill grants, освобождает reservations и уменьшает counter на один.
9. Для non-building output отдельный direct-use worker позже ломает закрытую коробку и materialize-ит manifest exactly once; это не является продолжением production order.
10. Тот же production worker остаётся owner всего manufacturing workflow; следующий order начинается только после terminal close/cancel/failure текущего.

## 7.1 Закрытые package categories и direct use

- `BuildingBox` не использует generic package-open command: selection, pickup, unpacking, relocation, assembly и packing остаются у существующей BuildingBox system.
- `food`, `weapon` и `tool` используют один generic output-package owner и различаются только stable package kind/name и содержимым manifest.
- Direct use требует selected resident, живую closed package entity, совпадающую version и reachable work position рядом с package cell.
- Hover highlight, animated use cursor и click обязаны разрешать одну identity/version; один pointer event создаёт не более одной command.
- До committed break cancel, route failure, worker removal или interruption не меняют package/manifest. После commit package terminal и не восстанавливается.
- Несколько stale commands не могут materialize-ить manifest повторно; первый successful commit побеждает.
- Package не блокирует Navigation movement, но остаётся world item occupancy для output/building placement policies согласно ordinary item rules.

## 7.2 Открытые решения unfinished staged package

- **Q-PROD-021:** можно ли поднять незакрытый package принудительно; если да, отменяется ли order или переносится вместе с progress.
- **Q-PROD-022:** cancel/failure после одного или нескольких processed steps уничтожает package, оставляет частично заполненный package либо возвращает только ещё не обработанные inputs.
- **Q-PROD-023:** save/load mid-step сохраняет package как отдельную inventory entity или восстанавливает его derived projection из order/material progress.
- **Q-PROD-024:** blocked/occupied finished-output zone не даёт стартовать worker либо package выбирает следующий right-side candidate.

Q-PROD-021..024 относятся только к незакрытому package между первым material step и close. Они не блокируют подтверждённый lifecycle уже закрытых `food`/`weapon`/`tool` коробок. До их решения код не должен молча придумывать observable поведение unfinished package.

## 8. Повтор, cancel, blocked и concurrency

- Repeat order занимает следующую свободную правую candidate cell.
- Cancel освобождает неиспользованные reservations; судьба уже processed steps/package остаётся Q-PROD-022.
- Blocked output не создаёт presentation-only package; start/retry policy остаётся Q-PROD-024.
- Каждый building владеет независимой queue/stock/active order.
- Два buildings не могут commit output в одну cell; occupancy перепроверяется в Finalize.

## 9. Save/load и diagnostics

Save включает workstation registration, delivery toggles/incoming, queue/status, material progress, consumed ledger, active job references и supply allocations. Закрытая `BuildingBox` сохраняется обычным BuildingBox contract. Закрытая `food`/`weapon`/`tool` package сохраняет package stack identity/location/version, kind, полный contents manifest и materialized marker; active direct-use job сохраняет worker/work position/stage. Способ сохранения незакрытого package остаётся Q-PROD-023. Derived zone geometry, hover/cursor phase и wait pose не сохраняются. Load не повторяет committed output, package materialization или skill grants.

Diagnostics показывают building/recipe/order/job IDs, stock current/incoming/capacity, material progress, assigned worker, output candidates/chosen cell, package stack/kind/version/manifest/materialized state, direct-use worker/stage, block reason и terminal completion result.

## 10. Acceptance

Domain/Application:

- protected internal stock не выбирается automatic supply;
- direct pickup забирает одну available unit;
- right candidates deterministic и без fallback;
- package close, BuildingBox либо closed-package commit и terminal order/job происходят атомарно;
- BuildingBox output использует только существующий BuildingBox lifecycle;
- food/weapon/tool close создаёт одну неподымаемую package entity с полным manifest;
- direct use materialize-ит весь manifest exactly once и удаляет package;
- duplicate production completion или stale break не создаёт второй package/output;
- save/load сохраняет package manifest, active use job и exactly-once semantics.

Unity Play Mode:

- слева и справа видны плоские trays без rear rail;
- internal units используют тот же art/hover/pickup cursor, несут exact `StackId`, видны/clickable и не блокируют navigation;
- enabled internal stock продолжает refill одновременно с production до capacity;
- product icon показывает segmented material progress;
- unfinished staged package workflow проверяется после решения Q-PROD-021..024;
- BuildingBox output использует обычный selection/unpack/pickup contract;
- food/weapon/tool package имеет enabled Use collider, animated use cursor и не показывает pickup affordance;
- resident ломает package, после чего содержимое видно/raycastable и использует ordinary item rules;
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
| 2026-08-01 | Closed building output следует BuildingBox rules; closed non-building boxes называются `food`, `weapon`, `tool`, показывают animated use cursor, ломаются resident direct-use action и exactly once выпускают содержимое. | User |
