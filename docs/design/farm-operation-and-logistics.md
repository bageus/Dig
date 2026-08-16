# Farm operation and logistics

Status: `APPROVED`.

Tracking: [issue #731](https://github.com/bageus/Dig/issues/731),
[PR #736](https://github.com/bageus/Dig/pull/736),
[PR #737](https://github.com/bageus/Dig/pull/737),
[PR #738](https://github.com/bageus/Dig/pull/738).

## Player workflow

- A completed farm exposes exactly three mutually exclusive icon switches:
  mushrooms, hamsters and grubs.
- The farm is visually a `2 x 1.5` dirt plot with a `0.5` high low fence.
- Switching mode immediately replaces the active supply demand. Feed left at the
  former internal stock is released from farm ownership. Existing mushrooms
  remain harvestable; former animals escape gradually.

## Authoritative rules

| Mode | Starter stock | Production | Protected reserve | Capacity |
|---|---:|---|---:|---:|
| Mushrooms | 1 mushroom cap, once | 3 simultaneous mushrooms; a harvested slot regrows | — | 3 |
| Hamsters | 2 hamsters, once | 1 adult every 2 game hours while fed | 2 | 8 |
| Grubs | 1 grub, once | 1 adult every game hour while fed | 1 | 8 |

Animal modes expose two mushroom-cap feed slots after starter stock arrives.
The farm consumes one cap at every global half-day boundary. Reproduction pauses
without feed and at population capacity.

## Logistics contract

- `FarmState` owns mode, internal stock, population, growth and timers.
- `InventoryState` owns every physical starter/feed item before delivery and the
  item while a resident carries it.
- A demand creates ordinary staged hauling work: acquire physical item, travel
  to the farm work position, deposit into the building-owned internal location,
  consume that physical item, then commit `FarmState.Deliver`.
- Each active delivery has one farm reservation and one inventory reservation.
  Repeated synchronization cannot create duplicate work for in-transit stock.
- Terminal/orphan jobs and removed farms release both reservations. Failed path
  assignment releases only the worker/slot claim so the same delivery may retry.
- Delivery is complete only after the item left the resident inventory, farm
  state changed and the job reached its terminal completed stage.

## Acceptance evidence

- Domain tests cover mode switching, stock, growth, breeding, feeding, capacity,
  escaping animals and snapshot restoration.
- Application integration tests cover creation of a physical farm haul and
  duplicate suppression while the stock is in transit.
- Unity source contracts, compilation and Play Mode must cover assignment,
  acquisition, movement to the farm work position, deposit, presentation refresh,
  mode change cancellation/reconciliation and retry after an unreachable route.

