# Resident equipment profiles

This slice reads equipped item locations from authoritative inventory snapshots and projects them onto resident visuals.

The inventory aggregate rejects a stack that is not carried by the acting resident and rejects a second occupied slot. The Unity session also checks the combined building and terrain inventories before routing the action.

Domain equipment profiles now define an appearance kind, supported work kind, and work interval. The demo mining profile uses a one-tick interval while the baseline terrain-work interval remains three ticks. A construction profile is present for hammer-shaped visuals and future building-work integration.

Unity distinguishes mining, construction, and generic silhouettes. Child geometry uses the Ignore Raycast layer and has no collider, preserving resident selection.

Terrain advancement is evaluated separately per resident. Only a resident assigned to an active Dig job receives the mining cadence; hauling and unrelated jobs keep the normal simulation tick. The work-rate policy reads the same combined inventory snapshots used by the equipment projection and rejects duplicate equipped state.

Focused tests cover ownership, occupied slots, matching and mismatched work profiles, baseline behavior without equipment, and duplicate cross-inventory state. The Quality workflow covers architecture, compatibility, Unity validation, build, tests, smoke, and deterministic soak runs.
