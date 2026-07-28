# Разрушаемые бочки с содержимым и падением

Статус: `IMPLEMENTED`.

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

Система добавляет stable barrel entities, которые стоят на твёрдой поверхности, могут содержать любые поддерживаемые item definitions, атакуются выбранными residents прямым приказом и после первого подтверждённого melee hit исчезают, exactly-once материализуя содержимое.

В текущий scope входят:

- direct attack выбранным resident;
- один successful melee hit всегда разрушает barrel;
- несколько residents могут одновременно атаковать одну barrel;
- zero skill/stat progression за удар и destruction;
- red hover highlight и слегка анимированный cursor удара мечом;
- generic contents: материалы, еда, BuildingBox, книги, зелья, оружие и другие item definitions;
- test contents: ровно один deterministic-random unit `stone` или `ore` в каждой barrel;
- contents появляются отдельным world item entity в бывшей logical cell barrel;
- четыре stable demo barrels: две в нижней пещере и две на верхней поверхности;
- visual стоит вертикально по world-up, имеет 70% прежнего presentation-размера и немного ниже resident;
- barrel не блокирует movement, но блокирует building footprint;
- barrel нельзя подбирать, переносить или размещать через player UI;
- automatic falling после потери support без damage или destruction.

Automatic attack, enemy interaction, procedural spawning, traps/explosions и player placement UI не входят в этот slice.

## 2. Подтверждённый пользовательский workflow

### Прямой приказ разрушения

1. Игрок выбирает resident.
2. После UI shielding и active modal/placement modes pointer resolver проверяет barrel target.
3. Доступная supported barrel подсвечивается красным.
4. Cursor становится слегка анимированным ударом меча.
5. LMB использует тот же resolved target/version и создаёт ровно одну direct command.
6. Resident получает route к допустимой attack position рядом с barrel.
7. На work position resident выполняет один authoritative melee hit.
8. Первый committed hit атомарно:
   - переводит barrel в `Destroyed`;
   - удаляет interaction target, collider и visual;
   - создаёт сохранённый contents unit в бывшей cell;
   - завершает job и освобождает reservations;
   - не создаёт skill, characteristic или combat-progression grant.
9. Другие simultaneous jobs на той же barrel получают typed stale/destroyed conflict и не создают повторный output.
10. Повторный click по destroyed barrel не создаёт command.

### Cancel/interruption

Поскольку barrel разрушается одним ударом, до committed hit authoritative damage отсутствует. Cancel, worker removal, route failure и interruption не меняют barrel. После committed hit barrel уже terminal и не восстанавливается.

### Несколько residents

- несколько residents могут иметь одновременно active attack jobs на одну barrel;
- barrel target не получает exclusive worker reservation;
- каждый job резервирует своего worker и допустимую work position обычным Jobs ledger;
- первый successful commit побеждает;
- все последующие commits завершаются typed conflict без output и progression.

### Потеря опоры и падение

1. Barrel находится в logical XYZ cell над допустимой твёрдой support surface.
2. Полный excavation/world commit удаляет support.
3. Support reconciliation обнаруживает unsupported barrel без отдельного удара или приказа.
4. Общий deterministic landing resolver выбирает первую допустимую твёрдую поверхность ниже.
5. Barrel меняет authoritative location по принятой item-fall timing policy.
6. Presentation перемещает visual/collider к landing cell.
7. Любая высота даёт zero barrel damage и не материализует contents.
8. После landing barrel снова доступна для direct attack.

### Demo bootstrap

Fresh demo session создаёт ровно четыре stable barrel entities: две на верхней поверхности и две в нижней пещере. Каждая получает ровно один quantity-1 contents entry, deterministic выбранный между stone и ore из world seed + stable barrel id. Повторная initialization и load не создают дополнительные barrels и не reroll-ят contents.

### Blocked/failure/retry

- без selected resident direct attack недоступна;
- unreachable target возвращает typed reason и не показывает success feedback;
- falling/destroyed/stale barrel не принимает новую command;
- failure одного job не останавливает simulation loop и jobs других barrels;
- retry/save-load не создают повторные contents;
- worker removal/cancel освобождает worker/work-position reservations;
- stale simultaneous completion завершается conflict без output.

## 3. Владение состоянием

- `BarrelState` владеет stable identity, definition, authoritative cell, lifecycle, contents manifest/generation, materialized marker и version.
- World/Navigation предоставляет support, landing и reachable attack-position snapshots.
- Application/Jobs владеет direct attack travel/work/finalize lifecycle и worker/work-position reservations.
- Inventory владеет созданным quantity-1 output item и его world location.
- Buildings читает immutable blocked barrel cells для footprint validation.
- Demo bootstrap владеет только idempotent fixture creation marker/stable IDs.
- Presentation владеет geometry, red hover highlight, animated sword cursor, attack/fall animation, collider projection и status text, но не lifecycle, contents или location truth.

