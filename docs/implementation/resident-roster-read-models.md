# Resident roster read models

## Purpose

The resident roster is an immutable Presentation projection. It combines confirmed snapshots from Agents, Society and Jobs without becoming another owner of resident state.

The existing `AgentViewModel` remains available for the current Unity demo. `ResidentRosterPresenter` adds the richer contract required by the persistent roster, selected expanded row and later skill inspector.

## Input snapshots

The presenter reads:

- `AgentSnapshot` for name fallback, alive state, needs, schedule, action, player order and the complete stable skill snapshot;
- `ResidentSocietySnapshot` for authoritative name and sex when Society membership is available;
- one current non-terminal `JobSnapshot` assigned to the resident.

Rows are sorted by stable `EntityId`. Dead Agents and Society residents marked deceased are excluded. A missing Society record is safe: the row keeps the Agent name and exposes `ResidentSexIndicator.Unknown` plus the non-color accessibility key `resident.sex.unknown`.

## Needs and mood boundaries

Domain values remain integers from 0 to 10000.

Needs bands are exact:

- 0–2500: `Critical`;
- 2501–5000: `Warning`;
- 5001–10000: `Healthy`.

Mood faces are exact:

- 0–2500: `Sad`;
- 2501–7500: `Neutral`;
- 7501–10000: `Joy`.

Every need also carries an accessibility/localization key. Alertness uses `resident.need.alertness.vigor`, allowing the Russian UI to display «Бодрость» without embedding that ready string in the snapshot.

## Skills

`ResidentSkillSetViewModel` stores one immutable full skill list in stable skill-id order. The compact top-five view is derived from the same values using:

1. level descending;
2. stable `AgentSkillId` ascending.

Combat skills participate in the same ordering as every other skill. The full inspector and compact row therefore cannot disagree because of separate queries.

## Typed activity

`ResidentActivityDescriptor` contains:

- typed `ResidentActivityKind`;
- actor and optional subject ids;
- optional logical destination;
- source Job, Agent intent and player order identifiers;
- source Job stage;
- bounded progress;
- optional block reason code;
- localization key and structured arguments.

It never stores a ready localized status sentence.

Current typed mappings include:

- Agent actions: flee, eat, sleep, move/player order, work, rest and idle;
- digging Jobs: `Dig`;
- hauling Jobs: `Pickup` during item acquisition, otherwise `Logistics`;
- production Jobs: `Craft`;
- healing Jobs: `Service`;
- strategic attack and retreat Jobs: `Attack` and `Flee`.

A typed Job takes precedence over a generic Agent `Work` action because it contains the more specific authoritative context. Unknown future Job definitions fall back to `Work` until their explicit classifier is added.

Work schedule with no action, Job or emergency becomes `Idle` with `IsIdleAtWork = true`. Free schedule becomes `FreeTime`, so the red idle-at-work marker does not misclassify legitimate leisure.

## Selection and virtualization

`ResidentRosterViewModel` contains the stable rows and the selected resident id. Exactly the selected row is expanded; selection does not mutate Agent or Society state.

`GetWindow(offset, count)` returns only the requested bounded row range. Unity's
`DigGameHudCanvas.RosterVirtualization` consumes that contract through a fixed
pool of sixteen row roots. Top and bottom layout spacers preserve scroll extent;
only a slot whose immutable row signature changed is rebound. The Unity adapter
keeps the presenter order and no longer re-sorts rows by display name.

## Validation

Tests cover:

- exact 25/26, 50/51 and 75/76 boundaries;
- stable resident ordering and one expanded row;
- accessible female, male and unknown sex indicators;
- full skill snapshot and deterministic top-five ordering including combat skills;
- Work+Idle versus FreeTime;
- typed digging and player-order descriptors;
- safe missing targets and typed block reasons;
- a bounded window over seventy residents.
- a Unity Play Mode regression with seventy residents, sixteen row roots and an
  unchanged neighbouring row after another row updates.

Click routing, scroll position and row pooling remain local Presentation state.
The Unity row reads the typed sex indicator already present in the read model;
it does not query or mutate the simulation while binding.
