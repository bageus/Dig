# Падение предметов, гномов и врагов в вертикальных тоннелях

Статус: `QUESTIONNAIRE`.

Tracking issue: [#396](https://github.com/bageus/Dig/issues/396).

Связанные документы:

- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md);
- [`resident-movement-occupancy-and-vertical-traversal.md`](resident-movement-occupancy-and-vertical-traversal.md);
- [`demo-starting-scenario.md`](demo-starting-scenario.md);
- [`../implementation/navigation.md`](../implementation/navigation.md).

## 1. Назначение

Система должна определить единый deterministic lifecycle падения через открытый вертикальный тоннель для свободных мировых предметов, residents и enemies. Она также должна связать combat knockback с переходом сущности в состояние падения без расхождения между authoritative position, visual и collider.

## 2. Подтверждённое направление

- все свободные мировые предметы в дальнейшем используют общую fall policy;
- та же пространственная модель применяется к гномам и врагам;
- гнома или врага можно сбить или вытолкнуть в открытый вертикальный тоннель;
- после начала падения сущность движется вниз до первой допустимой опоры;
- demo-коробка костра пока сразу начинается в нижней пещере и не демонстрирует процесс падения;
- после приземления logical cell, rendered position и collider обязаны указывать на одно место.

Эти правила задают направление системы, но не определяют damage, timing, interruption и collision policy.

## 3. Предполагаемые владельцы состояния

До утверждения архитектуры владельцы фиксируются только как границы ответственности:

- World предоставляет открытые вертикальные клетки и support snapshot;
- Inventory владеет authoritative location предметов;
- Agents/Creatures владеют authoritative position и active state живых сущностей;
- Combat создаёт knockback/impact result, но не должен напрямую изменять чужое состояние;
- Jobs/Reservations освобождают или переводят active work при падении actor;
- Presentation отображает trajectory, animation и impact feedback, но не выбирает landing cell.

## 4. Минимальный workflow

1. Authoritative owner получает событие потери опоры или подтверждённого knockback в вертикальную шахту.
2. Fall resolver проверяет непрерывную открытую вертикальную колонну и выбирает первую допустимую опору.
3. Actor/item переходит в явно определённое falling состояние либо атомарно получает landing cell — выбор остаётся открытым.
4. Active route, job, reservation и combat state обрабатываются по утверждённой interruption policy.
5. Presentation воспроизводит падение и impact, не меняя конечный результат.
6. После landing выполняются damage, stun, breakage, collision и diagnostics policies.

## 5. Инварианты

- одна сущность имеет одну authoritative position/location;
- падение не создаёт duplicate item quantity или duplicate actor;
- landing cell определяется одинаково при replay и save/load;
- visual не остаётся в воздухе после authoritative landing;
- collider не остаётся в source cell;
- falling actor не продолжает одновременно выполнять обычную ходьбу, копку или работу;
- combat attribution не изменяет spatial result;
- unsupported state не может зависеть от Unity frame rate.

## 6. Открытые вопросы

- **Q-FALL-001:** предметы и actors падают автоматически сразу после исчезновения опоры или только после отдельного воздействия?
- **Q-FALL-002:** падение занимает simulation ticks или authoritative landing выполняется атомарно с отдельной visual animation?
- **Q-FALL-003:** можно ли прервать падение, ухватиться за стену или край?
- **Q-FALL-004:** формула fall damage, stun, knockdown и death для residents/enemies.
- **Q-FALL-005:** получают ли предметы durability damage или destruction?
- **Q-FALL-006:** collision policy при landing на actor, item pile или building footprint.
- **Q-FALL-007:** одинаковы ли правила для разных размеров/массы существ и предметов?
- **Q-FALL-008:** считается ли vertical shaft опасным или запрещённым target для pathfinding/direct move?
- **Q-FALL-009:** combat attribution, hostility и experience за knockback-caused fall.
- **Q-FALL-010:** save/load mid-fall сохраняет falling state или уже вычисленную landing cell?

## 7. Диагностика

Inspector должен показывать source cell, trigger, support snapshot/version, выбранную landing cell, fall distance, interrupted action/job, impact target, damage result и attribution.

## 8. Acceptance после закрытия опросника

- item, resident и enemy используют согласованный landing resolver;
- потеря опоры и combat knockback имеют deterministic tests;
- active jobs/routes/reservations корректно завершаются или приостанавливаются;
- несколько сущностей в одной вертикальной колонне не создают nondeterministic order;
- save/load и replay дают тот же landing/damage result;
- Play Mode проверяет trajectory, impact, collider и authoritative cell.