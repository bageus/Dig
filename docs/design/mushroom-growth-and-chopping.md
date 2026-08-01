# Рост, прямая рубка и повторное появление грибов

Статус: `APPROVED`.

Tracking issue: [#423](https://github.com/bageus/Dig/issues/423).

Связанные системы:

- [`skills-and-progression.md`](skills-and-progression.md);
- [`contextual-input-cursors-and-selection.md`](contextual-input-cursors-and-selection.md);
- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md);
- [`demo-starting-scenario.md`](demo-starting-scenario.md);
- [`../implementation/simulation-runtime.md`](../implementation/simulation-runtime.md);
- [`../architecture/systems-core.md`](../architecture/systems-core.md).

## 1. Назначение и границы

Система создаёт постоянные mushroom growth sites, которые проходят стадии роста, могут быть срублены выбранным resident через прямой приказ, создают физические материалы в своей клетке и затем запускают повторный цикл роста в том же месте.

В текущий scope входят:

- deterministic growth;
- direct chopping выбранным resident;
- skill-dependent число ударов;
- exclusive worker takeover;
- drops, pickup, save/load, building-placement blocking;
- два demo mushroom sites и Unity presentation.

Automatic job generation и самостоятельный выбор грибов свободными residents намеренно не входят в этот slice. Будущая automatic policy обязана использовать тот же authoritative mushroom state и общий Jobs lifecycle, а не создавать второй owner.

## 2. Подтверждённый пользовательский workflow

### Рост

1. Mushroom site имеет постоянные `SiteId` и logical XYZ cell.
2. После создания или завершения absent/regrowth stage появляется tiny mushroom.
3. Через каждые 15 игровых минут он последовательно переходит `Tiny -> Small -> Medium -> Large`.
4. Для автоматических тестов используется отдельный test duration profile: одна секунда на переход.
5. `Large` является финальной видимой стадией и остаётся большой, пока его не срубят.
6. После успешной рубки site переходит в `AbsentRegrowing`, visual гриба исчезает, а site identity и blocked building cell сохраняются.
7. После regrowth duration site снова становится `Tiny` в той же cell.

### Прямой приказ рубки

1. Игрок выбирает resident.
2. Pointer на доступном видимом грибе показывает слегка анимированный топор, а сам гриб получает заметную hover-подсветку.
3. Hover, cursor и LMB используют одно и то же resolved mushroom target. Если показан топор, тот же LMB обязан создать один direct chopping command и не может вместо этого выбрать перекрывающее гриб строение или выдать другое действие.
4. LMB создаёт один direct chopping command и один ordinary chopping job, сразу предназначенный выбранному resident.
5. Resident освобождается от несовместимого небоевого direct action, получает route к допустимой work position и идёт к грибу. Work position обязана находиться на той же высоте `Y`, быть соседней по `X` или depth `Z` и иметь полную ровную actor support surface. Вертикальные `Y±1`, shaft-gap и partial-support клетки запрещены. Если боковые `X±1` клетки являются пропастью, resolver обязан рассмотреть поддерживаемые `Z±1` позиции за/перед грибом до blocked result. Требование полной опоры относится к конечной stationary work position и повторно проверяется перед swing; transit route использует обычную Navigation policy и может включать разрешённые vertical climb, shaft и depth transitions.
6. На work position resident выполняет authoritative swings.
7. После требуемого числа swings одна atomic completion transaction:
   - переводит mushroom site в `AbsentRegrowing`;
   - создаёт точные drop unit entities в той же logical cell;
   - завершает job и освобождает worker/site/work-position reservations;
   - выдаёт resident `0.8` point навыка `skill.woodworking` через idempotent grant source.
8. Drops становятся обычными world items и могут быть подняты существующим pickup workflow.
9. После completion шляпки и ножка не являются mushroom targets: у них нет `DigMushroomVisual`, mushroom collider или chop command. Если physical drop находится перед regrown mushroom в pointer hit stack, foreground world item блокирует axe target; обычный `LMB` использует общий item-profile pickup workflow.

### Повторный direct order

Один site одновременно имеет не более одного active chopping job/worker claim. Если другому resident выдаётся direct order рубить тот же гриб:

