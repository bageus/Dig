# Unity runtime startup and input diagnostics

The Unity demo now fails visibly instead of leaving an uninitialized HUD and a partial primitive.

- The HUD is created before simulation composition.
- Bootstrap records a named startup stage and reports exceptions in both the HUD and Console.
- Camera framing and resident/job counters are initialized before renderer work.
- Interaction and simulation drivers remain disabled until all initial renderers complete.
- Camera panning reads W/A/S/D and arrow keys directly instead of depending on Input Manager axis names.
- Jobs/reservations and navigation overlays use 3 and 4, including keypad equivalents.

A successful startup displays `Running` with a non-zero resident count. A failure displays `STARTUP FAILED [stage]` followed by the exception type and message; the full stack remains available in the Console and Editor.log.
