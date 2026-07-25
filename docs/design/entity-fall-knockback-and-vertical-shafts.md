# Падение предметов, гномов и врагов в вертикальных тоннелях

Статус: `QUESTIONNAIRE`.

Tracking issue: [#396](https://github.com/bageus/Dig/issues/396).

Связанные документы:

- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md);
- [`resident-movement-occupancy-and-vertical-traversal.md`](resident-movement-occupancy-and-vertical-traversal.md);
- [`demo-starting-scenario.md`](demo-starting-scenario.md);
- [`../implementation/navigation.md`](../implementation/navigation.md).

## 1. Назначение

Система определяет единый deterministic lifecycle падения через открытый вертикальный тоннель для свободных мировых предметов, residents и enemies. Она связывает потерю опоры и combat knockback с переходом сущности вниз без расхождения authoritative position, visual и collider.

## 2. Подтверждённые правила

- все свободные мировые предметы используют общую fall policy;
- та же пространственная модель применяется к residents и enemies;
- resident или enemy может быть сбит/вытолкнут в открытый vertical tunnel;
- после начала падения сущность движется вниз до первой допустимой опоры;
- residents и enemies не получают fall damage;
- landing не создаёт stun, knockdown или death только из-за высоты падения;
- demo-коробка костра пока сразу начинается в нижней пещере и не демонстрирует процесс падения;
- после landing logical cell, rendered position и collider указывают на одно место.

Правило нулевого урона относится именно к результату падения. Отдельный удар, вызвавший knockback, может наносить собственный combat damage по правилам Combat.

## 3. Владение состоянием

- World предоставляет vertical cells и support snapshot.
- Inventory владеет authoritative location предметов.
- Agents/Creatures владеют authoritative position и active state живых сущностей.
- Combat создаёт knockback/impact result, но не меняет чужое состояние напрямую.
- Jobs/Reservations освобождают или переводят active work при падении actor.
- Presentation отображает trajectory и impact, но не выбирает landing cell.

## 4. Минимальный workflow

1. Authoritative owner получает подтверждённый trigger потери опоры или knockback в shaft.
2. Fall resolver проверяет открытую vertical column.
3. Resolver выбирает первую допустимую landing support.
4. Active route, action, job и reservation обрабатываются по interruption policy.
5. Authoritative position/location переходит к landing state по утверждённой timing policy.
6. Presentation воспроизводит падение и обновляет collider.
7. Resident/enemy завершает landing без fall damage/stun/knockdown/death.

## 5. Landing result

Для resident/enemy:

- Health не уменьшается из-за fall distance;
- отдельный fall-damage event не создаётся;
- высота не влияет на death chance;
- actor после завершения landing возвращается в допустимое состояние согласно interruption/recovery policy;
- combat damage от исходного удара учитывается отдельно и не смешивается с fall result.

Для items durability/breakage policy остаётся открытой.

## 6. Инварианты

- одна сущность имеет одну authoritative position/location;
- падение не создаёт duplicate item quantity или actor;
- landing cell определяется одинаково при replay и save/load;
- visual не остаётся в воздухе после authoritative landing;
- collider не остаётся в source cell;
- falling actor не выполняет одновременно обычную ходьбу, копку или работу;
- residents/enemies не получают damage, stun, knockdown или death от fall distance;
- combat attribution не изменяет spatial result;
- unsupported state не зависит от Unity frame rate.

## 7. Решённые вопросы

- **Q-FALL-004:** residents и enemies не получают fall damage, stun, knockdown или death независимо от высоты.

## 8. Открытые вопросы

- **Q-FALL-001:** предметы и actors падают автоматически сразу после исчезновения опоры или только после отдельного воздействия?
- **Q-FALL-002:** падение занимает simulation ticks или authoritative landing выполняется атомарно с отдельной visual animation?
- **Q-FALL-003:** можно ли прервать падение или ухватиться за стену/край?
- **Q-FALL-005:** получают ли предметы durability damage или destruction?
- **Q-FALL-006:** collision policy при landing на actor, item pile или building footprint.
- **Q-FALL-007:** одинаковы ли spatial rules для разных размеров/массы существ и предметов?
- **Q-FALL-008:** считается ли vertical shaft опасным target для pathfinding/direct move?
- **Q-FALL-009:** combat attribution, hostility и experience за knockback-caused fall.
- **Q-FALL-010:** save/load mid-fall сохраняет falling state или вычисленную landing cell?
- **Q-FALL-011:** какое состояние получает actor сразу после landing, если его previous action/job был прерван?

## 9. Диагностика

Inspector показывает:

- source cell;
- trigger;
- support snapshot/version;
- landing cell;
- fall distance;
- interrupted action/job;
- impact target;
- `fall_damage = 0`;
- source combat damage отдельно;
- attribution и recovery state.

## 10. Acceptance после закрытия опросника

- item, resident и enemy используют согласованный landing resolver;
- потеря опоры и combat knockback имеют deterministic tests;
- любой fall distance для resident/enemy даёт zero fall damage;
- active jobs/routes/reservations корректно завершаются или приостанавливаются;
- несколько сущностей в одной vertical column не создают nondeterministic order;
- save/load и replay дают тот же landing result;
- Play Mode проверяет trajectory, impact, collider, authoritative cell и отсутствие fall damage.