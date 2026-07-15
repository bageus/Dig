# Материалы, руды и ингредиенты

Этот файл фиксирует названия ресурсов, встречающиеся в текущих design-рецептах и системе добычи. Он не утверждает, что перечислены все ресурсы игры.

Блокирующие терминологические решения находятся в [`../open-questions.md`](../open-questions.md), прежде всего Q-007, Q-008, Q-009 и Q-013.

## 1. Разделение terrain и item definitions

Нельзя использовать один display name как неявную ссылку между породой мира и предметом в Inventory.

- `MaterialId` описывает terrain/породу клетки World: solidity, hardness и визуальный профиль.
- `ItemId` описывает добытый предмет/стек Inventory: количество, категории, stack size и рецепты.
- `DepositDefinitionId` описывает жилу или ресурсное включение и связывает host terrain с item outputs.

Пример: `terrain.rock` может быть твёрдой породой, а `material.stone` — предметом, выпавшим после разработки. Эти определения связаны data-driven output rule, но не являются одной сущностью.

## 2. Известные definitions из рецептов

| Предлагаемый ItemId | Display name | Предлагаемые категории | Подтверждённые применения |
|---|---|---|---|
| `material.mushroom_leg` | Ножка гриба / ножка | `Organic`, `MushroomPart`, `CraftingIngredient` | комплекты зданий, эль, сидр, винокурня, университет, кинотеатр |
| `material.mushroom_cap` | Шляпка гриба / шляпка | `Organic`, `MushroomPart`, `CraftingIngredient` | игровые комнаты, эль, сидр, глипнир, огненная вода, университет |
| `material.stone` | Камень | `Mineral`, `BuildingMaterial` | игровые комнаты, винокурня; добыча — точное правило Q-009 |
| `material.hamster` | Хомяк | `Organic`, `CraftingIngredient` | комплекты зданий и экипировки |
| `material.iron` | Железо | `Metal`, `BuildingMaterial`, `WeaponMaterial` | индустриальная и люкс-комнаты, кинотеатр, ножны, разгрузка; связь с рудой Q-008 |
| `material.metal` | Металл | `Metal`, `BuildingMaterial` | винокурня, университет; связь с железом Q-008/Q-013 |
| `material.crystal` | Кристалл | `Mineral`, `Crystal`, `AlchemyIngredient` | люкс-комната, кинотеатр, глипнир, огненная вода; добывается из crystal deposit |
| `material.gold` | Золото | `Metal`, `Precious`, `AlchemyIngredient` | люкс-комната, кинотеатр, винокурня, огненная вода, разгрузка; связь с золотой рудой Q-007 |
| `material.larva` | Личинка | `Organic`, `BrewingIngredient` | эль |
| `material.coal` | Уголь | `Mineral`, `Fuel` | университет; добывается из coal deposit |

## 3. Кандидаты raw extraction content

Следующие IDs являются предлагаемыми и не должны считаться финальными до ответов на открытые вопросы.

| Предлагаемый ID | Display name | Тип | Возможный выход/назначение | Статус |
|---|---|---|---|---|
| `terrain.rock` | Каменная порода | `MaterialId` | host terrain, hardness, возможный выход `material.stone` | schema proposal |
| `deposit.stone` | Каменное включение | `DepositDefinitionId` | дополнительный/одиночный камень | смысл Q-009 |
| `deposit.crystal` | Кристаллическая жила | `DepositDefinitionId` | `material.crystal` | подтверждён тип, yield TBD |
| `ore.iron` | Железная руда | `ItemId` | переплавка в `material.iron` | Q-008 |
| `deposit.iron_ore` | Жила железной руды | `DepositDefinitionId` | `ore.iron` | подтверждён тип, yield TBD |
| `ore.gold` | Золотая руда | `ItemId` | переплавка в `material.gold` | Q-007 |
| `deposit.gold_ore` | Жила золотой руды | `DepositDefinitionId` | `ore.gold` | подтверждён тип, yield TBD |
| `deposit.native_gold` | Самородное золото | `DepositDefinitionId` | возможно `material.gold` напрямую | Q-007 |
| `deposit.coal` | Угольный пласт | `DepositDefinitionId` | `material.coal` | подтверждён тип, yield TBD |

