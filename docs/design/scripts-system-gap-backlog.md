# Системы из scripts: пробелы документации и backlog

## Назначение

Сопоставление legacy gameplay scripts с design-документами и issues Dig. Сюжетная кампания сейчас не рассматривается.

## Backlog

| № | Система | Issue | Статус design |
|---:|---|---|---|
| 1 | Полное technology tree | #126 | согласованный участок описан, продолжение открыто |
| 2 | Энергия | #127 | Q-049 закрыт; implementation открыта |
| 3 | Research eligibility/UI | #128 | Q-034/Q-036/Q-050 закрыты; implementation открыта |
| 4 | Основной боевой equipment catalog | #129 | Q-053 закрыт; implementation открыта |
| 5 | Дополнительное fantasy/creature equipment | #177 | deferred backlog; вне текущего runtime scope |
| 6 | Травмы/больница | #130 | открыто |
| 7 | Зелья | #131 | открыто; rejuvenation recipe подтверждён |
| 8 | Status Effects | #132 | открыто |
| 9 | Ловушки | #133 | открыто |
| 10 | Жидкости | #134 | закрыто not planned |
| 11 | Подводная работа | #135 | закрыто not planned |
| 12 | Двери/access | #136 | design решён; implementation открыта |
| 13 | Лестницы/лифты/mobility | #137 | Q-051 закрыт; numerical mobility multiplier — Q-014 |
| 14 | Combat lifecycle | #138 | открыто |
| 15 | Strategic AI | #139 | открыто |
| 16 | Кланы | #140 | открыто |
| 17 | Ownership/theft | #141 | открыто |
| 18 | Sleep comfort | #142 | design решён; implementation открыта |
| 19 | Leisure variety | #143 | design решён; implementation открыта |
| 20 | Personal food tastes | #144 | закрыто not planned |
| 21 | Partnership | #145 | Q-042/Q-052 закрыты; implementation открыта |
| 22 | Childhood/school | #146 | design решён; implementation открыта |
| 23 | Fog/vision | #147 | design закрыт; implementation #165 |
| 24 | Campaign/world sequence | — | исключено из scope |
| 25 | Creatures/ecology | #149 | design решён; balance TBD |
| 26 | Graves/rejuvenation/return | #150 | Q-048/Q-052 закрыты; implementation открыта |
| 27 | Clothing/appearance | #151 | design описан; implementation открыта |
| 28 | Conversation/social memory | #152 | открыто |

## Подтверждённые combat-content решения

- основной colony catalog ограничен десятью производимыми предметами;
- Оружейная кузница, Оружейная фабрика и Dojo принимают любой один из пяти combat skills на threshold;
- Меч/Палаш и Боевой топор используют `skill.two_handed_combat`;
- Рогатка, Лук и Ружьё используют `skill.ranged_combat`;
- Дубина и Световой меч используют `skill.one_handed_combat`;
- щиты используют `skill.defense` для confirmed defense results;
- щит совместим только с Мечом, Световым мечом и Рогаткой;
- ranged equipment не расходует ammo;
- все десять предметов не изнашиваются;
- 32 fantasy/creature classes имеют `DeferredBacklog` и вынесены в #177;
- современные special-mode classes и `Bombe` имеют `ExcludedFromColonyMode`.

## Другие подтверждённые решения

- research busy state белый;
- уголь/руды/iron имеют research weight 2;
- одно здание имеет один active research slot;
- начатый research завершается после снижения skill;
- zero-input fallback мгновенный;
- elevator emergency climb идёт к target platform;
- Reithamster/Hoverboard автоматически активируются из Inventory на дальнем пути;
- legacy использует engine `speedtype 3/2`, Hoverboard имеет приоритет;
- numeric personal-mobility speed в TCL отсутствует;
- новая active pair сохраняется после return прежнего партнёра.

## Источники

Combat:

- `scripts/misc/techtreetunes.tcl`;
- `scripts/classes/items/waffen.tcl`;
- `scripts/misc/genericfight.tcl`;
- `scripts/misc/genattribs.tcl`;
- `scripts/classes/zwerg/zwerg.tcl`.

Mobility:

- `scripts/classes/zwerg/z_dignwalk.tcl`;
- `scripts/classes/items/transport.tcl`;
- `scripts/classes/zwerg/z_anims.tcl`.

## Рекомендуемый порядок

1. #126/#128 — research graph/runtime.
2. #127 — energy runtime.
3. #136/#137 — doors/transport runtime.
4. #150/#151 — lifecycle/appearance.
5. #129/#138/#132 — основной combat scope.
6. #130–#133 — health/effects/traps.
7. #139–#141/#152 — factions/social.
8. #177 — только после отдельного решения вернуть deferred fantasy content в scope.

Непредоставленные числа остаются data-driven `BALANCE_TBD`.