1. текущий worker прекращает эту работу;
2. его job/agent/work-position reservations освобождаются;
3. target передаётся новому direct job/worker;
4. два residents не выполняют swings по одному site одновременно.

Накопленный chop progress при takeover сбрасывается. Новый resident получает новое полное required swing count, рассчитанное по его текущему Woodworking через следующий deterministic random draw. То же правило действует при cancel/interruption: незавершённые удары не сохраняются.

### Blocked/failure/retry

- `AbsentRegrowing` не имеет target/collider и не принимает direct chop command;
- unreachable или unsupported work position возвращает typed reason и не создаёт успешную axe feedback;
- потеря полной опоры после создания job отменяет текущую chop attempt до следующего swing; retry заново разрешает same-height `X/Z` work position;
- смерть, удаление или недоступность worker освобождает claim без удаления site;
- failure одного mushroom job не останавливает simulation loop и другие mushroom sites;
- retry не reroll-ит уже сохранённые deterministic inputs и не создаёт повторные drops/skill grant;
- если site был срублен другим commit до текущего completion, stale job завершается typed conflict без output.

## 3. Владение состоянием

- `MushroomState`/Ecology владеет site identity, fixed cell, stage, stage-start/deadline tick, chop state, required/completed swings, active chopping job id и completion generation.
- `JobSystem` владеет job lifecycle, assigned resident, retry state и reservations resident/site/work position.
- Agents/Skills владеет значением `skill.woodworking`, capacity redistribution и applied source keys.
- `InventoryState` владеет отдельными cap/leg unit entities, location и pickup reservations.
- World/Navigation предоставляет terrain, reachability и допустимые work positions без копирования mushroom lifecycle.
- Buildings получает immutable blocked mushroom-site cells при placement validation.
- Presentation владеет visual prefab/stage projection, axe cursor animation, chop animation, hover, status и reason text, но не growth/chop progress.

## 4. Модель данных

```text
MushroomDefinition
- DefinitionId
- StageDurations
- StageVisualProfiles
- StageDropProfiles
- WorkPositionPolicy
- HitBands
- SkillGrantProfile
- CapItemId
- LegItemId

MushroomSite
- SiteId
- DefinitionId
- Cell
- Stage
- StageStartedTick
- NextStageTick?
- GrowthGeneration
- ActiveChopJobId?
- RequiredSwings?
- CompletedSwings
- Version

MushroomChopJobDefinition
- JobId
- SiteId
- TargetCell
- WorkPosition
- Stage/Generation validation token
- Priority
- RetryPolicy
- CreatedTick
```

Stable content IDs:

- `skill.woodworking` — существующий authoritative skill;
- `material.mushroom_leg` — существующий ID ножки, уже используемый production content;
- `material.mushroom_cap` — stable ID шляпки для нового content slice;
- `ecology.mushroom.common` — mushroom definition;
- demo sites используют отдельные stable entity IDs, не display names.

Каждый drop создаётся через unit-item API с `Quantity == 1`. Две шляпки являются двумя Inventory entities с разными stable IDs.

## 5. Commands, events и queries

Commands:

- `AdvanceMushroomGrowthCommand`;
- `StartDirectMushroomChopCommand`;
- `AdvanceMushroomChopSwingCommand`;
- `CompleteMushroomChopCommand`;
- `ReleaseMushroomChopWorkerCommand`;
- save restore/migration commands через существующий save composition.

Events:

- `MushroomStageChanged`;
- `MushroomChopStarted`;
- `MushroomChopWorkerReplaced`;
- `MushroomChopSwingCompleted`;
- `MushroomChopped`;
- `MushroomDropsCreated`;
- обычные `JobStatusChanged` и skill grant events.

Queries:

- mushroom sites/stages and timers;
- direct chop target decision and reason;
- blocked building cells;
- active job/worker/progress;
- stage visual and drop preview diagnostics.

## 6. Состояния и переходы

```text
Tiny --duration--> Small --duration--> Medium --duration--> Large
 ^                                                    |
 |                                                    | successful chop
 +---------------- AbsentRegrowing <------------------+
```

