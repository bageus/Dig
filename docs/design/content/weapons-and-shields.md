# Боевое снаряжение, рецепты и технологии

Связанные задачи: [#129](https://github.com/bageus/Dig/issues/129), [#76](https://github.com/bageus/Dig/issues/76), [#77](https://github.com/bageus/Dig/issues/77), [#64](https://github.com/bageus/Dig/issues/64).

## Статус

Документ фиксирует безопасный игровой content-аудит legacy `scripts`. Он описывает только вымышленные игровые предметы и их data definitions; реальные инструкции по изготовлению оружия отсутствуют.

Источники:

- `scripts/misc/techtreetunes.tcl` — игровые recipes, research requirements, buildings и pre-invented products;
- `scripts/classes/items/waffen.tcl` — игровые item classes, weight и доступные legacy profile values;
- `scripts/misc/genericfight.tcl` — выбор weapon/shield и melee/ranged routing;
- `scripts/misc/genattribs.tcl` — пять combat skills;
- `scripts/classes/zwerg/zwerg.tcl` — отдельные ссылки текущего weapon и shield;
- `scripts/init/animinit.tcl` — дополнительные loot/special classes.

Числа, скрытые во встроенной engine database (`get_weapon_range`, attack timing и weapon IDs), не восстанавливаются предположениями и относятся к Q-014.

## Навыки

| Legacy | AgentSkillId Dig |
|---|---|
| `exp_F_Kungfu` | `skill.unarmed_combat` |
| `exp_F_Sword` | `skill.one_handed_combat` |
| `exp_F_Twohanded` | `skill.two_handed_combat` |
| `exp_F_Ballistic` | `skill.ranged_combat` |
| `exp_F_Defense` | `skill.defense` |
| `exp_Energie` | `skill.alchemy` |
| `exp_Metall` | `skill.metallurgy` |
| `exp_Stein` | `skill.stonework` |
| `exp_Holz` | `skill.woodworking` |

`exp_Kampf` не становится тринадцатым навыком. Его thresholds должны быть переведены в один из пяти конкретных combat skills. Legacy fractions переводятся в шкалу `0..100` умножением на 100 и округлением до целого.

## Слоты

Legacy runtime независимо хранит weapon и shield. Целевая модель Dig:

- один активный `Weapon` slot;
- один активный `Shield` slot;
- ItemStack остаётся в authoritative Inventory;
- визуальное извлечение не создаёт копию;
- совместимость определяется `AllowsShield` в definition;
- одна единица не может занимать два residents/slots.

Текущий универсальный одиночный equipped-tool slot должен быть мигрирован до полного Combat equipment.

## Прогрессия

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
        ├── Арсенал [подтверждённый Dig output]
        └── Оружейная фабрика
            ├── Ружьё [доступно сразу]
            ├── Световой меч
            └── Кристаллический щит
```

## Производящие здания

| BuildingId | Legacy | Игровой recipe | Research requirements | Energy | Products |
|---|---|---|---|---|---|
| `building.stone_mason` | `Steinmetz` | 4 камня, 2 ножки гриба | Камень 3 | class 0 | Рогатка, Дубина |
| `building.forge` | `Schmiede` | 3 ножки гриба, 2 камня, 1 железо | Камень 7, Железо 5 | class 0 | Простой щит, Боевой топор, Оружейная кузница |
| `building.weapon_forge` | `Waffenschmiede` | 2 ножки гриба, 2 камня, 5 железа | Железо 5 + legacy Combat 10 | class 1; 50/order | Меч, Лук, Металлический щит, Арсенал, Оружейная фабрика |
| `building.weapons_factory` | `Waffenfabrik` | 2 угля, 8 железа, 4 золота | Железо 2 + legacy Combat 3 | class 3; 100/order | Ружьё, Световой меч, Кристаллический щит |
| `building.dojo` | `Dojo` | 6 ножек гриба, 3 камня | Дерево 35 + legacy Combat 10 | class 0 | тренировки пяти combat skills |

`Арсенал` является подтверждённым Dig output Оружейной кузницы, хотя отдельного legacy product в `tttitems_Waffenschmiede` нет.

## Полный производимый каталог

| ItemId | Display name | Legacy class | Category candidate | Игровой recipe | Research | Production grants | Legacy profile |
|---|---|---|---|---|---|---|---|
| `weapon.stone_sling` | Рогатка | `Steinschleuder` | RangedWeapon | 1 ножка гриба, 1 камень | Камень 4 | Камень +1 | weight 0.05; ballistic 0.10 |
| `weapon.club` | Дубина | `Keule` | TwoHandedWeapon | 2 ножки гриба, 1 камень | Камень 1 + Combat 1 | Камень +1 | explicit values отсутствуют в TCL |
| `shield.basic` | Простой щит | `Schild` | Shield | 1 камень, 1 железо | pre-invented | Железо +1 | weight 0.05; shield 0.50 |
| `weapon.battle_axe` | Боевой топор | `Streitaxt` | TwoHandedWeapon | 1 ножка гриба, 2 железа | Железо 4 + Combat 12 | Железо +2 | explicit values отсутствуют в TCL |
| `weapon.sword` | Меч | `Schwert` | OneHandedWeapon | 2 угля, 3 железа | pre-invented | Железо +4 | weight 0.08; melee 0.15 |
| `weapon.bow` | Лук | `PfeilUndBogen` | RangedWeapon | 2 ножки гриба, 1 железо | Дерево 6, Железо 4, Combat 7 | Дерево +1, Железо +3 | weight 0.05; ballistic 0.12 |
| `shield.metal` | Металлический щит | `Metallschild` | Shield | 1 уголь, 3 железа | Железо 15, Combat 15 | Железо +2 | weight 0.08; shield 0.70 |
| `weapon.rifle` | Ружьё | `Buechse` | RangedWeapon | 1 ножка гриба, 3 железа, 2 угля | Железо 40, Алхимия 10, Combat 20 | Железо +5, Алхимия +2 | weight 0.05; ballistic 0.30 |
| `weapon.light_sword` | Световой меч | `Lichtschwert` | OneHandedWeapon | 4 кристалла, 1 железо | Алхимия 50, Combat 40 | Железо +2, Алхимия +2 | weight 0.10; melee 0.45 |
| `shield.crystal` | Кристаллический щит | `Kristallschild` | Shield | 3 кристалла, 2 золота | Камень 30, Железо 17, Combat 30 | Камень +2, Железо +2 | weight 0.05; shield 1.20 |

`pre-invented` означает доступность после изучения соответствующего building node без отдельного item research.

## Candidate mapping generic Combat

| Equipment | Candidate AgentSkillId |
|---|---|
| Дубина, Боевой топор | `skill.two_handed_combat` |
| Лук, Ружьё | `skill.ranged_combat` |
| Металлический и Кристаллический щиты | `skill.defense` |
| Световой меч | `skill.one_handed_combat` |

Рогатка, Простой щит и Меч не имеют отдельного generic Combat threshold в scripts.

## Дальнобойная policy и износ

В `techtreetunes.tcl` нет отдельных projectile/ammo products или recipes. Ballistic action использует сам weapon item; специальное применение Рогатки также не списывает отдельный projectile item.

Script-faithful candidate policy:

- Рогатка, Лук и Ружьё не требуют расходуемого ammo item;
- подтверждённой item durability в TCL нет;
- `attackpoints_bal`, `attackpoints_sr` и shield `hitpoints` рассматриваются как legacy profile values, не как доказанный износ;
- точные Dig damage/range/cooldown/block values являются data-driven Q-014.

## Дополнительные classes

`SetWeaponClasses` содержит 40 дополнительных классов без production recipe/technology placement. Полный список и классификация находятся в [`legacy-combat-equipment-appendix.md`](legacy-combat-equipment-appendix.md).

Ни один из них не переносится в production tree автоматически: требуется явная `loot-only`, `excluded` или новая recipe/technology policy.

## Не personal equipment

Combat-related building/trap products не занимают resident slots:

- Каменная ловушка-пресс;
- Медуза-ловушка;
- Кристаллический луч;
- Пост часового;
- Арсенал.

## Хранение

### Пост часового

- максимум 4 Weapon items;
- target stock 1, 2 или 4;
- щиты не занимают weapon slots;
- после боя выданный item остаётся у защитника.

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
- one/two-handed compatibility проверяется definition;
- одна единица не выдаётся двум residents;
- visual prop не создаёт item;
- production/research используют stable IDs;
- отсутствующая technology или loot policy — validation error;
- Save/Load сохраняет ItemId, location, active slots и definition version;
- animation callback не создаёт attack/defense result.

## Открыто: Q-053

1. Какие дополнительные fantasy/creature classes сохраняются как loot-only?
2. Подтвердить исключение special-mode classes из colony mode.
3. Подтвердить candidate mapping generic `exp_Kampf`, включая mixed building requirements.
4. Определить `AllowsShield` для каждого weapon type.
5. Подтвердить policy без расходуемых боеприпасов и без item durability.

После ответов точные числовые combat coefficients остаются Q-014.