# Системы из scripts: пробелы документации и backlog

## Назначение

Сопоставление legacy gameplay scripts с design-документами и issues Dig. Сюжетная кампания сейчас не рассматривается.

## Backlog

| № | Система | Issue | Статус design |
|---:|---|---|---|
| 1 | Полное technology tree | #126 | согласованный участок описан, продолжение открыто |
| 2 | Энергия | #127 | Q-049 закрыт; implementation открыта |
| 3 | Research eligibility/UI | #128 | Q-034/Q-036/Q-050 закрыты; implementation открыта |
| 4 | Боевой equipment catalog | #129 | открыто |
| 5 | Травмы/больница | #130 | открыто |
| 6 | Зелья | #131 | открыто; rejuvenation recipe подтверждён |
| 7 | Status Effects | #132 | открыто |
| 8 | Ловушки | #133 | открыто |
| 9 | Жидкости | #134 | закрыто not planned |
| 10 | Подводная работа | #135 | закрыто not planned |
| 11 | Двери/access | #136 | design решён; implementation открыта |
| 12 | Лестницы/лифты/mobility | #137 | Q-051 закрыт; numerical mobility multiplier — balance Q-014 |
| 13 | Combat lifecycle | #138 | открыто |
| 14 | Strategic AI | #139 | открыто |
| 15 | Кланы | #140 | открыто |
| 16 | Ownership/theft | #141 | открыто |
| 17 | Sleep comfort | #142 | design решён; implementation открыта |
| 18 | Leisure variety | #143 | design решён; implementation открыта |
| 19 | Personal food tastes | #144 | закрыто not planned |
| 20 | Partnership | #145 | Q-042/Q-052 закрыты; implementation открыта |
| 21 | Childhood/school | #146 | design решён; implementation открыта |
| 22 | Fog/vision | #147 | design закрыт; implementation #165 |
| 23 | Campaign/world sequence | — | исключено из scope |
| 24 | Creatures/ecology | #149 | design решён; balance TBD |
| 25 | Graves/rejuvenation/return | #150 | Q-048/Q-052 закрыты; implementation открыта |
| 26 | Clothing/appearance | #151 | design описан; implementation открыта |
| 27 | Conversation/social memory | #152 | открыто |

## Подтверждённые решения последней синхронизации

- research busy state белый и объясняется текстом;
- уголь/руды/iron имеют research weight 2;
- одно здание имеет один active research slot;
- начатый research завершается после снижения skill;
- zero-input fallback мгновенный;
- elevator emergency climb идёт к target platform;
- Reithamster/Hoverboard автоматически активируются из Inventory на дальнем пути;
- legacy использует одинаковые engine `speedtype 3/2`, Hoverboard имеет приоритет;
- numeric personal-mobility speed в TCL отсутствует;
- новая active pair сохраняется после return прежнего партнёра, старая relation остаётся historical.

## Источники mobility recovery

- `scripts/classes/zwerg/z_dignwalk.tcl`;
- `scripts/classes/zwerg/z_dignwalk.tcl_copy`;
- `scripts/classes/items/transport.tcl`;
- `scripts/classes/zwerg/z_anims.tcl`.

## Рекомендуемый порядок

1. #126/#128 — research graph/runtime.
2. #127 — energy runtime.
3. #136/#137 — doors/transport runtime.
4. #150/#151 — lifecycle/appearance.
5. #129/#138/#132 — combat.
6. #130–#133 — health/effects/traps.
7. #139–#141/#152 — factions/social.

Непредоставленные числа остаются data-driven `BALANCE_TBD`.