# Грунт, добыча ресурсов и переработка руд

## 1. Назначение

Документ описывает два независимых источника добычи:

1. случайные outputs обычной клетки грунта при excavation;
2. явную ресурсную жилу, заменяющую клетку породы.

Он также фиксирует цепочки `руда -> обработанный материал` и здания переработки.

## 2. Разделение сущностей

- `MaterialId` — тип грунта/породы клетки World;
- `DepositDefinitionId` — ресурсная жила, заменяющая обычную клетку;
- `ItemId` — выпавший предмет в Inventory/world stack;
- `RecipeDefinitionId` — производственный рецепт переработки;
- `BuildingDefinitionId` — здание, предоставляющее производственную capability.

Display name не используется как ссылка между этими сущностями.

## 3. Обычный грунт и случайное выпадение

Обычная excavation использует `TerrainOutputProfile`. Таблица определяет только допустимые outputs и относительную редкость. Точные вероятности и количества остаются data-driven `BALANCE_TBD`.

| Предлагаемый MaterialId | Название | Добываемость | Возможные outputs |
|---|---|---:|---|
| `terrain.sand` | Песчаный грунт | да | ничего |
| `terrain.stone_rock` | Каменная порода | да | только `material.stone` |
| `terrain.metal_bearing_rock` | Металлосодержащая / «серная» порода | да | камень; немного железной руды; очень мало золотой руды; мало угля |
| `terrain.crystalline_rock` | Кристаллическая порода | да | мало камня; железная руда; кристаллическая руда; мало золотой руды |
| `terrain.lava_rock` | Лавовая порода | да | золотая руда; мало камня; мало кристаллической руды; железная руда; уголь |
| `terrain.unmineable` | Недобываемая порода | нет | ничего |

Точное display name `terrain.metal_bearing_rock` блокировано Q-024.

### 3.1 Правила roll

- результат вычисляется детерминированно из `WorldSeed + GeneratorVersion + CellId + OutputProfileVersion`;
- Unity random/frame order не влияет на результат;
- одна клетка выполняет roll только один раз при успешном excavation commit;
- пустой результат допустим;
- outputs создаются как world stacks на земле;
- шахтёр не помещает их автоматически в личный inventory;
- failure/cancel до commit не расходует roll и не создаёт предметы;
- save/replay не может повторно выдать output одной клетки.

### 3.2 Недобываемая порода

`terrain.unmineable`:

- не принимает `Dig` designation;
- не создаёт digging job;
- блокирует template placement, если шаблон включает такую клетку;
- показывает понятную причину в preview/inspector;
- может использоваться как задняя/краевая граница мира.

## 4. Ресурсные жилы

Жила заменяет обычную клетку породы. Она не является overlay, который одновременно выдаёт базовый loot грунта.

Поддерживаемые текущим design типы:

| Предлагаемый DepositDefinitionId | Output ItemId |
|---|---|
| `deposit.iron_ore` | `ore.iron` |
| `deposit.gold_ore` | `ore.gold` |
| `deposit.crystal_ore` | `ore.crystal` |
| `deposit.coal` | `material.coal` |
| `deposit.stone` | `material.stone` |

### 4.1 Соседство

Фраза «жила занимает 1–4 клетки» означает, что generation может разместить рядом несколько deposit cells. Это не требует общей cluster-сущности:

- каждая клетка имеет собственный stable `DepositId`;
- каждая клетка истощается отдельно;
- соседние клетки могут иметь одинаковый тип и визуально выглядеть как одна жила;
- общий shared yield/depletion между соседями не создаётся;
- генератор может иметь правило группового размещения 1–4 клеток, но runtime semantics остаётся per-cell.

### 4.2 Разработка и геометрия

- разработка жилы требует больше work effort, чем обычный грунт;
- после успешного commit жила выдаёт только свой output;
- клетка становится пустой/проходимой;
- боковая стена исчезает при разработке клетки сбоку;
- задняя стена углубляется при разработке следующей клетки по `Z`;
- depleted cell не выдаёт output повторно;
- одна клетка не может одновременно иметь обычный terrain output и deposit output.

## 5. Предметы руды и готовые материалы

| ItemId | Display name | Назначение |
|---|---|---|
| `ore.iron` | Железная руда | вход плавки железа |
| `ore.gold` | Золотая руда | вход плавки золота |
| `ore.crystal` | Кристаллическая руда | вход обработки кристалла |
| `material.iron` | Железо / железный слиток / металл | готовый металл для рецептов |
| `material.gold` | Золото | готовый драгоценный металл |
| `material.crystal` | Кристалл | готовый кристалл |
| `material.coal` | Уголь | топливо и алхимический материал |
| `material.stone` | Камень | строительный материал |

`material.metal` не является отдельным предметом. Старые ссылки должны мигрировать в `material.iron`.

## 6. Здания переработки

### 6.1 Горн

Предлагаемый ID: `building.furnace`.

Capability:

- плавит только железную руду;
- успешная работа развивает `skill.metallurgy`;
- рецепт: `3 ore.iron + 2 material.mushroom_leg -> 2 material.iron`.

### 6.2 Литейный цех

Предлагаемый ID: `building.foundry`.

Capabilities:

- плавка железа;
- плавка золота;
- успешная работа развивает `skill.metallurgy`.

Рецепты:

- `3 ore.iron + 2 material.coal -> 2 material.iron`;
- `3 ore.gold + 2 material.coal -> 2 material.gold`.

### 6.3 Переработчик кристаллической руды

Технический ID до ответа Q-023: `building.crystal_processor`.

Capability:

- обрабатывает кристаллическую руду;
- успешная работа развивает `skill.alchemy`, поскольку она относится к кристаллам;
- рецепт: `1 ore.crystal -> 1 material.crystal`.

Официальное display name «Песчанник/Песчаник/другое» остаётся открытым.

## 7. Общий производственный поток

1. Production order резервирует все inputs.
2. Hauling доставляет раскрытые и доступные inputs согласно demand policy.
3. Worker резервирует рабочее место.
4. Work progress использует skill и building profile.
5. При успешном commit inputs списываются один раз.
6. Outputs создаются один раз в building inventory/output location.
7. Skill experience grant создаётся идемпотентно после подтверждённого вклада.
8. Cancel/failure освобождает reservations и не создаёт output.

## 8. Открытые параметры

- точные drop probabilities и количества для каждого terrain profile;
- частота появления типов грунта и deposit cells;
- work effort обычного грунта и жил;
- стоимость строительства, источник building kits и число работников горна, литейного цеха и crystal processor — Q-026;
- официальные названия «Песчанник» и «серная порода» — Q-023/Q-024.

## 9. Save/Load и validation

Сохраняются:

- `MaterialId` клетки и факт выполнения output roll;
- deposit id/type/depleted state;
- generator/output profile versions;
- world stacks и reservations;
- production orders и input/output state;
- stable recipe/building IDs.

Validation запрещает:

- ссылку рецепта на неизвестный ItemId;
- `material.metal` как отдельный новый предмет;
- mineable output у `terrain.unmineable`;
- одновременный terrain и deposit output одной клетки;
- повторный roll/depletion;
- недетерминированный Unity random как источник game rules.

## 10. Связанные issues

- #87 — общий feature копания;
- #91 — генерация жил;
- #92 — output resolver;
- #103/#105 — навыки работ;
- #108 — металлургические здания и рецепты;
- #109 — terrain profiles и случайные outputs;
- #110 — demand/fog-aware hauling.
