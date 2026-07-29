# Unity runtime verification gate

Status: repository gate implemented; licensed execution remains open in [#511](https://github.com/bageus/Dig/issues/511).

Authoritative specification: [`../design/unity-runtime-verification-and-release-gates.md`](../design/unity-runtime-verification-and-release-gates.md).

## Implementation

`.github/workflows/unity-playmode.yml` keeps its stable workflow name for existing checks and now runs GameCI `testMode: All`, which selects EditMode and PlayMode together when activation is configured.

Required fixtures:

- `UnityProjectEditModeTests.Main_scene_is_registered_with_runtime_bootstrap` checks enabled build-scene registration and one bootstrap owner;
- `RepresentativeSceneConsolePlayModeTests.Main_scene_bootstraps_without_console_errors` loads production `Main.unity`, waits for simulation/input startup, checks representative projections and writes `runtime/representative-scene.log` after `LogAssert.NoUnexpectedReceived`.

`tools/quality/validate_unity_runtime_evidence.py`:

- parses NUnit-style Unity XML recursively;
- rejects missing XML, zero tests and every non-`Passed` case;
- requires the named EditMode and PlayMode acceptance tests;
- requires the structured representative-scene runtime log;
- writes schema-versioned `verified`, `failed` or `blocked` JSON;
- has an executable self-test run by normal Quality CI.

Artifacts:

- `unity-editmode-playmode-results` retains raw GameCI output;
- `unity-runtime-evidence` retains the machine-readable manifest on every run;
- missing files are errors when licensed execution was attempted.

When activation is absent, the workflow writes `status: blocked`. Its green conclusion only means the guard and manifest path executed; it is not runtime verification.

## Tracking transfer

Repository implementation acceptance for #14 and #15 is complete. Their remaining licensed execution requirements move to #511, which also owns runtime baseline/budget calibration. This allows umbrella roadmap #16 to close without claiming `VERIFIED` for any Unity system.

## Validation

Available repository checks:

- `python tools/quality/validate_unity_runtime_evidence.py --self-test`;
- `python tools/quality/check_quality.py`;
- `python tools/quality/check_quality_workflow_contracts.py`;
- `python tools/quality/check_unity_source_contracts.py`;
- `python tools/quality/check_unity_excavation_playmode_contracts.py`;
- `.NET` source-contract tests, including `UnityRuntimeEvidenceGateTests`.

Actual EditMode/PlayMode result XML cannot be claimed until #511 receives licensed execution.
