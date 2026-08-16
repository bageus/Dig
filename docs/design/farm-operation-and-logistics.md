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

The presentation projects the authoritative farm snapshot inside the fenced plot:
three growth slots, up to eight animals, and both feed caps at the physical center
of the farm. These objects are presentation-only and do not own simulation state.
Visible hamsters and grubs wander within clamped plot bounds; their motion never
crosses the low fence and does not create physics or authoritative movement state.
Clicking a farm with an available mushroom while a resident is selected assigns
that resident a normal mushroom-chop job and projects the axe cursor. The resident
walks to the farm work position, performs
the axe swings, then removes one authoritative growth slot and creates one
physical cap plus one physical leg at the farm origin. The empty slot is then
eligible for the normal farm regrowth step.
The axe cursor and command are available only when the selected resident has a
supported navigation route to a valid work position beside the farm.
Active harvest work is recoverable from its persisted mushroom-chop definition;
terminal work, removed farms and invalidated work support cannot leave a stale
farm harvest reservation behind.
The save document persists every authoritative farm snapshot field, including
mode, stock, populations, reproduction/feed timers and gradual escape progress.
Active incoming and outgoing logistics reservations are persisted with their job
and farm identities so in-transit hauling resumes without duplication after load.
Older documents without a farms section restore an empty repository and rebuild
completed farms through normal registration.

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
- A mode change cancels incoming jobs whose demand no longer exists, releasing
  their inventory reservation and resident slot before planning the new demand.
- Delivery is complete only after the item left the resident inventory, farm
  state changed and the job reached its terminal completed stage.
- Animal production above the protected reserve is transferred into the farm's
  building-owned Inventory as unit items. A staged outgoing hauling job makes a
  resident collect each unit at the farm work position and place the physical
  hamster or grub at the farm output cell. Failed jobs leave the unit in internal
  stock for deterministic retry; they never subtract the protected breeders twice.
- Switching away from an animal mode materializes escaping animals gradually at
  the farm origin. Released feed becomes an ordinary world mushroom-cap stack at
  the same location and no longer belongs to the farm.

## Acceptance evidence

- Domain tests cover mode switching, stock, growth, breeding, feeding, capacity,
  escaping animals and snapshot restoration.
- Application integration tests cover creation of a physical farm haul and
  duplicate suppression while the stock is in transit, plus outgoing unit
  materialization and protected breeder reserves.
- Unity source contracts, compilation and Play Mode must cover assignment,
  acquisition, movement to the farm work position, deposit, presentation refresh,
  mode change cancellation/reconciliation and retry after an unreachable route.
