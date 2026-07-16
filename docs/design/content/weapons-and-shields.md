# Боевое снаряжение, рецепты и технологии

Связанные задачи: [#129](https://github.com/bageus/Dig/issues/129), [#177](https://github.com/bageus/Dig/issues/177), [#76](https://github.com/bageus/Dig/issues/76), [#77](https://github.com/bageus/Dig/issues/77), [#64](https://github.com/bageus/Dig/issues/64).

## Статус

Authoritative каталог текущего colony-mode боевого снаряжения Dig. Script-аудит и content-selection Q-053 завершены.

Источники legacy-reference:

- `scripts/misc/techtreetunes.tcl` — recipes, requirements, buildings и pre-invented products;
- `scripts/classes/items/waffen.tcl` — item classes, weight и доступные legacy profile values;
- `scripts/misc/genericfight.tcl` — weapon/shield selection и melee/ranged routing;
- `scripts/misc/genattribs.tcl` — пять combat skills;
- `scripts/classes/zwerg/zwerg.tcl` — независимые current weapon/shield references.

Числа, скрытые во встроенной engine database (`get_weapon_range`, attack timing, weapon IDs), не восстанавливаются предположениями и относятся к Q-014.

## Combat skills

| AgentSkillId | Назначение |
|---|---|
| `skill.unarmed_combat` | кулачный бой |
| `skill.one_handed_combat` | одноручный профиль |
| `skill.two_handed_combat` | двуручный профиль |
| `skill.ranged_combat` | дальнобойное оружие |
| `skill.defense` | результаты защиты щитом |

Старый `exp_Kampf` не становится тринадцатым навыком.

Для Оружейной кузницы, Оружейной фабрики и Dojo generic Combat threshold выполнен, когда хотя бы один из пяти combat skills достигает требования:

```text
max(Unarmed, OneHanded, TwoHanded, Ranged, Defense) >= threshold
```

## Слоты и совместимость

- один active `Weapon` slot;
- один active `Shield` slot;
- ItemStack остаётся в authoritative Inventory;
- visual prop не создаёт второй item;
- skill profile и физическая совместимость со щитом — независимые definition fields;
- текущий universal equipped-tool slot требует migration.

| Weapon | Skill | AllowsShield |
|---|---|---:|
| `weapon.sword` — Меч/Палаш | `skill.two_handed_combat` | true |
| `weapon.battle_axe` — Боевой топор | `skill.two_handed_combat` | false |
| `weapon.stone_sling` — Рогатка | `skill.ranged_combat` | true |
| `weapon.bow` — Лук | `skill.ranged_combat` | false |
| `weapon.rifle` — Ружьё | `skill.ranged_combat` | false |
| `weapon.club` — Дубина | `skill.one_handed_combat` | false |
| `weapon.light_sword` — Световой меч | `skill.one_handed_combat` | true |

Название «Палаш» трактуется как ссылка на существующий `weapon.sword` / legacy `Schwert`. Stable ItemId остаётся `weapon.sword`; окончательное display-name переименование можно выполнить отдельно без изменения gameplay contract.

Простой, Металлический и Кристаллический щиты используют `skill.defense` для подтверждённых defense result events. Простой щит не имеет отдельного Combat research threshold.

При `AllowsShield=false` несовместимый комплект нельзя активировать. Смена должна быть атомарной либо сначала явно снимать конфликтующий предмет.

## Технологическая прогрессия

```text
Мастерская каменщика
├── Рогатка
├── Дубина
└── Кузница
    ├── Простой щит [доступен сразу]
    ├── Боевой топор
    └── Оружейная кузница
        ├── Меч [доступен сразу]
        ├── Лук
        ├── Металлический щит
        ├── Арсенал
        └── Оружейная фабрика
            ├── Ружьё [доступно сразу]
            ├── Световой меч
            └── Кристаллический щит
```

## Производящие здания

| BuildingId | Recipe | Research requirements | Energy | Products |
|---|---|---|---|---|
| `building.stone_mason` | 4 камня, 2 ножки гриба | Камень 3 | class 0 | Рогатка, Дубина |
| `building.forge` | 3 ножки гриба, 2 камня, 1 железо | Камень 7, Железо 5 | class 0 | Простой щит, Боевой топор, Оружейная кузница |
| `building.weapon_forge` | 2 ножки гриба, 2 камня, 5 железа | Железо 5 + любой combat skill 10 | class 1; 50/order | Меч, Лук, Металлический щит, Арсенал, Оружейная фабрика |
| `building.weapons_factory` | 2 угля, 8 железа, 4 золота | Железо 2 + любой combat skill 3 | class 3; 100/order | Ружьё, Световой меч, Кристаллический щит |
| `building.dojo` | 6 ножек гриба, 3 камня | Дерево 35 + любой combat skill 10 | class 0 | тренировки пяти combat skills |

Арсенал является подтверждённым Dig BuildingBox-output Оружейной кузницы.

## Производимый каталог

| ItemId | Legacy | Category | Recipe | Research | Production grants | Legacy reference |
|---|---|---|---|---|---|---|
| `weapon.stone_sling` | `Steinschleuder` | RangedWeapon | 1 ножка гриба, 1 камень | Камень 4 | Камень +1 | weight 0.05; ballistic 0.10 |
| `weapon.club` | `Keule` | MeleeWeapon | 2 ножки гриба, 1 камень | Камень 1, Одноручный бой 1 | Камень +1 | explicit profile отсутствует в TCL |
| `shield.basic` | `Schild` | Shield | 1 камень, 1 железо | pre-invented | Железо +1 | weight 0.05; shield 0.50 |
| `weapon.battle_axe` | `Streitaxt` | TwoHandedWeapon | 1 ножка гриба, 2 железа | Железо 4, Двуручный бой 12 | Железо +2 | explicit profile отсутствует в TCL |
| `weapon.sword` | `Schwert` | Weapon | 2 угля, 3 железа | pre-invented | Железо +4 | weight 0.08; melee 0.15 |
| `weapon.bow` | `PfeilUndBogen` | RangedWeapon | 2 ножки гриба, 1 железо | Дерево 6, Железо 4, Дальний бой 7 | Дерево +1, Железо +3 | weight 0.05; ballistic 0.12 |
| `shield.metal` | `Metallschild` | Shield | 1 уголь, 3 железа | Железо 15, Защита 15 | Железо +2 | weight 0.08; shield 0.70 |
| `weapon.rifle` | `Buechse` | RangedWeapon | 1 ножка гриба, 3 железа, 2 угля | Железо 40, Алхимия 10, Дальний бой 20 | Железо +5, Алхимия +2 | weight 0.05; ballistic 0.30 |
| `weapon.light_sword` | `Lichtschwert` | Weapon | 4 кристалла, 1 железо | Алхимия 50, Одноручный бой 40 | Железо +2, Алхимия +2 | weight 0.10; melee 0.45 |
| `shield.crystal` | `Kristallschild` | Shield | 3 кристалла, 2 золота | Камень 30, Железо 17, Защита 30 | Камень +2, Железо +2 | weight 0.05; shield 1.20 |

`pre-invented` означает доступность после изучения building node без отдельного item research.

## Ammo и durability

Authoritative policy:

- Рогатка, Лук и Ружьё имеют `AmmoPolicy = NoneRequired`;
- отдельные стрелы, камни или патроны не создаются и не расходуются;
- все десять производимых предметов имеют `DurabilityPolicy = NoWear`;
- удар, выстрел и блок не уменьшают quantity или condition;
- legacy `attackpoints_*` и shield `hitpoints` не трактуются как износ.

Точные damage/range/cooldown/block values — data-driven Q-014.

## Дополнительный legacy content

### Fantasy/creature classes

32 дополнительных класса не применяются в текущей игре. Они имеют `ContentPolicy = DeferredBacklog` и перенесены в [#177](https://github.com/bageus/Dig/issues/177). Они не производятся, не появляются как loot и не входят в runtime registry.

Полный список: [`legacy-combat-equipment-appendix.md`](legacy-combat-equipment-appendix.md).

### Современный special-mode набор

`AK47`, `MP5`, `M4`, `Para`, `M3_super_90`, `Duals`, `Awp`, `Deagle` и `Bombe` имеют `ContentPolicy = ExcludedFromColonyMode`.

Они не входят в colony content registry, technology tree, loot, Save/Load или migration.

## Не personal equipment

Каменная ловушка-пресс, Медуза-ловушка, Кристаллический луч, Пост часового и Арсенал являются building/trap content и не занимают resident equipment slots.

## Хранение

### Пост часового

- максимум 4 Weapon items;
- target stock 1, 2 или 4;
- щиты не занимают weapon stock slots;
- выданный после боя item остаётся у защитника.

### Арсенал

- 20 weapon slots: 10 слева и 10 справа;
- 6 shield slots;
- принимает visible/reachable world Weapon/Shield stacks;
- не забирает автоматически личные items или stock постов;
- производится как BuildingBox в Оружейной кузнице.

## Content model

```text
CombatEquipmentDefinition
- ItemId
- LocalizationKey
- LegacyClassId?
- Category
- WeaponSlotCost
- ShieldSlotCost
- AllowsShield
- RequiredSkillId?
- RequiredSkillThreshold
- DamageProfileId?
- DefenseProfileId?
- RangeProfileId?
- AttackTimingProfileId?
- AmmoPolicy
- DurabilityPolicy
- Weight
- VisualAttachmentId
- RecipeId?
- ProductionBuildingId?
- TechnologyId?
- LootPolicy
- Version
```

## Инварианты

- weapon и shield — разные authoritative slots;
- skill mapping и physical handedness независимы;
- одна единица не выдаётся двум residents;
- visual prop не создаёт item;
- production/research используют stable IDs;
- ranged attack не создаёт неописанный ammo item;
- производимое снаряжение не изнашивается;
- deferred content не входит в runtime;
- excluded modern content является validation error при colony reference;
- Save/Load сохраняет ItemId, location, active slots и definition version;
- animation callback не создаёт authoritative attack/defense result.

## Q-053

**ANSWERED.**

- основной каталог ограничен десятью производимыми предметами;
- 32 fantasy/creature класса перенесены в backlog #177;
- восемь современных классов и `Bombe` окончательно исключены из colony mode;
- skill mapping, mixed-building threshold, shield compatibility, ammo и durability policy утверждены;
- непредоставленные числовые combat coefficients остаются Q-014.