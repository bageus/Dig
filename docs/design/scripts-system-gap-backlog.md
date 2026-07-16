# Системы из scripts: пробелы документации и backlog

## Назначение

Сопоставление legacy gameplay scripts с текущими design-документами и issues Dig. Campaign/world sequence сейчас не рассматривается.

## Backlog

| № | Система | Issue | Текущий design status |
|---:|---|---|---|
| 1 | Technology tree | #126 | согласованный участок описан, продолжение открыто |
| 2 | Energy | #127 | источники/классы описаны; Q-049 |
| 3 | Research eligibility/UI | #128 | основные правила описаны; Q-034, Q-036, Q-050 |
| 4 | Combat equipment | #129 | открыто |
| 5 | Treatment/hospital | #130 | открыто |
| 6 | Consumables | #131 | открыто |
| 7 | Status Effects | #132 | открыто |
| 8 | Traps | #133 | открыто |
| 9 | Liquids | #134 | закрыто not planned |
| 10 | Underwater work | #135 | закрыто not planned |
| 11 | Doors | #136 | design решён, implementation открыта |
| 12 | Ladders/elevators | #137 | базовый dispatch описан; Q-051 |
| 13 | Combat lifecycle | #138 | открыто |
| 14 | Strategic AI | #139 | открыто |
| 15 | Clans | #140 | открыто |
| 16 | Ownership/theft | #141 | открыто |
| 17 | Sleep comfort | #142 | design решён, implementation открыта |
| 18 | Leisure variety | #143 | design решён, implementation открыта |
| 19 | Personal food tastes | #144 | закрыто not planned; используется #99 |
| 20 | Partnership | #145 | design решён, implementation открыта |
| 21 | Childhood/school | #146 | design решён, implementation открыта |
| 22 | Fog/vision | #147 | design закрыт, implementation #165 |
| 23 | Campaign | — | исключено из scope |
| 24 | Creatures/ecology | #149 | design решён, balance TBD |
| 25 | Grave/return/rejuvenation | #150 | базовые rules описаны; Q-048 |
| 26 | Role appearance | #151 | role headwear описан; Q-047 |
| 27 | Conversation/social memory | #152 | открыто |

## Authoritative документы

- `technology-tree.md`;
- `research-availability-duration-and-ui.md`;
- `energy-generation-and-production-pausing.md`;
- `ladders-and-elevators.md`;
- `resident-role-headwear.md`;
- `death-graves-resurrection-and-rejuvenation.md`;
- `leisure-variety-and-selection.md`;
- `partnership-pregnancy-and-birth.md`;
- `childhood-school-and-inheritance.md`;
- `sleep-comfort-and-bed-assignment.md`;
- `ecology-creatures-and-special-drops.md`;
- `doors-access-and-lifecycle.md`;
- `exploration-fog-of-war.md`.

## Подтверждённые решения

- Пилорама -> Винокурня и Университет.
- Мебельная мастерская -> три игровые комнаты.
- Оружейная кузница -> Арсенал.
- Плавильня = Горн; Литейный цех = advanced coal smelting; Песчаник = crystal processing.
- Diving bell, liquids, underwater gameplay, carts и rails исключены.
- Qualified resident лично выполняет Research job в Work schedule.
- Research duration выводится из weighted production recipe.
- Energy: два class-1, один class-2 и один class-3 source; refill ниже 15%.
- Production progress сохраняется при временном отсутствии питания.
- Ladders отличаются длиной; elevator обслуживает same-direction requests.
- Role headwear отражает current/last work role.
- Identity-linked cap участвует в grave/lifecycle system.
- Cooking влияет только на скорость.
- Leisure repeat 5/10 -> Mood multiplier 0.5.
- Pair/reproduction, school, sleep, ecology и doors описаны в отдельных documents.

## Рекомендуемый порядок

1. Закрыть Q-034, Q-036, Q-050 и завершить #126/#128.
2. Закрыть Q-049 и реализовать #127.
3. Закрыть Q-051 и реализовать #137.
4. Закрыть Q-047/Q-048 и реализовать #150/#151.
5. #129/#138/#132, затем #130–#133.
6. Реализация #142/#143/#145/#146/#149/#165.
7. #139–#141 и #152.

Непредоставленные числа остаются data-driven `BALANCE_TBD`.
