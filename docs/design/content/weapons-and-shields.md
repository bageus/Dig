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

`exp_Kampf` не становится тринадцатым навыком. Legacy fractions переводятся в шкалу `0..100` умножением на 100 и округлением до целого.

Для mixed building requirements Оружейной кузницы, Оружейной фабрики и Dojo generic Combat threshold считается выполненным, когда хотя бы один из пяти combat skills достиг требуемого значения:

```text
max(
  skill.unarmed_combat,
  skill.one_handed_combat,
  skill.two_handed_combat,
  skill.ranged_combat,
  skill.defense
) >= RequiredCombatThreshold
```

## Слоты

Legacy runtime независимо хранит weapon и shield. Целевая модель Dig:

- один активный `Weapon` slot;
- один активный `Shield` slot;
- ItemStack остаётся в authoritative Inventory;
- визуальное извлечение не создаёт копию;
- совместимость определяется `AllowsShield` в definition;
- одна единица не может занимать два residents/slots.

Текущий универсальный одиночный equipped-tool slot должен быть мигрирован до полного Combat equipment.

Физическая совместимость со щитом и используемый combat skill являются независимыми полями definition. Поэтому предмет может использовать один skill, но блокировать или не блокировать `Shield` slot по отдельному owner rule.

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
| `building.weapon_forge` | `Waffenschmiede` | 2 ножки гриба, 2 камня, 5 железа | Железо 5 + любой combat skill 10 | class 1; 50/order | Меч, Лук, Металлический щит, Арсенал, Оружейная фабрика |
| `building.weapons_factory` | `Waffenfabrik` | 2 угля, 8 железа, 4 золота | Железо 2 + любой combat skill 3 | class 3; 100/order | Ружьё, Световой меч, Кристаллический щит |
| `building.dojo` | `Dojo` | 6 ножек гриба, 3 камня | Дерево 35 + любой combat skill 10 | class 0 | тренировки пяти combat skills |

`Арсенал` является подтверждённым Dig output Оружейной кузницы, хотя отдельного legacy product в `tttitems_Waffenschmiede` нет.

## Полный производимый каталог

| ItemId | Display name | Legacy class | Category | Игровой recipe | Research | Production grants | Legacy profile |
|---|---|---|---|---|---|---|---|
| `weapon.stone_sling` | Рогатка | `Steinschleuder` | RangedWeapon | 1 ножка гриба, 1 камень | Камень 4 | Камень +1 | weight 0.05; ballistic 0.10 |
| `weapon.club` | Дубина | `Keule` | MeleeWeapon | 2 ножки гриба, 1 камень | Камень 1 + Одноручный бой 1 | Камень +1 | explicit values отсутствуют в TCL |
| `shield.basic` | Простой щит | `Schild` | Shield | 1 камень, 1 железо | pre-invented | Железо +1 | weight 0.05; shield 0.50 |
| `weapon.battle_axe` | Боевой топор | `Streitaxt` | TwoHandedWeapon | 1 ножка гриба, 2 железа | Железо 4 + Двуручный бой 12 | Железо +2 | explicit values отсутствуют в TCL |
| `weapon.sword` | Меч | `Schwert` | OneHandedWeapon | 2 угля, 3 железа | pre-invented | Железо +4 | weight 0.08; melee 0.15 |
| `weapon.bow` | Лук | `PfeilUndBogen` | RangedWeapon | 2 ножки гриба, 1 железо | Дерево 6, Железо 4, Дальний бой 7 | Дерево +1, Железо +3 | weight 0.05; ballistic 0.12 |
| `shield.metal` | Металлический щит | `Metallschild` | Shield | 1 уголь, 3 железа | Железо 15, Защита 15 | Железо +2 | weight 0.08; shield 0.70 |
| `weapon.rifle` | Ружьё | `Buechse` | RangedWeapon | 1 ножка гриба, 3 железа, 2 угля | Железо 40, Алхимия 10, Дальний бой 20 | Железо +5, Алхимия +2 | weight 0.05; ballistic 0.30 |
| `weapon.light_sword` | Световой меч | `Lichtschwert` | OneHandedWeapon | 4 кристалла, 1 железо | Алхимия 50, Одноручный бой 40 | Железо +2, Алхимия +2 | weight 0.10; melee 0.45 |
| `shield.crystal` | Кристаллический щит | `Kristallschild` | Shield | 3 кристалла, 2 золота | Камень 30, Железо 17, Защита 30 | Камень +2, Железо +2 | weight 0.05; shield 1.20 |

