# Продукция и производственные выходы

Этот файл перечисляет предметы, создаваемые Production system. Еда описана в [`food.md`](food.md), алкоголь — в [`alcohol.md`](alcohol.md), грунт и руды — в [`../terrain-resource-output-and-processing.md`](../terrain-resource-output-and-processing.md).

## Общие правила

- каждый продукт имеет stable `ItemId`;
- output создаётся только после successful production commit;
- inputs резервируются и списываются транзакционно;
- один order не выдаёт output дважды;
- quantity задаётся RecipeDefinition или building/tier profile;
- display name не используется как ссылка;
- `material.metal` мигрирует в `material.iron`;
- production skill experience начисляется за каждую committed produced unit;
- один result может выдать mixed skill bundle;
- worker reservation существует только у active order и освобождается terminal policy.

## Комплекты зданий

| ItemId | Продукт | Производитель | Входы | Выход |
|---|---|---|---|---:|
| `building_kit.game_room.basic` | Обычная игровая комната | Мебельная мастерская | 4 ножки, 4 шляпки, 4 камня, 1 хомяк | 1 |
| `building_kit.game_room.industrial` | Индустриальная игровая комната | Мебельная мастерская | 4 ножки, 2 шляпки, 2 камня, 2 хомяка, 4 железа | 1 |
| `building_kit.game_room.luxury` | Игровая комната «Люкс» | Мебельная мастерская | 6 ножек, 6 кристаллов, 2 камня, 2 золота, 6 железа | 1 |
| `building_kit.cinema` | Кинотеатр | Кристаллическая кузня | 8 ножек, 4 кристалла, 2 хомяка, 4 золота, 4 железа | 1 |
| `building_kit.distillery` | Винокурня | Пилорама | 4 ножки, 4 хомяка, 3 камня, 4 железа, 1 золото | 1 |
| `building_kit.university` | Университет | Пилорама | 4 ножки, 4 железа, 2 хомяка, 2 угля, 4 шляпки | 1 |
| `building_kit.arsenal` | Арсенал | Оружейная кузница | data-driven/TBD | 1 |

Системная реализация комплектов: #75. Полные source-workshop связи принадлежат `technology-tree.md`.

## Переработка руд

| RecipeDefinitionId | Продукт | Здание | Входы | Выход | Навык |
|---|---|---|---|---:|---|
| `recipe.furnace.iron` | Железный слиток | Горн | 3 `ore.iron`, 2 ножки гриба | 2 `material.iron` | `skill.metallurgy` |
| `recipe.foundry.iron` | Железный слиток | Литейный цех | 3 `ore.iron`, 2 угля | 2 `material.iron` | `skill.metallurgy` |
| `recipe.foundry.gold` | Золото | Литейный цех | 3 `ore.gold`, 2 угля | 2 `material.gold` | `skill.metallurgy` |
| `recipe.crystal_processor.crystal` | Кристалл | Песчаник | 1 `ore.crystal` | 1 `material.crystal` | `skill.alchemy` |

Каждая produced unit умножает per-unit grant recipe profile. Например, output 2 может выдать двойной per-unit Metallurgy amount.

## Готовые блюда

Выход разрешённого recipe зависит от кухни:

- костёр — 2;
- средневековая — 2;
- индустриальная — 3;
- люксовая — 3.

| ItemId | Продукт | Входы | Разрешённые кухни |
|---|---|---|---|
| `food.grilled_mushroom` | Гриль-гриб | 1 шляпка | все |
| `food.roasted_hamster` | Жареный хомяк | 1 хомяк | medieval, industrial, luxury |
| `food.hamster_stew` | Рагу из хомяка | 1 хомяк, 1 ножка | medieval, industrial, luxury |
| `food.mushroom_stew` | Рагу из грибов | 1 шляпка, 1 ножка | medieval, industrial, luxury |
| `food.larva_soup` | Суп из личинок | 1 личинка, 1 ножка | medieval, industrial, luxury |
| `food.mushroom_bread` | Грибной хлеб | 2 шляпки | industrial, luxury |
| `food.larva_jelly` | Желе из личинок | 1 шляпка, 1 личинка | industrial, luxury |
| `food.meat_pie` | Мясной пирог | 1 шляпка, 1 хомяк | industrial, luxury |
| `food.larva_foie_gras` | Фуагра из личинок | 1 шляпка, 2 личинки | luxury |

Полная модель: [`food.md`](food.md), #98.

## Алкоголь

| ItemId | Продукт | Производитель | Входы | Выход |
|---|---|---|---|---:|
| `alcohol.beer.barrel` | Пиво | Пивоварня | existing/TBD | TBD |
| `alcohol.ale.barrel` | Эль | Пивоварня | 2 ножки, 2 шляпки, 1 личинка | 2 |
| `alcohol.cider.barrel` | Сидр | Пивоварня | 3 ножки, 3 шляпки | 3 |
| `alcohol.glipnir.bottle` | Глипнир | Винокурня | 2 кристалла, 2 шляпки | 1 |
| `alcohol.firewater.bottle` | Огненная вода | Винокурня | 3 кристалла, 2 шляпки, 1 золото | 1 |

«Грибной самогон» — legacy name Огненной воды, не отдельный ItemId.

## Единицы продукции

- порция, бочонок, бутылка, руда и слиток — одна единица `ItemStack.Quantity`;
- начатая meal списывает одну порцию;
- тара/тарелка/форма не создаёт nested authoritative item;
- compatible outputs складываются только по Inventory rules.

## Сервисные результаты

Игровая комната, кинотеатр, бар и Университет создают domain effects, а не ItemStack:

- Mood/Leisure;
- fertility modifier;
- drink/service effects и skill grants;
- TotalSkillCapacity increase.

## Validation

- inputs/outputs существуют;
- building предоставляет capability;
- source workshop соответствует tree;
- output quantity положителен;
- один order коммитится один раз;
- `material.metal` не используется;
- skill profile references валидны;
- produced-unit experience совпадает с quantity;
- worker освобождается после terminal order;
- изменение stable ID требует migration.
