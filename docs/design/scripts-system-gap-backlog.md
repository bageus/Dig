# Системы из scripts: пробелы документации и backlog

## Назначение

Сопоставление legacy gameplay scripts с текущими design-документами и GitHub issues Dig. Сюжетная кампания/последовательность миров сейчас не рассматриваются.

## Backlog

| № | Система | Issue | Текущий статус design |
|---:|---|---|---|
| 1 | Полное technology tree | #126 | открыто, согласованный участок описан |
| 2 | Энергия | #127 | открыто |
| 3 | Research eligibility/UI | #128 | открыто, Q-034–Q-036 |
| 4 | Боевой equipment catalog | #129 | открыто |
| 5 | Травмы/больница | #130 | открыто |
| 6 | Зелья | #131 | открыто |
| 7 | Status Effects | #132 | открыто |
| 8 | Ловушки | #133 | открыто |
| 9 | Жидкости | #134 | открыто |
| 10 | Подводная работа | #135 | открыто; водолазный колокол исключён |
| 11 | Двери/access | #136 | комментарии перенесены; остаётся Q-046 |
| 12 | Транспорт | #137 | открыто |
| 13 | Combat lifecycle | #138 | открыто |
| 14 | Strategic AI | #139 | открыто |
| 15 | Кланы | #140 | открыто |
| 16 | Ownership/theft | #141 | открыто |
| 17 | Comfort tiers/personal beds | #142 | комментарии перенесены; остаётся Q-044 |
| 18 | Leisure variety/history | #143 | personal preferences отклонены; предложен novelty algorithm; остаётся Q-041 |
| 19 | Personal food tastes | #144 | **закрыто not planned**; используется общая #99; остаётся Q-040 о Mood cap |
| 20 | Partnership/reproduction | #145 | thresholds перенесены; остаётся Q-042 |
| 21 | Childhood/school/inheritance | #146 | правила перенесены; остаётся Q-043 |
| 22 | Fog/vision | #147 | **design закрыт**; authoritative doc `exploration-fog-of-war.md`; implementation #165 |
| 23 | Campaign/world sequence | — | исключено из текущего scope |
| 24 | Creatures/ecology | #149 | species/lifecycle перенесены; остаётся Q-045 |
| 25 | Graves/rejuvenation/return | #150 | открыто |
| 26 | Clothing/appearance | #151 | открыто |
| 27 | Conversation/social memory | #152 | открыто |

## Подтверждённые решения

- Пилорама -> Винокурня и Университет.
- Мебельная мастерская -> три игровые комнаты.
- Оружейная кузница -> Арсенал.
- Плавильня = Горн; Литейный цех = advanced coal smelting; Песчаник = crystal processing.
- Водолазный колокол исключён.
- Грибной самогон = Огненная вода.
- Dojo трактуется как направление «Кулачный бой».
- Cooking влияет только на скорость приготовления.
- Personal taste profiles для еды и персональные leisure preferences не используются.
- Exploration имеет три состояния, persistent explored и geometry-blocked 3D vision.

## Рекомендуемый порядок

1. #126/#128 — research graph/lifecycle.
2. #127 — energy.
3. #129/#138/#132 — combat content/contracts.
4. #134–#137 — liquids, underwater, doors, transport.
5. #130–#133 — health/consumables/statuses/traps.
6. Закрыть Q-040–Q-046 и завершить #142, #143, #145, #146, #149, #136.
7. #139–#141, #150–#152.
8. Реализация Exploration — #165.

Непредоставленные числовые коэффициенты остаются data-driven `BALANCE_TBD`.