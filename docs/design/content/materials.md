# Материалы, руды и ингредиенты

Этот файл фиксирует authoritative каталог известных ресурсов, руд и типов грунта. Системные правила случайной добычи и переработки подробно описаны в [`../terrain-resource-output-and-processing.md`](../terrain-resource-output-and-processing.md).

## 1. Разделение типов

- `MaterialId` — грунт/порода клетки World;
- `DepositDefinitionId` — ресурсная жила, заменяющая обычную клетку;
- `ItemId` — предмет или stack Inventory;
- `AgentSkillId` — навык; он никогда не совпадает с ItemId.

Пример: `terrain.stone_rock`, `deposit.stone`, `material.stone` и `skill.stonework` — четыре разные сущности.

## 2. Обработанные материалы и ингредиенты

| ItemId | Название | Категории | Подтверждённые применения |
|---|---|---|---|
| `material.mushroom_leg` | Ножка гриба | `Organic`, `MushroomPart`, `CraftingIngredient`, `FuelIngredient` | здания, еда, напитки, горн |
| `material.mushroom_cap` | Шляпка гриба | `Organic`, `MushroomPart`, `CraftingIngredient` | здания, еда, напитки |
| `material.stone` | Камень | `Mineral`, `BuildingMaterial` | строительство и рецепты |
| `material.hamster` | Хомяк | `Organic`, `CraftingIngredient`, `FoodIngredient` | здания, экипировка, еда |
| `material.larva` | Личинка | `Organic`, `BrewingIngredient`, `FoodIngredient` | еда и эль |
| `material.iron` | Железо / железный слиток / металл | `Metal`, `BuildingMaterial`, `WeaponMaterial` | все прежние рецепты «железа» и «металла» |
| `material.gold` | Золото | `Metal`, `Precious`, `AlchemyIngredient` | здания, экипировка, напитки |
| `material.crystal` | Кристалл | `Mineral`, `Crystal`, `AlchemyIngredient` | здания, алхимия, напитки |
| `material.coal` | Уголь | `Mineral`, `Fuel`, `AlchemyIngredient` | литейный цех, университет, алхимия |

### 2.1 Железо и металл

`material.metal` не является отдельным предметом. «Металл» — допустимое общее/display название железного слитка `material.iron` и/или категория `Metal`.

Все ранее описанные рецепты, использовавшие `material.metal`, должны использовать `material.iron`. Изменение требует миграции ссылок до стабилизации save schema.

## 3. Руды

| ItemId | Название | Переработка |
|---|---|---|
| `ore.iron` | Железная руда | горн или литейный цех -> `material.iron` |
| `ore.gold` | Золотая руда | литейный цех -> `material.gold` |
| `ore.crystal` | Кристаллическая руда | Песчаник -> `material.crystal` |

Отдельное самородное золото текущим design не требуется.

## 4. Типы грунта

| MaterialId | Название | Добываемость | Допустимые случайные outputs |
|---|---|---:|---|
| `terrain.sand` | Песчаный грунт | да | нет |
| `terrain.stone_rock` | Каменная порода | да | только камень |
| `terrain.metal_bearing_rock` | Рудная порода | да | камень; немного железной руды; очень мало золотой руды; мало угля |
| `terrain.crystalline_rock` | Кристаллическая порода | да | мало камня; железная руда; кристаллическая руда; мало золотой руды |
| `terrain.lava_rock` | Лавовая порода | да | золотая руда; мало камня; мало кристаллической руды; железная руда; уголь |
| `terrain.unmineable` | Недобываемая порода | нет | нет |

Точные вероятности и количества — data-driven `BALANCE_TBD`. Официальное display name `terrain.metal_bearing_rock` — «Рудная порода».

## 5. Жилы

| DepositDefinitionId | Название | Output |
|---|---|---|
| `deposit.iron_ore` | Жила железной руды | `ore.iron` |
| `deposit.gold_ore` | Жила золотой руды | `ore.gold` |
| `deposit.crystal_ore` | Жила кристаллической руды | `ore.crystal` |
| `deposit.coal` | Угольная жила | `material.coal` |
| `deposit.stone` | Каменная жила/глыба | `material.stone` |

Жила заменяет клетку обычного грунта и выдаёт только свой output. После истощения клетка становится пустой и расширяет проход/помещение.

Несколько deposit cells могут находиться рядом, но каждая имеет собственные id, output и depletion state.

## 6. Переработка

| RecipeDefinitionId | Здание | Вход | Выход |
|---|---|---|---|
| `recipe.furnace.iron` | Горн | 3 железной руды + 2 ножки гриба | 2 железа |
| `recipe.foundry.iron` | Литейный цех | 3 железной руды + 2 угля | 2 железа |
| `recipe.foundry.gold` | Литейный цех | 3 золотой руды + 2 угля | 2 золота |
| `recipe.crystal_processor.crystal` | Песчаник | 1 кристаллическая руда | 1 кристалл |

Плавка железа/золота развивает `skill.metallurgy`. Обработка кристаллической руды развивает `skill.alchemy`.

Строительные параметры этих зданий не дублируются здесь: используются существующие `BuildingDefinition` и связанные content data, которые будут дорабатываться в той же системе.

## 7. Правила definitions

Каждый ItemDefinition определяет stable ID, localization, категории, stack size, visual, storage filters и источники/рецепты.

Каждый terrain profile определяет solidity, hardness, mineability, visual и deterministic output table.

Каждый deposit определяет output, work effort, reveal, visual и depleted behavior.

## 8. Validation

До запуска проверяется:

- уникальность IDs в своих каталогах;
- существование recipe inputs/outputs;
- отсутствие отдельного `material.metal`;
- отсутствие output у `terrain.unmineable`;
- валидность вероятностей/количеств;
- невозможность одновременного terrain и deposit output одной клетки;
- deterministic generation/output versions;
- миграция при изменении stable IDs;
- поиск Песчаника и Рудной породы по stable ID, а не display name.

## 9. Матрица использования

| Ресурс | Основное назначение |
|---|---|
| Ножка гриба | строительство, еда, напитки, топливо горна |
| Шляпка гриба | строительство, еда, напитки |
| Камень | строительство |
| Железо | здания, оружие, экипировка, прежние рецепты «металла» |
| Золото | дорогие здания, экипировка, алхимия |
| Кристалл | дорогие здания и алхимия |
| Уголь | литейный цех, университет, алхимия |
| Железная/золотая/кристаллическая руда | входы переработки |

## 10. Открытые параметры

- Q-014 — точные вероятности, yields и трудоёмкость;
- будущая доработка существующих BuildingDefinition для производственных зданий.