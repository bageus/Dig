# Unity tool-aware job assignment

## Purpose

The Unity runtime now composes digging and construction assignment with the same authoritative Inventory and Jobs services used by engine-independent tests.

The runtime does not infer a tool from a rendered model. It reads immutable Inventory snapshots through `InventoryAwareJobCandidateProvider`, asks `AssignAvailableJobsHandler` to choose a resident, and resolves the selected automatic or suggest-only policy before the Job claim.

## Runtime composition

After the building demo creates the resident tool inventory, `DigTerrainWorkSession.InitializeToolAwareJobAssignment` replaces the plain assignment handlers used by:

- dynamic digging designation jobs;
- BuildingBox packing jobs;
- BuildingBox assembly jobs.

Each handler keeps its existing candidate source and execution lifecycle. The wrapper only enriches candidates with authoritative tool readiness:

- an equipped matching tool receives the strongest priority;
- a safe matching carried tool may be switched automatically;
- unavailable or reserved tools do not receive the tool bonus;
- the selected tool stack is reserved by Jobs together with the resident and work target.

Mining prefers the pickaxe profile. Packing and assembly prefer the construction hammer profile.

## Runtime policy selection

The HUD exposes `Automatic` and `Suggest only` buttons. The runtime starts in `Automatic` mode.

The selected policy applies only to future assignment attempts. Existing claimed or in-progress Jobs keep their resident, tool reservation and Inventory state. `Suggest only` still selects and reserves the matching carried tool, but leaves Inventory locations unchanged and reports `Suggested` instead of `Switched`.

`JobToolPreparationModeControl` implements the optional Application `IJobToolPreparationModeSource`. Unity passes the same live source to all three tool-aware handlers. Command-only callers without a source continue to use `AssignAvailableJobsCommand.ToolPreparationMode` unchanged.

## Diagnostic ownership

`AssignAvailableJobsHandler` can publish its immutable `JobAssignmentReport` to an optional `IJobAssignmentReportSink`.

The shared `InMemoryExecutionJournal` implements that sink and retains only the latest successful assignment report for each Job id. Repeated assignment passes therefore do not append one diagnostic record per tick. A later successful reassignment for the same Job replaces the earlier decision.

The journal keeps successful assignment decisions only. A specialized Unity assignment pass can observe unrelated available Jobs that belong to another candidate provider, so retaining all `CandidateUnavailable` results would create misleading cross-system failure diagnostics. Immediate command callers still receive the complete report, including failures.

`JobOverlayPresenter.LoadIndexed` maps the per-Job reports into immutable HUD diagnostics containing:

- assignment tick and score;
- `AlreadyEquipped`, `Suggested`, `Switched`, or `None` preparation outcome;
- selected tool stack id.

For retained `Suggested` assignments, the presenter also maps a typed immutable `PrepareSuggestedTool` action. Presentation owns its enabled state and stable disabled reason. Unity renders the action and dispatches the typed command without reinterpreting diagnostic or reservation text.

Deleting or rebuilding the HUD, resident models, job markers, or equipment visuals cannot change Inventory locations, Job reservations, the selected future policy, or the recorded assignment result.

## Validation

Engine-independent tests verify that:

- `AssignAvailableJobsHandler` records a successful decision through the optional sink;
- the journal replaces the prior diagnostic for the same Job instead of growing per tick;
- the indexed presentation path shows the retained tool preparation outcome and tool stack;
- the policy control defaults to `Automatic` and changes explicitly;
- a live `Suggest` source suppresses switching even for an automatic command;
- a live `Automatic` source performs preparation even for a suggest command;
- typed Job actions expose enabled and disabled states without Unity-side inference.

The normal Quality workflow validates architecture boundaries, file sizes, C# 9 compatibility, Release build, all tests, headless smoke, and deterministic soak profiles. Unity Editor behavior still requires a local Play Mode check.
