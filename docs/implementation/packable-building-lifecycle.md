# Packable building lifecycle

## Scope

This document describes the Domain foundation introduced for issue #328. It does not yet implement campfire content, inventory transport, placement, jobs, Unity UI or save/load adapters.

## Authoritative owner

`PackableBuildingLifecycle` is the single authoritative owner of whether one logical building package is:

- packed in the world;
- packed in a resident inventory;
- planned for unpacking;
- being unpacked;
- an active building;
- planned for packing;
- being packed;
- interrupted with resumable progress.

Presentation code may project this state but must not keep a second mutable lifecycle copy.

## Identity

Every lifecycle has:

- a stable package identity;
- a stable building-definition identity.

Later inventory, world-item and building adapters must reference this identity instead of creating independent packed and unpacked objects at the same time.

## Work model

Packing and unpacking use `PackableBuildingWorkProgress`.

The progress object owns:

- operation direction;
- total iteration count;
- base work minutes required per iteration;
- completed iteration count;
- partial work in the current iteration;
- an immutable projection of iteration-completion records.

The Domain model receives normalized base work minutes. A later Logistics policy may convert elapsed game time into base work without changing lifecycle ownership.

## Commands and events

The aggregate currently exposes command-like methods for:

- planning packing or unpacking;
- cancelling a planned operation;
- starting planned work;
- interrupting active work;
- resuming interrupted work;
- advancing normalized work.

`PackableBuildingIterationCompletion` represents an already completed fact. Application handlers added in later issues will translate player and job intent into these Domain transitions and publish external events where required.

## Invariants

- Only stable states may be used to create a new lifecycle.
- Packing starts only from an active building.
- Unpacking starts only from a packed form.
- Cancelling a plan restores its previous stable form.
- Interrupting work preserves completed and partial iteration progress.
- Only the active worker receives completion attribution.
- Every iteration number is recorded once.
- Final completion converts unpacking to an active building and packing to a world box.
- Completed work cannot be advanced or finalized again.

## Testing

`PackableBuildingLifecycleTests` covers:

- legal pack and unpack paths;
- inventory-origin unpacking;
- cancellation;
- partial progress preservation;
- worker replacement;
- per-iteration attribution;
- multi-iteration advancement;
- duplicate-finalization protection;
- invalid initial states and transitions.

Save/load round trips are intentionally deferred to #335, after authoritative world and inventory locations are integrated.