Barrel не является Agent, не использует Health и не создаёт второй combat progression owner.

## 4. Модель данных

```text
BarrelDefinition
- DefinitionId
- VisualProfile
- SupportPolicy
- ContentsPool
- OutputPlacementPolicy

BarrelEntity
- BarrelId
- DefinitionId
- Cell
- Lifecycle: Supported | Falling | Destroyed
- ContentsItemId
- ContentsGeneration
- ContentsMaterialized
- Version

BarrelAttackJobDefinition
- JobId
- BarrelId
- TargetCell
- WorkPosition
- BarrelVersion/ContentsGeneration validation token
- Priority
- CreatedTick
```

Stable fixture/content IDs не зависят от display names. Contents выбираются named deterministic stream и сразу сохраняются; retry/load не reroll-ят manifest.

## 5. Commands, events и queries

Commands:

- `CreateBarrelCommand`;
- `StartDirectBarrelAttackCommand`;
- `ArriveAtBarrelCommand`;
- `CompleteBarrelHitCommand`;
- `CancelBarrelAttackCommand`;
- `ReconcileBarrelSupportCommand`;
- save restore/migration commands.

Events:

- `BarrelCreated`;
- `BarrelAttackStarted`;
- `BarrelSupportLost`;
- `BarrelLanded`;
- `BarrelDestroyed`;
- `BarrelContentsMaterialized`;
- обычные `JobStatusChanged` events.

Queries:

- barrel snapshots/lifecycle/location/version;
- direct attack target decision and typed reason;
- support/landing decision;
- active jobs/work positions;
- contents diagnostics;
- demo fixture identities and cells;
- immutable building-blocked barrel cells.

## 6. Состояния и переходы

```text
Supported --support lost--> Falling --landing--> Supported
    |
    +--first committed melee hit--> Destroyed
```

`Destroyed` — terminal state. Падение не переводит barrel в `Destroyed`, не меняет contents и не создаёт output.

## 7. Input, UI и Presentation

После UI shielding:

1. active placement/modal modes имеют более высокий приоритет;
2. selected resident + valid barrel hover создаёт resolved action `AttackBarrel`;
3. barrel подсвечивается красным только если тот же snapshot допустим для LMB;
4. cursor показывает слегка анимированный удар меча;
5. LMB создаёт только barrel attack command и не создаёт move, excavation, item selection/pickup или building command тем же event;
6. unavailable/unreachable/falling/destroyed target использует default cursor и typed reason;
7. status во время travel/work: `Атакует бочку`;
8. visual стоит вертикально по world-up основанием на walk surface; renderer root не наследует terrain/bootstrap rotation; presentation равномерно уменьшена на 30% относительно предыдущего размера (`scale = 0.70`, итоговая высота `0.49` world unit);
9. destroy commit убирает geometry/collider до следующего pointer resolution;
10. barrel не имеет pickup/placement affordance.

## 8. Зависимости и конфликты

- Input router использует одну classification для red highlight, sword cursor и click command.
- Direct attack заменяет несовместимое небоевое direct action выбранного resident.
- Несколько attack jobs могут ссылаться на один barrel; exclusive barrel reservation запрещена.
- Worker и work-position reservations остаются обычными exclusive Jobs reservations.
- Support reconciliation выполняется после authoritative excavation/world topology commit и до новых attack commands.
- Contents materialization использует обычные Inventory APIs; BuildingBox contents остаются item entities.
- Falling barrel не атакуется.
- Standing barrel не блокирует Navigation movement, но её cell входит в building footprint blocked set.

## 9. Инварианты

- один `BarrelId` имеет одну authoritative lifecycle/location;
- contents выбираются не более одного раза;
- destruction materialize-ит contents не более одного раза;
- атака/destruction никогда не выдаёт skill/stat progression;
- fall distance никогда не разрушает barrel;
- destroyed barrel не имеет collider/visual и не принимает commands;
- simultaneous jobs создают максимум один successful destruction/output;
- red highlight, sword cursor и LMB относятся к одному target/version;
- один pointer event создаёт не более одной command;
- support-loss detection не зависит от Unity frame rate;
- save/load/retry не дублируют barrel или contents;
- demo bootstrap создаёт ровно четыре barrels один раз;
- barrel нельзя переносить;
- barrel block-ит building footprint, но не movement.

## 10. Save/Load и migration

Сохраняются barrel id, definition/version, authoritative cell, lifecycle/fall data согласно общей fall policy, selected contents item, generation, materialized marker и active attack job references. Selection, hover, cursor phase и visual animation не сохраняются.

