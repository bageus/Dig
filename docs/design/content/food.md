# Еда, трапезы и разнообразие рациона

Главная задача: #96. Реализация трапезы: #97–#101. Continuous needs: #159.

## Назначение

Authoritative design-каталог готовой еды, выбора блюда, укусов, истории последних трапез и кухонного производства.

## Владение состоянием

- Agents — Nutrition, Mood, active Eat action, desired dish и diet history;
- Inventory — готовые порции до commit трапезы;
- Production — cooking orders, ingredients и outputs;
- Technology — исследованный каталог блюд;
- Exploration — текущая visibility еды;
- Society — читает итоговый Mood, не копирует food history;
- Presentation — отображает snapshots и animation profile.

## Шкала сытости

UI/design использует `0..100`, Domain — `0..10000`.

```text
DomainNutrition = DesignNutrition × 100
```

Пример: `15 -> 1500`; один из трёх укусов гриль-гриба даёт `500` Domain units.

## Выбор желаемого блюда

При создании Eat intent:

1. одно желаемое блюдо детерминированно-случайно выбирается из всех исследованных FoodDefinition;
2. выбор сохраняется в action и не reroll после Save/Load;
3. сначала ищется незарезервированная порция desired dish в текущей видимой зоне;
4. если её нет, ищется любая другая видимая незарезервированная еда;
5. если еды нет, intent получает `food_unavailable`.

Индивидуальных вкусовых профилей, taste vectors и наследуемых food preferences **нет**. Все исследованные блюда имеют равный базовый вес выбора. #144 закрывается как ненужное расширение.

## Трапеза из трёх укусов

### Старт

- одна порция резервируется за resident;
- interruption до commit освобождает reservation без потери;
- при commit quantity списывается из Inventory;
- порция становится payload active Eat action, а не вторым ItemStack;
- action хранит desired/consumed IDs и `Matched`.

### Прогресс

- стандартная порция имеет три simulation bites;
- каждый завершённый bite немедленно применяет треть Nutrition;
- matched meal может применять data-driven положительный Mood per bite;
- fallback meal восстанавливает Nutrition, но не даёт положительный базовый food Mood;
- третий bite завершает action без повторного full effect;
- animation callback не начисляет effects.

Точный смысл фразы о восстановлении Mood «до 50» остаётся Q-040: нужно подтвердить cap базового food Mood и взаимодействие с diversity bonus.

### Прерывание

После commit:

- effects завершённых укусов сохраняются;
- оставшиеся укусы теряются;
- остаток порции не возвращается и не падает на землю;
- completion не выдаёт повторный effect.

## История последних 10 приёмов пищи

`DietHistoryEntry` содержит:

- desired FoodVarietyId;
- consumed FoodVarietyId;
- Matched;
- first completed bite tick;
- researched dish count;
- source meal id.

Entry создаётся после первого завершённого укуса. Meal без завершённых укусов не входит в историю. Хранится максимум 10 entries.

## Формула разнообразия

После добавления текущей записи:

```text
U = число исследованных блюд

если history count < 10:
    delta = 0
иначе если Mismatches >= 6:
    delta = -U
иначе если Matches >= 6 и current Matched:
    delta = +U
иначе:
    delta = 0
```

Следствия:

- `5/5 -> 0`;
- current fallback не создаёт положительный diversity delta;
- плохое окно продолжает штрафовать, пока история не улучшится;
- сила эффекта зависит от количества исследованных блюд;
- рождаемость изменяется только косвенно через Mood;
- алкоголь не входит в food variety без Food tag.

## Кухни и работник

Постоянного повара нет.

- worker нужен только при active cooking order;
- claim/reservation выполняется Production/Jobs;
- completion/cancel/terminal block освобождают worker;
- пустая очередь не удерживает resident.

`skill.cooking` влияет **только на скорость приготовления**.

Он не изменяет:

- ingredients;
- output quantity;
- Nutrition;
- Mood effects;
- число укусов;
- качество блюда;
- шанс дополнительных результатов.

Точная monotonic speed curve хранится в data и остаётся balance tuning. Все расчёты скорости используют один Recipe/Work speed contract.

## Выход по tier

| Kitchen tier | ID | Порций за cycle |
|---|---|---:|
| Костёр | `kitchen.campfire` | 2 |
| Средневековая кухня | `kitchen.medieval` | 2 |
| Индустриальная кухня | `kitchen.industrial` | 3 |
| Люксовая кухня | `kitchen.luxury` | 3 |

Output определяется tier, а не Cooking skill.

## Каталог блюд

| ItemId | Блюдо | Ингредиенты | Nutrition UI | Domain | За укус UI | Доступность |
|---|---|---|---:|---:|---:|---|
| `food.grilled_mushroom` | Гриль-гриб | 1 шляпка | 15 | 1500 | 5 | все кухни |
| `food.roasted_hamster` | Жареный хомяк | 1 хомяк | 18 | 1800 | 6 | medieval+ |
| `food.hamster_stew` | Рагу из хомяка | 1 хомяк + 1 ножка | 24 | 2400 | 8 | medieval+ |
| `food.mushroom_stew` | Рагу из грибов | 1 шляпка + 1 ножка | 21 | 2100 | 7 | medieval+ |
| `food.larva_soup` | Суп из личинок | 1 личинка + 1 ножка | 24 | 2400 | 8 | medieval+ |
| `food.mushroom_bread` | Грибной хлеб | 2 шляпки | 27 | 2700 | 9 | industrial+ |
| `food.larva_jelly` | Желе из личинок | 1 шляпка + 1 личинка | 27 | 2700 | 9 | industrial+ |
| `food.meat_pie` | Мясной пирог | 1 шляпка + 1 хомяк | 27 | 2700 | 9 | industrial+ |
| `food.larva_foie_gras` | Фуагра из личинок | 1 шляпка + 2 личинки | 33 | 3300 | 11 | luxury |

Новые блюда из Ecology, включая омлет из паучьего яйца, добавляются только после разрешения вопросов #149/Q-045 и не меняют общую модель укусов/истории автоматически.

## Save/Load

Сохраняются:

- desired/consumed IDs и Matched;
- committed meal payload;
- completed bites и применённые effects;
- progress до следующего bite;
- ring buffer последних 10 entries;
- deterministic random source id/state;
- cooking orders и worker/ingredient reservations;
- content/schema version.

Load не reroll desired dish, не возвращает committed quantity и не дублирует history/effects.

## Инварианты

- одна quantity не кормит двух residents;
- каждый bite применяется максимум один раз;
- сумма трёх Nutrition bites равна полному значению;
- fallback не даёт положительный базовый food Mood;
- history содержит максимум 10 entries;
- Cooking skill не меняет output/effects;
- output tier restriction проверяется вне UI;
- Presentation не начисляет Needs.

## Диагностика

Inspector показывает desired/consumed/match, visibility/fallback reason, source reservation, bite progress, Nutrition/Mood effects, history 10, Matches/Mismatches, researched dish count, formula branch, kitchen tier, worker, speed multiplier и block reason.

## Связанные issues

- #97 — bites/interruption;
- #98 — recipes, tiers, worker и Cooking speed;
- #99 — desired dish и diet history;
- #100 — UI/animations;
- #101 — Save/Load/tests;
- #144 — taste profiles отклонены;
- #159 — continuous needs.