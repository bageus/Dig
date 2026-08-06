# Universal forced resident order replacement

Status: **APPROVED**

Date: 2026-08-05

Tracking issue: [#646](https://github.com/bageus/Dig/issues/646)

System index: [`docs/systems/README.md`](../systems/README.md)

Related authoritative specifications:

- [`runtime-selection-excavation-item-placement-decisions.md`](runtime-selection-excavation-item-placement-decisions.md)
- [`resident-work-needs-traits-lifecycle.md`](resident-work-needs-traits-lifecycle.md)
- [`runtime-pickup-combat-vuker-correction-2026-08-05.md`](runtime-pickup-combat-vuker-correction-2026-08-05.md)

## Confirmed rule

Every valid explicit/forced player order replaces the selected resident's current non-combat order and current work before the new order is committed.

The rule is command-type independent. It applies to forced movement, direct excavation, pickup, chopping, production, building supply/assembly/relocation, package use and every later forced resident command that enters the same authoritative direct-command boundary.

Examples:

- work, rest, eat or study -> forced move: interrupt the current activity, release its work and reservations, then start movement;
- dig tunnel A -> forced dig tunnel B: release tunnel A and its approach/reservations, then assign tunnel B;
- forced move -> forced work: cancel the old route, then commit the selected work;
- forced order A -> forced order B: the latest valid order wins.

Combat remains governed by the authoritative combat disengage/override policy. A direct command must use that policy and must never leave unrelated work active in parallel.

## Authoritative state owners

- `AgentState.PlayerOrder` owns the current explicit resident order.
- `AgentState.ActiveAction` owns the current domain action.
- Jobs and their specialized repositories own claimed/in-progress work, targets, approaches and resource reservations.
- Runtime manual-command state owns manual routes and command-specific pending state.

No layer may treat a visual destination change as sufficient cancellation while another owner still retains the previous work.

## Replacement sequence

For each selected resident, after the new command has passed target/input validation and before it is committed:

1. interrupt combat only through the established combat direct-command policy;
2. cancel active manual movement and command-specific pending state;
3. interrupt the current food/rest/study/work domain action;
4. clear the previous `PlayerOrder`;
5. release every claimed or in-progress Job through the existing cancellation/release handlers so specialized reservations are released;
6. commit the new forced command;
7. persist affected authoritative repositories and publish diagnostics/events.

The replacement path is idempotent. Repeating it when no previous order/work exists succeeds without inventing work or duplicate ownership.

## Validation, failure and retry

- A rejected or invalid new command must not cancel the resident's existing valid command.
- Once replacement starts, failure must be reported with the resident and subsystem that rejected cancellation; the new command must not be silently presented as active while old ownership remains.
- Retrying a valid command is safe; the latest successfully committed forced command is authoritative.

## Multiple residents and conflicts

Replacement runs independently for every selected resident. A failure for one resident must be diagnosable and must not create duplicate ownership for another resident.

When several residents target the same exclusive work, normal Job claim/reservation rules decide the winner after each resident's previous work is released.

## Save/load and presentation

Save/load persists only the authoritative post-commit state. Cursor, selection, panel and status text must refresh from that state and must not display the replaced job, route or activity as active.

## Diagnostics

The replacement reason is `direct_command_replaced`. Domain events and runtime diagnostics must identify interrupted actions/orders and released Jobs without creating a second command-history source of truth.

## Acceptance

- every forced command uses one replacement boundary before commit;
- the boundary clears both `ActiveAction` and the previous `PlayerOrder`;
- all claimed/in-progress Jobs and specialized reservations are released through existing handlers;
- direct move and direct excavation are regression-covered;
- move -> dig -> move and dig A -> dig B leave only the latest valid command active;
- invalid commands preserve the current command;
- domain tests cover replacement and idempotence;
- runtime/source tests cover all direct-command callers;
- licensed Unity Play Mode or equivalent end-to-end evidence is required before status can become VERIFIED.
