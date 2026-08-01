# Падение предметов, гномов и врагов в вертикальных тоннелях

Статус: `QUESTIONNAIRE`.

Tracking issue: [#396](https://github.com/bageus/Dig/issues/396).

Связанные документы:

- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md);
- [`resident-movement-occupancy-and-vertical-traversal.md`](resident-movement-occupancy-and-vertical-traversal.md);
- [`demo-starting-scenario.md`](demo-starting-scenario.md);
- [`../implementation/navigation.md`](../implementation/navigation.md).

## 1. Назначение

Система определяет deterministic lifecycle падения через открытый вертикальный тоннель для свободных мировых предметов, residents и enemies. Пространственный landing resolver общий, но trigger различается: предметы падают автоматически после потери опоры, а residents/enemies — только после подтверждённого внешнего воздействия, которое сталкивает их в vertical shaft.

## 2. Подтверждённые правила

- все свободные мировые предметы используют общую fall policy; новый item/material автоматически участвует без отдельной регистрации или ground flag;
- свободный world item автоматически входит в fall workflow сразу после обнаружения потери допустимой опоры; отдельный удар, приказ или interaction не требуется;
- та же пространственная модель landing применяется к residents и enemies;
- для resident/enemy в текущей модели мира не существует обычного `SupportLost` trigger: actor либо стоит в валидной traversable cell, либо карабкается по валидному vertical link;
- resident или enemy начинает падение только после подтверждённого внешнего воздействия — knockback, push или другого impact result, который сталкивает actor в открытый vertical tunnel;
- после начала падения сущность движется вниз до первой допустимой опоры;
- residents и enemies не получают fall damage;
- landing не создаёт stun, knockdown или death только из-за высоты падения;
- demo-коробка костра пока сразу начинается в нижней пещере и не демонстрирует процесс падения;
- после landing logical cell, rendered position и collider указывают на одно место;
- Presentation автоматически совмещает нижнюю geometry bound ordinary world item с floor и не требует per-item vertical offset.

«Сразу после потери опоры» определяет автоматический trigger предмета, но не закрывает вопрос о том, является ли само перемещение атомарной транзакцией или занимает несколько simulation ticks.

Правило нулевого урона относится именно к результату падения. Отдельный удар, вызвавший knockback, может наносить собственный combat damage по правилам Combat.

Если в будущем появятся разрушаемые платформы, обрушения или другая механика, способная удалить traversable cell/vertical link под actor, она должна быть описана отдельной спецификацией. Текущая fall system не должна придумывать такой workflow заранее.

## 3. Владение состоянием

- World предоставляет vertical cells и support snapshot для items, а также valid traversal links для actors.
- Inventory владеет authoritative location предметов и инициирует item fall при подтверждённой потере опоры.
- Agents/Creatures владеют authoritative position и active state живых сущностей.
- Navigation гарантирует, что обычное движение actor выполняется только через валидные horizontal/vertical transitions.
- Combat создаёт knockback/push/impact result, являющийся trigger падения actor, но не меняет чужое состояние напрямую.
- Jobs/Reservations освобождают или переводят active work при падении actor.
- Presentation отображает trajectory и impact, но не выбирает trigger или landing cell.

## 4. Trigger workflow

### 4.1 Свободный мировой предмет

1. Inventory/World support check обнаруживает отсутствие допустимой опоры у свободного предмета.
2. Без дополнительного воздействия создаётся authoritative item-fall transition.
3. Fall resolver проверяет открытую vertical column и выбирает первую допустимую landing support.
4. Authoritative location переходит к landing state по утверждённой timing policy.
5. Presentation воспроизводит падение и синхронно обновляет visual/collider.

Held, reserved или site item не считается свободным и использует отдельную явно утверждённую policy.

### 4.2 Resident или enemy

1. При обычном движении actor находится в валидной traversable cell либо выполняет climbing transition по валидному vertical link.
2. Обычный movement/navigation workflow не создаёт для actor событие `SupportLost`.
3. Combat/interaction system подтверждает внешний impact result: knockback, push или эквивалентное воздействие.
4. Result должен фактически столкнуть actor в открытый vertical shaft.
5. Fall resolver проверяет vertical column и выбирает первую допустимую landing support.
6. Active route, action, job и reservation обрабатываются по interruption policy.
7. Authoritative position переходит к landing state по утверждённой timing policy.
8. Presentation воспроизводит падение и обновляет collider.
9. Actor завершает landing без fall damage/stun/knockdown/death.

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
- свободный unsupported item не ожидает отдельного воздействия для начала fall workflow;
- обычное движение resident/enemy не создаёт unsupported actor state;
- climbing actor опирается на valid vertical traversal link, а не на исчезающую floor support;
- actor-fall transition требует подтверждённого external impact result;
- landing cell определяется одинаково при replay и save/load;
- visual не остаётся в воздухе после authoritative landing;
- prefab pivot/mesh height не могут изменять grounded result или требовать item-specific registration;
- collider не остаётся в source cell;
- falling actor не выполняет одновременно обычную ходьбу, копку или работу;
- residents/enemies не получают damage, stun, knockdown или death от fall distance;
- combat attribution не изменяет spatial result;
- unsupported item detection и actor impact processing не зависят от Unity frame rate.

## 7. Решённые вопросы

- **Q-FALL-001:** свободные предметы падают автоматически после потери опоры; residents/enemies падают только после внешнего воздействия, которое сталкивает их в vertical shaft.
- **Q-FALL-004:** residents и enemies не получают fall damage, stun, knockdown или death независимо от высоты.
- **Q-FALL-012:** сценарий «у actor исчезла опора без внешнего воздействия» отсутствует в текущей модели. Resident/enemy либо стоит в валидной cell, либо карабкается по валидному vertical link; отдельный workflow не требуется.
- **Q-FALL-013:** ordinary items/materials автоматически получают common gravity и geometry-derived floor grounding; отдельный opt-in для каждого нового content definition запрещён.

## 8. Открытые вопросы

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

- entity kind;
- source cell;
- trigger kind: `SupportLost` только для item либо конкретный `Knockback/Push/Impact` для actor;
- support/traversal snapshot version;
- landing cell;
- fall distance;
- interrupted action/job;
- impact target;
- `fall_damage = 0` для resident/enemy;
- source combat damage отдельно;
- attribution и recovery state.

## 10. Acceptance после закрытия опросника

- свободный item автоматически начинает fall workflow после потери опоры без дополнительного воздействия;
- новый material/tool/weapon/food/BuildingBox участвует в item fall и floor grounding без per-item регистрации;
- обычный movement/climbing actor не создаёт `SupportLost` fall trigger;
- подтверждённый knockback/push в shaft запускает resident/enemy fall;
- item, resident и enemy используют согласованный deterministic landing resolver;
- любой fall distance для resident/enemy даёт zero fall damage;
- source hit damage отделён от fall result;
- active jobs/routes/reservations корректно завершаются или приостанавливаются;
- несколько сущностей в одной vertical column не создают nondeterministic order;
- save/load и replay дают тот же trigger/landing result;
- Play Mode проверяет item support-loss trigger, actor impact-only trigger, normal climbing without false fall, trajectory, collider, authoritative cell и отсутствие fall damage.
