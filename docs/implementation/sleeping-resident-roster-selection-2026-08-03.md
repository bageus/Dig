# Sleeping resident roster, selection and survival correction — 2026-08-03

Status: `IMPLEMENTED IN BRANCH`.

Tracking: #113, #159, #390.

## Reported symptom

A prone resident disappeared from the upper-right roster (`No dwarfs`) and could not be selected. The screenshot also contained a death notification. The runtime had no dedicated Sleep pose: the prone visual was the terminal `Death` pose, and dead residents were correctly removed from actionable roster/selection.

## Root cause

Two independent contracts combined into the visible symptom:

1. `ResidentActionVisualState` had no Sleep state, so living sleepers could not project a distinct sleep pose/status through the resident rig.
2. `AgentAutonomySystem` advances passive needs before the active targeted action interval. Critical Alertness therefore applied `-500` Health before Sleep added its small per-interval Health share. A resident could lose Health and die while an already committed Sleep action was recovering Alertness.

Roster and world selection already filter by authoritative alive state rather than by Sleep. Making dead residents selectable would hide the cause and violate lifecycle ownership.

## Correction

- `AgentState.AdvanceNeeds` detects an active Sleep with at least one committed interval.
- `AgentNeedsState.AdvancePassive` ignores Alertness-only critical Health/Mood damage during that committed Sleep action.
- Critical Nutrition remains damaging during Sleep.
- Walking to a Bed has `ElapsedTicks == 0` and receives no protection.
- Presentation adds stable `ResidentActionVisualState.Sleep = 10`; existing enum values are unchanged.
- `ResidentVisualPresenter` maps the Sleep intent to a looping Sleep state.
- `DigResidentRig` renders Sleep with a distinct prone pose while keeping the resident root/collider selectable; Death remains terminal and visually distinct.

## Regression evidence

- Domain: committed Sleep protects exhaustion-only recovery; critical Nutrition still damages.
- Presentation: living Sleep remains in roster, selected and typed as `ResidentActivityKind.Sleep`.
- Visual projection: Sleep is looping and distinct from Death.
- Checked-in Play Mode: a low-Health Floor sleeper survives the former failure window, remains in `LoadResidentRoster`, remains in `DigAgentRenderer.GetHudModels`, and can be selected by stable resident ID.

## Verification boundary

Repository Quality, Release build, the full .NET suite, smoke and deterministic soaks can establish `IMPLEMENTED`. `VERIFIED` still requires licensed Unity EditMode/PlayMode execution and confirmation that the Console is clean while the sleeping resident remains selectable in the rendered scene.
