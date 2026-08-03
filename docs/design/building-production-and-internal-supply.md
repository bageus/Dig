# Производство в зданиях и внутреннее снабжение

Статус: `APPROVED`; staged package lifecycle, closed package categories, cancel/interruption, save/load и right-side output policy подтверждены.

Tracking issues: [#433](https://github.com/bageus/Dig/issues/433), [#609](https://github.com/bageus/Dig/issues/609).

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

Campfire использует stable IDs `building.campfire`, `building_box.campfire`, `food.grilled_mushroom` и `food.roasted_hamster`. Внутренний запас содержит mushroom cap, mushroom leg, stone и hamster по data-driven capacity/toggle rules. Mushroom cap, mushroom leg и stone начинают с включённой доставкой. Hamster имеет capacity `2`, но его delivery toggle по умолчанию выключен: свободные стартовые животные не резервируются и не забираются supply-системой, пока игрок явно не включит hamster stock. Одна food recipe может записывать несколько единиц в одном package manifest entry; при разрушении закрытой food-коробки каждая единица материализуется отдельным quantity-one world stack. Resident ingress затем применяет отдельное правило unit-per-slot из resident inventory specification.

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
- Для package kind `food` quantity каждого manifest entry раскрывается в отдельные world entities: `food.grilled_mushroom x2` создаёт два distinct stack ID с quantity `1`, а не один stack quantity `2`.
- `weapon` и `tool` сохраняют прежнее правило: один manifest entry создаёт один world stack с quantity entry.
- Выпавшие contents становятся обычными world item entities и далее используют существующие selection/pickup/use/equipment rules.
- `WorldItemViewModel.IsInteractive` является authoritative для collider. Unfinished package имеет interaction collider disabled; closed non-building package имеет Use collider и `CanPickup = false`; BuildingBox сохраняет собственный contract.

## 6. Supply lifecycle

Demand создаётся для completed workstation с enabled delivery и недостающей capacity. Stock rule с выключенным toggle не создаёт demand, source reservation или resident transit, пока он не требуется non-terminal production order. Каждый queued/active order принудительно включает delivery toggle для всех своих recipe inputs; это не меняет stock priority и не включает unrelated stock rules. Принудительно включённый toggle остаётся обычным видимым состоянием склада и автоматически не выключается после завершения или отмены order. Поэтому hamster delivery по умолчанию остаётся выключенной, но queued roasted-hamster order включает её как required production input. Planner читает revealed, navigation-connected и unreserved world stacks; клетка считается reachable только если она связана с work position строения актуальным navigation snapshot, а не просто является explored/open. Reservations текущего production order исключаются через `AvailableQuantity`, поэтому refill не крадёт используемые inputs.

Один completed production building имеет единственного active operation owner. `ProductionWorkJob` и `BuildingSupplyJob` одного building используют общую building-level reservation и не могут быть claimed или active одновременно. Production сохраняет её до закрытия output package и возвращения worker к `WorkPosition`; supply сохраняет её до committed deposit/cancel.

При наличии non-terminal production queue Supply ориентируется только на item types следующего recipe. Для каждого required stock low-water threshold равен `ceil(capacity / 2)`; значение ровно на половине ещё допускает производство. Пока следующий order runnable и все его required stocks не ниже threshold, разрешены последовательные Production units без промежуточной доставки. После close+return каждого unit turn становится `Supply`, но batch создаётся только если следующий order не runnable либо хотя бы один required stock ниже threshold. Один batch забирает все доступные сейчас revealed/reachable/unreserved world units целевых типов, которые помещаются resident-у; отсутствующие типы не заставляют batch ждать. После committed batch turn становится `Production`, поэтому следующий unit запускается при полном input set даже если частичная доставка не вернула все stocks к threshold/capacity. Для campfire cap capacity `4` последовательность `4 -> 3 -> 2 -> 1` означает три производства подряд и refill только перед четвёртым.

Если требуемого world source нет, существующий supported extraction/harvest planner создаёт dependency. Пока текущий `Supply` turn ещё не дал committed delivery, такая source-unresolved dependency удерживает следующий production unit в ожидании, но не резервирует building operation. После любого partial supply commit turn становится `Production`: следующий runnable unit может стартовать, а оставшиеся dependencies продолжают добычу и ждут свободного building operation для доставки. Если direct source отсутствует и supported dependency создать невозможно, runnable production не блокируется навсегда. Unsupported material остаётся shortage без synthetic extraction. Без production queue enabled refill по-прежнему планирует все включённые stock rules до `current + incoming == capacity` по мере появления свободных материалов. Недоступный source или failed batch освобождает source quantity, resident slot claims и incoming capacity атомарно. Полный cross-system contract: [`runtime-needs-supply-sleep-food-recovery.md`](runtime-needs-supply-sleep-food-recovery.md).

Если для enabled missing stock нет revealed/reachable/unreserved world source, но существует поддерживаемый revealed/reachable extraction/harvest target, один planning pass создаёт extraction/harvest job и dependent `BuildingSupply` job с requested item/quantity. Для campfire автоматическая добыча поддерживает mushroom cap и mushroom leg через один `Large` mushroom chop; planner выбирает одну недостающую единицу с наибольшим stock priority, а остальные drops остаются обычными world sources для следующих supply batches. Dependency planning работает независимо от queued recipe и не блокируется active production order.

Dependent supply остаётся `Created` и не получает worker/source/incoming reservations до успешного завершения dependency. После completion resolver перебирает всех доступных residents в deterministic distance/id order: отказ ближайшего resident из-за inventory capacity не блокирует следующего кандидата. Если completed dependency больше не имеет требуемого world output, dependent supply отменяется как stale без phantom incoming, после чего следующий synchronization pass может создать новую extraction/supply pair. Cancel/failure dependency также завершает dependent supply; повторная synchronization не создаёт duplicate pair. Эта dependency-модель является частью continuous refill до `current + incoming == capacity`.

## 7. Production lifecycle

1. Order может ожидать inputs в queue; icon зелёный при наличии полного input set. Пока order non-terminal, delivery toggle каждого recipe input принудительно включён без изменения stock priority.
2. Полный input set резервируется во внутреннем stock.
3. Один eligible resident получает `ProductionWorkJob`; segmented overlay получает по одному делению на material step.
4. Worker создаёт order-owned unfinished package в первой доступной right-side output cell.
5. Для каждого material step worker берёт одну конкретную order-reserved единицу с внутреннего склада в resident inventory и возвращается к workstation/campfire.
6. При active production перед building `WorkPosition` показывается небольшая derived колода/workbench. Это presentation-only visual без entity, collider, reservation и save state; после terminal/cancel production она исчезает.
7. По прибытии worker выкладывает raw material на колоду. В этот commit raw unit exactly once удаляется из resident inventory; reservation переносится в authoritative `Staged` phase order-а, поэтому raw icon больше не остаётся в слоте во время обработки.
8. Worker обрабатывает только staged material у workstation. В demo/test composition processing временно занимает один simulation tick. Завершение переводит step в transient `ProcessedAwaitingPackage`; отдельный authoritative processed item в Inventory/world не создаётся, но Presentation показывает у worker derived carry visual этого material во время маршрута к package.
9. Worker обязан подойти к order-owned unfinished package и committed deposit-ом перевести step в `Deposited`. Только этот deposit заполняет соответствующее деление segmented progress. До deposit следующий material step не начинается.
10. Если остаются material steps, тот же worker возвращается к internal stock и повторяет `pickup -> log/workbench stage -> one-tick processing -> processed carry -> package deposit` для следующей единицы.
11. После deposit последнего step worker закрывает package в той же output position. Close atomically создаёт BuildingBox либо closed `food`/`weapon`/`tool` package с manifest, terminal-ит order, выдаёт skill grants, освобождает input/package reservations и уменьшает counter на один, но production job сохраняет building reservation до возврата.
12. Worker возвращается к `WorkPosition` перед building. Только после arrival `ProductionWorkJob` становится terminal, workbench visual скрывается и building operation освобождается. Если scheduler не создал следующую работу, resident переходит в ordinary idle.
13. Для non-building output отдельный direct-use worker позже ломает закрытую коробку и materialize-ит manifest exactly once; это не является продолжением production order.
14. Тот же production worker остаётся owner всего manufacturing workflow.

Authoritative material-step phases: `AwaitingMaterial -> CarriedRaw -> StagedOnWorkbench -> Processing -> ProcessedAwaitingPackage -> Deposited`. `CarriedRaw` остаётся физическим Inventory state; остальные transition states принадлежат `ProductionState`. Runtime не может считать step completed только по истечению processing timer: package deposit является обязательным отдельным commit.

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
- Для `food` required output-ID count равен сумме quantities manifest, и каждый generated output имеет quantity `1`.
- Для `weapon`/`tool` required output-ID count равен числу manifest entries, и quantity каждого entry сохраняется в соответствующем stack.
- До committed break cancel, route failure, worker removal или interruption не меняют package/manifest. После commit package terminal и не восстанавливается.
- Несколько stale commands не могут materialize-ить manifest повторно; первый successful commit побеждает.

## 8. Повтор, blocked и concurrency

- Repeat order занимает первую доступную клетку дальше вправо без фиксированного lateral limit.
- Occupied output cell пропускается; production блокируется только если до правой границы мира нет ни одной валидной клетки.
- Каждый building владеет независимой queue/stock/active order.
- Два buildings не могут commit package/output в одну cell; occupancy перепроверяется при package creation/close.
- Unfinished package нельзя поднять, переместить или использовать.

## 9. Save/load и diagnostics

Save включает workstation registration, delivery toggles/incoming, queue/status, current material-step phase (`AwaitingMaterial`, `StagedOnWorkbench`, `ProcessedAwaitingPackage`, `Deposited`), processing ticks, consumed ledger, active job references и supply allocations. Load восстанавливает exact phase: staged raw material не возвращается в inventory, processed step не пропускает обязательный package deposit, а deposited step не обрабатывается повторно.

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
- production and supply of one building share one building-level operation reservation and cannot be active concurrently; queued refill is threshold-driven per next-recipe input, where `current + incoming < ceil(capacity / 2)` requests Supply and equality still allows Production;
- pre-production Supply ждёт supported extraction delivery, когда direct batch невозможен; после любого partial batch один runnable Production turn больше не блокируется оставшимися unresolved dependencies;
- a committed supply batch yields one Production turn when the next input set is complete, even if unavailable materials kept a target stock below threshold;
- queued supply targets the next recipe inputs and takes only currently eligible world units, while no-queue refill continues across all enabled stocks to capacity;
- enabled missing campfire cap/leg без eligible world source создаёт не более одной mushroom-chop/deferred-supply pair независимо от queued recipe и active production;
- deferred extraction dependency перебирает resident candidates, не резервирует phantom incoming и отменяется, если completed dependency не оставила requested world output;
- direct pickup забирает одну available unit;
- right candidates deterministic, без фиксированного limit и без side fallback;
- occupied nearest cells выбирают следующую правую cell;
- unfinished package является inventory entity, не pickup/use target;
- active explicit cancel сохраняет workflow до normal output;
- forced move удаляет package, теряет consumed materials, освобождает unused reservations, reset-ит progress и оставляет counter/order non-terminal;
- package close atomically terminal-ит order/output, а production job terminal-ится только после возврата worker к building work position;
- food/weapon/tool direct use materialize-ит manifest exactly once;
- `food.grilled_mushroom x2` materialize-ится как два distinct quantity-one world stacks в бывшей package cell;
- weapon/tool package materialization сохраняет один stack на manifest entry и прежнюю quantity;
- save/load сохраняет unfinished/closed package entity и active use job.

Unity Play Mode:

- fresh demo сохраняет двух hamster free/world-owned после initial production synchronization, без `R:1` и resident inventory transit;
- queued roasted-hamster order включает hamster delivery, но не меняет stock priority;
- disconnected explored/open source не резервируется, а failed supply route не блокирует следующий refill batch;
- internal units используют тот же art/hover/pickup cursor и exact `StackId`;
- production и internal-stock refill одного building никогда не активны одновременно; campfire cap `4 -> 3 -> 2 -> 1` даёт три production units подряд, затем один threshold-triggered refill attempt и следующий runnable Production turn;
- product icon показывает segmented material progress;
- unfinished package видна справа, не поднимается и сохраняется после save/load; временная колода видна только при active production, one-tick demo processing приводит к processed carry, package deposit, close и возврату к work position;
- explicit cancel active unit даёт finished output;
- forced move удаляет package, оставляет counter и заново ставит order в ожидание inputs;
- несколько занятых right-side cells сдвигают package дальше вправо;
- BuildingBox output использует обычный selection/unpack/pickup contract;
- food package имеет animated use cursor, ломается и выпускает каждую manifest quantity отдельным quantity-one world item exactly once;
- weapon/tool package use сохраняет прежнюю materialization семантику.

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
| 2026-08-02 | Решение о concurrent supply отменено: одно building обслуживает один resident; production и refill чередуются, refill имеет приоритет после close+return, demo processing = 1 tick, transient log/workbench и processed carry являются derived presentation. | Пользовательское уточнение runtime workflow |
| 2026-08-02 | Strict one-unit/one-batch alternation заменена threshold policy: required stock ниже `ceil(capacity/2)` инициирует supply, equality допускает production; batch берёт только доступные сейчас recipe inputs, extraction dependencies не блокируют runnable production, а без queue refill остаётся continuous-to-capacity. | Пользовательское уточнение runtime workflow |
| 2026-08-04 | При разрушении `food` package каждая manifest quantity создаёт отдельный quantity-one world stack; `weapon`/`tool` остаются без изменения. | Пользовательский runtime bug report |