Successful chop разрешён на каждой видимой стадии. Confirmed drop table:

| Stage | Drops |
|---|---|
| `Tiny` | 1 `material.mushroom_cap` |
| `Small` | 1 `material.mushroom_cap` |
| `Medium` | 2 `material.mushroom_cap` unit entities |
| `Large` | 2 `material.mushroom_cap` + 1 `material.mushroom_leg` unit entities |
| `AbsentRegrowing` | drops отсутствуют; stage не является chop target |

Production duration каждого перехода — 15 игровых минут. Test configuration подменяет duration data на одну секунду, не меняя state machine или используя Unity frame count как owner.

## 7. Число ударов и Woodworking

При старте допустимой chop attempt текущее значение `skill.woodworking` переводится из fixed-point units в design points и выбирает диапазон:

| Woodworking points | Required swings |
|---:|---:|
| 0–10 | 6–8 |
| 11–20 | 5–6 |
| 21–40 | 3–5 |
| 41–60 | 2–3 |
| 61–80 | 1–2 |
| >80 | 1 |

Точное значение внутри диапазона выбирается deterministic named random stream и сохраняется в authoritative chop state. Save/load и retry не reroll-ят required swings.

Успешная рубка создаёт один grant:

```text
AgentSkillId = skill.woodworking
RequestedAmount = 80 units = 0.8 point
SourceId = mushroom chop completion generation
```

Замах/animation callback, отменённая работа и stale completion опыт не начисляют.

## 8. Input, UI и Presentation

После UI shielding и active modal/placement modes:

1. при selected resident hover по доступному видимому mushroom target разрешает direct chop;
2. cursor становится слегка анимированным топором;
3. LMB выдаёт только mushroom chop command и не создаёт move/excavation/object-selection command тем же event;
4. недоступный/unreachable/absent target оставляет default cursor и показывает typed reason;
5. resident status во время travel и work показывает «Добывает гриб»;
6. во время `PerformWork` resident разворачивается к mushroom target и проигрывает повторяющуюся chopping/dig pose на authoritative cadence;
7. visible stage и current chop progress обновляются из authoritative snapshot;
8. mushroom geometry стоит вертикально основанием на walk surface; URP presentation не может отображаться magenta из-за неподдерживаемого shader;
9. `Large` визуально только немного выше resident: ориентир — около 110% высоты resident interaction collider, а не кратно выше гнома;
10. `AbsentRegrowing` не имеет mushroom geometry, collider или direct interaction target.
11. Физическая шляпка/ножка после рубки остаётся foreground world-item target: она может подсвечиваться/подниматься как материал и никогда не маршрутизируется в `ChopMushroom`, даже если site уже успел отрасти в той же cell.
12. Mushroom visual и collider располагаются внутри depth-volume своей authoritative `CellId.Z`: центр по глубине совпадает с `DepthOrigin + Z * DepthSpacing`, декоративный offset не может сдвигать гриб в соседний Z-слой или за заднюю границу `Z=3`.

Автоматический job UI и auto-designation гриба остаются будущим scope.

## 9. Зависимости и конфликты

- Direct chop имеет тот же общий принцип player-direct action, что direct excavation: оно заменяет несовместимое небоевое action выбранного resident, но не combat/self-defense.
- Site reservation key запрещает двух simultaneous workers.
- Новый direct order для того же site атомарно release/cancel-ит старый nonterminal chopping job перед claim нового.
- Mushroom site cell входит в building blocked-cell query на всех стадиях, включая absent/regrowth.
- World items не входят в этот blocked set: caps, legs и другие items могут лежать в mushroom cell и не мешают regrowth.
- Mushroom visual/collider не становится Navigation occupancy; work position выбирается рядом с site.
- На всём протяжении active chopping job, включая travel, stage deadline заморожен. При cancel/interruption оставшееся stage time сохраняется сдвигом deadline на длительность паузы. Successful chop заменяет старый deadline новым `AbsentRegrowing` deadline.

## 10. Инварианты

