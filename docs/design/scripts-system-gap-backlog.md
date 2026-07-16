# Системы из scripts: пробелы документации и backlog

## Назначение

Сопоставление legacy gameplay scripts с текущими design-документами и GitHub issues Dig. Сюжетная кампания/последовательность миров сейчас не рассматриваются.

## Backlog

| № | Система | Issue | Текущий статус design |
|---:|---|---|---|
| 1 | Полное technology tree | #126 | открыто, согласованный участок описан |
| 2 | Энергия | #127 | открыто; water wheel deferred вместе с liquids |
| 3 | Research eligibility/UI | #128 | открыто, Q-034–Q-036 |
| 4 | Боевой equipment catalog | #129 | открыто |
| 5 | Травмы/больница | #130 | открыто |
| 6 | Зелья | #131 | открыто |
| 7 | Status Effects | #132 | открыто |
| 8 | Ловушки | #133 | открыто |
| 9 | Жидкости | #134 | **закрыто not planned**; текущий roadmap не содержит fluids |
| 10 | Подводная работа | #135 | **закрыто not planned** вместе с liquids |
| 11 | Двери/access | #136 | design решён Q-046; implementation открыта |
| 12 | Транспорт | #137 | открыто |
| 13 | Combat lifecycle | #138 | открыто |
| 14 | Strategic AI | #139 | открыто |
| 15 | Кланы | #140 | открыто |
| 16 | Ownership/theft | #141 | открыто |
| 17 | Sleep comfort/personal beds | #142 | design решён Q-044; implementation открыта |
| 18 | Leisure variety/history | #143 | design решён Q-041; implementation открыта |
| 19 | Personal food tastes | #144 | **закрыто not planned**; используется общая #99; открыт только Q-040 о Mood cap |
| 20 | Partnership/reproduction | #145 | design решён Q-042; implementation открыта |
| 21 | Childhood/school/inheritance | #146 | design решён Q-043; implementation открыта |
| 22 | Fog/vision | #147 | **design закрыт**; authoritative doc `exploration-fog-of-war.md`; implementation #165 |
| 23 | Campaign/world sequence | — | исключено из текущего scope |
| 24 | Creatures/ecology | #149 | design решён Q-045; implementation открыта, balance caps/drops TBD |
| 25 | Graves/rejuvenation/return | #150 | открыто |
| 26 | Clothing/appearance | #151 | открыто |
| 27 | Conversation/social memory | #152 | открыто |

## Authoritative документы закрытых design-блоков

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
- Водолазный колокол исключён.
- Грибной самогон = Огненная вода.
- Dojo трактуется как направление «Кулачный бой».
- Cooking влияет только на скорость приготовления.
- Personal taste profiles для еды и персональные leisure preferences не используются.
- Exploration имеет три состояния, persistent explored и geometry-blocked 3D vision.
- Leisure повторение 5/10 даёт Mood multiplier 0.5.
- Репродуктивная пара эксклюзивна; conception гарантировано; pregnancy 1 day.
- Школа поддерживает 12 навыков, 1 учителя и 4 учеников, работает 24/7.
- Sleep gaps используют multipliers 1.00/0.75/0.50/0.25.
- Creature population caps обязательны, но их значения остаются balance data.
- Door auto-close delay = 2 clear simulation ticks.
- Liquids и underwater gameplay исключены из активного roadmap.

## Рекомендуемый порядок

1. #126/#128 — research graph/lifecycle.
2. #127 — energy без water wheel/liquids.
3. #129/#138/#132 — combat content/contracts.
4. #136/#137 — doors и transport.
5. #130–#133 — health/consumables/statuses/traps.
6. Реализация #142/#143/#145/#146/#149 и #165.
7. #139–#141, #150–#152.

Непредоставленные числовые коэффициенты остаются data-driven `BALANCE_TBD`.
