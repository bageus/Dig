# Zombie mode: смерть resident и превращение в зомби

Статус: `QUESTIONNAIRE`.

Tracking issue: [#586](https://github.com/bageus/Dig/issues/586).
Parent ordinary-death design: [`death-graves-resurrection-and-rejuvenation.md`](death-graves-resurrection-and-rejuvenation.md), [#150](https://github.com/bageus/Dig/issues/150).

## Назначение

Зафиксировать отдельный mode-specific исход смерти гнома в zombie mode. Этот документ не изменяет ordinary death/grave rules и не блокирует реализацию обычного колпака, уведомления и надгробия.

## Подтверждённые правила

- правило применяется только в zombie mode;
- при terminal death resident немедленно покидает active resident roster и resident world selection;
- current actions/jobs/reservations живого resident прекращаются через общий death cleanup contract;
- именной предмет `Колпак {ИмяГнома}` не создаётся;
- погибший resident становится враждебным зомби;
- ordinary cap/grave outcome и zombie conversion взаимоисключающие;
- Presentation не придумывает локальную конвертацию: zombie state создаётся authoritative Domain/Application workflow;
- обычный режим не меняется: личные stacks остаются в клетке смерти, создаётся поднимаемый identity-linked колпак, выводится уведомление `Гном {ИмяГнома} умер`, а из конкретного колпака в Мастерской каменщика производится персональное надгробие;
- resurrection относится к будущему building/service slice и не заменяется временной Presentation-кнопкой.

## Полный пользовательский workflow — подтверждённая часть

1. В zombie mode здоровье resident достигает terminal death condition либо приходит другая authoritative death cause.
2. Один death event очищает resident actions/jobs/reservations и удаляет его из active resident roster.
3. Система выбирает zombie outcome, поэтому ordinary identity cap не создаётся.
4. В мире появляется/активируется hostile zombie, связанный с погибшим resident по ещё не утверждённому identity/provenance contract.
5. Дальнейшее поведение, combat targeting, second death и save/load зависят от ответов ниже.

## Открытые бизнес-решения

### Q-ZD-001 — активация режима

Zombie mode:

- выбирается только при создании новой игры;
- является отдельным scenario preset;
- или может переключаться в существующем сохранении?

Нужно определить также поведение уже существующих saves при смене режима.

### Q-ZD-002 — личные вещи

При превращении в зомби личный Inventory погибшего:

- полностью выпадает в логическую клетку смерти;
- остаётся прикреплённым к зомби;
- частично выпадает по item capability;
- или уничтожается?

Нужно определить Weapon/Main/Cargo, equipment visuals, reservations и stack ownership.

### Q-ZD-003 — момент конвертации

Зомби:

- появляется в той же клетке и на том же simulation tick;
- создаётся после corpse/transition delay;
- либо ждёт допустимую свободную клетку?

Нужно определить blocking, occupancy, interruption и что видит игрок между death и hostile activation.

### Q-ZD-004 — identity и authoritative entity

Нужно определить:

- тот же `EntityId` меняет faction/lifecycle;
- либо создаётся новый enemy `EntityId`, связанный с прежним `ResidentId/DeathInstanceId`;
- сохраняются ли имя, пол, внешность, одежда, роль, family history и skills как provenance;
- какой owner хранит terminal resident record и zombie link.

### Q-ZD-005 — combat profile и movement

Нужно утвердить data-driven zombie profile:

- health;
- attack/damage/cadence;
- sight/aggro range;
- move cadence;
- допустимые traversal types;
- использование лестниц, стен, шахт и дверей;
- реакция на knockback/fall.

### Q-ZD-006 — цели и faction policy

Нужно определить, кого атакует zombie:

- всех живых residents;
- только колонию погибшего;
- других friendly/neutral creatures;
- cave enemies;
- других zombies.

Также нужны assist, pursuit, disengage, target priority и конфликт одновременных целей.

### Q-ZD-007 — повторная смерть

После уничтожения zombie остаётся:

- ничего;
- обычный loot;
- corpse;
- специальный ресурс;
- или новый identity item?

Нужно определить, может ли исходный resident когда-либо быть возвращён, возникает ли новый `DeathInstanceId` и как исключается повторная конвертация.

### Q-ZD-008 — UI, уведомления и история

Нужно определить:

- текст первого death/zombie notification;
- нужно ли отдельное уведомление о превращении;
- показывается ли прежнее имя в hover/health bar/chronicle;
- допускается ли hostile selection, но не resident selection;
- куда ведёт notification focus после конвертации;
- как отображается запись о смерти в family/history UI.

### Q-ZD-009 — cancel/failure/retry

Нужно определить поведение, если zombie нельзя создать:

- нет допустимой клетки;
- entity cap достигнут;
- combat profile/content отсутствует;
- save migration не содержит нужных данных.

Нужен authoritative blocked/retry/fallback contract без возврата resident в живой roster.

### Q-ZD-010 — save/load и migration

Нужно определить:

- zombie conversion сохраняется как terminal mode-specific death outcome;
- replay/load не создаёт второго zombie;
- старые saves не получают ретроактивную конвертацию уже умерших residents либо получают её по явной migration policy;
- что происходит при загрузке zombie-mode save в обычном режиме, если переключение вообще разрешено.

## Владение состояния — до утверждения

Подтверждено:

- Society/Lifecycle хранит terminal death instance и исторический resident record;
- Agents/Application выполняет death cleanup;
- Game mode выбирает ordinary или zombie outcome;
- Combat/Enemies должен владеть hostile zombie state;
- Presentation читает один authoritative projection.

Открыто: сохраняется ли тот же entity, кто владеет provenance link, Inventory outcome и transition state.

## Commands, events и queries — требуемый contract

До реализации должны быть определены типизированные:

- command/input, задающий game mode;
- death outcome decision;
- zombie conversion event;
- query для resident-history → zombie provenance;
- hostile snapshot/read model;
- diagnostics/reason codes для blocked conversion;
- save schema и migration version.

Локализованные строки не являются identifiers.

## Acceptance после закрытия вопросов

- один death event создаёт ровно один outcome: ordinary cap или zombie conversion, никогда оба;
- jobs, reservations, actions, resident roster и selection очищаются атомарно;
- conversion не дублируется после retry, replay или save/load;
- Inventory outcome не теряет и не дублирует quantity;
- hostile selection, combat acquisition, notification, history и visual projection читают один authoritative zombie state;
- second death имеет однозначный terminal outcome;
- migration не конвертирует старые deaths без подтверждённой policy;
- unit, integration, deterministic и Unity Play Mode покрывают death → conversion → combat → second death → reload.

## Не реализовывать до ответов

Нельзя предполагать Inventory drop, timing, identity ownership, zombie stats, target policy, second-death loot или migration. Подтверждённые ordinary death rules реализуются независимо в #150.
