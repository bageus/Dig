# Непрерывные действия потребностей

Связанная задача: [#159](https://github.com/bageus/Dig/issues/159).

## Назначение

Этот документ задаёт целевую design-модель действий `Eat`, `Sleep` и `Leisure`. Текущая реализация частично использует complete-only effects; до завершения #97 и #159 это является известным implementation gap, а не альтернативным правилом дизайна.

## Владение состоянием

- Agents владеет active action, Nutrition, Alertness, Mood и уже применённым progress effect;
- Health owner владеет Health и принимает regeneration intervals от допустимых действий;
- Inventory владеет едой до commit начала трапезы;
- Buildings владеет Bed/Leisure places;
- Jobs/Reservations владеют общим lifecycle назначений и мест;
- Presentation отображает progress и animation profile, но не начисляет effects.

## Общий принцип

Потребность или Health восстанавливаются тогда, когда resident фактически выполняет подтверждённое действие.

- каждый подтверждённый simulation interval имеет stable idempotency key;
- эффект применяется сразу после interval commit;
- прерывание сохраняет уже применённый эффект;
- после прерывания новые intervals не создаются;
- completion не повторяет сумму уже применённых эффектов;
- значения clamp в authoritative диапазоне после каждого применения.

Natural Health regeneration разрешена только во время еды, сна и отдыха. Работа, бой, перемещение и обычное бездействие сами по себе Health не восстанавливают. Точные rates относятся к Q-014.

## Еда

- стандартная порция состоит из трёх укусов;
- каждый завершённый укус восстанавливает одну треть Nutrition блюда;
- еда также восстанавливает Mood по data-driven `MoodPerBite`;
- пока resident фактически ест, применяются data-driven Health regeneration intervals;
- если фактически съеденное блюдо не совпало со случайно выбранным желаемым блюдом, положительный food Mood effect этой трапезы не применяется;
- после первого подтверждённого укуса трапеза считается фактически начатой для diet history;
- interruption сохраняет завершённые bite effects и уже восстановленный Health, затем уничтожает оставшуюся часть порции;
- interruption до commit начала освобождает reservation и не создаёт diet entry.

Подробная модель еды: [`content/food.md`](content/food.md).

## Сон

Пока действие `Sleep` активно:

- Alertness восстанавливается через повторяющиеся simulation intervals;
- Mood также восстанавливается через интервалы;
- Health постепенно восстанавливается через отдельный data-driven regeneration profile;
- точные значения и частота задаются effect profiles;
- пробуждение, приказ, угроза, смерть или потеря Bed place прекращают новые intervals;
- уже восстановленные Alertness, Mood и Health не откатываются;
- Bed reservation освобождается по единой finish/interruption policy.

## Досуг и отдых

Пока действие `Leisure` или подтверждённый отдых активно:

- Mood восстанавливается через повторяющиеся simulation intervals;
- Health постепенно восстанавливается через data-driven regeneration intervals;
- скорость Mood зависит от `LeisureActivityDefinition`, качества места и будущих modifiers;
- interruption сохраняет уже полученные Mood и Health;
- завершение или interruption освобождает Leisure place;
- анимация не является источником Mood или Health.

## Action snapshot

Целевой snapshot содержит:

- action kind;
- target/reservation id;
- start tick;
- completed interval count;
- accumulated Nutrition/Alertness/Mood/Health;
- next interval progress;
- effect profile/version;
- interruption reason;
- committed food payload и bite index для Eat.

## Save/Load

Сохранение в середине действия обязано восстанавливать:

- active action;
- target reservation;
- completed intervals/bites;
- накопленные effects, включая Health;
- progress до следующего interval;
- food payload без повторного создания ItemStack.

После загрузки уже применённые intervals не начисляются повторно.

## Инварианты

- один interval/bite применяется не более одного раза;
- неактивное действие не восстанавливает needs или Health;
- completion не создаёт второй полный bonus;
- interruption не откатывает применённый effect;
- target loss освобождает reservation;
- один Bed/Leisure place не используется двумя residents;
- одна food quantity не существует одновременно в Inventory и meal payload;
- deterministic replay даёт одинаковые needs и Health.

## Диагностика

Inspector показывает:

- action и target;
- completed/next interval;
- effects per interval;
- accumulated effects, включая Health regeneration;
- reservation state;
- desired/consumed food и match flag;
- interruption reason;
- current block reason.
