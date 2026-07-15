# Двери, режимы доступа и lifecycle прохода

Связанная задача: #136. Navigation: #4. Exploration: #147/#165.

## Каталог

Подтверждённые материалы:

- деревянная дверь;
- металлическая дверь;
- кристаллическая дверь.

Каменная дверь исключена как отдельный вариант. Её раннюю роль выполняет деревянная дверь.

Материал влияет прежде всего на durability и content prerequisites.

## Physical state

```text
Closed
Opening
Open
Closing
Destroyed
```

Mode и physical state являются разными данными.

## Режимы

### ClosedForAll

- дверь закрыта для своей faction, союзников и врагов;
- автоматическое открытие не запускается;
- Navigation считает проход заблокированным;
- игрок должен явно сменить режим.

### OpenForAll

- дверь остаётся открытой;
- проход разрешён всем;
- Navigation считает проём обычной связью.

### AutomaticOwnOnly

- открыть дверь автоматически может только resident faction-владельца;
- союзник не считается своим;
- враг и союзник не запускают opening;
- когда дверь физически открыта для своего resident, любой entity может успеть пройти через проём;
- закрытая AutomaticOwnOnly дверь не считается постоянным доступным путём для enemy pathfinding.

## Автоматическое открытие и закрытие

1. Свой resident входит в trigger/queue.
2. Door state переходит `Closed -> Opening -> Open`.
3. Navigation/vision link становится доступным после фактического открытия.
4. После выхода последнего своего resident начинается ожидание.
5. Начальная задержка автозакрытия — **2 simulation ticks**.
6. Если в проёме находится любой occupant, closing откладывается.
7. После освобождения doorway отсчёт двух ticks начинается заново.
8. Затем выполняется `Open -> Closing -> Closed`.

Задержка задаётся data-driven параметром, но 2 ticks являются утверждённым значением по умолчанию.

## Разрушение

После уничтожения двери:

- physical state становится `Destroyed`;
- дверь больше не блокирует Navigation или обзор;
- остаётся свободный проход;
- repairable rubble object не создаётся;
- восстановление возможно только через новую установку двери.

## Жидкости и автоматизация

Жидкостная simulation не входит в текущий этап и может вообще не войти в игру. Поэтому двери сейчас не имеют gameplay-контракта остановки воды, лавы или других жидкостей.

Кнопки, переключатели, link-network и power-controlled door automation в первую версию не входят. Режим меняется через building panel command.

## Интеграция

- Navigation использует physical state;
- Exploration: Closed/Opening блокируют обзор, Open разрешает распространение через проход; Closing переключает passability по утверждённой transition point;
- Combat может атаковать дверь и уменьшать durability;
- destruction обновляет Navigation/Exploration dirty region один раз;
- Presentation отображает состояние, но не меняет mode напрямую.

## Save/Load

Сохраняются DoorDefinitionId, mode, physical state, transition progress, close-delay ticks и occupant/queue refs. Load не повторяет command и не закрывает дверь поверх occupant.

## Критерии приёмки

- существуют только деревянная, металлическая и кристаллическая doors;
- три режима имеют различное поведение;
- союзник не открывает AutomaticOwnOnly;
- враг может пройти только через фактически открытый проём;
- closing начинается через 2 simulation ticks после освобождения;
- любой occupant откладывает закрытие;
- destroyed door оставляет свободный проход;
- liquids, switches и buttons отсутствуют в первой версии;
- Save/Load сохраняет state/таймер без повторного transition.
