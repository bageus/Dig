# Universal forced resident order replacement — implementation note

Date: 2026-08-05

Status: **IMPLEMENTED ON BRANCH; RUNTIME VERIFICATION PENDING**

Authoritative design: [`../design/universal-forced-resident-order-replacement.md`](../design/universal-forced-resident-order-replacement.md)

Tracking issue: [#646](https://github.com/bageus/Dig/issues/646)

## Root cause

Direct resident commands already shared a runtime cancellation helper for combat, manual movement and assigned Jobs, but the helper only interrupted an active food meal in `AgentState`. A resident could therefore receive a new visual/manual command while the domain `ActiveAction` and previous `PlayerOrder` still described the replaced activity.

The split ownership allowed the old work/action to survive beside the new destination until another subsystem happened to clear it.

## Implementation

- added `AgentState.PrepareForForcedOrder(reason, tick)` as the domain invariant for clearing the active action and previous player order;
- integrated that invariant into `DigTerrainWorkSession.PrepareResidentsForDirectCommand` before assigned Jobs are released;
- preserved the established order of combat disengage, manual movement cancellation, meal interruption, Job-specific cancellation and reservation release;
- kept target validation at the command entry point, so a rejected destination does not cancel the resident's current valid command;
- retained specialized cancellation handlers for pickup, mushroom chopping, barrel attack, production, building supply/assembly/relocation, package use and excavation.

## Regression coverage

`ForcedOrderReplacementTests` covers:

- domain active-action replacement and idempotence;
- source contract that the domain invariant clears both action and player order;
- source contract that the common runtime boundary calls the domain invariant and retains all specialized Job cancellation paths;
- direct movement use of the common replacement boundary.

## Verification state

The branch still requires the repository Release build/full .NET suite and licensed Unity Play Mode evidence for:

1. work -> forced move;
2. move -> direct excavation;
3. tunnel A -> tunnel B;
4. pickup/chop/production/building work -> another forced command;
5. invalid direct target preserving the current command;
6. repeated replacements leaving only the latest valid command active.

Do not promote the authoritative status to `VERIFIED` until that evidence exists.
