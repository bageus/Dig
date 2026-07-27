# Разрушаемые бочки с содержимым и падением

Статус: `QUESTIONNAIRE`.

Tracking issue: [#443](https://github.com/bageus/Dig/issues/443).

Связанные системы:

- [`contextual-input-cursors-and-selection.md`](contextual-input-cursors-and-selection.md);
- [`entity-fall-knockback-and-vertical-shafts.md`](entity-fall-knockback-and-vertical-shafts.md);
- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md);
- [`demo-starting-scenario.md`](demo-starting-scenario.md);
- [`mushroom-growth-and-chopping.md`](mushroom-growth-and-chopping.md);
- [`../implementation/combat-factions-strategy.md`](../implementation/combat-factions-strategy.md);
- [`../implementation/inventory-storage-hauling.md`](../implementation/inventory-storage-hauling.md).

## 1. Назначение и границы

Система добавляет разрушаемые barrel entities, которые стоят на твёрдой поверхности, могут содержать любые поддерживаемые item definitions, принимают прямую команду атаки выбранного resident, исчезают после успешного разрушения и exactly-once материализуют содержимое по утверждённой output policy.

Подтверждённый scope:

- direct attack выбранным resident;
- zero skill/stat progression за удары и destruction;
- red hover highlight и слегка анимированный cursor удара мечом;
- исчезновение barrel visual/collider после authoritative destruction;
- generic contents: материалы, еда, BuildingBox/постройки, книги, зелья, оружие и другие item definitions;
- deterministic demo contents на основе камня и руды;
- четыре stable demo barrels: две в нижней пещере и две на верхней поверхности;
- barrel visual немного ниже resident;
- автоматическое падение после потери support;
- падение никогда не разрушает barrel и не наносит ему damage.

Автоматическая атака, enemy interaction, procedural barrel spawning, trap/explosion behavior и player placement UI пока не подтверждены и не входят в текущий scope.

## 2. Подтверждённый пользовательский workflow

### Прямой приказ разрушения

1. Игрок выбирает resident.
2. После UI shielding и active modal/placement modes pointer resolver проверяет barrel target.
3. Доступная supported barrel подсвечивается красным.
4. Cursor становится слегка анимированным ударом меча.
5. LMB использует тот же resolved barrel snapshot и создаёт не более одной direct command.
6. Resident получает route к допустимой work/attack position рядом с barrel.
7. Authoritative attack execution применяет утверждённую durability/hit policy.
8. После successful destruction одна atomic completion transaction:
   - помечает barrel destroyed;
   - удаляет barrel interaction target, collider и visual projection;
   - materialize-ит contents exactly once;
   - завершает direct action/job и освобождает reservations;
   - не создаёт skill, characteristic или combat-progression grant.
9. Повторный click по уже destroyed barrel не создаёт command или contents.

### Потеря опоры и падение

1. Barrel существует в logical XYZ cell над допустимой твёрдой support surface.
2. Полный excavation/world commit удаляет support.
3. Support reconciliation обнаруживает unsupported barrel без отдельного удара или приказа.
4. Общий deterministic landing resolver выбирает первую допустимую твёрдую поверхность ниже.
5. Barrel меняет authoritative location по утверждённой item-fall timing policy.
6. Presentation перемещает visual/collider к landing cell.
7. Любая высота падения даёт zero barrel damage и не материализует contents.
8. После landing barrel снова доступна для direct attack.

### Demo bootstrap

1. Fresh demo session создаёт ровно четыре stable barrel entities.
2. Две barrel находятся на поддерживаемых cells верхней поверхности.
3. Две barrel находятся на поддерживаемых cells нижней пещеры.
4. Visual каждой barrel немного ниже resident interaction/visual height.
5. Contents выбираются deterministic random stream из подтверждённого test loot rule.
6. Повторная initialization и load существующего save не создают дополнительные barrels.

### Blocked/failure/retry

