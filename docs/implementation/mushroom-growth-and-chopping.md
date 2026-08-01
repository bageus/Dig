# Mushroom growth and direct chopping implementation

Статус: `IMPLEMENTED` — implementation slice rebased на текущий `main`; Quality run #5267 и Stage 2 source exports прошли.

Authoritative design: [`../design/mushroom-growth-and-chopping.md`](../design/mushroom-growth-and-chopping.md).
Tracking issue: [#423](https://github.com/bageus/Dig/issues/423).
Validated implementation head: `ec59dc944dadac73183039a1eecb77b9b672af73`.

## Реализованные владельцы

- `Dig.Domain.Ecology.MushroomState` владеет stable growth sites, стадией, deadline, generation, active chop owner и swing progress.
- ordinary `JobSystem` владеет `MushroomChopJobDefinition`, worker claim, ecology-target и work-position reservations.
- `InventoryState` создаёт каждую шляпку и ножку отдельной quantity-1 entity в cell гриба.
- `AgentSkillGrantService` выдаёт exactly-once `80` fixed-point units `skill.woodworking` после authoritative completion.
- `BuildingPlacementValidator` принимает отдельный immutable ecology-blocked set; world items не входят в этот set.
- Unity Presentation отображает стадии, collider только видимого гриба и анимированный axe cursor, но не владеет progress или drops.

## Runtime workflow

```text
selected resident + mushroom hover
-> ContextInputRouter: ChopMushroom
-> PrepareResidentsForDirectCommand
-> StartDirectMushroomChopCommand
-> ordinary JobSystem travel
-> ArriveAtMushroomCommand
-> CompleteMushroomSwingCommand per work cadence
-> CompleteMushroomChopCommand
-> AbsentRegrowing + unit drops + Woodworking grant
-> growth timer returns site to Tiny
```

Takeover сначала отменяет старый job, освобождает target и полностью очищает swing progress. Новый worker получает новый deterministic required-swing roll. На время active job, включая travel, growth deadline заморожен; cancel сдвигает deadline на полную длительность паузы.

## Save/load

Save format v6 содержит mushroom section со stage/deadline/generation/active job/worker/swing progress. `MushroomChopJobSaveCodec` сохраняет target, fixed work position, generation snapshot и required swings. v5 migration создаёт пустой mushroom section. Loader проверяет cross-reference active site ↔ active mushroom job ↔ assigned worker.

## Demo и Presentation

- bootstrap создаёт два stable sites после completed demo buildings: один supported surface cell Z0, один supported lower-cave cell;
- mushroom cells исключают existing buildings и world items;
- Tiny/Small/Medium/Large имеют разные размеры; Large выше одной world cell и немного выше resident visual;
- `AbsentRegrowing` не имеет visual или collider;
- cap/leg definitions добавлены в обычный demo item catalog, поэтому drops сразу используют существующий pickup pipeline.

## Исправление PR #424

- ветка перенесена поверх `main` после merge PR #425;
- сохранён утверждённый depth-based BuildingBox workflow: Z0 relocation остаётся world-item placement, ecology blocking применяется к Z1–Z3 assembly footprint;
- исправлена незакрытая область имён в `JobValues.cs`, которая останавливала Release build;
- исправлено ожидание `StageStartedTick` после переходов `Tiny -> Small -> Medium -> Large`;
- save migration tests учитывают обязательный шаг `save.v5_to_v6.mushrooms`;
- mushroom application fixtures используют реальные Woodworking bands и проверяют grant как увеличение исходного уровня на `0.8`;
- временные source-export workflow и технический PR не входят в итоговый diff.

## Unity Play Mode compiler boundary

`Dig.Unity.PlayModeTests` собирается отдельной assembly. Поэтому test code не может напрямую вызывать `internal DigMushroomRenderer.Render` или читать `internal ActiveCount`, даже когда runtime и test namespaces совпадают. Play Mode regression теперь вызывает эти members через существующий reflection helper и продолжает проверять observable geometry/collider/absence behavior, не расширяя production API только ради теста.

Repository source contract запрещает прямые `renderer.Render(...)` и `renderer.ActiveCount` в этой test assembly, чтобы ошибка снова не проявлялась только после входа Unity в Safe Mode.

## Выполненные проверки

На head `ec59dc944dadac73183039a1eecb77b9b672af73` фактически прошли:

- architecture, file-size и C# compatibility gates;
- все Unity source-contract validators;
- .NET restore и Release build;
- полный `Dig.Tests`;
- headless smoke;
- standard deterministic soak;
- large-settlement deterministic soak;
- Stage 2 v2/v3 source exports.

Unity Editor/Play Mode нельзя считать пройденным, пока fixture реально не выполнен Unity Test Runner. Поэтому статус не повышается до `VERIFIED`.

## Runtime interaction and visual follow-up (2026-07-27)

A screenshot-driven verification found four observable regressions that were not covered by the original partial Play Mode fixture:

- mushroom primitives used the built-in `Standard` shader inside the URP project, so their materials rendered magenta;
- mushroom dimensions were authored in unscaled world units while residents are rendered under a `0.5` world scale, making `Large` much taller than a resident;
- axe cursor resolution scanned mushroom hits before other objects, but LMB processed completed buildings first, so an overlapping building could consume the click after an axe cursor had already been shown;
- mushroom jobs projected as generic work and had no hover highlight, target-facing or repeated work pose.

The follow-up keeps the Domain/Application mushroom state unchanged and fixes the Unity/presentation adapters:

- `DigMushroomVisual` uses `Universal Render Pipeline/Lit`, vertical identity transforms, a walk-surface-based collider and stage sizes ending at `0.84` world units for `Large`;
- a per-renderer property block supplies reachable-target hover highlighting without mutating shared authoritative state;
- pointer hover and axe cursor share `TryResolveReachableMushroomHit`, while the raw mushroom LMB branch now precedes completed-building handling;
- `JobOverlayViewModel.IsMushroomChop` projects the typed job target without claiming that chopping requires an equipped Inventory tool;
- resident activity projects `GatherMushroom` / `resident.activity.gathermushroom` (`Добывает гриб`);
- active `PerformWork` mushroom jobs face their XYZ target and drive a repeating `Dig` rig pose until the authoritative job leaves that stage.

Regression coverage now includes:

- source contracts for mushroom-before-building click priority, shared hover/cursor resolver, URP shader, vertical/base-aligned collider, bounded `Large` size, hover property block, typed status and work animation;
- .NET presenter tests for mushroom XYZ target, `IsMushroomChop` and `Добывает гриб` localization key;
- an expanded Unity Play Mode fixture for two demo sites, direct start/arrive/swings/completion, exact Large drops, same-cell regrowth, URP material, base alignment, resident-relative height, hover highlight and absent-stage removal.

The system remains `IMPLEMENTED` until this expanded fixture is actually executed by Unity Test Runner. Source-contract success alone is not runtime verification.

## World orientation and material-targeting follow-up (2026-07-28)

The second runtime screenshot exposed two gaps in the previous source and Play Mode contracts:

- `DigUnityBootstrap` rotates the shared side-view root by 90 degrees, while `DigMushroomRenderer` parented its world-projected root with `worldPositionStays: false`; local identity therefore inherited the bootstrap rotation and laid the mushroom on the floor;
- pointer resolvers scanned the complete sorted hit stack independently for each target type, so a regrown mushroom behind a foreground cap/leg could still win the mushroom branch even though the player was pointing at a physical material.

The correction keeps Domain/Application completion unchanged and fixes the Unity adapter boundary:

- `DigMushroomRenderer` now mirrors the world-item renderer and keeps its root in world orientation with `SetParent(..., worldPositionStays: true)`;
- mushroom, BuildingBox, generic-item and completed-building resolvers stop at the first relevant foreground item/mushroom boundary instead of scanning through it;
- reachable hover and pickup reuse the same foreground-aware item resolver;
- cap/leg remain quantity-one Inventory entities whose `ItemDefinition.ItemInteractionProfile` resolves ordinary pickup; they never acquire `DigMushroomVisual` identity.

Regression coverage now rotates the renderer parent exactly like the runtime bootstrap, verifies world-up stem/collider orientation, renders completion drops as pickup-only world visuals, and exercises a physical foreground item ray before a regrown mushroom. The system remains `IMPLEMENTED` until Unity Test Runner executes the checked-in Play Mode scenarios.

## Pointer-hit syntax regression (2026-07-28)

A subsequent Unity Safe Mode compile exposed `CS0106` at the declarations of `TryProjectResidentBounds`, `DistanceToRect` and `ComparePointerHits`, followed by `CS1513` at end of `DigWorldInteraction.PointerHits.cs`.

The foreground-targeting follow-up closed the resident candidate comparison `if` but omitted the closing brace of the surrounding `for` loop in `TryResolveAgentNearPointer`. PR #458 restores that method boundary without changing pointer priority or targeting behavior.

`PointerHitUnitySyntaxContractTests` now verifies balanced braces in the partial file and requires the exact `if -> for -> agent assignment -> next method` boundary. On code head `d540b6bdfbece8509437032cfd9202605e78a0d5`, Quality #5755 (`30315315950`), Stage 2 v2 #455 (`30315315946`) and Stage 2 v3 #460 (`30315315930`) passed, including Release build, full `Dig.Tests`, headless smoke and both deterministic soaks.

The fix addresses the reported Unity parser failure at its source. Status remains `IMPLEMENTED` until Unity Editor/Test Runner executes the complete interaction workflow.

## Z0-Z3 depth-slab correction (2026-07-28)

A third runtime screenshot showed the upright mushroom behind the `Z=3` back plane. The site cell was valid; the Unity adapter added `FrontOffset = -0.66f` after `ResidentWorldPosition`, while one logical depth step is only `-0.55f`. Presentation therefore moved every mushroom by more than one complete Z layer and could place a `Z=3` site outside the four-layer world.

The correction removes the independent mushroom depth offset. `DigMushroomRenderer` now uses the exact `DepthOrigin + CellId.Z * DepthSpacing` projection already returned by `ResidentWorldPosition`; only the walk-surface Y correction remains. Domain, jobs, navigation, save data and demo site cells are unchanged.

Regression coverage now:

- rejects any `FrontOffset` in the mushroom renderer source contract;
- requires the renderer position to add `0f` on world Z;
- renders Large fixtures at `Z=0`, `Z=1`, `Z=2` and `Z=3` under the rotated bootstrap parent;
- verifies visual/collider center against each authoritative depth projection and keeps collider bounds inside the corresponding half-spacing slab.

The system remains `IMPLEMENTED` until Unity Test Runner executes the checked-in Play Mode scenario.

## Mushroom movement planner duplicate regression (2026-07-28)

A Unity Safe Mode compile reported `CS0111` in `DigTerrainWorkSession.Mushrooms.cs`: the partial `DigTerrainWorkSession` declared `TryPlanMushroomMovement` twice with the same parameters.

The duplicate came from overlapping mushroom and campfire integration changes. `DigTerrainWorkSession.Mushrooms.cs` contained a simplified planner while `DigTerrainWorkSession.MushroomNavigation.cs` already owned the complete route-planning implementation, including `_routePlans` diagnostics. The correction removes the duplicate from `Mushrooms.cs` and keeps `MushroomNavigation.cs` as the single owner. Domain/Application mushroom behavior, work-position selection and navigation policy are unchanged.

`MushroomMovementPlannerSourceContractTests` now scans every `DigTerrainWorkSession*.cs` partial and requires exactly one `bool TryPlanMushroomMovement(...)` declaration, owned by `DigTerrainWorkSession.MushroomNavigation.cs`, with route-plan projection preserved.

## Agent-reservation runtime regression (2026-07-28)

A Unity runtime screenshot exposed `InvalidOperationException: Validated mushroom start failed: jobs.agent_unavailable` from `StartDirectMushroomChopCommandHandler`. Two stale ownership paths converged on the same failure:

- building-production synchronization reused an `AgentViewModel` availability snapshot after production/supply assignment in the same tick, so the automatic grilled-mushroom dependency could select a resident whose `ReservationKey.ForAgent` was already owned;
- `PrepareResidentsForDirectCommand` collected only a whitelist of excavation/item/barrel jobs, so a selected resident could retain another nonterminal production, supply or logistics assignment before the direct mushroom claim.

The correction keeps existing gameplay rules and ownership boundaries:

- automatic-work availability now also reads the current `JobSystem` agent reservation ledger;
- direct-command preparation releases every nonterminal job assigned to the selected resident, while preserving specialized cancellation for pickup, mushroom, barrel and BuildingBox lifecycles;
- all route caches for the replaced job are cleared through the shared `RemoveAllRoutePlans` owner;
- mushroom start preflights the worker reservation before consuming a deterministic swing draw and returns typed `jobs.agent_unavailable` instead of throwing;
- any later start-stage rejection cancels the newly created job, releases reservations and leaves the mushroom site unchanged.

Regression coverage includes an Application test for an already-reserved worker and Unity source contracts for current reservation-ledger availability, complete direct-assignment collection and full route cleanup. The system remains `IMPLEMENTED` until the checked-in runtime scenarios execute in Unity Test Runner.
