# Пещерные столкновения, боевой баланс и иерархия врагов

Статус: `QUESTIONNAIRE` для полной иерархии; первый vertical slice с пещерными монстрами — `APPROVED`.

Tracking issues: [#559](https://github.com/bageus/Dig/issues/559), [#634](https://github.com/bageus/Dig/issues/634).

Связанные authoritative systems:

- [`combat-spatial-execution.md`](combat-spatial-execution.md);
- [`content/weapons-and-shields.md`](content/weapons-and-shields.md);
- [`ecology-creatures-and-special-drops.md`](ecology-creatures-and-special-drops.md);
- [`resident-movement-occupancy-and-vertical-traversal.md`](resident-movement-occupancy-and-vertical-traversal.md);
- [`contextual-input-cursors-and-selection.md`](contextual-input-cursors-and-selection.md);
- [`../implementation/combat-factions-strategy.md`](../implementation/combat-factions-strategy.md).

## 1. Назначение и границы

Система добавляет в стартовый мир реальных hostile actors и связывает существующие Combat, Navigation, Inventory, Skills, Health и creature Presentation в полный наблюдаемый workflow.

Первый implemented slice охватывает:

- детерминированную пару пещерных монстров на нижнем ярусе;
- медленное deterministic patrol-блуждание монстров вне боя по их горизонтальной X/Z-плоскости;
- vision/LoS aggro и player-initiated/autonomous melee combat;
- persistent enemy aggro после обнаружения цели;
- asymmetric disengage: resident выходит из боя прямым приказом, enemy продолжает преследование;
- hover highlight hostile creature независимо от возможности отдать приказ атаки;
- выбор carried weapon или fallback на кулаки;
- data-driven damage, accuracy и defense scaling;
- exactly-once combat skill progression;
- health bars участников боя;
- death/target-loss/cancel/retry через существующий combat state machine.

Хищная лиана, Живоглот и Паук входят в подтверждённую hierarchy, но их специальные lifecycle и traversal mechanics не входят в первый slice.

## 2. Владелец состояния

- `CombatState` владеет intent/execution/action/cooldown/status и exactly-once resolution.
- `AgentState` владеет position, alive state, Health и skill levels как для residents, так и для combat actors текущего vertical slice.
- `InventoryState` владеет carried Weapon и `HeldItemPurpose.WeaponUse`; visual prop не является вторым предметом.
- `FactionState` владеет hostility и membership.
- `TunnelNavigationVolume` владеет допустимыми traversal edges и routes.
- Enemy species/profile catalog владеет species id, display name, population group, traversal policy, Health и attack profile id.
- Presentation только проецирует actor state, combat stage, held item и Health.

Нажатие на creature не имеет права создавать hidden combatant. Hostile actor существует до первого ввода и остаётся единственной authoritative сущностью для Health, position и target identity.

## 3. Fresh-world encounter

Fresh demo детерминированно создаёт две adult hostile entities:

```text
SpeciesId: enemy.vuker
DisplayName: Пещерный монстр
Faction: faction.hostiles
Health: 7000 / 10000 scale (70 UI points)
Spawn group: 2
Spawn region: нижняя пещера, CaveFloorY
Spawn constraints:
- open and supported cells;
- distinct cells;
- away from the vertical shaft entrance where possible;
- stable ordering by CellId;
- every entity has a stable EntityId.
```

Spawn выполняется один раз при fresh session composition. Click, renderer refresh и repeated initialization не создают дополнительных monsters. Save/load должен восстанавливать существующие identities, Health, position, patrol anchor и combat state, а не повторять seed.

### 3.1 Patrol вне боя

Для `enemy.vuker` утверждён data-driven patrol profile:

```text
Patrol wander radius: 6 cells от stable spawn anchor
Patrol cadence: 1 шаг каждые 4 simulation ticks
Patrol plane: неизменный Y; X/Z cardinal или legal diagonal step
Allowed patrol edges: SupportedWalk и DepthTraverse
VerticalClimb вне боя: запрещён
```

- patrol использует тот же authoritative `AgentState.Position`, common movement command и tunnel traffic coordinator;
- следующий сосед выбирается детерминированно из legal supported candidates по world seed, enemy id и patrol sequence;
- blocked patrol step ничего не меняет и повторно оценивается на следующем due tick;
- patrol прекращается сразу после создания active combat intent;
- cadence заведомо медленнее существующего ecology wandering хомяков и червей;
- combat approach может использовать полный traversal profile вида, включая vertical climb и depth/Z.

### 3.2 Vision и aggro

- sight range пещерного монстра: `6` cells в 3D Manhattan distance;
- обнаружение дополнительно требует World/Tunnel line of sight без solid terrain между actor и target;
- ближайшая видимая живая hostile resident выбирается по distance, затем stable `EntityId`;
- обнаружение создаёт один autonomous attack intent; hover/click никогда не создаёт enemy actor или скрытую цель;
- intent пещерного монстра persistent: после aggro он не истекает по времени, не завершается из-за временной потери sight и не выбирает tactical retreat;
- enemy продолжает approach/replan к authoritative current target cell, пока target жив и существует;
- смерть/удаление цели разрешает typed completion/retarget; player command не может отменить enemy intent.

## 4. Enemy hierarchy

| Вид | Stable id | Группа | Traversal | Legal spawn/attachment surfaces | Special behavior |
|---|---|---:|---|---|---|
| Пещерный монстр | `enemy.vuker` | 2 | patrol: horizontal X/Z plane; combat: horizontal, vertical climb, depth/Z | supported lower-cave cells | slow patrol, sight aggro, persistent pursuit/retaliation |
| Хищная лиана | `enemy.plant.predatory_vine` | 1 | fully stationary; no horizontal, vertical or depth/Z route | horizontal tunnel interior, cave floor, cave wall | attacks actors passing through its attack area |
| Живоглот | `enemy.demon.swallower` | 2–3 | supported horizontal + depth/Z; no vertical climb | supported floor | swallows maximum one eligible Inventory unit; same identity drops on death |
| Паук | `enemy.spider` | 1–2 | horizontal, vertical, depth/Z, wall/ceiling anchors | supported floor, wall and ceiling anchors | ceiling ambush |

Movement capability является data-driven species profile и фильтрует common Navigation edges; runtime не ветвится по display name.

### Q-ENEMY-001 — ANSWERED: stationary vine anchors

Подтверждено 2026-08-02:

- Хищная лиана полностью неподвижна после появления;
- она не строит horizontal, vertical или depth/Z route и не переходит между Z-слоями;
- legal attachment surfaces: interior горизонтального тоннеля, пол пещеры и стена пещеры;
- cave ceiling не входит в подтверждённые поверхности лианы;
- attachment surface и cell являются authoritative spawn state и сохраняются при save/load;
- атака выполняется с текущего anchor без approach movement.

Общее правило межслойного перемещения относится только к мобильным видам.

## 5. Combat start и input priority

### Player attack

1. User выбирает живого resident.
2. Hover hostile creature использует общий hover/click classifier и sword cursor.
3. LMB создаёт один `CombatIntentSource.PlayerOrder` для уже существующей hostile entity.
4. Current work/movement прерывается typed cleanup path.
5. Execution выбирает equipment, engagement cell и обычный Navigation route.
6. Melee resolve допустим только при immediate valid traversal edge между соседними cells.
7. RMB отменяет player intent; уже resolved attacks не откатываются и не повторяются.

### Autonomous enemy

- Пещерный монстр оценивает ближайшего живого hostile resident в sight range `6` с valid LoS.
- При обнаружении он создаёт собственный persistent `Autonomous` attack intent и прекращает patrol.
- Получив player attack intent как target, monster retaliates тем же existing intent pipeline и получает тот же persistent aggro contract.
- Каждый monster имеет один active intent и одну execution; группа не получает shared hidden command.
- Временная потеря sight, истечение обычного intent lifetime, low Health и bounded route retry не выводят monster из боя: execution переоценивается/replans, а intent остаётся active.
- Смерть/удаление target завершает или детерминированно retargets enemy intent согласно `combat-spatial-execution.md`.

### Resident direct disengage

- любой успешный прямой приказ resident сначала отменяет его active `Attack` intent через common direct-command preparation boundary;
- это относится к manual movement, excavation, pickup/use, mushroom/barrel/building/production commands, которые уже проходят через `PrepareResidentsForDirectCommand`;
- уже resolved damage/skill grants не откатываются;
- enemy intent и target identity этим не отменяются: monster продолжает преследовать resident;
- отдельный RMB cancel остаётся допустим для resident `PlayerOrder`, но не является командой enemy.

## 6. Equipment selection

Selection выполняется перед engagement и повторно валидируется перед resolve.

Resident:

1. если current held item является carried Weapon, он используется;
2. иначе выбирается доступный carried Weapon по data-driven selection priority, затем resident slot order и stable stack id;
3. Inventory atomically переключает reference на `HeldItemPurpose.WeaponUse`;
4. если Weapon отсутствует, current non-weapon held reference освобождается и используется unarmed profile;
5. visual в правой руке читает тот же held reference;
6. после боя weapon остаётся current held/equipped reference по существующей Inventory semantics; отдельного автоматического восстановления предыдущего tool нет.

Enemy использует species attack profile и не создаёт Inventory weapon.

Current slice maps:

- no weapon -> `combat.weapon.unarmed`, `skill.unarmed_combat`;
- `weapon.club` -> `combat.weapon.club`, `skill.one_handed_combat`;
- `enemy.vuker` -> `combat.enemy.cave_bite`, без resident skill grant.

## 7. Approved starting balance

Все значения используют fixed-point scale `10000 = 100%` и data definitions.

### Base profiles

| Profile | Accuracy | Base damage | Cooldown | Range |
|---|---:|---:|---:|---:|
| Fists | 6000 (60%) | 500 (5 Health) | 2 ticks | adjacent edge |
| Club | 6500 (65%) | 850 (8.5 Health) | 2 ticks | adjacent edge |
| Cave bite | 7000 (70%) | 650 (6.5 Health) | 3 ticks | adjacent edge |

### Offensive skill scaling

Let `S` be skill units on the `0..10000` domain scale.

```text
AccuracyBonus = min(2500, S * 25 / 100)
FinalHitChance = clamp(BaseAccuracy + AccuracyBonus - TargetEvasion, 0, 9500)
DamageBonus = min(4000, S * 40 / 100)
ScaledBaseDamage = BaseDamage * (10000 + DamageBonus) / 10000
```

At 100 skill points this gives at most `+25` percentage points accuracy and `+40%` damage. Accuracy never becomes guaranteed.

### Defense scaling

```text
DefenseReduction = min(3000, DefenseSkillUnits * 30 / 100)
DamageAfterDefense = DamageAfterArmor * (10000 - DefenseReduction) / 10000
DamageAfterBlock = max(0, DamageAfterDefense - ShieldBlockValue)
```

At 100 Defense the resident receives 30% less damage. Armor penetration applies to equipment armor before skill reduction. Defense does not turn a hit into a miss.

### Skill grants

- every new non-miss offensive resolution with an offensive `CombatSkillProfile`: `+25 units` (`+0.25 point`);
- every new non-miss hit received by an eligible resident: `skill.defense +10 units` (`+0.10 point`);
- confirmed shield block may additionally use its existing shield grant profile;
- miss grants neither offensive skill nor Defense;
- replayed `CombatActionId` grants nothing.

Skill capacity/redistribution remains owned by the existing Agent skill system.

## 8. Health bars, hover и animation

- Hover по живому hostile creature всегда включает существующий creature outline/highlight, даже если resident не выбран или attack command недоступна.
- Sword cursor показывается только при выбранном живом resident и валидном hostile attack target; hover highlight не создаёт command и не меняет selection.
- Уход pointer, UI shielding, disable/destroy interaction очищают hover highlight; selected creature highlight, если появится через отдельный selection workflow, сохраняется.
- Resident health bar is shown while the resident owns an active combat intent/execution or is the target of one.
- Hostile health bar is shown while the creature owns an active combat intent/execution or is the target of one.
- Bar value reads authoritative current/max Health each presentation refresh.
- Compact world-space bar stays above the visual root, faces the camera and is hidden outside combat or after removal.
- Health bars reuse two shared materials and prefer the authored lightweight `Dig/Stylized Unlit` shader; package `Universal Render Pipeline/Unlit` is fallback only, чтобы runtime bar creation не инициировал тяжёлую full URP shader compilation.
- Weapon draw uses Inventory held reference; unarmed attack shows no weapon prop.
- `WindUp`, `ResolveAttack`, `Recover`, hit reaction and death are projections of typed state/events.
- Animation callback and health bar never issue commands or apply damage.

## 9. Failure, retry, cancel and concurrency

- invalid/dead/stale target returns typed rejection and cannot fall through to movement/excavation;
- route/topology change re-enters engagement selection;
- blocked execution uses bounded retry and typed terminal reason;
- resident direct command/cancel releases только resident combat intent/execution ownership but does not undo damage or skill grants;
- persistent enemy aggro не отменяется resident direct command, temporary sight loss, ordinary expiry, retreat evaluation или retry exhaustion;
- two monsters and multiple residents use independent intents and derived soft engagement claims;
- shared logical cells follow the resident movement policy; direct same-tick swap remains forbidden;
- one action id applies at most one damage and one set of skill grants.

## 10. Save/load and migration

Existing Combat save format stores active intent/execution/resolved ids. The first deterministic pair uses stable entity ids and is composed before restore; existing agent position/runtime/skills sections then restore its Health and position, while the stable encounter catalog restores species binding. The loader must never create an additional pair.

Derived routes, health-bar objects, animation state, equipment props and threat candidates are rebuilt after load. A restored resolved action cannot damage or grant skills again. Dynamic populations for later enemy species require their own authoritative encounter/ecology save section before implementation.

## 11. Diagnostics

Runtime diagnostics expose:

- enemy entity/species/faction/current cell/Health;
- current intent source and target;
- execution stage/engagement/route/retry reason;
- selected weapon profile and held stack id;
- offensive skill, hit chance, damage multiplier;
- target Defense and damage reduction;
- last action id/outcome/damage/replay flag;
- health-bar visibility reason;
- autonomous detection/retaliation reason;
- patrol anchor/radius/cadence/last move and blocked reason;
- sight range/LoS candidate and persistent-aggro flag;
- resident direct-disengage reason;
- hostile hover-highlight identity.

## 12. Test matrix

- Domain: fixed-point skill scaling, caps, damage reduction, adjacency and deterministic rolls.
- Application: weapon/fists selection, exactly-once offensive/Defense grants, target death and replay.
- Integration: fresh pair seed, deterministic slow same-Y patrol, autonomous sight/LoS detection, persistent approach after sight loss, vertical/depth combat movement, retaliation, direct resident disengage and multi-actor combat.
- Save/load: enemy identity/Health/execution and no repeated resolution.
- Source contracts: no click-created combatant, no visual damage, one Inventory held reference.
- Unity Play Mode: visible pair on lower tier, slow patrol before aggro, hover highlight with/without selected resident, sword cursor, sight aggro, persistent enemy pursuit after resident direct order, weapon prop/fists, both health bars, damage/skills, death, cancel and retry; no URP shader-compilation error when bars appear.

## 13. Acceptance

First slice is `IMPLEMENTED` only after merge and green automated suite. It is `VERIFIED` only after actual licensed Unity Play Mode executes the complete observable scenario, включая patrol, sight aggro, asymmetric disengage, hover highlight и health-bar shader creation. Q-ENEMY-001 is answered; the full hierarchy remains `QUESTIONNAIRE` until the remaining vine ambush lifecycle, swallower ingestion/drop and spider ambush workflows are complete.

## Runtime presentation adjustment — 2026-08-04

- Health bars for residents and enemies are informational, not precision combat widgets.
- They should render higher above the actor silhouette and use one visually consistent width across dwarves and hostile creatures.


## Combat Health-bar presentation correction — 2026-08-04

- Resident and enemy Health bars are notification indicators with one shared world-space width.
- Parent/model scale cannot make one actor bar wider or narrower than another.
- The bar root follows the top active renderer bound of its owner plus a stable gap, so it stays above residents and enemies during movement/animation.
- Bar visibility and normalized fill continue to use authoritative Health/combat state; only Presentation geometry changes.
- Bar parts remain collider-free and camera-facing.