## 4. Железная руда, железо и металл

До реализации запрещено молча объединять или подменять эти definitions.

Нужно решить:

1. `ore.iron -> smelting -> material.iron`;
2. является ли `material.metal` отдельным обработанным ресурсом;
3. является ли `Metal` только категорией, а не предметом;
4. какие existing recipes должны использовать raw ore, iron или generic metal.

Решение фиксируется в Q-008/Q-013 до появления production/save data.

## 5. Золото и золотая руда

Новый design одновременно перечисляет золото и золотую руду. Возможные модели:

- только `ore.gold`, которое переплавляется в `material.gold`;
- `ore.gold` плюс редкий `native_gold`, сразу выдающий `material.gold`;
- слово «золото» в списке жил было синонимом/дубликатом золотой руды.

Финальная модель блокирована Q-007.

## 6. Камень

`Камень` используется минимум в трёх разных смыслах, которые должны иметь разные IDs:

- terrain material/порода;
- добытый item `material.stone`;
- возможно название навыка, блокирующего шаблоны комнат.

Если пороги 20/40/60 относятся к навыку, рекомендуется stable id вроде `skill.stonework`, а не строка `stone`, общая с предметом. См. Q-003.

Точное правило выхода камня из обычной породы блокировано Q-009.

## 7. Ножка и шляпка

Короткие названия `ножка` и `шляпка` в рецептах интерпретируются как части гриба. В UI допустимы полные display names «ножка гриба» и «шляпка гриба», но ссылки используют стабильные ItemId.

## 8. Хомяк и личинка

Названия сохраняются без переосмысления. Каталог не утверждает, что эти предметы являются едой. Они считаются ингредиентами/материалами, пока отдельное food definition не укажет иное.

## 9. Правила ItemDefinition

Каждый материал/руда должен определять:

- stable ItemId;
- display name и localization key;
- категории;
- `MaxStackSize`;
- массу/объём, если такие ограничения войдут в scope;
- visual definition;
- допустимость прямого использования;
- storage filters;
- technology/source definition;
- spoilage, если применимо.

## 10. Правила DepositDefinition

Каждая жила должна определять:

- stable `DepositDefinitionId`;
- допустимые host terrain categories;
- output ItemId или набор outputs;
- yield/range;
- work effort/hardness modifier;
- cluster size/shape policy;
- reveal conditions;
- visual profile для hidden/revealed/depleted состояния;
- biome/layer generation weights;
- depleted behavior.

Одна deposit cell не должна повторно выдавать output после depletion. Несколько клеток одной жилы связываются stable `DepositId`.

## 11. Content validation

Проверки до запуска симуляции:

- MaterialId, ItemId и DepositDefinitionId уникальны в своих каталогах;
- recipe input ссылается на существующий ItemId;
- deposit output ссылается на существующий ItemId;
- host terrain category существует;
- количество и yield положительны;
- категории существуют;
- stack size положителен;
- building kit recipe не использует неразрешённый ingredient;
- `железо`/`металл` и `золото`/`золотая руда` не разрешаются через display-name comparison;
- изменение стабильного ID требует save migration;
- одинаковый seed/version создаёт одинаковые deposit IDs и locations.

## 12. Матрица использования в текущих рецептах

| Ресурс | Игровые комнаты | Кинотеатр | Винокурня | Университет | Алкоголь | Инвентарь/экипировка |
|---|---:|---:|---:|---:|---:|---:|
| Ножка | да | да | да | да | эль, сидр | разгрузка |
| Шляпка | да | нет | нет | да | эль, сидр, глипнир, огненная вода | нет |
| Камень | да | нет | да | нет | нет | нет |
| Хомяк | да | да | да | да | нет | ножны, разгрузка |
| Железо | индустриальная, люкс | да | нет | нет | нет | ножны, разгрузка |
| Металл | нет | нет | да | да | нет | нет |
| Кристалл | люкс | да | нет | нет | глипнир, огненная вода | нет |
| Золото | люкс | да | да | нет | огненная вода | разгрузка |
| Личинка | нет | нет | нет | нет | эль | нет |
| Уголь | нет | нет | нет | да | нет | нет |

Матрица является навигационной и не заменяет RecipeDefinition.