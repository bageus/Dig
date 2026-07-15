# Еда, трапезы и разнообразие рациона

Главная задача расширения: [#96](https://github.com/bageus/Dig/issues/96).

## Назначение

Этот файл является authoritative design-каталогом готовой еды. Он описывает блюда, рецепты, кухонные уровни, поэтапное употребление, выбор желаемого блюда и влияние последних трапез на настроение.

Текущая реализация содержит `Nutrition`, Utility AI intent `Eat`, резервирование еды и complete-only consumption. Целевая модель заменяет её тремя simulation bites и непрерывными effects согласно #97 и #159.

## Владение состоянием

- Agents владеет `Nutrition`, `Mood`, active `Eat` action, bite progress, выбранным желаемым блюдом и diet history;
- Inventory владеет количеством готовой еды до commit начала трапезы;
- Production владеет cooking orders, ingredients и output;
- Technology предоставляет список исследованных блюд;
- World/Exploration предоставляет visibility query;
- Society использует итоговый Mood и не копирует diet history;
- Presentation отображает snapshots и animation profile, но не начисляет effects.

## Масштаб сытости

Design/UI значения блюд заданы в шкале `0..100`. Текущий Domain хранит needs в `0..10000`.

```text
DomainNutrition = DesignNutrition × 100
```

Примеры:

- блюдо `15` даёт `1500` Domain units;
- при трёх укусах один укус даёт `5` UI units или `500` Domain units;
- блюдо `33` даёт `3300`, по `1100` за укус.

Content definitions сохраняют design value и заранее вычисленный/валидируемый domain value либо используют единый converter. Разные системы не реализуют собственные коэффициенты.

## Выбор желаемого блюда

Когда resident принимает намерение поесть:

1. из всех **исследованных** `FoodDefinition` выбирается одно желаемое блюдо;
2. выбор использует отдельный детерминированный random stream и сохраняется в action, поэтому Save/Load не выполняет reroll;
3. сначала система ищет незарезервированную порцию желаемого блюда в текущей видимой зоне;
4. если такая порция найдена, она резервируется и resident идёт к ней;
5. если желаемого блюда нет, система ищет любую другую видимую незарезервированную еду;
6. если нет никакой доступной еды, intent получает `food_unavailable`.

Критический голод не отменяет выбор желаемого блюда, но разрешает fallback на любую еду.

## Модель трапезы

### Резервирование и старт

1. Одна порция резервируется за конкретным resident.
2. До commit начала interruption освобождает reservation без потери еды.
3. При commit начала одна quantity атомарно списывается из Inventory.
4. Порция становится payload единственного active `Eat` action, а не новым ItemStack.
5. Action хранит desired FoodId, consumed FoodId и `Matched = desired == consumed`.

### Три укуса

- стандартная трапеза состоит из `3` укусов;
- каждый завершённый simulation bite немедленно восстанавливает одну треть Nutrition;
- каждый bite может восстановить data-driven базовый Mood;
- положительный базовый Mood еды применяется только для matched meal;
- fallback meal восстанавливает Nutrition, но его положительный food Mood равен нулю;
- третий bite завершает action без повторного полного эффекта;
- animation event только отображает authoritative bite index.

Точные значения базового Mood еды остаются balance data и не выводятся из Nutrition автоматически.

### Прерывание

Если начатая трапеза прервана:

- Nutrition и Mood завершённых укусов сохраняются;
- оставшиеся укусы не применяются;
- остаток порции исчезает;
- еда не возвращается в Inventory и не падает на землю;
- диагностический отчёт показывает completed/wasted bites;
- completion effect не применяется повторно.

Пример: гриль-гриб даёт `15`. После одного укуса resident получает `5` Nutrition UI units. При interruption оставшиеся `10` теряются.

## История последних трапез

Для каждого resident хранится ring buffer из последних `10` фактически начатых трапез.

`DietHistoryEntry` содержит:

- desired `FoodVarietyId`;
- consumed `FoodVarietyId`;
- `Matched`;
- первый committed bite tick;
- число исследованных блюд на момент расчёта;
- source meal action id.

Запись создаётся после первого завершённого укуса. Трапеза, прерванная до первого укуса, не входит в историю. Повторные укусы не создают новые записи.

## Формула разнообразия

После добавления текущей записи система оставляет последние десять и считает `Matches`/`Mismatches`.

Пока записей меньше десяти, diversity Mood delta равна нулю.

Для полного окна:

```text
U = число исследованных блюд на момент завершённого укуса/трапезы

если Mismatches >= 6:
    DiversityMoodDelta = -U
иначе если Matches >= 6 и текущая трапеза Matched:
    DiversityMoodDelta = +U
иначе:
    DiversityMoodDelta = 0
```

Следствия:

- окно `5/5` не изменяет Mood;
- текущий fallback meal никогда не создаёт положительный delta;
- плохая история продолжает давать отрицательный delta, пока окно не улучшится;
- чем больше исследованных блюд, тем сильнее положительное или отрицательное влияние;
- Mood clamp применяется после delta;
- рождаемость меняется только косвенно через Mood;
- алкоголь не входит в выбор без `Food`/`FoodVarietyId`.

## FoodDefinition

Каждое блюдо определяет:

- stable `ItemId`;
- `FoodVarietyId`;
- localization key;
- `TotalNutritionDesignUnits`;
- `TotalNutritionDomainUnits` или единый conversion contract;
- `BiteCount = 3`;
- `BaseMoodPerBite`;
- `AnimationProfileId`;
- stack/storage/hauling rules;
- recipe reference;
- content version.

## Уровни кухни

| Kitchen tier | Предлагаемый ID | Выход любого разрешённого рецепта |
|---|---|---:|
| Костёр | `kitchen.campfire` | 2 порции |
| Средневековая кухня | `kitchen.medieval` | 2 порции |
| Индустриальная кухня | `kitchen.industrial` | 3 порции |
| Люксовая кухня | `kitchen.luxury` | 3 порции |

Выход зависит от kitchen tier, а не от display name блюда.

## Работник кухни

Кухня не требует постоянного закреплённого работника.

- worker нужен только при наличии cooking order;
- Production/Jobs выбирают и резервируют допустимого resident;
- после completion, cancellation или terminal block worker reservation освобождается;
- пустая очередь готовки не удерживает resident у кухни;
- количество worker places определяется BuildingDefinition;
- влияние уровня `skill.cooking` на скорость/output/effects остаётся Q-039.

## Каталог блюд

Обозначения: `К` — костёр, `С` — средневековая, `И` — индустриальная, `Л` — люксовая.

| ItemId | Блюдо | Ингредиенты | Сытость UI | Domain | За укус UI | К | С | И | Л |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|
| `food.grilled_mushroom` | Гриль-гриб | 1 шляпка гриба | 15 | 1500 | 5 | 2 | 2 | 3 | 3 |
| `food.roasted_hamster` | Жареный хомяк | 1 хомяк | 18 | 1800 | 6 | — | 2 | 3 | 3 |
| `food.hamster_stew` | Рагу из хомяка | 1 хомяк, 1 ножка | 24 | 2400 | 8 | — | 2 | 3 | 3 |
| `food.mushroom_stew` | Рагу из грибов | 1 шляпка, 1 ножка | 21 | 2100 | 7 | — | 2 | 3 | 3 |
| `food.larva_soup` | Суп из личинок | 1 личинка, 1 ножка | 24 | 2400 | 8 | — | 2 | 3 | 3 |
| `food.mushroom_bread` | Грибной хлеб | 2 шляпки | 27 | 2700 | 9 | — | — | 3 | 3 |
| `food.larva_jelly` | Желе из личинок | 1 шляпка, 1 личинка | 27 | 2700 | 9 | — | — | 3 | 3 |
| `food.meat_pie` | Мясной пирог | 1 шляпка, 1 хомяк | 27 | 2700 | 9 | — | — | 3 | 3 |
| `food.larva_foie_gras` | Фуагра из личинок | 1 шляпка, 2 личинки | 33 | 3300 | 11 | — | — | — | 3 |

Число в столбце кухни — output quantity одного production cycle.

## Utility AI

Food choice учитывает:

1. критичность голода;
2. сохранённый desired dish roll;
3. visibility;
4. незарезервированный stock;
5. путь;
6. fallback availability;
7. риск переполнения Nutrition;
8. срочные действия.

После выбора target reservation выполняется атомарно. Несколько residents не могут выбрать одну quantity.

## Анимации

Разные блюда могут использовать разные `AnimationProfileId`, но:

- число укусов задаёт simulation;
- Presentation читает bite index;
- потерянный callback не задерживает authoritative effect бесконечно;
- повторный callback не создаёт второй effect;
- interruption немедленно переключает view после command result.

## Save/Load

Сохраняются:

- active meal desired/consumed IDs;
- match flag;
- committed payload;
- completed bites;
- уже применённые Nutrition/Mood;
- diet ring buffer из 10 entries;
- deterministic random state/id для следующего выбора;
- content/schema version.

Load не выбирает желаемое блюдо заново и не добавляет history entry повторно.

## Инварианты

- одна food quantity не кормит двух residents;
- после commit порция не существует одновременно в Inventory и meal payload;
- каждый bite применяется один раз;
- сумма трёх Nutrition bites равна полному значению;
- fallback не даёт положительный базовый food Mood;
- history содержит максимум 10 entries;
- одна meal action создаёт максимум одну entry;
- diversity delta использует authoritative Technology snapshot/count;
- worker существует только для активного order;
- kitchen tier restriction проверяется вне UI.

## Диагностика

Inspector показывает:

- desired/consumed FoodId и match;
- random stream/source decision id;
- source stack/reservation;
- completed bites;
- Nutrition/Mood per bite и accumulated effect;
- lost remainder;
- последние 10 entries;
- Matches/Mismatches;
- `UnlockedDishCount`;
- base и diversity Mood deltas;
- kitchen tier, worker, order и block reason.

## Связанные issues

- [#97](https://github.com/bageus/Dig/issues/97) — bite-based meal;
- [#98](https://github.com/bageus/Dig/issues/98) — recipes, tiers и cooking worker;
- [#99](https://github.com/bageus/Dig/issues/99) — desired dish и history formula;
- [#100](https://github.com/bageus/Dig/issues/100) — UI/animations;
- [#101](https://github.com/bageus/Dig/issues/101) — Save/Load/tests;
- [#159](https://github.com/bageus/Dig/issues/159) — continuous need effects.