- без selected resident direct barrel attack недоступна;
- unreachable target возвращает typed reason и не показывает success feedback;
- falling, destroyed или stale-version barrel не принимает новую attack command;
- failure одного barrel action не останавливает simulation loop или действия по другим barrels;
- retry/save-load не создают повторные contents;
- worker removal/cancel освобождает agent/barrel/work-position reservations согласно утверждённой damage-progress policy;
- если barrel destroyed другим committed action, stale completion завершается typed conflict без output.

## 3. Владение состоянием

- `BarrelState`/WorldObjects владеет stable barrel identity, definition, authoritative cell, lifecycle, durability/progress, active direct-action reference, contents manifest/generation и version.
- World/Navigation предоставляет immutable support, landing и reachable work-position snapshots.
- Combat/Application владеет player attack intent/execution и применяет damage только через barrel command handler; barrel не становится Agent и не использует Health owner residents.
- Jobs/DirectActions владеет worker assignment, travel/work/finalize lifecycle и reservations.
- Inventory владеет материализованными output unit entities, их item definitions, quantity и world locations.
- Demo bootstrap владеет только idempotent fixture creation marker/stable IDs.
- Presentation владеет geometry, red hover highlight, animated sword cursor, attack animation, fall animation, collider projection и reason/status text, но не durability, contents или location truth.

## 4. Модель данных

```text
BarrelDefinition
- DefinitionId
- VisualProfile
- AttackProfile
- SupportPolicy
- ContentsPolicy
- OutputPlacementPolicy

BarrelEntity
- BarrelId
- DefinitionId
- Cell
- Lifecycle: Supported | Falling | Destroyed
- Durability/RequiredHits/CompletedHits (по Q-BARREL-001/002)
- ActiveAttackJobId?
- ContentsManifest
- ContentsGeneration
- Version

BarrelContentsEntry
- ItemDefinitionId
- Quantity
- OptionalUnitItemMetadata

BarrelAttackJobDefinition
- JobId
- BarrelId
- TargetCell
- WorkPosition
- BarrelVersion/ContentsGeneration validation token
- CreatedTick
```

Stable fixture/content IDs не зависят от display names. Random contents выбираются named deterministic stream из world seed и stable barrel id, затем сохраняются; retry и load не reroll-ят manifest.

## 5. Commands, events и queries

Commands:

- `CreateBarrelCommand`;
- `StartDirectBarrelAttackCommand`;
- `AdvanceBarrelAttackCommand`;
- `CompleteBarrelDestructionCommand`;
- `CancelBarrelAttackCommand`;
- `ReconcileBarrelSupportCommand`;
- save restore/migration commands через существующую save composition.

Events:

- `BarrelCreated`;
- `BarrelAttackStarted`;
- `BarrelAttackAdvanced`;
- `BarrelAttackCancelled`;
- `BarrelSupportLost`;
- `BarrelLanded`;
- `BarrelDestroyed`;
- `BarrelContentsMaterialized`.

Queries:

- barrel snapshots/lifecycle/location;
- direct attack target decision and typed reason;
- support/landing decision;
- active worker/job/progress;
- contents preview/diagnostics where permitted;
- demo fixture identities and cells.

## 6. Состояния и переходы

```text
Supported --support lost--> Falling --landing--> Supported
    |
    +--successful direct attack--> Destroyed
```

`Destroyed` — terminal state. Падение не переводит barrel в `Destroyed`, не уменьшает durability/progress и не создаёт contents output.

Attack progress, cancel/takeover semantics и количество simultaneous workers остаются открыты в Q-BARREL-001..003.

## 7. Input, UI и Presentation

После UI shielding:

