# Каталог игрового контента Dig

## Назначение

Этот каталог хранит согласованные design-правила для зданий, продукции, оружия, материалов, еды, алкоголя и навыков. Он описывает **что должно происходить в игре**, а runtime-реализация остаётся в соответствующих Domain/Application системах.

Главная рабочая задача расширения зданий и сервисов: [#74](https://github.com/bageus/Dig/issues/74).

Поэтапное питание, рецепты кухонь и разнообразие рациона: [#96](https://github.com/bageus/Dig/issues/96).

Навыки гномов и прогрессия за деятельность: [#103](https://github.com/bageus/Dig/issues/103).

Система копания, шаблонных пещер и ресурсных жил описана в [`../excavation-room-templates-and-deposits.md`](../excavation-room-templates-and-deposits.md). Принятая модель полноценного 3D-мира находится в [`../world-3d-depth.md`](../world-3d-depth.md). Все неоднозначности и решения ведутся в постоянном [`../open-questions.md`](../open-questions.md).

## Файлы

- [`buildings.md`](buildings.md) — здания, вместимость, работники, кухни и функциональное поведение;
- [`products.md`](products.md) — производимые предметы, готовые блюда, комплекты зданий и выходы рецептов;
- [`weapons-and-shields.md`](weapons-and-shields.md) — категории оружия, хранение и аварийная выдача;
- [`materials.md`](materials.md) — известные материалы, руды, terrain/item/deposit separation;
- [`food.md`](food.md) — блюда, три укуса, interruption, кухни и разнообразие рациона;
- [`alcohol.md`](alcohol.md) — напитки, производство, бар и эффекты;
- [`skills.md`](skills.md) — стабильные идентификаторы навыков и основные источники опыта;
- [`../skills-and-progression.md`](../skills-and-progression.md) — полная система практического развития и связь с университетом;
- [`../world-3d-depth.md`](../world-3d-depth.md) — authoritative `X,Y,Z`, глубина `0..3` и миграция 2D;
- [`../excavation-room-templates-and-deposits.md`](../excavation-room-templates-and-deposits.md) — тоннели, шаблонные пещеры, своды, жилы и выдача добычи;
- [`../open-questions.md`](../open-questions.md) — вопросы, ответы, статусы и журнал решений.

## Правила ведения каталога

1. Display name не используется как ссылка. Каждое определение получает стабильный content ID.
2. Числа, явно заданные в design, считаются исходным балансом.
3. Непредоставленные числа помечаются `TBD` и остаются data-driven. Их нельзя молча придумывать в runtime-коде.
4. Inventory остаётся единственным владельцем предметов, количества и местоположения.
5. Buildings владеет runtime-состоянием здания и функциональными местами, но не копирует предметы из Inventory.
6. World владеет terrain cells `X,Y,Z`, designations и resource-deposit state; visual wall tiles не являются источником истины.
7. Agents/Skills владеет значениями навыков и capacity; Jobs/Production/Combat публикуют типизированные grants, но не изменяют навыки напрямую.
8. Production владеет заказами и прогрессом, а RecipeDefinition — входами и выходами.
9. Jobs/Hauling используют обычные reservations и не создают отдельную систему доставки для конкретного здания или ресурса.
10. Presentation отображает authoritative snapshots. Полки, стойки, бутылки, оружие, своды, жилы и еда в руках не являются источником истины.
11. MaterialId terrain, ItemId предмета, AgentSkillId и DepositDefinitionId являются разными типами ссылок.
12. Начатая трапеза не создаёт второй ItemStack: meal progress принадлежит active action.
13. Любое изменение content ID, категории, рецепта или ссылки проходит content validation до запуска симуляции.
14. Новые идеи добавляются сначала в соответствующий файл каталога, затем связываются с GitHub issue.
15. При обнаружении двойного смысла или конфликта создаётся запись `Q-XXX` в `open-questions.md`; блокирующая schema не утверждается до ответа.

## Статусы данных

- **Подтверждено** — правило или число явно задано и должно быть реализовано;
- **TBD** — требуется отдельное решение по балансу или UX;
- **Существующее** — должно использовать уже реализованное определение без дублирования;
- **Предлагаемый ID** — рекомендуемый стабильный идентификатор, который можно скорректировать до появления сохранений;
- **Blocked by Q-XXX** — реализация/схема ожидает ответа в реестре вопросов.

## Принятые решения

- мир является полноценной сеткой `X,Y,Z`;
- доступная глубина ограничена четырьмя клетками: `Z = 0..3`;
- «Камень» — навык каменного дела `skill.stonework`, а не предмет или запас ресурса;
- шаблоны средней, большой и высокой пещеры используют пороги stonework 20/40/60;
- skill IDs, material IDs и item IDs не сравниваются по display name.

## Открытые решения

Актуальный список находится в [`../open-questions.md`](../open-questions.md). Блокирующими остаются:

- постоянство открытия шаблонов после потери квалифицированного гнома;
- объёмная форма трапециевидных комнат;
- `железная руда -> железо -> металл`;
- `золотая руда -> золото` и возможное самородное золото;
- правило выхода камня;
- автоматический hauling добычи;
- перевод design-сытости в доменную шкалу `Nutrition 0–10000`;
- точные правила разнообразия рациона;
- полный перечень interruption, уничтожающих остаток еды;
- работник кухни и влияние кулинарного навыка;
- несколько навыков за смешанную работу;
- опыт щита и оружия в одном боевом цикле;
- момент начисления опыта и шкала capacity 100/200.

Непредоставленные числовые значения остаются data-driven `BALANCE_TBD`.

## Связанные issues зданий и сервисов

- [#75](https://github.com/bageus/Dig/issues/75) — комплекты зданий;
- [#76](https://github.com/bageus/Dig/issues/76) — пост часового;
- [#77](https://github.com/bageus/Dig/issues/77) — арсенал;
- [#78](https://github.com/bageus/Dig/issues/78) — игровые комнаты;
- [#79](https://github.com/bageus/Dig/issues/79) — кинотеатр;
- [#80](https://github.com/bageus/Dig/issues/80) — винокурня и рецепты алкоголя;
- [#81](https://github.com/bageus/Dig/issues/81) — бар;
- [#82](https://github.com/bageus/Dig/issues/82) — университет.

## Связанные issues еды

- [#97](https://github.com/bageus/Dig/issues/97) — три укуса и interruption;
- [#98](https://github.com/bageus/Dig/issues/98) — блюда и kitchen tiers;
- [#99](https://github.com/bageus/Dig/issues/99) — разнообразие питания;
- [#100](https://github.com/bageus/Dig/issues/100) — UI и анимации;
- [#101](https://github.com/bageus/Dig/issues/101) — Save/Load и тесты.

## Связанные issues навыков

- [#104](https://github.com/bageus/Dig/issues/104) — каталог, grants и capacity;
- [#105](https://github.com/bageus/Dig/issues/105) — опыт за работы и производство;
- [#106](https://github.com/bageus/Dig/issues/106) — боевые навыки;
- [#107](https://github.com/bageus/Dig/issues/107) — UI, Save/Load и diagnostics.
