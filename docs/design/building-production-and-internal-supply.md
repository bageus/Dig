# Производство в зданиях и внутреннее снабжение

Статус: `APPROVED`; staged package lifecycle, closed package categories, cancel/interruption, save/load и right-side output policy подтверждены.

Tracking issue: [#433](https://github.com/bageus/Dig/issues/433).

Связанные документы:

- [`campfire-cooking-and-food-use.md`](campfire-cooking-and-food-use.md);
- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md);
- [`resident-inventory-expansion.md`](resident-inventory-expansion.md);
- [`building-box-placement-and-packing.md`](building-box-placement-and-packing.md).

## 1. Назначение

Completed workstation производит предметы и BuildingBox через data-driven recipes, автоматически пополняет защищённый внутренний запас и показывает spatial workflow в мире. Новые здания, stock rules и recipes добавляются content definitions, а не building-specific runtime branches.

## 2. Владение состоянием

- `ProductionContentCatalog` владеет immutable recipes и workstation definitions.
- `ProductionState` владеет очередями, active order, material-step progress, consumed-input ledger и package manifest/lifecycle.
- `InventoryState` владеет физическими item entities, quantity, reservations, `ItemLocation.InBuilding`, unfinished/closed package stacks и materialized world outputs.
- `BuildingSupplyState` владеет delivery toggles, incoming quantities и active supply request.
- `JobSystem` владеет production/supply/package-use lifecycle, worker claims и reservations.
- `BuildingsState` владеет footprint, orientation и work position.
- `Agents` владеет authoritative resident position и skills.
- `Presentation` только проецирует icons, counters, zones, items, hover и post-work pose.

## 3. Основной content

Campfire использует stable IDs `building.campfire`, `building_box.campfire`, `food.grilled_mushroom` и `food.roasted_hamster`. Внутренний запас содержит mushroom cap, mushroom leg, stone и hamster по data-driven capacity/toggle rules. Mushroom cap, mushroom leg и stone начинают с включённой доставкой. Hamster имеет capacity `2`, но его delivery toggle по умолчанию выключен: свободные стартовые животные не резервируются и не забираются supply-системой, пока игрок явно не включит hamster stock. Одна food recipe может производить несколько единиц в одном world stack; resident ingress затем применяет отдельное правило unit-per-slot из resident inventory specification.

## 4. UI и очередь

- LMB по product icon добавляет один order.
- RMB по тому же icon отменяет один order; при нуле это consumed no-op.
- Отдельная minus-кнопка запрещена.
- Queued order отменяется немедленно.
- Если RMB относится к уже active order, текущая производимая единица не уничтожается: worker завершает её, закрывает коробку и производит output обычным success path. Counter уменьшается только после normal close/completion.
- Counter равен числу non-terminal orders.
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

### 5.2 Готовая продукция и output справа

- После назначения production worker справа от workstation создаётся authoritative unfinished package entity для конкретного order; presentation-only placeholder запрещён.
- Output zone не имеет фиксированной длины. Resolver проверяет клетки вправо от footprint последовательно `right edge + 1`, `+2`, `+3` и далее до границы мира. Занятая клетка не блокирует запуск, а пропускается; side/left/rear fallback запрещён.
- Все unfinished/closed package используют единый нейтральный package visual; категория определяется authoritative package kind, а не отличающейся геометрией.
- Каждый обработанный material step помещается в package и заполняет одно деление progress overlay.
- После последнего step worker закрывает package, terminal-ит production order/job и уменьшает counter на один.
- Если recipe производит здание, закрытый результат является обычной `BuildingBox` и полностью следует `building-box-placement-and-packing.md`.
- Если recipe производит еду, закрытая коробка имеет package kind/name `food`; если оружие — `weapon`; остальные производимые предметы — `tool`.
- `food`, `weapon` и `tool` являются quantity-one world package entities с сохранённым manifest произведённых item IDs и quantities. Они не подбираются ordinary pickup.
- Hover по доступной `food`/`weapon`/`tool` при выбранном resident показывает слегка анимированный cursor использования; LMB создаёт один direct use/break command для той же package identity/version.
- Resident подходит к допустимой соседней work position, одним committed действием ломает коробку, удаляет её interaction target и exactly once материализует весь manifest в прежней world cell.
- Выпавшие contents становятся обычными world item entities и далее используют существующие selection/pickup/use/equipment rules.
- `WorldItemViewModel.IsInteractive` является authoritative для collider. Unfinished package имеет interaction collider disabled; closed non-building package имеет Use collider и `CanPickup = false`; BuildingBox сохраняет собственный contract.