1. active placement/modal modes сохраняют более высокий приоритет;
2. selected resident + valid barrel hover создаёт resolved action `AttackBarrel`;
3. barrel подсвечивается красным только если тот же snapshot допустим для LMB command;
4. cursor показывает слегка анимированный удар меча;
5. LMB создаёт только barrel attack command и не создаёт move, excavation, item selection/pickup или building command тем же event;
6. unavailable/unreachable/falling/destroyed target использует default cursor и typed reason;
7. barrel visual стоит вертикально основанием на walk surface и немного ниже resident;
8. destroy commit убирает geometry/collider до следующего pointer resolution;
9. fall presentation не меняет authoritative landing decision.

Точный resident status text остаётся открытым в Q-BARREL-006.

## 8. Зависимости и конфликты

- Input router обязан использовать одну classification для red highlight, sword cursor и click command.
- Direct attack заменяет несовместимое небоевое direct action выбранного resident; взаимодействие с active combat/self-defense определяется существующим combat intent priority.
- Barrel target/work-position reservations запрещают недетерминированное выполнение нескольких actions.
- Support reconciliation выполняется после authoritative excavation/world topology commit и до новых attack reservations.
- Contents materialization использует обычные Inventory APIs; BuildingBox contents остаются item entities и не превращаются сразу в completed buildings.
- Falling barrel не может одновременно выполнять supported attack workflow.
- Four demo barrels имеют независимые IDs, contents manifests, actions и fall states.

## 9. Инварианты

- один `BarrelId` имеет одну authoritative lifecycle/location;
- contents manifest выбирается не более одного раза для generation;
- destruction materialize-ит contents не более одного раза;
- разрушение никогда не выдаёт skill/stat progression;
- fall distance никогда не разрушает barrel и не изменяет contents quantity;
- destroyed barrel не имеет selectable collider/visual и не принимает commands;
- red highlight, sword cursor и LMB относятся к одному resolved target/version;
- один pointer event создаёт не более одной command;
- support-loss detection не зависит от Unity frame rate;
- save/load/retry не дублируют barrel или contents;
- demo bootstrap создаёт ровно четыре barrels один раз.

## 10. Save/Load и migration

Сохраняются:

- barrel id, definition/version и authoritative cell;
- lifecycle и, если timing policy потребует, falling source/landing/progress;
- durability/attack progress после решения Q-BARREL-001/002;
- active attack job/worker references и reservations;
- contents manifest, generation и materialized marker;
- demo bootstrap marker/stable IDs.

Hover, cursor phase, red highlight, visual animation phase и transient selection не сохраняются.

Load должен валидировать barrel ↔ active job ↔ worker cross-references, не reroll-ить contents, не повторять materialization и reconciliation-ить unsupported supported-state barrel через общий support resolver.

Legacy save migration добавляет пустую barrel section; fresh-session fixture bootstrap создаёт четыре barrels только при отсутствии save/session marker.

## 11. Диагностика

Inspector/logs показывают:

- barrel/definition id, lifecycle, cell и version;
- support snapshot/reason, source/landing cell и fall distance;
- contents manifest/generation/materialized state;
- active job/worker/work position/reservations;
- durability or required/completed hits после утверждения policy;
- last command/event/transition;
- input target decision, hover/cursor parity и blocked reason;
- `skill_grant = none` / `stat_progression = none`;
- demo fixture identity and bootstrap state.

## 12. Тестовая матрица

Domain unit:

- lifecycle guards `Supported/Falling/Destroyed`;
- deterministic contents manifest and no reroll;
- exactly-once materialization;
- zero fall damage/destruction;
- zero skill/stat progression contract;
- stale completion/version conflict.

Application/integration:

- direct command -> travel -> attack -> destruction -> output;
- unreachable/cancel/retry/worker removal;
- concurrent actions on multiple barrels;
- takeover/simultaneous policy after Q-BARREL-003;
- support removal -> landing -> subsequent attack;
- BuildingBox and ordinary item contents use Inventory ownership.

Deterministic/save:

- four fixture manifests stable for world seed and barrel IDs;
- frame partition independence;
- save/load supported, active attack, falling and destroyed states;
- no duplicate barrels/contents after migration/retry/load.

