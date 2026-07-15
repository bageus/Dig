# Непрерывные действия потребностей

Связанная задача: [#159](https://github.com/bageus/Dig/issues/159).

## Назначение

Этот документ задаёт целевую design-модель действий `Eat`, `Sleep` и `Leisure`. Текущая реализация частично использует complete-only effects; до завершения #97 и #159 это является известным implementation gap, а не альтернативным правилом дизайна.

## Владение состоянием

- Agents владеет active action, Nutrition, Alertness, Mood и уже применённым progress effect;
- Inventory владеет едой до commit начала трапезы;
- Buildings владеет Bed/Leisure places;
- Jobs/Reservations владеют общим lifecycle назначений и мест;
- Presentation отображает progress и animation profile, но не начисляет effects.

## Общий принцип

Потребность восстанавливается тогда, когда resident фактически выполняет действие.

- каждый подтверждённый simulation interval имеет stable idempotency key;
- эффект применяется сразу после interval commit;
- прерывание сохраняет уже применённый эффект;
- после прерывания новые intervals не создаются;
- completion не повторяет сумму уже применённых эффектов;
- значения clamp в authoritative диапазоне после каждого применения.

## Еда

- стандартная порция состоит из трёх укусов;
- каждый завершённый укус восстанавливает одну треть Nutrition блюда;
- еда также восстанавливает Mood по data-driven `MoodPerBite`;
- если фактически съеденное блюдо не совпало со случайно выбранным желаемым блюдом, положительный food Mood effect этой трапезы не применяется;
- после первого подтверждённого укуса трапеза считается фактически начатой для diet history;
- interruption сохраняет завершённые bite effects и уничтожает оставшуюся часть порции;
- interruption до commit начала освобождает reservation и не создаёт diet entry.

Подробная модель еды: [`content/food.md`](content/food.md).

## Сон

Пока действие `Sleep` активно:

- Alertness восстанавливается через повторяющиеся simulation intervals;
- Mood также восстанавливается через интервалы;
- точные значения и частота задаются `SleepEffectProfile`;
- пробуждение, приказ, угроза, смерть или потеря Bed place прекращают новые intervals;
- уже восстановленные Alertness и Mood не откатываются;
- Bed reservation освобождается по единой finish/interruption policy.

## Досуг

Пока действие `Leisure` активно:

- Mood восстанавливается через повторяющиеся simulation intervals;
- скорость зависит от `LeisureActivityDefinition`, качества места и будущих modifiers;
- interruption сохраняет уже полученный Mood;
- завершение или interruption освобождает Leisure place;
- анимация не является источником Mood.

## Action snapshot

Целевой snapshot содержит:

- action kind;
- target/reservation id;
- start tick;
- completed interval count;
- accumulated Nutrition/Alertness/Mood;
- next interval progress;
- effect profile/version;
- interruption reason;
- committed food payload и bite index для Eat.

## Save/Load

Сохранение в середине действия обязано восстанавливать:

- active action;
- target reservation;
- completed intervals/bites;
- накопленный effect;
- progress до следующего interval;
- food payload без повторного создания ItemStack.

После загрузки уже применённые intervals не начисляются повторно.

## Инварианты

- один interval/bite применяется не более одного раза;
- неактивное действие не восстанавливает needs;
- completion не создаёт второй полный bonus;
- interruption не откатывает применённый effect;
- target loss освобождает reservation;
- один Bed/Leisure place не используется двумя residents;
- одна food quantity не существует одновременно в Inventory и meal payload;
- deterministic replay даёт одинаковые needs.

## Диагностика

Inspector показывает:

- action и target;
- completed/next interval;
- effects per interval;
- accumulated effects;
- reservation state;
- desired/consumed food и match flag;
- interruption reason;
- current block reason.
