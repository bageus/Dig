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

Direct resident commands preserve the clicked X/Z point through the coarse route and
commit that point as the final authoritative `SurfacePose`, including destinations inside
the resident's current cell. Horizontal resident/enemy route execution uses the approved
cell-corridor behaviour: when a movement step is due, the actor commits the matching exit
boundary pose, then the entry pose in the adjacent cell. It does not spend additional
simulation ticks walking through a sequence of `SurfacePoseSteering` micro-poses inside
the same cell. Unity presentation may interpolate between those confirmed authoritative
poses so the visible actor moves smoothly without changing movement ownership.

A vertical coarse step attaches an approved mover to the nearest legal wall, approaches
and crosses the matching cell boundary, and detaches at the matching floor edge before
horizontal movement or work. The domain command rejects vertical poses for explicit
ground-only movers, and `NegativeZ` at `Z0` is excluded during attachment selection.

Spatial excavation and mushroom chopping resolve a deterministic floor point facing the
target and require that pose before work begins. Once the resident is in the destination
cell, the runtime commits the required work pose directly rather than inserting extra
in-cell steering ticks. World-item pickup is intentionally less strict: direct pickup and
automatic hauling may acquire once the resident is in the source cell on a fully supported
floor; local `SurfaceU/SurfaceV` offsets do not block acquisition. The authoritative
world-to-inventory transfer completes in the same simulation tick that pickup arrival is
accepted.

Building-box assembly uses the floor edge facing the construction site; packing, box
pickup/relocation and hauling use phase-dependent source or destination poses. Multi-step
handlers re-check the pose after every phase transition so acquisition cannot immediately
become a remote deposit. Production resolves poses for output-package placement,
internal-stock acquisition, workstation processing and processed-material deposit.
Building supply resolves separate workstation, reserved-source and destination poses;
both execution paths gate inventory or phase mutation on the authoritative pose.

The 2026-08-09 cell-route restoration supersedes the short-lived resident/enemy runtime
behaviour that advanced toward horizontal boundaries and final points through bounded
200-unit microsteps. That implementation produced visible stationary stepping followed by
abrupt cell transitions and also delayed source-cell pickup completion. The fixed-point
`SurfacePose` model remains authoritative, but resident/enemy coarse route execution is
restored to direct boundary/entry commits. Hamsters and worms retain their persisted free
floor `SurfacePose`; deterministic wandering may select non-centre floor points and their
presentation projects those exact coordinates. Legacy ecology saves still load at the
centre of the saved floor cell and acquire a free pose on later movement.

When excavation removes full support beneath an active spatial worker, the authoritative
pose attaches to the nearest exposed vertical face instead of remaining on an imaginary
floor. Spatial work accepts that persisted vertical pose while support is absent. Once the
job no longer owns movement, the existing unsupported-resident recovery planner routes the
resident to the nearest reachable fully supported cell; ordinary traversal does not invent
a support-loss fall.