Unity Play Mode:

- two upper-surface and two lower-cave barrels;
- visual height slightly below resident;
- selected resident hover -> red highlight + animated sword cursor;
- LMB -> travel -> attack animation -> authoritative disappearance;
- expected contents visible/raycastable/pickable;
- excavation removes support -> barrel falls and survives;
- landed barrel can be attacked normally;
- second barrel workflow proves repeated use and isolation.

## 13. Acceptance

После закрытия questionnaire:

- fresh demo session contains exactly four stable barrels: two upper surface and two lower cave;
- each barrel stands on a valid solid support and renders slightly shorter than resident;
- hover/cursor/click parity produces one direct attack command;
- successful destruction removes barrel and materialize-ит saved contents exactly once;
- no attack/destruction step grants any skill or characteristic progress;
- stone/ore fixture contents follow the confirmed random rule;
- unsupported barrel automatically falls to first valid support and never breaks from falling;
- landing keeps logical cell, visual and collider consistent;
- cancel/takeover/retry follow confirmed damage-progress rules;
- save/load never rerolls or duplicates barrels/contents;
- Unity Play Mode validates the full observable workflow, not only source contracts.

## 14. Открытые вопросы

- **Q-BARREL-001 — durability/удары.** Как именно resident разрушает barrel?
  - A: один successful melee attack всегда разрушает её;
  - B: фиксированное число ударов для всех residents/weapons;
  - C: barrel имеет durability, а урон каждого удара берётся из текущего weapon/combat profile;
  - D: отдельная таблица hit count по типу оружия.
  Ответ определяет attack state, animation cadence, save data и tests.

- **Q-BARREL-002 — cancel/interruption.** Если barrel требует несколько ударов, сохраняется ли нанесённый damage/progress после отмены, ухода worker, save/load и повторного приказа?
  - A: сохраняется на barrel;
  - B: полностью сбрасывается;
  - C: сохраняется только damage, но незавершённая animation/action не сохраняется.

- **Q-BARREL-003 — несколько residents.** Что происходит при втором direct order на ту же barrel?
  - A: новый resident заменяет старого и получает exclusive claim;
  - B: первый приказ остаётся, второй отклоняется;
  - C: несколько residents могут атаковать одновременно.

- **Q-BARREL-004 — test loot.** Фраза «камни и руда случайным образом по 1 единице» означает:
  - A: каждая barrel содержит ровно один случайный unit — stone или ore;
  - B: каждая barrel содержит один stone и один ore;
  - C: среди четырёх barrels deterministic random распределяет отдельные quantity-1 stone/ore entries по другой таблице.

- **Q-BARREL-005 — materialization.** После destruction contents:
  - A: появляются отдельными world item entities в бывшей cell barrel;
  - B: раскладываются по ближайшим свободным cells;
  - C: сразу попадают в inventory атакующего resident.
  Ответ также определяет поведение нескольких outputs и BuildingBox contents.

- **Q-BARREL-006 — occupancy и status.** Standing barrel:
  - A: блокирует проход и building footprint; resident status «Разрушает бочку»;
  - B: не блокирует проход, но блокирует building footprint; status «Атакует бочку»;
  - C: не блокирует movement/building placement; нужен другой status/interaction contract.
  Отдельно подтвердить, можно ли barrel переносить до разрушения; по текущему scope pickup/placement UI не реализуется.

## 15. Журнал решений

| Дата | Решение | Кто подтвердил | Изменённые разделы/issues |
|---|---|---|---|
| 2026-07-27 | Зафиксированы destructible barrel, direct attack без progression, red highlight, animated sword cursor, generic contents, disappearance, visual чуть ниже resident, четыре demo barrels и safe falling after support loss. Неопределённые durability, cancel, concurrency, loot/output и occupancy rules вынесены в Q-BARREL-001..006. | Пользователь | Все разделы, #443 |
