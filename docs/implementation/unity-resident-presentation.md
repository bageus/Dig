# Unity resident presentation

## Purpose

This vertical slice makes residents observable in the Unity host without turning scene objects into simulation state. It adds authoritative logical positions, immutable presentation models, visual interpolation, object selection and a Utility AI inspector.

## State ownership

`AgentState` owns the resident's logical `CellId` position together with needs, schedule, skills, traits, player order and active action.

A position change is accepted only through `AgentState.MoveTo`. The operation:

- rejects dead residents;
- rejects negative logical coordinates;
- treats movement to the current cell as a no-op;
- increments the aggregate version exactly once for a real move;
- emits one `AgentMoved` event containing previous and current cells.

The Application layer exposes `MoveAgentCommandHandler`. Unity never writes the position property or repository directly.

World-bound and path-validity checks remain the responsibility of the navigation or movement orchestrator that issues the command. The resident aggregate only owns the accepted logical position.

## Read path

`GetAgentSnapshotsQuery` returns all residents in stable repository order. `AgentPresenter` converts snapshots into immutable `AgentViewModel` values containing:

- stable id, name, version and alive state;
- logical cell coordinates;
- nutrition, alertness, mood and health;
- scheduled activity;
- active intent and action progress;
- decision reason and explanation;
- all seven Utility AI alternatives, including score, availability, critical state and rejection reason.

Presentation models contain no mutable `AgentState`, repository or Unity references.

## Unity composition

`DigUnityBootstrap` creates the world and resident demo sessions, then wires:

- `DigAgentRenderer` for rebuildable resident GameObjects;
- `DigAgentVisual` for movement between two confirmed logical cells;
- `DigAgentSimulationDriver` for fixed demo ticks;
- `DigWorldInteraction` for cell and resident selection;
- `DigHudOverlay` for needs and Utility AI diagnostics.

Resident visuals are capsules with colliders. Deleting all resident GameObjects does not alter the repository; calling `Render` again rebuilds them from current view models.

## Interpolation

The simulation updates integer cell positions. `AgentPositionInterpolator` computes a clamped presentation-only position between the previous and current cells. Unity applies that value over most of the demo tick interval.

Interpolation never writes back to `AgentState`. A frame-rate change therefore changes only visual smoothness, not simulation results.

## Demo behavior

The Unity demo creates four residents on walkable cells from the generated cavern. Each fixed demo tick:

1. `AgentAutonomySystem` advances needs and records a real deterministic Utility AI decision.
2. A deterministic route selector chooses another cell from the current world view's non-solid cells.
3. `MoveAgentCommandHandler` commits the new logical position.
4. The presenter produces fresh immutable resident models.
5. Unity interpolates the existing visual to the confirmed target cell.

The route selector is intentionally a host demonstration, not final navigation. Job travel, path following, collision avoidance and route invalidation remain future integration work.

## Inspector and controls

Left-clicking a resident selects it and shows:

- current logical cell and aggregate version;
- schedule and active intent;
- action progress;
- all four needs;
- selected decision reason and explanation;
- every Utility AI option and its availability or rejection code.

Left-clicking a cell switches the HUD back to cell diagnostics. Right-clicking a cell continues to toggle its digging designation through the Application command.

## Validation

The engine-independent tests cover:

- movement command success and the emitted event;
- invalid movement rollback;
- stable all-resident presentation ordering;
- position and needs mapping;
- complete Utility AI diagnostics;
- interpolation endpoints, midpoint and clamping.

Normal CI still runs architecture and file-size checks, C# 9 compatibility, Release build, all tests, headless smoke and both deterministic soak profiles. Unity Editor execution remains a local Play Mode validation step.