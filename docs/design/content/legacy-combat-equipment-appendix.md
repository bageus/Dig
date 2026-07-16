# Appendix: дополнительные legacy-классы боевого снаряжения

Связанные документы:

- [`weapons-and-shields.md`](weapons-and-shields.md);
- issue [#129](https://github.com/bageus/Dig/issues/129).

## Назначение

Это инвентаризация вымышленных игровых class IDs из `scripts/classes/items/waffen.tcl`. У перечисленных предметов нет production recipe или technology placement в `scripts/misc/techtreetunes.tcl`.

`SetWeaponClasses` создаёт для них storable/selectable game item и marker `is_weapon`, но не задаёт в TCL полный damage/range/cooldown profile.

## Fantasy, creature и campaign candidates

| Legacy class | Предварительная группа | Policy |
|---|---|---|
| `Streitkolben` | melee | OWNER_TBD |
| `Krumsaebel` | one-handed melee | OWNER_TBD |
| `Hellebarde` | two-handed melee | OWNER_TBD |
| `Lanze_1` | two-handed melee | OWNER_TBD |
| `Lanze_2` | two-handed melee | OWNER_TBD |
| `Zauberstab` | special/ranged | OWNER_TBD |
| `Dolch_1` | one-handed melee | OWNER_TBD |
| `Dolch_2` | one-handed melee | OWNER_TBD |
| `Trollschild_1` | creature shield | OWNER_TBD |
| `Trollschild_2` | creature shield | OWNER_TBD |
| `Trollschild_3` | creature shield | OWNER_TBD |
| `Axt_1` | melee | OWNER_TBD |
| `Axt_2` | melee | OWNER_TBD |
| `Axt_3` | melee | OWNER_TBD |
| `Axt_4` | melee | OWNER_TBD |
| `Axt_unq_1` | unique melee | OWNER_TBD |
| `Axt_unq_2` | unique melee | OWNER_TBD |
| `Axt_unq_3` | unique melee | OWNER_TBD |
| `Axt_unq_4` | unique melee | OWNER_TBD |
| `Schwert_1` | one-handed melee | OWNER_TBD |
| `Schwert_2` | one-handed melee | OWNER_TBD |
| `Schwert_3` | one-handed melee | OWNER_TBD |
| `Schwert_4` | one-handed melee | OWNER_TBD |
| `Schild_1` | shield | OWNER_TBD |
| `Schild_2` | shield | OWNER_TBD |
| `Schild_3` | shield | OWNER_TBD |
| `Schild_unq_1` | unique shield | OWNER_TBD |
| `Schild_unq_2` | unique shield | OWNER_TBD |
| `Amulett_1` | special equipment | OWNER_TBD |
| `Amulett_2` | special equipment | OWNER_TBD |
| `Amulett_3` | special equipment | OWNER_TBD |
| `Drachenschuppe` | creature shield/special | OWNER_TBD |

## Special-mode class IDs

Эти восемь class IDs относятся к современному special-mode набору и не имеют colony technology definitions:

- `AK47`;
- `MP5`;
- `M4`;
- `Para`;
- `M3_super_90`;
- `Duals`;
- `Awp`;
- `Deagle`.

Отдельно в `waffen.tcl` существует `Bombe` со специальным multiplayer state machine. Это не обычный production equipment item.

Начальная рекомендуемая policy: special-mode classes и `Bombe` исключены из colony mode, пока владелец дизайна явно не решит обратное.

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