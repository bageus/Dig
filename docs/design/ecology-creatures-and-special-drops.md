# Существа, экология и особые предметы

Связанная задача: #149. Combat: #12/#138. Status effects: #132.

Focused enemy-death lifecycle: [`enemy-death-loot-and-corpse-dissolution.md`](enemy-death-loot-and-corpse-dissolution.md), tracking [#559](https://github.com/bageus/Dig/issues/559).

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

Focused authoritative lifecycle: [`vuker-reproduction-questionnaire.md`](vuker-reproduction-questionnaire.md), tracking [#569](https://github.com/bageus/Dig/issues/569). Dynamic pair formation, cap `10` per connected cave region, non-combat child patrol and `Alt+ЛКМ` taming are approved.

- появляются парами;
- reproduction cooldown — 7 игровых дней;
- одна пара имеет максимум 3 reproduction cycles;
- детёныш взрослеет за 3 дня;
- выбранный живой гном похищает свободного детёныша через `Alt+ЛКМ`: подходит к нему и атомарно переводит в приручённого guard actor без persistent carried-item состояния;
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
- после смерти тот же physical item identity атомарно получает `ItemLocation.InWorld(DeathCell)`;
- nearest-cell fallback запрещён: предмет остаётся в exact XYZ cell смерти, даже если там уже лежат другие world items;
- проглоченный предмет участвует в общем exactly-once enemy death-release вместе со всеми carried/equipped/held contents;
- растворение трупа не удаляет выпавший предмет.

Это правило подтверждено 2026-08-04 и заменяет прежнее размещение «рядом с местом смерти» с fallback в ближайшую допустимую клетку.

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

## Общее правило смерти врага и loot

При Health `0` enemy actor умирает, падает и затем исчезает через dissolve lifecycle. Все предметы, уже находившиеся внутри него или принадлежавшие ему, а также materialized species drops оказываются в exact authoritative death cell. Item identities/quantities сохраняются; death replay не создаёт дубликаты; corpse visual и world loot имеют независимый lifecycle.

Полный owner/transaction/save/input/test contract находится в [`enemy-death-loot-and-corpse-dissolution.md`](enemy-death-loot-and-corpse-dissolution.md).

## Владение состоянием

- World/Ecology владеет identity, age, growth, reproduction, wild/tamed state и population caps;
- Combat владеет attacks, damage, hostility и equipment use;
- Inventory владеет eggs, swallowed items, enemy-owned contents и drops;
- enemy death lifecycle/Application координирует exactly-once release в death cell;
- Status Effects владеет poison/fire/omelet modifiers;
- Presentation не создаёт существа и предметы и не удаляет loot при dissolve corpse.

## Save/Load

Сохраняются individuals, age/growth, cycle counters, cooldowns, tame owner, swallowed item location, egg incubation/block state, maximum Need modifiers, deterministic random state, enemy death identity/cell/tick, loot-release commit и corpse lifecycle progress.

## Критерии приёмки

- population cap нельзя превысить reproduction/spawn race;
- приручённый Вукер не размножается;
- проглоченный и любой другой enemy-owned предмет выпадает в exact death cell и имеет одного owner/location;
- занятая death cell не переносит loot в соседнюю клетку;
- corpse dissolve не удаляет world loot;
- death replay не дублирует contents или species drops;
- яйцо может завершить incubation внутри любого inventory;
- blocked hatch не дублирует паука;
- каждый омлет повышает ровно один случайный maximum на 10;
- повторные омлеты складываются и могут поднять maximum выше 100;
- drop chances находятся в data, а не в универсальном коде;
- Save/Load сохраняет следующий lifecycle result.
