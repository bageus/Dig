# Unity item prefab and bounded stack pipeline

## Ownership

Inventory, stack quantity, reservations, item location and interaction kind remain authoritative Domain/Application facts. Presentation receives immutable `WorldItemViewModel` or `ResidentInventoryAttachmentViewModel` values and may only resolve assets, arrange bounded visual instances and expose pointer colliders allowed by the published interaction kind.

No visual animation, quantity badge, prefab collider or carry socket commits an inventory move, reservation or pickup command.

## Deterministic quantity projection

`ItemStackVisualLayoutPresenter` maps every positive quantity into one of four bands:

| Quantity | Band | Visible instances |
|---:|---|---:|
| 1 | Single | 1 |
| 2–4 | Small | 2 |
| 5–9 | Medium | 3 |
| 10+ | Large | 4 |

The same stack id, item id and band always produce the same offsets, scales and quarter-turn variants. Changing quantity inside a band changes the quantity badge and visual version but preserves `LayoutVersion` and instance placement. Reservation changes also preserve geometry.

The immutable layout publishes:

- quantity and reserved quantity;
- available quantity;
- quantity band;
- None, Partial or Full reservation state;
- one to four ordered layout instances;
- decimal quantity badge;
- stable layout and full visual versions.

There is no instance per authoritative unit.

## Typed profile

`DigItemVisualCatalog` resolves a stable `ItemId` to `DigItemVisualProfile` metadata:

- Material, Ore, BuildingBox, Food, Alcohol or Equipment family;
- shared prefab and optional icon;
- material and tint;
- None, Hand, Cargo, Weapon or Back carry socket policy;
- world and carry scales;
- Fixed, stack-quarter-turn or quantity-band lean rotation policy;
- collider policy;
- maximum visible instance count, restricted to one through four.

A prefab requires `DigVisualPrefabRoot`. Missing or invalid profiles use the normal catalog/runtime fallback without changing item identity or interaction facts.

## World stack renderer

`DigWorldItemRenderer` owns one stable root per visible stack. The root has exactly one `BoxCollider` used for typed item interaction. Prefab children have all colliders disabled and use the Ignore Raycast layer.

- interactive stack root: normal interaction layer and enabled root collider;
- non-interactive stack root: Ignore Raycast and disabled root collider;
- child instances: always Ignore Raycast with colliders disabled.

The renderer no longer chooses a production Cube or Sphere from `IsBuildingBox`. It resolves every item through `ItemId`. The generic cube passed to `DigVisualPrefabFactory` is only the explicit missing-asset fallback for every item family.

Each root creates a fixed child pool up to the profile limit. Quantity-band changes activate or hide existing children. Removed stack roots enter a bounded renderer pool and may be reused by later stacks. Quantity growth cannot create more than four child instances.

Partial and full reservation remain readable without colour alone:

- partial reservation slightly tilts and stretches the stack root;
- full reservation tilts and compresses it;
- tint is an additional signal, not the sole signal.

## Carry visuals

Resident inventory expansion attachments resolve the same `DigItemVisualProfile` by authoritative `ItemId`. Cargo and Weapon group facts still come from the resident inventory read model. Profile socket metadata controls only the local Presentation transform.

A carry attachment uses one prefab instance, the profile carry scale and no collider. Existing `VisualAttachmentId` remains a stable attachment diagnostic token; it does not select a separate gameplay item or inventory stack.

## Rebuild and pooling contract

A stack visual rebuilds child prefab instances only when:

- the resolved asset changes;
- the catalog is explicitly replaced or invalidated;
- the profile maximum instance capacity changes.

Quantity, reservation, location and interaction changes update transforms, active flags, badge text and the root collider without replacing the child pool.

Current hard limits:

- at most four visible prefab children per stack;
- one interaction collider per stack root;
- at most 64 inactive stack roots retained by the world renderer;
- one carry prefab instance per active inventory expansion group.

## Remaining #208 work

This foundation removes production primitive branching and establishes deterministic bounded layouts and common world/carry profiles. Follow-up slices still need:

- authored/representative profile content for base materials, ores, BuildingBox, food, alcohol and equipment;
- icon assets and quantity badge rendering in the gameplay camera/HUD;
- selected and last-known overlay inputs when those immutable facts are published;
- measured item LOD and profile-specific batching budgets;
- Unity Editor validation gallery and Play Mode verification.
