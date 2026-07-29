# Пространственное выполнение боя

Статус: `QUESTIONNAIRE`.

Tracking issue: [#508](https://github.com/bageus/Dig/issues/508).

Связанные системы:

- [`../architecture/systems-gameplay.md`](../architecture/systems-gameplay.md#4-конфликт);
- [`../implementation/combat-factions-strategy.md`](../implementation/combat-factions-strategy.md);
- [`resident-movement-occupancy-and-vertical-traversal.md`](resident-movement-occupancy-and-vertical-traversal.md);
- [`content/weapons-and-shields.md`](content/weapons-and-shields.md);
- [`entity-fall-knockback-and-vertical-shafts.md`](entity-fall-knockback-and-vertical-shafts.md);
- [`open-questions.md`](open-questions.md#q-013q-014--контент-и-баланс).

## 1. Назначение и границы

Система должна преобразовать существующие combat intentions и tactical decisions в полный пространственный workflow: выбор цели и снаряжения, выбор позиции атаки, движение, подготовка, единственный authoritative attack resolve, recovery, повторную оценку угрозы и завершение либо отступление.

Система не определяет числовой balance оружия — он остаётся Q-014. Система также не утверждает knockback/fall behavior, которое принадлежит отдельному questionnaire #396.

## 2. Подтверждённый пользовательский workflow

Подтверждено существующими contracts:

1. Combat intent содержит stable identity, actor, kind, source и optional entity/cell target.
2. У actor не более одного active combat intent; новый intent заменяет предыдущий.
3. Tactical evaluator может вернуть `Defend`, `Approach`, `Attack` или `Retreat`.
4. Weapon profile задаёт minimum/maximum range и cooldown.
5. Attack result рассчитывается Domain/Application и идемпотентен по `CombatActionId`.
6. Health и authoritative actor position принадлежат Agents; route/transition принадлежат Navigation/Movement.
7. Presentation проигрывает typed stages/events, но не создаёт damage или attack success.
8. Combat interruption освобождает excavation assignment и не откатывает committed terrain work.

Не подтверждены engagement geometry, pursuit, assistance, line-of-sight, target-loss continuation и retreat destination.

## 3. Владение состоянием

- `CombatState` — active intent, resolved actions, cooldown facts, statuses и будущий spatial execution state.
- Agents/Creatures — authoritative actor cell, alive state и Health.
- Navigation — reachable routes и typed traversal edges.
- Movement/Application — authoritative transition execution.
- Inventory/Equipment — active weapon/shield identity.
- Factions — hostility.
- Presentation — interpolation, facing, wind-up/recover animation и VFX projection.

Engagement preview, animation phase и visual offset не являются authoritative combat state.

## 4. Модель данных

Предлагаемая, но не утверждённая модель:

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
- Completed
- Cancelled
- Blocked

CombatEngagementCandidate
- CellId
- DistanceToTarget
- RouteCost
- TraversalProfile
- LineOfSightResult?
- ClaimKind?
- ReasonCode
```

Поля pursuit budget, assist source, retreat target и engagement claim добавляются только после ответов на questionnaire.

## 5. Commands, events и queries

Кандидатные contracts:

- commands: start/advance/cancel spatial execution, commit engagement target, resolve one attack;
- events: target acquired/lost, engagement selected/rejected, approach blocked, attack started/resolved, retreat selected, execution completed/cancelled;
- queries: current stage, target, weapon range, candidate cells, route/LoS rejection reasons, pursuit/assist state.

Точные имена фиксируются после утверждения workflow.

## 6. Состояния и переходы

Подтверждённая часть:

```text
Intent -> tactical decision
Attack -> authoritative ResolveCombatAttack
Retreat -> movement to a policy-selected cell
terminal/cancel/replacement -> no second attack result
```

Полная spatial state machine остаётся заблокированной открытыми вопросами о engagement, target loss, pursuit, assistance и retreat.

## 7. Input, UI и Presentation

Подтверждено:

- player order является `CombatIntentSource.PlayerOrder`, а не UI-owned attack state;
- animation callback не создаёт attack result;
- HUD/inspector может показывать intent, stage, target, reason и range.

Не подтверждены cursor/selection details direct attack, cancel behavior после target loss и presentation overlap нескольких attackers.

## 8. Зависимости и конфликты

- emergency/survival имеет приоритет над обычной работой;
- Combat может прерывать excavation через существующий cleanup path;
- shared logical cells разрешены Movement design и сами по себе не блокируют actor;
- direct horizontal swap одним tick запрещён;
- vertical/depth traversal использует существующие typed edges;
- падение actor требует отдельного confirmed impact result из #396.

Приоритет explicit player attack против tactical retreat остаётся открытым.

## 9. Инварианты

- одна attack action identity создаёт не более одного damage result;
- один actor имеет одну authoritative position и один active combat intent;
- spatial execution не копирует Health, weapon ownership, faction hostility или route state;
- Presentation не меняет Combat/Agents напрямую;
- cancel/retry не повторяет уже resolved attack;
- shared-cell/overlap policy не создаёт duplicate actor;
- combat movement использует те же World/Navigation topology versions, что обычное movement.

## 10. Save/Load и migration

Подтверждённо сохраняются Combat intents, resolutions, cooldowns/statuses, Agents position/Health и equipment. Открыто, сохраняется ли mid-execution stage/pursuit/engagement claim или после load выполняется deterministic reevaluation из intent snapshot.

## 11. Диагностика

Необходимы:

- intent/source/target;
- tactical decision и reason;
- selected weapon/range;
- current execution stage;
- engagement candidates и rejection reasons;
- route/topology version;
- LoS result, если LoS утверждён;
- pursuit/assist/retreat reason;
- action id и replay/idempotency result;
- interruption и terminal reason.

## 12. Тестовая матрица

После утверждения:

- Domain: deterministic candidate ordering, target/range decisions, no duplicate resolution;
- Application: movement-to-attack pipeline, cancellation, target death/loss, assistance и retreat;
- deterministic simulation: несколько attackers/targets и stable tie-break;
- save/load: active intent/execution recovery;
- integration: excavation interruption and return/reassignment;
- Unity Play Mode: approach, facing, wind-up, one damage commit, recover, retarget/retreat, cursor/HUD/status.

## 13. Acceptance

Acceptance будет закрыт после questionnaire. Минимально полный сценарий обязан проверить: direct и autonomous start, melee/ranged approach, несколько actors, target loss, blocked route/retry, retreat, cancel/replacement, save/load, diagnostics и Play Mode evidence.

## 14. Открытые вопросы

1. **Q-COMBAT-SPATIAL-001 — melee engagement:** target cell, соседняя cell или data-driven ring?
2. **Q-COMBAT-SPATIAL-002 — несколько attackers:** общая cell, soft tactical claims или hard reservation?
3. **Q-COMBAT-SPATIAL-003 — ranged LoS:** только range или World terrain/depth line-of-sight?
4. **Q-COMBAT-SPATIAL-004 — pursuit:** до отмены, до потери sight или bounded distance/time?
5. **Q-COMBAT-SPATIAL-005 — target loss/death:** nearest threat, previous action или завершение intent?
6. **Q-COMBAT-SPATIAL-006 — помощь:** автоматический assist radius/priority и создаётся ли autonomous intent?
7. **Q-COMBAT-SPATIAL-007 — retreat cell:** nearest safe supported, home/territory anchor или maximum threat distance?
8. **Q-COMBAT-SPATIAL-008 — player order vs retreat:** может ли direct attack подавлять retreat и до какого critical Health?
9. **Q-COMBAT-SPATIAL-009 — vertical/depth:** разрешены ли атаки через depth/vertical/shaft-gap topology и какая 3D distance policy?
10. **Q-COMBAT-SPATIAL-010 — ranged collisions:** friendly fire/body blocking отсутствуют или входят в scope?
11. **Q-COMBAT-SPATIAL-011 — save/load:** сохранять exact mid-stage или deterministic reevaluate active intent?
12. **Q-COMBAT-SPATIAL-012 — direct-order UI:** какой pointer/cursor/cancel workflow считается authoritative observable behavior?

## 15. Журнал решений

| Дата | Решение | Кто подтвердил | Изменённые разделы/issues |
|---|---|---|---|
| 2026-07-29 | Создан questionnaire; inherited foundation contracts отделены от неутверждённого spatial behavior | системный аудит | #508 |
