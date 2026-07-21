# Unity resident rig presentation pipeline

## Ownership

Resident identity, life state, cell position, route, active intent and action progress remain immutable Presentation inputs produced from authoritative simulation state. The Unity resident rig may choose assets, colors, child transforms and presentation poses only.

No `Animator`, animation event, socket transform or visual interpolation completes an action, changes inventory, moves a resident in the simulation or commits a command.

## Stable root and interaction

Each resident has one stable root GameObject owned by `DigAgentRenderer`:

- the root contains `DigAgentVisual`;
- the root contains exactly one `CapsuleCollider` used by existing resident selection;
- the root follows the established `DigTunnelProjection.ResidentWorldPosition` mapping and is anchored at the resident's feet on `WalkSurfaceY`;
- route playback and normal interpolation continue to move the root;
- the composite visual rig is a collider-free child.

Visual height never offsets the stable root. Both representative and authored rigs use a foot-level pivot; the emergency capsule fallback keeps the same foot-level root and offsets only its collider-free visual child. This prevents a change of resident mesh or rig from lifting authoritative movement above the walk surface.

Selection never depends on a particular child renderer. `DigResidentRig.SetSelected` applies a `MaterialPropertyBlock` to every child renderer, so authored multi-mesh or skinned prefabs preserve selection without replacing the stable resident root.

## Deterministic appearance projection

`ResidentVisualPresenter` projects a stable appearance from resident id and explicit appearance facts:

- neutral, masculine or feminine body variant;
- young, adult or old age band;
- bald, short, braided, long or old-sparse hair silhouette;
- none, worker, miner, builder or hauler headwear role;
- eight clothing palette indices;
- six hair palette indices;
- four face variants.

The same resident id and appearance facts always produce the same immutable appearance version. Save/load does not need to serialize Unity objects or random state.

The current demo has no authoritative sex, age or role appearance snapshot, so it deliberately requests the neutral/default profile. A later content bridge may pass authoritative values without changing rig ownership.

## Typed visual profiles

`DigResidentVisualCatalog` resolves `resident.default` through `DigResidentVisualProfile` metadata:

- stable id;
- body variant;
- optional prefab and material;
- tint and world scale;
- maximum renderer budget from 10 through 24.

A prefab must include `DigVisualPrefabRoot`. An authored prefab with too few renderers or one exceeding its profile budget falls back to the representative rig and emits the existing catalog validation diagnostics instead of changing resident identity.

## Representative low-poly rig

The missing-asset fallback is a composite low-poly humanoid, not a Capsule visual. It contains ten shared-material renderers:

- body;
- head;
- hair;
- headwear;
- two arms;
- two legs;
- two hands.

Generated primitive colliders are destroyed. The representative rig remains replaceable by an authored prefab through the same typed catalog.

## Sockets

Every rig exposes six fixed Presentation sockets:

- Head;
- LeftHand;
- RightHand;
- Back;
- Cargo;
- VFX.

Equipment uses RightHand. Inventory expansion attachments use Cargo or Back according to their authoritative expansion group. Socket transforms are attachment placement metadata only and never become inventory locations.

## Action projection

`ResidentVisualPresenter.PresentAction` maps immutable facts to:

- Idle;
- Walk;
- Dig;
- Carry;
- Build;
- Pickup;
- Drop;
- Hit;
- Death.

Movement wins over ordinary work intents, death is terminal, and published `ActionProgress` is preserved. The representative rig uses bounded limb/root poses. Future Animator controllers may consume the same state and normalized progress, but root motion and action-completion animation events remain forbidden.

## Performance boundary

Current hard bounds:

- one interactive root collider per resident;
- one composite rig root per resident;
- 10 to 24 child renderers according to profile validation;
- one shared instanced base material with property-block palette/selection overrides;
- no material allocation per resident;
- no visual rebuild during ordinary movement, selection or action-progress changes.

Measured camera LOD and animator update tiers for 64+ residents remain a follow-up slice of issue #209.

## Remaining work

The foundation intentionally leaves these items for later slices:

- authoritative sex, age, role and headwear snapshots;
- authored skinned male/female resident prefabs;
- Animator state controller and update throttling;
- hair/headwear mesh activation from appearance metadata;
- audio/VFX-only animation events;
- measured LOD and 64+ resident gallery/Play Mode validation.
