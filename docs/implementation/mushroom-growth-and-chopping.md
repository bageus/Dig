# Mushroom growth and direct chopping implementation

Статус: implementation slice rebased на текущий `main`; автоматические build/test evidence выполняются в PR #424.

Authoritative design: [`../design/mushroom-growth-and-chopping.md`](../design/mushroom-growth-and-chopping.md).
Tracking issue: [#423](https://github.com/bageus/Dig/issues/423).
Rebased implementation commit: `b2e2a7c9fe9bf5406eac51cbb4636789b71d8b82`.

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
- временные source-export workflow и технический PR не входят в итоговый diff.

## Проверки в PR

- Domain lifecycle, growth pause, takeover reset, drop matrix, skill bands и permanent blocked cells;
- Application direct start/arrival/swings/finalize/cancel, unit outputs и exactly-once skill grant;
- save/load mid-chop, active job cross-reference и v5→v6 migration;
- input router priority and reason codes;
- BuildingBox preview/confirmation ecology blocking;
- Unity source contracts;
- checked-in Play Mode fixture для двух demo sites, visible/absent renderer и Large scale.

Unity Editor/Play Mode нельзя считать пройденным, пока fixture реально не выполнен Unity Test Runner.