- один `SiteId` всегда связан с одной и той же logical XYZ cell;
- chopping не удаляет site identity;
- `Large` не переходит в другую видимую стадию без chop;
- один site имеет не более одного active chopping job и worker;
- один completion generation создаёт drops и skill grant максимум один раз;
- drop quantities зависят только от authoritative stage policy, не от animation;
- каждый drop — отдельная Inventory unit entity quantity 1;
- cap/leg drop не имеет mushroom target identity и не может принимать chop command;
- foreground drop collider блокирует выбор regrown mushroom за ним, пока pointer направлен на материал;
- item pickup не изменяет mushroom growth state;
- building footprint никогда не занимает mushroom site cell, даже когда гриб отсутствует;
- world item placement в site cell разрешён;
- два demo sites имеют независимые timers, jobs и drops;
- Presentation не хранит authoritative growth/chop progress.
- resident никогда не выполняет mushroom swing в воздухе, на вертикальной соседней клетке или над частично выкопанной опорой.
- visual/collider каждого гриба остаётся внутри depth slab его logical `Z=0..3`; presentation offset не меняет слой и не выводит geometry за заднюю плоскость мира.

## 11. Save/Load и migration

Сохраняются:

- site id, definition id/version и fixed cell;
- stage, stage-start/next-stage tick и growth generation;
- active chop job id, required/completed swings и captured deterministic inputs;
- Jobs assignment/stage/retry/reservations;
- applied chop-completion skill source key;
- drop unit entities через Inventory save data;
- demo bootstrap marker/stable IDs, чтобы load не создавал дополнительные sites.

Selection, hover, cursor animation и transient axe animation не сохраняются.

Load должен:

- продолжать stage timer без real-time catch-up вне simulation clock policy;
- не reroll required swings;
- не дублировать drops/XP после committed completion;
- восстановить blocked building cells и active worker claim;
- безопасно release/block stale job reference с typed diagnostic.

Старые saves без mushroom section получают deterministic migration policy/fixture seeding, которая будет определена при реализации save version. Новая session создаёт ровно два demo sites; load существующего save не запускает bootstrap повторно.

## 12. Диагностика

Inspector/logs показывают:

- site/definition ID и cell;
- current stage, stage start/deadline и remaining ticks;
- growth generation;
- active job/worker/work position;
- skill points/band и deterministic required swings;
- completed/remaining swings;
- reservation owner;
- last command/event/transition;
- drop profile and generated unit IDs;
- skill grant source/applied state;
- building blocked-cell reason;
- blocked/failure/retry reason.

## 13. Тестовая матрица

Domain unit:

- every stage transition and Large persistence;
- exact drop table and unit identities;
- all Woodworking bands and deterministic range boundaries;
- exclusive job owner/takeover;
- exactly-once drops and 80-unit grant;
- building blocked cell in visible/absent stages.

Application/integration:

- direct command route -> swings -> completion;
- unreachable, stale target, worker removal, retry;
- two sites and two residents concurrently;
- pickup drops while site regrows;
- items coexist with regrowth;
- building placement rejection but item placement success.

Deterministic/save:

- production 15-minute durations and test one-second profile;
- frame partition independence;
- save/load each stage and mid-chop;
- no reroll, duplicate output or duplicate XP;
- legacy migration and demo bootstrap idempotency.

Unity Play Mode:

- one surface and one lower-cave mushroom;
- four visible sizes, Large slightly above resident height;
- selected resident hover -> highlighted mushroom + animated axe cursor;
- overlapping building/mushroom hit stack keeps hover/cursor/LMB parity and starts chopping;
- LMB -> status «Добывает гриб» -> travel -> repeated chop animation -> disappearance;
- mushroom base remains on walk surface, supported shader is not magenta, Large is only slightly taller than resident;
- для fixture-sites на `Z=0`, `Z=1`, `Z=2` и `Z=3` visual/collider центр совпадает с projection этой клетки и не пересекает соседний depth slab;
- exact physical drops visible/raycastable/pickable;
- regrowth in same cell;
- second resident takeover;
- building ghost invalid on site while item drop remains valid.

## 14. Acceptance