`pre-invented` означает доступность после изучения соответствующего building node без отдельного item research.

## Подтверждённое skill mapping

| Equipment | AgentSkillId |
|---|---|
| Меч (`weapon.sword`; в ответе владельца назван «Палаш») и Боевой топор | `skill.two_handed_combat` |
| Рогатка, Лук и Ружьё | `skill.ranged_combat` |
| Металлический и Кристаллический щиты | `skill.defense` |
| Дубина и Световой меч | `skill.one_handed_combat` |

Название «Палаш» пока трактуется как ссылка на производимый `weapon.sword` / legacy `Schwert`; stable ItemId и текущий display name «Меч» не меняются без отдельного решения о переименовании.

Простой щит не имеет отдельного Combat research threshold, но подтверждённый удар, обработанный любым щитом, использует `skill.defense` для Combat grant.

## Подтверждённая совместимость со щитом

| Weapon | AllowsShield | Причина |
|---|---:|---|
| `weapon.sword` — Меч | true | физически одноручное оружие |
| `weapon.light_sword` — Световой меч | true | физически одноручное оружие |
| `weapon.stone_sling` — Рогатка | true | разрешена одновременная защита щитом |
| `weapon.club` — Дубина | false | занимает обе руки |
| `weapon.battle_axe` — Боевой топор | false | занимает обе руки |
| `weapon.bow` — Лук | false | занимает обе руки |
| `weapon.rifle` — Ружьё | false | занимает обе руки |

При `AllowsShield=false` экипировка оружия блокируется, пока `Shield` slot занят; экипировка щита блокируется, пока такое оружие активно. Операция смены комплекта должна выполняться атомарно либо через явное снятие несовместимого предмета.

## Дальнобойная policy и износ

В `techtreetunes.tcl` нет отдельных projectile/ammo products или recipes. Ballistic action использует сам weapon item; специальное применение Рогатки также не списывает отдельный projectile item.

Owner policy:

- `weapon.stone_sling`, `weapon.bow` и `weapon.rifle` имеют `AmmoPolicy = NoneRequired`;
- все десять производимых предметов имеют `DurabilityPolicy = NoWear`;
- выстрел, удар и блок не уменьшают quantity или condition предмета;
- `attackpoints_bal`, `attackpoints_sr` и shield `hitpoints` являются legacy profile values, а не износом;
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
- skill mapping и physical handedness задаются независимо;
- one/two-handed compatibility проверяется `AllowsShield`;
- одна единица не выдаётся двум residents;
- visual prop не создаёт item;
- production/research используют stable IDs;
- отсутствующая technology или loot policy — validation error;
- Save/Load сохраняет ItemId, location, active slots и definition version;
- animation callback не создаёт attack/defense result;
- ranged attack не создаёт и не расходует неописанный ammo item;
- производимое снаряжение не изнашивается.

## Открыто: Q-053

Остались только content-selection решения:

1. какие из 32 дополнительных fantasy/creature classes сохранить как loot-only;
2. подтвердить исключение восьми special-mode classes и `Bombe` из colony mode.

Skill mapping, mixed-building requirement, shield compatibility, отсутствие расходуемых боеприпасов и отсутствие износа подтверждены 2026-07-16.

После закрытия content selection точные числовые combat coefficients остаются Q-014.
