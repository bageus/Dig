# Resident construction equipment

Equipment profiles now control resident visuals and deterministic activity intervals.

Mining uses a base interval of three ticks. Construction uses a base interval of two ticks. The demo pickaxe changes mining to one tick, and the demo hammer changes construction to one tick. Other combinations keep the base value.

Assembly and packing receive an optional interval decision from authoritative inventory snapshots. Movement and lifecycle transitions remain unchanged; the interval applies only when adding progress.

The first demo resident carries both tools and can equip one at a time. The resident panel shows the current item, effective intervals, and speed multipliers.

Tests cover the projection, interval calculation, and progress handlers. Quality validation covers architecture, compatibility, Unity modules, build, tests, smoke, and deterministic soak runs.
