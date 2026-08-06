# Continuous surface movement

Статус: `IMPLEMENTED`.

Tracking issue: [#669](https://github.com/bageus/Dig/issues/669).

Unity Play Mode verification остаётся открытой; этот документ нельзя повышать до
`VERIFIED` без фактически пройденного runtime-сценария.

## Rules

- Excavation, construction, materials and coarse tunnel topology remain voxel/cell based.
- An actor position is a deterministic `SurfacePose`: voxel face plus continuous local
  coordinates. `CellId` remains the containing navigation/world index, not a requirement
  that the actor stand at the cell centre.
- Residents, enemies, hamsters and worms may move to any point of a legal horizontal
  floor surface.
- Residents, cave monsters and spiders may climb exposed vertical faces.
- The external `NegativeZ` face at world depth `Z0` is never climbable.
- Other ground enemies, hamsters and worms cannot enter vertical surfaces.
- Movement simulation uses fixed-point thousandths of a cell. Unity may interpolate
  between confirmed poses, but must not own or invent the authoritative pose.

## Compatibility

- Existing cell routes remain the coarse corridor used by jobs, excavation and topology.
- A legacy `MoveTo(CellId)` places the resident at the centre of that cell's floor.
- New movement uses `MoveOnSurface(SurfacePose)` and emits `AgentSurfaceMoved` even when
  the containing cell does not change.
- Old saves without surface fields load at the centre of the saved cell's floor. New saves
  persist face and local `U/V` coordinates.

## Capability matrix

| Actor | Floor | Vertical face | External `Z0` face |
|---|---:|---:|---:|
| Resident | yes | yes | no |
| Cave monster | yes | yes | no |
| Spider | yes | yes | no |
| Other enemy | yes | no | no |
| Hamster | yes | no | no |
| Worm/grub | yes | no | no |

## Integration sequence

1. Domain surface pose, capability policy, snapshots and save compatibility.
2. Capability-aware coarse routes and cave-monster vertical patrol.
3. Continuous floor corridor generation and real-time steering in Application/Unity.
4. Continuous work/interaction points and local avoidance.
5. Surface graph transitions for floor-to-wall and wall-to-floor movement.
6. Runtime and Play Mode verification for excavation invalidation, save/load, combat,
   narrow tunnels and multiple actors.

Combat engagement now derives a precise, non-serialized pose from the selected
engagement cell, active weapon mode and current target cell. Melee approaches the
floor edge facing the target; ranged combat uses the stable floor centre. Approach
and attack resolution both require that same authoritative pose, so target movement
or pose invalidation returns execution to engagement selection before damage.

The first two items are represented by the current implementation. Direct resident
commands preserve the clicked X/Z point through the coarse route and commit that point
as the final authoritative `SurfacePose`, including destinations inside the resident's
current cell. Horizontal manual routes now approach and cross matching fixed-point
boundary poses instead of teleporting centre-to-centre. Spatial work routes and the
shared automatic movement used by residents, enemies and other autonomous actors now
use the same two-phase horizontal corridor and persist both confirmed poses. A vertical
coarse step now attaches an approved mover to the nearest legal wall, approaches and
crosses the matching cell boundary, and detaches at the matching floor edge before
horizontal movement or work. The domain command rejects vertical poses for explicit
ground-only movers, and `NegativeZ` at `Z0` is excluded during attachment selection.
Spatial excavation and mushroom chopping now resolve a deterministic floor point facing
the target, route the resident to that exact pose, and require the pose before work
begins. World-item pickup uses the same authoritative channel and requires the resident
to reach the centre of the item's floor cell before acquisition. Building-box assembly
uses the floor edge facing the construction site; packing, box pickup/relocation and
hauling use exact phase-dependent source or destination poses. Multi-step handlers
re-check the pose after every phase transition so acquisition cannot immediately become
a remote deposit. Production now resolves exact poses for output-package placement,
internal-stock acquisition, workstation processing and processed-material deposit.
Building supply resolves separate workstation, reserved-source and destination poses;
both execution paths gate every inventory or phase mutation on the authoritative pose.
Horizontal local avoidance is a presentation preference and never rejects or delays an
authoritative fixed-point micro-step. A conflicting target immediately uses the approved
visual overlap fallback; vertical climbers likewise never block each other. Runtime
verification remains required before continuous movement is verified in playable Unity.

When excavation removes full support beneath an active spatial worker, the authoritative
pose attaches to the nearest exposed vertical face instead of remaining on an imaginary
floor. Spatial work accepts that persisted vertical pose while support is absent. Once the
job no longer owns movement, the existing unsupported-resident recovery planner routes the
resident to the nearest reachable fully supported cell; ordinary traversal does not invent
a support-loss fall.
