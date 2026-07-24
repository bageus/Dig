# Campfire packable-building production completion

Issues #328 through #335 are implemented by extending the existing authoritative owners rather than adding a second lifecycle aggregate.

## Authoritative ownership

- `BuildingsState` owns the campfire, planned unpack site, packing plan, work totals and footprint.
- `InventoryState` owns the packed campfire box and its world/resident/site location.
- `JobSystem` owns assignment, stages and reservations.
- `PackableBuildingExecutionRegistry` owns resumable operation direction, active worker, completed-iteration attribution and the frozen active-iteration clock.
- `AgentSkillSet` owns the persisted idempotency ledger for the per-iteration Logistics grants.

## Production loop

The data-driven campfire definition supplies three pack iterations, three unpack iterations, a ten-minute base duration, 0.7 Logistics per completed iteration and outdoor-only flat placement.

Runtime assembly and packing both:

1. create or reuse one execution for the Job/package pair;
2. start or resume with the assigned resident;
3. freeze the duration resolved from that resident's current Logistics skill;
4. apply the existing authoritative Buildings mutation after the duration expires;
5. record one execution iteration and grant 0.7 Logistics to that iteration's worker;
6. reject stale workers and repeated completion;
7. keep completed iterations when another resident resumes.

## Save and load

`SaveGameDocument.PackableBuildingExecutions` persists operation/package/definition identity, direction, lifecycle status, completed iterations, worker attribution and the active iteration clock. Load validates the section against restored Jobs and Buildings, recovers stale worker ownership and preserves autosave state.

The existing Buildings, Inventory, Jobs, resident skills and terrain sections continue to persist their own state. No Unity object or visual is serialized as authoritative simulation data.

## Presentation

`PackableBuildingExecutionPresenter` projects planned, active, interrupted and terminal operation state with deterministic iteration progress. Visual ids are provided by a definition-keyed presentation catalog. The lower building context consumes this projection for iteration, percentage, worker, Continue state and active highlighting.

Existing building visual states distinguish assembly and packing forms. `PresentationDomainEffectProjector` emits `ConstructionProgress` from authoritative `BuildingConstructionProgressed` and `BuildingPackingProgressed` events, producing one effect for each accepted work iteration.

## Automated coverage

Coverage includes:

- campfire content and placement policy;
- world and resident-inventory box ownership;
- cancellation and duplicate prevention;
- assembly and packing terminal paths;
- interruption, reassignment and stale-worker rejection;
- every Logistics timing boundary;
- proportional per-worker experience and stable iteration source ids;
- operation save/load, active-clock round trip and stale-worker recovery;
- autosave retention;
- data-driven progress presentation and lower-HUD projection;
- an acceptance integration that interrupts, saves, resumes with another resident and completes all three iterations without duplicate experience.
