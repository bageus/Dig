# Appendix: дополнительные legacy-классы боевого снаряжения

Связанные документы:

- [`weapons-and-shields.md`](weapons-and-shields.md);
- issue [#129](https://github.com/bageus/Dig/issues/129).

## Назначение

Это инвентаризация вымышленных игровых class IDs из `scripts/classes/items/waffen.tcl`. У перечисленных предметов нет production recipe или technology placement в `scripts/misc/techtreetunes.tcl`.

`SetWeaponClasses` создаёт для них storable/selectable game item и marker `is_weapon`, но не задаёт в TCL полный damage/range/cooldown profile.

## Fantasy, creature и campaign candidates — 32 класса

Эти classes показаны владельцу дизайна для отдельного решения `loot-only` или `excluded`. До ответа ни один из них не входит в runtime catalog.

| № | Legacy class | Предварительная группа | Policy |
|---:|---|---|---|
| 1 | `Streitkolben` | melee | OWNER_TBD |
| 2 | `Krumsaebel` | one-handed melee | OWNER_TBD |
| 3 | `Hellebarde` | two-handed melee | OWNER_TBD |
| 4 | `Lanze_1` | two-handed melee | OWNER_TBD |
| 5 | `Lanze_2` | two-handed melee | OWNER_TBD |
| 6 | `Zauberstab` | special/ranged | OWNER_TBD |
| 7 | `Dolch_1` | one-handed melee | OWNER_TBD |
| 8 | `Dolch_2` | one-handed melee | OWNER_TBD |
| 9 | `Trollschild_1` | creature shield | OWNER_TBD |
| 10 | `Trollschild_2` | creature shield | OWNER_TBD |
| 11 | `Trollschild_3` | creature shield | OWNER_TBD |
| 12 | `Axt_1` | melee | OWNER_TBD |
| 13 | `Axt_2` | melee | OWNER_TBD |
| 14 | `Axt_3` | melee | OWNER_TBD |
| 15 | `Axt_4` | melee | OWNER_TBD |
| 16 | `Axt_unq_1` | unique melee | OWNER_TBD |
| 17 | `Axt_unq_2` | unique melee | OWNER_TBD |
| 18 | `Axt_unq_3` | unique melee | OWNER_TBD |
| 19 | `Axt_unq_4` | unique melee | OWNER_TBD |
| 20 | `Schwert_1` | one-handed melee | OWNER_TBD |
| 21 | `Schwert_2` | one-handed melee | OWNER_TBD |
| 22 | `Schwert_3` | one-handed melee | OWNER_TBD |
| 23 | `Schwert_4` | one-handed melee | OWNER_TBD |
| 24 | `Schild_1` | shield | OWNER_TBD |
| 25 | `Schild_2` | shield | OWNER_TBD |
| 26 | `Schild_3` | shield | OWNER_TBD |
| 27 | `Schild_unq_1` | unique shield | OWNER_TBD |
| 28 | `Schild_unq_2` | unique shield | OWNER_TBD |
| 29 | `Amulett_1` | special equipment | OWNER_TBD |
| 30 | `Amulett_2` | special equipment | OWNER_TBD |
| 31 | `Amulett_3` | special equipment | OWNER_TBD |
| 32 | `Drachenschuppe` | creature shield/special | OWNER_TBD |

## Special-mode exclusions under review

Эти восемь class IDs относятся к современному special-mode набору и не имеют colony technology definitions:

1. `AK47`;
2. `MP5`;
3. `M4`;
4. `Para`;
5. `M3_super_90`;
6. `Duals`;
7. `Awp`;
8. `Deagle`.

Отдельно в `waffen.tcl` существует `Bombe` со специальным multiplayer state machine. Это не обычный production equipment item.

Рекомендуемая policy: восемь special-mode classes и `Bombe` имеют `LootPolicy = ExcludedFromColonyMode`. Она станет authoritative только после явного подтверждения владельца дизайна.

## Уже учтённые generic classes

`Keule` и `Streitaxt` также создаются через `SetWeaponClasses`, но имеют recipes и technology placement. Они входят в основной производимый каталог как `weapon.club` и `weapon.battle_axe` и здесь повторно не считаются.

## Migration rule

Для сохранения любого candidate нужны:

- новый stable ItemId;
- localization key;
- category и slot compatibility;
- concrete combat skill;
- game profile;
- loot source либо recipe/technology;
- Save/Load version.

Class ID без явной policy является validation error и не попадает в runtime catalog.
