# Issue 574 — automatic tunnel junction trim lifecycle

Status: `SUPERSEDED` by the confirmed placement-only junction rule recorded on 2026-08-03.

Authoritative specification: [`../design/room-purposes-upgrades-and-tunnel-reinforcement.md`](../design/room-purposes-upgrades-and-tunnel-reinforcement.md).  
Tracking issue: [#574](https://github.com/bageus/Dig/issues/574).  
Replacement note: [`issue-574-junction-placement-and-room-overlay-visibility-2026-08-03.md`](issue-574-junction-placement-and-room-overlay-visibility-2026-08-03.md).

## Historical scope

PR #582 implemented a pending/completed decorative junction target together with an automatic low-priority material job. That automatic-job behavior is no longer authoritative.

The retained compatible state is limited to:

- stable vertical-junction identity derived from left/right horizontal chains;
- completed junction stone-trim provenance used by manual placement and rebuildable visuals;
- save-codec compatibility for legacy `JunctionStoneTrim` automatic job definitions.

The following behavior is superseded and must not be recreated:

- automatic junction stone-trim job creation;
- automatic range/source selection for junction stone;
- automatic stone reservation or worker assignment;
- junction work/job marker or other clickable reinforcement point;
- automatic Stonework completion through `CompleteTunnelAutomaticWorkHandler`.

Junction/floor stone trim now starts only through resident-owned manual placement mode using the selected exact stone stack. Runtime synchronization cancels legacy non-terminal automatic junction-trim jobs and releases their Inventory reservations before ordinary assignment.

Stone trim remains decorative. It is not a structural anchor, does not reset the rolling wooden-support chain and does not protect against collapse.

## Historical validation

The original PR #582 validation remains evidence only for the former implementation:

- Release build: `0` warnings, `0` errors;
- full .NET suite: `1412/1412`;
- headless smoke and deterministic soaks passed;
- actual Unity EditMode/PlayMode execution was skipped because activation was unavailable.

Current validation is recorded in the replacement implementation note and its pull request.
