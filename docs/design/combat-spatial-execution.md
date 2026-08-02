# Пространственное выполнение боя

Статус: `APPROVED`.

Tracking issue: [#508](https://github.com/bageus/Dig/issues/508).

Связанные системы:

- [`../architecture/systems-gameplay.md`](../architecture/systems-gameplay.md#4-конфликт);
- [`../implementation/combat-factions-strategy.md`](../implementation/combat-factions-strategy.md);
- [`resident-movement-occupancy-and-vertical-traversal.md`](resident-movement-occupancy-and-vertical-traversal.md);
- [`content/weapons-and-shields.md`](content/weapons-and-shields.md);
- [`entity-fall-knockback-and-vertical-shafts.md`](entity-fall-knockback-and-vertical-shafts.md);
- [`contextual-input-cursors-and-selection.md`](contextual-input-cursors-and-selection.md);
- [`open-questions.md`](open-questions.md#q-013q-014--контент-и-баланс).

## 1. Назначение и границы

Система преобразует combat intentions и tactical decisions в полный spatial workflow: выбор цели и снаряжения, выбор позиции атаки, движение, подготовка, единственный authoritative attack resolve, recovery, повторную оценку угрозы, завершение intent либо retreat.

Числовой balance общего каталога остаётся data-driven. Первый cave-encounter slice утверждён в [`enemy-combat-and-cave-encounters.md`](enemy-combat-and-cave-encounters.md): fists/club/cave bite, offensive skill scaling и Defense reduction являются approved starting profiles Q-014. Knockback/fall behavior принадлежит questionnaire #396 и не выводится из spatial execution автоматически.

## 2. Подтверждённый пользовательский workflow

### 2.1 Direct player attack

1. Пользователь выбирает resident.
2. Hover по hostile actor показывает sword cursor, только если input router классифицирует target как допустимый combat target.
3. LMB создаёт один `CombatIntentSource.PlayerOrder` intent `Attack` для выбранного resident и target entity.
4. Новый combat intent прерывает несовместимое active work через существующий typed cleanup path; уже committed excavation/work progress не откатывается.
5. Execution выбирает active equipment и ближайшую достижимую engagement cell внутри диапазона оружия.
6. Resident подходит через обычный Navigation/Movement pipeline.
7. После arrival выполняются `FaceTarget -> WindUp -> ResolveAttack -> Recover -> Reevaluate`.
8. `ResolveAttack` создаёт один stable `CombatActionId`; animation/VFX не создают второй result.
9. Пока intent active и target допустим, цикл повторяется с учётом cooldown и движения target.
10. RMB отменяет active player combat intent; отмена не повторяет уже resolved attack и не восстанавливает прерванную работу автоматически.

### 2.2 Autonomous/Alarm combat

1. Threat detector публикует ordered hostile candidates.
2. Tactical evaluator выбирает `Defend`, `Approach`, `Attack` или `Retreat`.
3. Нападение на союзника создаёт typed alarm stimulus для союзников в data-driven radius.
4. Каждый союзник самостоятельно оценивает threat и может создать собственный `Autonomous`/`Alarm` intent; исходный intent не копируется принудительно.
5. При смерти/исчезновении target autonomous/alarm execution может выбрать ближайшую detected hostile threat по существующему stable threat order.

### 2.3 Pursuit и target loss

- Intent отслеживает target entity до `ExpiresTick` либо явной cancel/replacement/terminal policy.
- При временной потере sight execution идёт к последней подтверждённой target cell.
- Если target не обнаружен в последней известной cell после arrival/recheck, player intent завершается с `target_lost`.
- Autonomous/Alarm execution после target loss сначала пытается выбрать следующую ближайшую detected hostile threat; при её отсутствии завершается.
- Смерть target завершает player intent; autonomous/alarm может retarget согласно тому же правилу.

### 2.4 Retreat

- `Flee/Retreat` является emergency intent и имеет приоритет выше `PlayerOrder`.
- Direct attack не подавляет подтверждённый tactical retreat.
- Retreat resolver выбирает reachable supported cell, которая увеличивает минимальную distance до обнаруженных угроз.
- При равенстве предпочитается cell собственной территории, затем меньшая route cost, затем stable `CellId`.
- Exact tactical retreat thresholds остаются data-driven; starting damage/accuracy/Defense coefficients определены в enemy cave encounter specification.

## 3. Владение состоянием

- `CombatState` владеет active intent, authoritative spatial execution identity/stage, selected target/equipment/engagement cell, last-known target cell, pursuit/assist/retreat reasons, resolved actions, cooldown facts и statuses.
- Agents/Creatures владеют authoritative actor cell, alive state и Health.
- Navigation владеет reachability, route, route cost и typed traversal edges.
- Movement/Application выполняет authoritative cell transitions.
- Inventory/Equipment владеет active weapon/shield identity.
- World предоставляет solid/open terrain и topology snapshot для LoS.
- Factions владеет hostility и territory ownership.
- Presentation владеет interpolation, facing, wind-up/recover animation, sword cursor и VFX projection.

Route, engagement candidate set, LoS projection, animation phase и visual offset являются derived и не становятся вторым authoritative state.

## 4. Модель данных

```text
CombatExecutionId
CombatExecutionStage
- AcquireTarget
- SelectEquipment
- SelectEngagementCell
- Approach
- FaceTarget
- WindUp
- ResolveAttack
- Recover
- Reevaluate
- Retreat
- Completed
- Cancelled
- Blocked

CombatExecutionSnapshot
- ExecutionId
- IntentId
- ActorId
- TargetEntityId?
- LastKnownTargetCell?
- SelectedWeaponProfileId?
- SelectedEngagementCell?
- Stage
- NextStageTick
- LastResolvedActionId?
- Source
- ReasonCode
- Version

CombatEngagementCandidate
- CellId
- DistanceToTarget
- RouteCost
- TraversalProfile
- LineOfSightResult
- SoftClaimCount
- ReasonCode
```

Soft tactical claims являются derived preference: они помогают распределять attackers, но не блокируют shared logical cell и не входят в hard ReservationLedger.

## 5. Engagement geometry и несколько attackers

- Melee выбирает ближайшую reachable cell, имеющую непосредственный валидный movement/traversal edge к target cell и попадающую в weapon range.
- Ranged выбирает любую reachable supported cell внутри `[MinimumRange, MaximumRange]` с valid World LoS.
- Кандидаты сортируются детерминированно: valid range/LoS, меньше soft claims, меньше route cost, меньше distance-to-target, stable `CellId`.
- Несколько attackers могут находиться в одной logical cell согласно Movement shared-cell policy.
- Hard reservation engagement cell отсутствует; stationary actor не создаёт permanent combat wait.
- Direct horizontal swap одним tick остаётся запрещённым.

## 6. Range, 3D distance и line-of-sight

- Combat range использует 3D Manhattan distance по `CellId(X,Y,Z)` для compatibility с существующим `WeaponProfile` integer range contract.
- Melee разрешён только при непосредственном валидном traversal edge между attacker и target cells; числовой range не позволяет ударить сквозь закрытый terrain.
- Ranged LoS проверяет World terrain вдоль deterministic 3D grid ray; любая промежуточная solid cell блокирует shot.
- Target cell не считается blocker.
- Residents/creatures не блокируют ranged shot.
- Friendly fire отсутствует в текущем scope.
- `VerticalClimb`, `DepthTraverse` и `ShaftGapTraverse` разрешены для approach, если Navigation считает их валидными.
- Ranged attack между Y/Z layers разрешён только при range + LoS.
- Melee через vertical/depth transition разрешён только для непосредственного валидного edge.

## 7. Commands, events и queries

Commands:

- start/advance/cancel spatial execution;
- commit selected engagement cell;
- commit movement transition через существующий Movement command;
- resolve one attack через существующий `ResolveCombatAttackCommand`;
- publish/respond to combat alarm;
- select/commit retreat destination.

Events:

- execution started/stage changed/blocked/completed/cancelled;
- target acquired/lost/retargeted;
- engagement selected/rejected;
- approach blocked/replanned;
- attack wind-up started/resolved/recovered;
- ally attacked/alarm published/assist intent created;
- retreat destination selected/reached.

Queries:

- current execution stage/target/weapon/range;
- engagement candidates and rejection reasons;
- route/topology version;
- LoS result;
- pursuit/assist/retreat reason;
- action id and replay/idempotency result.

## 8. Состояния и переходы

```text
Intent
 -> AcquireTarget
 -> SelectEquipment
 -> SelectEngagementCell
 -> Approach
 -> FaceTarget
 -> WindUp
 -> ResolveAttack
 -> Recover
 -> Reevaluate
      -> SelectEngagementCell / Approach / WindUp
      -> Retreat
      -> Completed

Any active stage
 -> Blocked (typed retry/replan)
 -> Cancelled (intent cancel/replacement/expiry)
```

- `ResolveAttack` разрешён только из `WindUp` после подтверждения alive/hostile/range/LoS/cooldown.
- Один execution advance создаёт не более одного attack request.
- После target movement execution возвращается к `SelectEngagementCell` или `Approach`.
- Route/topology staleness создаёт typed replan, а не teleport.
- Blocked execution использует bounded retry policy; exhaustion завершает intent с typed reason.

## 9. Input, UI и Presentation

- Selected resident + hostile actor hover показывает sword cursor.
- Sword cursor и click используют один combat-target classification result.
- LMB создаёт не более одной player combat command.
- UI shielding выполняется до world combat routing.
- RMB отменяет active player combat intent и очищает combat cursor/feedback.
- Failed command не запускает success animation.
- HUD/inspector показывает intent source, target, stage, selected weapon, range, blocked/replan reason и retreat state.
- Facing/wind-up/recover/VFX читают typed execution/attack events; animation callback не применяет damage.

## 10. Зависимости и конфликты

- Emergency/survival и `Retreat` имеют приоритет над обычной работой и `PlayerOrder`.
- Combat interruption использует существующий cleanup path для excavation/jobs/reservations.
- Shared logical cells и visual overlap следуют Movement design.
- Navigation topology/version является тем же источником для обычного и combat movement.
- Knockback/fall требует отдельного confirmed impact result из #396.
- Equipment owner валидирует active weapon; spatial execution не создаёт equipment copy.

## 11. Инварианты

- один `CombatActionId` создаёт не более одного damage/status/skill result;
- один actor имеет одну authoritative position, один active combat intent и не более одного active spatial execution;
- execution target/equipment references принадлежат существующим owners;
- hard reservation engagement cell отсутствует;
- friendly fire и actor body blocking ranged shot отсутствуют;
- Presentation не меняет Combat/Agents/Inventory напрямую;
- cancel/retry/load не повторяет уже resolved attack;
- target movement не создаёт teleport или attack вне range/LoS;
- combat movement использует актуальные World/Navigation versions;
- alarm создаёт stimulus, а не принудительную копию intent.

## 12. Save/Load и migration

Сохраняются authoritative active combat intent, execution identity, stage, selected entity/cell target, last-known target cell, selected equipment identity, next-stage tick, retry state и already-resolved action IDs.

Navigation route, engagement candidate set, soft claims, LoS projection, interpolation и animation state не сериализуются. После load они пересчитываются из восстановленных World, Agents, Equipment, Factions и Combat snapshots. Loader валидирует target/equipment references и либо детерминированно продолжает stage, либо переводит execution в typed reevaluation/blocked/cancelled result без повторения attack.

Добавление execution state требует новой save format migration и coverage registration в production composition root.

## 13. Диагностика

Inspector/read model показывает:

- execution/intent IDs и source;
- actor/target/last-known target cell;
- tactical decision и reason;
- selected weapon/range;
- current stage/next-stage tick/retry count;
- engagement candidates, soft-claim counts и rejection reasons;
- route/topology versions;
- LoS cells/result;
- pursuit/assist/retreat reasons;
- action id, outcome и replay/idempotency flag;
- interruption и terminal reason.

## 14. Тестовая матрица

- Domain: candidate ordering, 3D range, melee edge, 3D LoS, soft claims, pursuit, retarget, assist stimulus, retreat scoring, no duplicate resolution.
- Application/integration: direct and autonomous pipelines, movement-to-attack, target movement/death/loss, blocked route/retry, excavation interruption, cancel/replacement, alarm assistance.
- Deterministic simulation: multiple attackers/targets, shared cells, stable tie-break, replay.
- Save/load/migration: active execution round-trip, rebuilt route/candidates/LoS, no repeated resolved attack.
- Source contracts: no Unity/animation authority, no hard engagement reservation, no second damage path.
- Unity Play Mode: sword cursor, LMB start, approach, facing, wind-up, one damage commit, recover, retarget/target loss, retreat, RMB cancel, HUD/status.

## 15. Acceptance

- direct LMB hostile order проходит полный approach-to-attack workflow;
- autonomous threat может создать и выполнить собственный intent;
- melee не атакует через solid cell и требует valid adjacent traversal edge;
- ranged attack требует range и World 3D LoS;
- terrain блокирует shot, actors не блокируют, friendly fire отсутствует;
- несколько attackers не deadlock из-за engagement occupancy;
- moving target вызывает deterministic re-engagement/replan;
- player target death завершает intent; autonomous/alarm может retarget nearest detected threat;
- target loss ведёт к last-known cell, затем к typed completion/retarget;
- ally alarm создаёт индивидуальные autonomous evaluations;
- tactical retreat выше direct player attack и выбирает deterministic safe supported cell;
- cancel/replacement/expiry не создаёт второй attack result;
- save/load продолжает active execution без duplicate damage;
- diagnostics объясняют target, engagement, route, LoS, pursuit, assist, retreat и terminal reason;
- Unity Play Mode подтверждает полный observable workflow.

## 16. Решённые вопросы

- **Q-COMBAT-SPATIAL-001:** melee использует nearest reachable cell с непосредственным valid traversal edge к target.
- **Q-COMBAT-SPATIAL-002:** shared logical cells + derived soft tactical claims; hard reservations отсутствуют.
- **Q-COMBAT-SPATIAL-003:** ranged требует World terrain/depth 3D LoS.
- **Q-COMBAT-SPATIAL-004:** pursuit действует до `ExpiresTick`; после потери sight используется last-known cell.
- **Q-COMBAT-SPATIAL-005:** player intent завершается при death/loss; autonomous/alarm может retarget nearest detected threat.
- **Q-COMBAT-SPATIAL-006:** ally attack публикует alarm; каждый союзник самостоятельно создаёт autonomous/alarm intent.
- **Q-COMBAT-SPATIAL-007:** retreat максимизирует minimum threat distance, затем предпочитает own territory, route cost и stable `CellId`.
- **Q-COMBAT-SPATIAL-008:** `Retreat` выше `PlayerOrder`; exact tactical thresholds остаются data-driven.
- **Q-COMBAT-SPATIAL-009:** 3D Manhattan range; ranged разрешён между layers с LoS, melee только через immediate valid edge.
- **Q-COMBAT-SPATIAL-010:** friendly fire и actor body blocking отсутствуют.
- **Q-COMBAT-SPATIAL-011:** authoritative execution stage сохраняется; route/candidates/LoS пересчитываются.
- **Q-COMBAT-SPATIAL-012:** sword cursor; LMB start; RMB cancel; единый hover/click classifier и UI shielding.

## 17. Открытые вопросы

Нет открытых business rules для spatial workflow. Starting melee balance и enemy integration определены в #559; future catalog tuning остаётся data-driven; knockback/fall остаётся #396.

## 18. Журнал решений

| Дата | Решение | Кто подтвердил | Изменённые разделы/issues |
|---|---|---|---|
| 2026-07-29 | Создан questionnaire; inherited foundation contracts отделены от неутверждённого spatial behavior | системный аудит | #508 |
| 2026-07-29 | Retreat priority и save/load boundary унаследованы из Utility AI и Save/Load specifications | repository reconciliation | sections 2, 10, 16; #508 |
| 2026-07-29 | Утверждён полный пакет engagement, soft claims, 3D LoS, pursuit, retarget, assist, retreat, vertical/depth и direct-order UI | пользователь | sections 2–17; #508 |