## 6. Supply lifecycle

Demand создаётся для completed workstation с enabled delivery и недостающей capacity независимо от active production order. Stock rule с выключенным toggle не создаёт demand, source reservation или resident transit, пока он не требуется non-terminal production order. Каждый queued/active order принудительно включает delivery toggle для всех своих recipe inputs; это не меняет stock priority и не включает unrelated stock rules. Принудительно включённый toggle остаётся обычным видимым состоянием склада и автоматически не выключается после завершения или отмены order. Поэтому hamster delivery по умолчанию остаётся выключенной, но queued roasted-hamster order включает её как required production input. Planner читает revealed, navigation-connected и unreserved world stacks; клетка считается reachable только если она связана с work position строения актуальным navigation snapshot, а не просто является explored/open. Reservations текущего production order исключаются через `AvailableQuantity`, поэтому refill не крадёт используемые inputs. Одновременно на building существует не более одного active supply batch. Production worker сохраняет эксклюзивную reservation craft/work position, но supply batch не резервирует эту production-позицию: его concurrency ограничивают `BuildingSupplyState.ActiveSupplyJobId`, destination/building ownership и authoritative movement occupancy. Поэтому другой resident может выполнить `workstation check -> reserved sources -> workstation deposit` одновременно с active production. Пока toggle включён, система повторяет planning после каждого deposit/consumption/pickup до `current + incoming == capacity` либо отсутствия reachable candidates. Route/acquire failure не может оставить permanent blocker: source quantity, resident slot claims и incoming capacity освобождаются атомарно, failed batch terminal-ится, а следующий synchronization pass может создать новый batch с другим source/resident.

Если для enabled missing stock нет revealed/reachable/unreserved world source, но существует поддерживаемый revealed/reachable extraction/harvest target, один planning pass создаёт extraction/harvest job и dependent `BuildingSupply` job с requested item/quantity. Для campfire автоматическая добыча поддерживает mushroom cap и mushroom leg через один `Large` mushroom chop; planner выбирает одну недостающую единицу с наибольшим stock priority, а остальные drops остаются обычными world sources для следующих supply batches. Dependency planning работает независимо от queued recipe и не блокируется active production order.

Dependent supply остаётся `Created` и не получает worker/source/incoming reservations до успешного завершения dependency. После completion resolver перебирает всех доступных residents в deterministic distance/id order: отказ ближайшего resident из-за inventory capacity не блокирует следующего кандидата. Если completed dependency больше не имеет требуемого world output, dependent supply отменяется как stale без phantom incoming, после чего следующий synchronization pass может создать новую extraction/supply pair. Cancel/failure dependency также завершает dependent supply; повторная synchronization не создаёт duplicate pair. Эта dependency-модель является частью continuous refill до `current + incoming == capacity`.

## 7. Production lifecycle