Load валидирует barrel/job cross-references, не reroll-ит contents, не повторяет materialization и reconciliation-ит unsupported supported-state barrel. Legacy migration добавляет пустую barrel section; fixture bootstrap выполняется только для fresh session без marker.

## 11. Диагностика

Inspector/logs показывают barrel/definition id, lifecycle, cell/version, support/landing reason, contents/generation/materialized state, active jobs/work positions, last transition, input target decision, `skill_grant = none`, `stat_progression = none` и demo bootstrap state.

## 12. Тестовая матрица

Domain unit:

- `Supported/Falling/Destroyed` guards;
- one-hit destruction;
- deterministic contents and no reroll;
- exactly-once materialization;
- simultaneous stale commits;
- zero fall damage and zero progression;
- building blocked cells without movement occupancy.

Application/integration:

- direct command -> travel -> one hit -> destruction -> world output;
- unreachable/cancel/retry/worker removal;
- multiple residents attack one barrel concurrently;
- multiple barrels independently;
- support removal -> landing -> subsequent attack;
- BuildingBox and ordinary item contents use Inventory ownership.

Deterministic/save:

- four fixture manifests stable for world seed and IDs;
- frame partition independence;
- save/load supported, active attack, falling and destroyed states;
- no duplicate barrels/contents after migration/retry/load.

Unity Play Mode:

- two upper-surface and two lower-cave barrels;
- world-upright visual at 70% of the previous presentation size (`0.49` world-unit height), independent from bootstrap rotation;
- selected resident hover -> red highlight + animated sword cursor;
- LMB -> travel -> one attack animation -> disappearance;
- contents visible/raycastable/pickable in former cell;
- two residents attack one barrel, but only one output appears;
- excavation removes support -> barrel falls and survives;
- landed barrel can be attacked;
- second barrel proves repeated use and isolation.

## 13. Acceptance

- fresh demo contains exactly four stable barrels: two upper and two lower;
- each stands world-upright on valid support and renders at exactly 70% of the previous presentation size (`0.49` world-unit height);
- each saved manifest contains exactly one quantity-1 stone or ore;
- hover/cursor/click parity produces one direct command;
- one committed hit destroys barrel;
- contents appear in the former barrel cell exactly once;
- no step grants skill or characteristic progress;
- multiple residents may attack concurrently, but only first commit succeeds;
- standing barrel blocks building footprint but not movement;
- barrel cannot be picked up or moved;
- unsupported barrel falls to first valid support and never breaks;
- save/load never rerolls or duplicates barrels/contents;
- Unity Play Mode validates full observable workflow.

## 14. Решённые вопросы

- **Q-BARREL-001 = A:** один successful melee hit всегда разрушает barrel.
- **Q-BARREL-002 = A:** подтверждённый damage сохранялся бы на barrel; при one-hit policy до commit частичного damage не существует.
- **Q-BARREL-003 = C:** несколько residents могут атаковать одну barrel одновременно; первый commit побеждает.
- **Q-BARREL-004 = A:** каждая barrel содержит ровно один случайный quantity-1 stone или ore.
- **Q-BARREL-005 = A:** contents появляется отдельным world item entity в бывшей cell barrel.
- **Q-BARREL-006 = B:** barrel не блокирует movement, но блокирует building footprint; status `Атакует бочку`; перенос запрещён.

## 15. Журнал решений

| Дата | Решение | Кто подтвердил | Изменённые разделы/issues |
|---|---|---|---|
| 2026-07-27 | Зафиксированы destructible barrel, direct attack без progression, red highlight, animated sword cursor, generic contents, disappearance, visual чуть ниже resident, четыре demo barrels и safe falling after support loss. | Пользователь | Все разделы, #443 |
| 2026-07-28 | Q-BARREL-001=A, 002=A, 003=C, 004=A, 005=A, 006=B; перенос barrel запрещён. Статус повышен до APPROVED. | Пользователь | Workflow, ownership, conflicts, save/test acceptance, #443 |
| 2026-07-28 | После post-merge audit исправлены Unity interaction wiring, AttackBarrel dispatch, cursor priority/identity, resident attack status/pose, barrel height и Z projection; demo contents теперь зависят от world seed + stable barrel id; добавлены source-contract и Play Mode regression fixtures; система возвращена в обязательный индекс. | Пользователь | Runtime, Input/UI/Presentation, deterministic contents, test evidence, #443, #468 |
| 2026-07-28 | По runtime screenshot barrel visual обязан стоять вертикально независимо от bootstrap transform и быть равномерно уменьшен на 30% от предыдущего presentation-размера: scale `0.70`, итоговая высота `0.49`. | Пользователь | Scope, Presentation, Play Mode acceptance, #443 |