- fresh demo session contains exactly two stable mushroom sites: surface and lower cave;
- each grows `Tiny -> Small -> Medium -> Large` at configured simulation durations;
- Large stands indefinitely until chopped;
- direct axe cursor, hover highlight and click use one resolved target decision and one command;
- когда axe cursor виден, перекрывающее строение или другой world target не перехватывает тот же LMB;
- mushroom visual вертикален, стоит на walk surface, использует поддерживаемый URP shader и `Large` лишь немного выше resident;
- mushroom visual/collider находится в depth slab authoritative `CellId.Z` для каждого `Z=0..3`; запрещён отдельный front/back offset, который переносит его в соседний слой или за `Z=3`;
- active resident status отображается как «Добывает гриб», а `PerformWork` показывает повторяющуюся рубящую pose;
- hit bands use current Woodworking and deterministic required swings;
- one site cannot be chopped concurrently by two residents;
- successful chop atomically removes visible mushroom, creates stage drops и gives `0.8` Woodworking;
- caps/leg are ordinary pickable unit items, have no mushroom target/collider identity and cannot be chopped;
- when a drop is in front of a regrown mushroom, pointer hover/cursor/LMB resolve the drop first; ordinary `LMB` starts the shared item-profile pickup instead of another chop;
- site reappears in the same cell after absent/regrowth duration;
- building placement is blocked in the permanent site cell, item placement is not;
- save/load/retry never duplicate mushroom drops or skill progress;
- Play Mode validates the complete observable workflow, not only source contracts.

## 15. Решённые вопросы

- **Q-MUSH-001 = A:** `AbsentRegrowing` не имеет target/collider, не рубится и не создаёт drops; после timer возвращается `Tiny`.
- **Q-MUSH-002 = B:** takeover, cancel и interruption сбрасывают completed swings. Новый direct worker получает новое полное required swing count по своему текущему Woodworking.
- **Q-MUSH-003 = A:** stage и remaining growth time замораживаются на всё время active chop job, включая travel. После cancel/interruption deadline сдвигается на длительность паузы; successful chop запускает новый absent/regrowth timer.

## 16. Журнал решений

| Дата | Решение | Кто подтвердил | Изменённые разделы/issues |
|---|---|---|---|
| 2026-07-27 | Зафиксированы site lifecycle, четыре видимые стадии, 15 игровых минут/1 секунда test profile, direct axe workflow, Woodworking hit bands, +0.8 grant, stage drops, exclusive worker takeover, permanent building block и два demo sites. Противоречия вынесены в Q-MUSH-001..003. | Пользователь | Все разделы, #423 |
| 2026-07-27 | Q-MUSH-001=A: absent не интерактивен; Q-MUSH-002=B: takeover/interruption сбрасывает progress; Q-MUSH-003=A: рост и remaining duration замораживаются на время active chop. Статус повышен до APPROVED. | Пользователь | Workflow, state machine, conflicts, save/test acceptance, #423 |
| 2026-07-27 | По runtime screenshot подтверждены обязательные исправления presentation/input: вертикальная установка на walk surface, URP-compatible material, Large около 110% resident height, hover highlight, единый hover/cursor/LMB target без перехвата overlapping building, статус «Добывает гриб» и chopping pose. | Пользователь | Workflow, Input/UI/Presentation, Play Mode acceptance, #423 |
| 2026-07-28 | По повторному runtime screenshot уточнено: side-view bootstrap rotation не должна передаваться mushroom root; growing mushroom обязан оставаться world-upright. После рубки cap/leg — только foreground pickable materials без mushroom identity; они блокируют axe target для regrown site за ними. | Пользователь | Workflow, Input/UI/Presentation, invariants, Play Mode acceptance, #423 |
| 2026-07-28 | По screenshot подтверждено depth-правило: visual/collider гриба обязан оставаться в slab своей authoritative клетки `Z=0..3`; дополнительный offset не может переносить гриб за `Z=3` или в соседний слой. | Пользователь | Input/UI/Presentation, invariants, Play Mode acceptance, #423 |

## 13. Decision log

| Date | Decision | Source |
|---|---|---|
| 2026-07-30 | Любая mushroom work position находится на той же высоте, имеет полную опору и выбирается по X/Z; supported depth position используется, когда боковые клетки являются пропастью. | user, #423 |
