# Существа, экология и особые предметы

Связанная задача: #149. Combat: #12/#138. Status effects: #132.

## Область

На текущем этапе используется один biome profile и один общий ecology simulation cycle. Сюжетные уникальные существа и campaign sequencing не рассматриваются.

## Каталог

Мирные/ресурсные существа:

- хомяк;
- личинка.

Враги:

- Ядовитое растение;
- Огненное растение;
- Вукер;
- Серный вукер;
- Паук;
- Демон-проглот;
- Лавовый демон;
- Тролль;
- Гоблин.

Каждый вид получает stable `CreatureDefinitionId` и data-driven population cap. Детальный authoritative workflow для хомяков и grub/личинок вынесен в [`hamsters-and-grubs-ecology.md`](hamsters-and-grubs-ecology.md) и tracked в #524.

## Population caps

Жёсткие ограничения численности обязательны. Точные значения по большинству видов остаются `BALANCE_TBD` и задаются в `CreaturePopulationProfile`, а не в общем runtime-коде. Для свободных hamster и grub пользователь подтвердил отдельный cap `10` на одну плоскость; определение plane component остаётся Q-HG-001 в focused specification #524.

Spawn/reproduction transaction не создаёт новую особь, если cap соответствующего вида достигнут.

## Вукеры

- появляются парами;
- reproduction cooldown — 7 игровых дней;
- одна пара имеет максимум 3 reproduction cycles;
- детёныш взрослеет за 3 дня;
- свободного Inventory-backed детёныша можно подобрать ordinary `ЛКМ` по общему item-profile contract;
- похищенный детёныш становится приручённым guard creature поселения;
- приручённый Вукер не размножается.

Вукер и Серный вукер используют отдельные definitions; числовые различия остаются content/balance data.

Runtime naming and movement hierarchy for #559:

- `enemy.vuker` has display name «Пещерный монстр», seeds as a pair in the lower cave and supports horizontal, vertical and Z/depth traversal;
- `enemy.plant.predatory_vine` is fully stationary, cannot traverse Z, and may anchor in a horizontal tunnel, on a cave floor or on a cave wall;
- `enemy.demon.swallower` has display name «Живоглот», supports flat/depth traversal but no vertical climb, and keeps the existing one swallowed item contract;
- `enemy.spider` supports horizontal, vertical and Z/depth traversal plus future wall/ceiling ambush anchors.

Q-ENEMY-001 is answered in `enemy-combat-and-cave-encounters.md`: the vine never moves between cells or Z layers; only its initial legal anchor surface varies.

## Растения

- распространяют семена на расстояние до 10 клеток;
- одна особь имеет один reproduction cycle;
- новая особь может размножаться через 10 дней;
- рост до взрослой стадии занимает 3 дня;
- poison/fire variants используют разные Combat/StatusEffect profiles.

## Демоны

- reproduction создаёт сразу взрослую особь;
- одна особь имеет один reproduction cycle;
- cooldown новой особи — 10 дней;
- Лавовый демон имеет более высокий attack profile, чем Демон-проглот.

### Проглоченный предмет

Демон-проглот может хранить максимум один предмет.

- предмет не уничтожается;
- Inventory location становится `InsideCreature(CreatureId)`;
- пока демон жив, предмет нельзя одновременно видеть в мире или другом inventory;
- после смерти демон атомарно выбрасывает предмет рядом с местом смерти;
- если обычная drop-cell занята, используется ближайшая допустимая клетка.

## Паук и яйцо

- cooldown reproduction — 10 дней;
- максимум 2 reproduction cycles;
- создаётся physical spider egg item;
- incubation — 3 игровых дня;
- после incubation появляется сразу взрослый агрессивный паук;
- яйцо можно похитить и приготовить;
- паук может выдавать кристаллическую руду по data-driven drop table.

### Вылупление в контейнере

Яйцо может вылупиться:

- на земле;
- в personal inventory;
- на складе;
- в building inventory.

Паук создаётся у world anchor владельца/контейнера на ближайшей допустимой свободной клетке. Если legal spawn cell отсутствует, яйцо остаётся в состоянии `IncubationCompleteBlocked` и повторяет deterministic spawn check, не создавая вторую особь.

## Омлет из паучьего яйца

- готовится только на Luxury kitchen;
- ingredient: 1 spider egg;
- один омлет выбирает детерминированно-случайно один максимум из Health, Alertness, Nutrition или Mood;
- выбранный maximum увеличивается на 10 design units;
- эффект постоянный;
- эффект складывается при повторном употреблении;
- maximum может превышать 100;
- текущий value автоматически не увеличивается: изменяется только maximum;
- один consumed omelet применяет эффект ровно один раз.

## Тролль

- не размножается;
- может иметь melee weapon, shield и small/medium healing elixir;
- каждый предмет выпадает независимо;
- допустим результат без drops;
- точные вероятности остаются `BALANCE_TBD`.

## Гоблин

- не размножается;
- drop table выдаёт золото либо золотую руду;
- точные вероятности остаются `BALANCE_TBD`.

## Владение состоянием

- World/Ecology владеет identity, age, growth, reproduction, wild/tamed state и population caps;
- Combat владеет attacks, damage, hostility и equipment use;
- Inventory владеет eggs, swallowed items и drops;
- Status Effects владеет poison/fire/omelet modifiers;
- Presentation не создаёт существа и предметы.

## Save/Load

Сохраняются individuals, age/growth, cycle counters, cooldowns, tame owner, swallowed item location, egg incubation/block state, maximum Need modifiers и deterministic random state.

## Критерии приёмки

- population cap нельзя превысить reproduction/spawn race;
- приручённый Вукер не размножается;
- проглоченный предмет выпадает после смерти и имеет одного owner/location;
- яйцо может завершить incubation внутри любого inventory;
- blocked hatch не дублирует паука;
- каждый омлет повышает ровно один случайный maximum на 10;
- повторные омлеты складываются и могут поднять maximum выше 100;
- drop chances находятся в data, а не в универсальном коде;
- Save/Load сохраняет следующий lifecycle result.
