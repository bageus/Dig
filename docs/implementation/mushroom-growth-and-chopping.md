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