1. Order может ожидать inputs в queue; icon зелёный при наличии полного input set. Пока order non-terminal, delivery toggle каждого recipe input принудительно включён без изменения stock priority.
2. Полный input set резервируется во внутреннем stock.
3. Один eligible resident получает `ProductionWorkJob`; segmented overlay получает по одному делению на material step.
4. Worker создаёт order-owned unfinished package в первой доступной right-side output cell.
5. Для каждого material step worker берёт одну конкретную единицу с внутреннего склада в resident inventory, подходит к workstation/campfire и выкладывает материал на derived virtual workbench.
6. После обработки material превращается в transient processed-step state; отдельный processed item в resident inventory не показывается.
7. Worker переносит processed step в package; input расходуется exactly once и соответствующее деление заполняется.
8. После последнего step worker закрывает package. Close atomically создаёт BuildingBox либо closed `food`/`weapon`/`tool` package с manifest, terminal-ит order/job, выдаёт skill grants, освобождает reservations и уменьшает counter на один.
9. Для non-building output отдельный direct-use worker позже ломает закрытую коробку и materialize-ит manifest exactly once; это не является продолжением production order.
10. Тот же production worker остаётся owner всего manufacturing workflow.

## 7.1 Cancel и forced movement

### Explicit cancel через product icon

- queued order отменяется и counter уменьшается;
- active order не обрывается: текущая единица производится до конца и output появляется;
- active package, material progress, reservations и worker сохраняются до normal close;
- после completion order/job terminal и counter уменьшается ровно один раз.

### Принудительное перемещение production worker

Если игрок отправляет занятого production worker в другое место:

1. production job отменяется;
2. unfinished package entity удаляется;
3. уже consumed/processed materials теряются без возврата;
4. ещё не использованные reservations освобождаются;
5. тот же order возвращается в `Queued` с нулевым material progress;
6. counter остаётся неизменным, потому что order не terminal;
7. повторный planning заново требует полный input set и создаёт новую package entity.

Forced movement после normal close не меняет уже созданный output.

## 7.2 Closed package direct use

- `BuildingBox` не использует generic package-open command.
- `food`, `weapon` и `tool` используют один generic output-package owner и различаются stable package kind/name и manifest.
- Direct use требует selected resident, живую closed package entity, совпадающую version и reachable work position.
- Hover highlight, animated use cursor и click обязаны разрешать одну identity/version; один pointer event создаёт не более одной command.
- До committed break cancel, route failure, worker removal или interruption не меняют package/manifest. После commit package terminal и не восстанавливается.
- Несколько stale commands не могут materialize-ить manifest повторно; первый successful commit побеждает.

## 8. Повтор, blocked и concurrency

- Repeat order занимает первую доступную клетку дальше вправо без фиксированного lateral limit.
- Occupied output cell пропускается; production блокируется только если до правой границы мира нет ни одной валидной клетки.
- Каждый building владеет независимой queue/stock/active order.
- Два buildings не могут commit package/output в одну cell; occupancy перепроверяется при package creation/close.
- Unfinished package нельзя поднять, переместить или использовать.

## 9. Save/load и diagnostics

Save включает workstation registration, delivery toggles/incoming, queue/status, material progress, consumed ledger, active job references и supply allocations.

- Unfinished package сохраняется как настоящая Inventory item entity со stable stack ID/location, owner order ID, lifecycle/version и текущим manifest/progress reference.
- Closed `food`/`weapon`/`tool` package сохраняет stack identity/location/version, kind, полный contents manifest и materialized marker.
- Active direct-use job сохраняет worker/work position/stage.
- `BuildingBox` сохраняется своим существующим contract.
- Load не возвращает consumed inputs, не повторяет package close/materialization и не дублирует skill grants.

Diagnostics показывают building/recipe/order/job IDs, stock current/incoming/capacity, material progress, assigned worker, output candidates/chosen cell, package stack/kind/version/manifest, direct-use worker/stage, interruption reason и terminal completion result.

## 10. Acceptance

Domain/Application:

