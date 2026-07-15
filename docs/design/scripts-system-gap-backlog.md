# Системы из scripts: пробелы документации и backlog

## Назначение

Этот файл хранит результат сопоставления старых gameplay scripts с текущими design-документами и GitHub issues Dig.

Для каждой самостоятельной функции создана отдельная задача. В каждой issue явно указано:

- что уже существует в Dig;
- чего пока нет;
- что необходимо описать;
- владельцы состояния, инварианты, UI, Save/Load и tests.

Сюжетная кампания и последовательность миров из пункта 23 анализа **сейчас не рассматриваются** и отдельная issue для них не создавалась.

## Backlog

| № анализа | Система/функция | Issue | Состояние до issue |
|---:|---|---|---|
| 1 | Полное дерево технологий и research graph | [#126](https://github.com/bageus/Dig/issues/126) | #9 реализовал общий механизм, но authoritative content tree отсутствовал |
| 2 | Генераторы, классы и снабжение энергией | [#127](https://github.com/bageus/Dig/issues/127) | в #9 был только общий placeholder-контракт |
| 3 | Skill eligibility и запуск исследования из здания | [#128](https://github.com/bageus/Dig/issues/128) | skills и Technology были раздельны; функции eligibility/click/UI не было |
| 4 | Каталог боевого снаряжения, recipes и technologies | [#129](https://github.com/bageus/Dig/issues/129) | существовали общий Combat и частичный каталог, полного графа не было |
| 5 | Травмы, лечение и больница | [#130](https://github.com/bageus/Dig/issues/130) | #12 упоминал лечение, конкретного lifecycle не было |
| 6 | Зелья, употребление и временные эффекты | [#131](https://github.com/bageus/Dig/issues/131) | общего potion catalog/use flow не было |
| 7 | Единая система Status Effects | [#132](https://github.com/bageus/Dig/issues/132) | эффекты были рассеяны по будущим системам, authoritative collection отсутствовал |
| 8 | Ловушки и автоматическая оборона | [#133](https://github.com/bageus/Dig/issues/133) | ничего конкретного не было |
| 9 | Жидкости, потоки и затопление | [#134](https://github.com/bageus/Dig/issues/134) | World существовал, fluid simulation отсутствовала |
| 10 | Подводная работа и запас воздуха | [#135](https://github.com/bageus/Dig/issues/135) | ничего конкретного не было; водолазный колокол исключён |
| 11 | Двери, режимы доступа и автоматизация | [#136](https://github.com/bageus/Dig/issues/136) | Building/Navigation существовали, Door contract отсутствовал |
| 12 | Лестницы, лифты, вагонетки и средства передвижения | [#137](https://github.com/bageus/Dig/issues/137) | были общие Navigation links и Inventory, gameplay transport отсутствовал |
| 13 | Детальный lifecycle игровых столкновений | [#138](https://github.com/bageus/Dig/issues/138) | #12 содержал общий epic без конкретной state machine |
| 14 | Стратегические экспедиции, защита и экспансия | [#139](https://github.com/bageus/Dig/issues/139) | #12 упоминал Strategic AI, конкретной FSM не было |
| 15 | Кланы и асимметричные стартовые профили | [#140](https://github.com/bageus/Dig/issues/140) | Factions существовал только как общий контракт отношений |
| 16 | Владение предметами, кража и дипломатическая реакция | [#141](https://github.com/bageus/Dig/issues/141) | Inventory location существовал, ownership/theft policy отсутствовала |
| 17 | Уровни цивилизации и качество помещений | [#142](https://github.com/bageus/Dig/issues/142) | отдельные buildings/needs существовали, общей quality model не было |
| 18 | Индивидуальные предпочтения досуга | [#143](https://github.com/bageus/Dig/issues/143) | общий Leisure существовал, personal preferences отсутствовали |
| 19 | Вкусовые профили и выбор еды | [#144](https://github.com/bageus/Dig/issues/144) | Food/variety были описаны, taste vector отсутствовал |
| 20 | Партнёрство, беременность и рождение | [#145](https://github.com/bageus/Dig/issues/145) | #11 задавал общий scope без конкретных eligibility/formula/lifecycle |
| 21 | Детство, школа и наследование характеристик | [#146](https://github.com/bageus/Dig/issues/146) | Университет был описан, детская школа и lifecycle отсутствовали |
| 22 | Туман войны и обзор от жителей/зданий | [#147](https://github.com/bageus/Dig/issues/147) | `Explored` и fog-aware hauling существовали, единой visibility model не было |
| 23 | Сюжетная кампания и последовательность миров | — | исключено владельцем дизайна из текущего рассмотрения |
| 24 | Существа, биомы и экологические циклы | [#149](https://github.com/bageus/Dig/issues/149) | World/Combat существовали, Creature/Ecology catalog отсутствовал |
| 25 | Могилы, омоложение и возвращение жителя | [#150](https://github.com/bageus/Dig/issues/150) | #11 имел death event, post-death lifecycle отсутствовал |
| 26 | Одежда, головные уборы и visual identity | [#151](https://github.com/bageus/Dig/issues/151) | resident renderer существовал, data-driven appearance отсутствовал |
| 27 | Разговорные темы и социальная память | [#152](https://github.com/bageus/Dig/issues/152) | #11 упоминал social memory, conversation system отсутствовала |

## Подтверждённые решения, влияющие на backlog

- Пилорама содержит Винокурню и Университет.
- Мебельная мастерская содержит три игровые комнаты.
- Плавильня из scripts соответствует Горну.
- Литейный цех выполняет продвинутую плавку железа и золота на угле.
- Песчаник обрабатывает кристаллическую руду.
- Водолазный колокол исключён.
- Арсенал изучается и производится в Оружейной кузнице.
- «Грибной самогон» является legacy-названием Огненной воды.
- старый `Dojo` трактуется как направление «Кулачный бой».
- Театр, Боулинг, Дискотека, Тренажёрный зал и Бордель пока остаются будущими узлами.

## Рекомендуемый порядок спецификации

1. #126, #128 — дерево и research interaction.
2. #127 — энергия, потому что она блокирует позднее Production.
3. #129, #138, #132 — content и contracts игровых столкновений.
4. #134–#137 — жидкости, подводная работа, двери и транспорт.
5. #130–#133 — здоровье, consumables, statuses и ловушки.
6. #142–#147 — needs, society, education и exploration.
7. #139–#141, #149–#152 — стратегический ИИ, кланы и будущий content.

Порядок не означает автоматического утверждения balance values. Непредоставленные числа остаются `TBD_OWNER` или `BALANCE_TBD`.
