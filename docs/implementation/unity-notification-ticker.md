# Unity notification ticker

## Scope

This slice implements the event-driven notification requirements of issues
#14, #113 and #116. Presentation owns only the active queue, dismissed state,
scroll/animation state and navigation attempt. Domain/Application owners still
produce the facts.

## Confirmed event path

`InMemoryExecutionJournal` assigns every appended Domain event a monotonic
runtime sequence. `ReadEventsAfter` exposes a bounded read-only cursor even
when the diagnostic journal trims older retained events. The Unity terrain
session forwards that stream without translating facts into UI strings.

`GameNotificationProjector` maps confirmed typed events:

- `CombatAttackResolved` -> resident attacked;
- `ResidentBorn` -> birth;
- `AgentNeedThresholdCrossed` -> hunger or critical Mood;
- `ResidentLifeStageChanged(Old)` -> old age;
- `ResidentDied` / `AgentDied` -> death;
- `TechnologyUnlocked` -> technology discovery;
- `JobStatusChanged(Completed)` -> task completion.

Hunger is emitted only on the exact downward crossing from `>= 1500` to
`< 1500`. Recovery to `>= 1500` followed by another downward crossing creates
a new event key. Critical Mood follows the same rule at `500`.

## Queue and lifecycle

`GameNotification` contains a stable id, source event key, tick, priority,
localization key/arguments, navigation target and active state. The queue:

- rejects duplicate source event keys;
- keeps simultaneous messages separate;
- orders by priority descending, tick ascending and stable id ascending;
- has no automatic timeout and no history screen;
- removes the current message after LMB navigation attempt or RMB dismissal.

The ticker panel is a raycast target and remains part of HUD shielding, so the
same pointer event cannot reach world routing.

## Navigation

- attack, birth, hunger, old age and critical Mood select/focus a resident;
- death focuses the authoritative last-known cell without selecting or reviving
  a removed resident;
- completed work selects the job and focuses its target when present;
- technology opens a Presentation-owned description panel without inventing a
  world position.

Stale targets produce feedback and do not block the rest of the queue.

## Related HUD completion

The resident roster uses a sixteen-root virtualized pool over
`ResidentRosterViewModel.GetWindow`. Inventory sections render in the required
`Weapon -> Main -> Cargo` order.

## Verification

Automated coverage includes exact threshold/recovery, idempotency, deterministic
priority, journal cursor trimming, death navigation, a seventy-resident pooled
roster and LMB/RMB UI lifecycle. Static Unity contracts also lock the bounded
pool, event stream, source navigation, technology panel and inventory order.

The Unity Play Mode tests are included in the project; execution still requires
a Unity Editor or CI runner.