- protected internal stock не выбирается automatic supply;
- hamster stock имеет capacity `2`, default delivery выключен, но queued/active roasted-hamster order принудительно включает его как required input;
- queued/active production order force-enables delivery only for its required inputs and never changes stock priority;
- ordinary supply considers actual navigation connectivity to the workstation, and a route/acquire failure releases all external reservations so later synchronization can replan;
- active production and one supply batch for the same building can be claimed by different residents concurrently; supply does not take the production work-position reservation;
- enabled missing campfire cap/leg без eligible world source создаёт не более одной mushroom-chop/deferred-supply pair независимо от queued recipe и active production;
- deferred extraction dependency перебирает resident candidates, не резервирует phantom incoming и отменяется, если completed dependency не оставила requested world output;
- direct pickup забирает одну available unit;
- right candidates deterministic, без фиксированного limit и без side fallback;
- occupied nearest cells выбирают следующую правую cell;
- unfinished package является inventory entity, не pickup/use target;
- active explicit cancel сохраняет workflow до normal output;
- forced move удаляет package, теряет consumed materials, освобождает unused reservations, reset-ит progress и оставляет counter/order non-terminal;
- package close, BuildingBox либо closed-package commit и terminal order/job происходят атомарно;
- food/weapon/tool direct use materialize-ит manifest exactly once;
- save/load сохраняет unfinished/closed package entity и active use job.

Unity Play Mode:

- fresh demo сохраняет двух hamster free/world-owned после initial production synchronization, без `R:1` и resident inventory transit;
- queued roasted-hamster order включает hamster delivery, но не меняет stock priority;
- disconnected explored/open source не резервируется, а failed supply route не блокирует следующий refill batch;
- internal units используют тот же art/hover/pickup cursor и exact `StackId`;
- enabled internal stock продолжает refill одновременно с production до capacity, причём active production worker и supply worker существуют одновременно и remote source требует полный outbound/return route;
- product icon показывает segmented material progress;
- unfinished package видна справа, не поднимается и сохраняется после save/load;
- explicit cancel active unit даёт finished output;
- forced move удаляет package, оставляет counter и заново ставит order в ожидание inputs;
- несколько занятых right-side cells сдвигают package дальше вправо;
- BuildingBox output использует обычный selection/unpack/pickup contract;
- food/weapon/tool package имеет animated use cursor, ломается и выпускает обычные world items exactly once.

## 11. Журнал решений

| Дата | Решение | Подтвердил |
|---|---|---|
| 2026-07-27 | Generic production, protected internal stock, progressive consumption и deferred replenishment. | User |
| 2026-07-29 | Left internal zone, right output zone, same-worker Finalize, pickup-capable output и camera-facing wait. | User |
| 2026-07-30 | Оба tray плоские без спинки; interaction collider следует authoritative read model. | User |
| 2026-07-31 | Internal stock identity/refill, flat-route priority, segmented progress и staged output package. | User |
| 2026-08-01 | Closed categories `food`/`weapon`/`tool` ломаются use-action и выпускают contents; BuildingBox сохраняет existing rules. | User |
| 2026-08-01 | Unfinished package не поднимается; explicit cancel завершает current unit; forced move уничтожает package/used materials, reset-ит order без изменения counter; package сохраняется как item entity; output search вправо не имеет фиксированного лимита. | User |
| 2026-08-01 | Enabled cap/leg refill создаёт harvest/deferred-supply pair без recipe/active-production gate; stale dependency освобождается, resolver перебирает всех residents. | User |
| 2026-08-01 | Hamster internal-stock delivery является opt-in: capacity/recipe сохраняются, но default toggle выключен, чтобы fresh free hamster не резервировались continuous-refill системой сразу после старта. | Пользовательский runtime bug report |
| 2026-08-01 | Любой non-terminal production order принудительно включает delivery toggle для своих recipe inputs без изменения stock priority; actual supply reachability определяется navigation connectivity, а blocked/failed batch не может навсегда блокировать склад. | Подтверждение пользователя в проектном чате |
| 2026-08-02 | Active production сохраняет exclusive craft-position reservation, а concurrent supply не резервирует ту же позицию; один supply batch ограничивается building-level ledger и movement occupancy. | Пользовательский runtime bug report |
