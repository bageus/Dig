# Системы из scripts: пробелы документации и backlog

## Назначение

Сопоставление legacy gameplay scripts с текущими design-документами и GitHub issues Dig. Сюжетная кампания/последовательность миров сейчас не рассматриваются.

## Backlog

| № | Система | Issue | Текущий статус design |
|---:|---|---|---|
| 1 | Полное technology tree | #126 | открыто, согласованный участок описан |
| 2 | Энергия | #127 | частично описано; Q-049 остаётся open |
| 3 | Research eligibility/UI | #128 | основа описана; Q-034/Q-036/Q-050 остаются open |
| 4 | Боевой equipment catalog | #129 | открыто |
| 5 | Травмы/больница | #130 | открыто |
| 6 | Зелья | #131 | открыто; rejuvenation recipe подтверждён |
| 7 | Status Effects | #132 | открыто |
| 8 | Ловушки | #133 | открыто |
| 9 | Жидкости | #134 | закрыто not planned |
| 10 | Подводная работа | #135 | закрыто not planned |
| 11 | Двери/access | #136 | design решён Q-046; implementation открыта |
| 12 | Лестницы/лифты/мобильность | #137 | основа описана; emergency climb и mount/hoverboard details open |
| 13 | Combat lifecycle | #138 | открыто |
| 14 | Strategic AI | #139 | открыто |
| 15 | Кланы | #140 | открыто |
| 16 | Ownership/theft | #141 | открыто |
| 17 | Sleep comfort/personal beds | #142 | design решён Q-044; implementation открыта |
| 18 | Leisure variety/history | #143 | design решён Q-041; implementation открыта |
| 19 | Personal food tastes | #144 | закрыто not planned; используется #99 |
| 20 | Partnership/reproduction | #145 | design решён Q-042; return conflict Q-052 |
| 21 | Childhood/school/inheritance | #146 | design решён Q-043; implementation открыта |
| 22 | Fog/vision | #147 | design закрыт; implementation #165 |
| 23 | Campaign/world sequence | — | исключено из текущего scope |
| 24 | Creatures/ecology | #149 | design решён Q-045; balance TBD |
| 25 | Graves/rejuvenation/return | #150 | основа и recipes описаны; Q-052 open |
| 26 | Clothing/appearance | #151 | role hats и aging appearance описаны; implementation open |
| 27 | Conversation/social memory | #152 | открыто |

## Новые authoritative документы

- `resident-role-headwear.md`;
- `death-graves-resurrection-and-rejuvenation.md`;
- `energy-generation-and-production-pausing.md`;
- `research-availability-duration-and-ui.md`;
- `ladders-and-elevators.md`.

## Подтверждённые решения

- role hats cosmetic only; work waits for headwear state;
- identity cap after death is separate from current role hat;
- grave = 3 stone + cap, non-packable;
- temple return = cap + hamster + 4 gold + 2 crystal ore;
- rejuvenation potion = hamster + crystal + iron ore + 2 gold;
- research can be queued from orange/yellow state;
- research uses recipe only for duration and consumes no materials;
- all ores have research weight 2; research grants no XP;
- ladders spans 12/16/24 by material;
- elevators capacity 4 and energy classes 1/2/3;
- carts, rails and wheelbarrows excluded;
- riding hamster and hoverboard remain future mobility content.

## Рекомендуемый порядок

1. #126/#128 — research graph/lifecycle.
2. #127 — energy allocation.
3. #136/#137 — doors and elevator runtime.
4. #150/#151 — death identity and appearance.
5. #129/#138/#132 — combat content/contracts.
6. #130–#133 — health/consumables/statuses/traps.
7. #139–#141/#152 — factions/social systems.

Непредоставленные числовые коэффициенты остаются data-driven `BALANCE_TBD`.