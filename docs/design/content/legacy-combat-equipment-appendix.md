# Appendix: дополнительные legacy-классы боевого снаряжения

Связанные задачи:

- основной каталог — [#129](https://github.com/bageus/Dig/issues/129);
- отложенный fantasy/creature backlog — [#177](https://github.com/bageus/Dig/issues/177);
- [`weapons-and-shields.md`](weapons-and-shields.md).

## Назначение

Это инвентаризация игровых class IDs из `scripts/classes/items/waffen.tcl`, у которых нет production recipe или technology placement в `scripts/misc/techtreetunes.tcl`.

`SetWeaponClasses` создаёт для них storable/selectable item и marker `is_weapon`, но не задаёт в TCL полный damage/range/cooldown profile.

## Fantasy, creature и campaign candidates — отложены

Все 32 класса исключены из текущего runtime scope и перенесены в backlog #177.

Текущая policy для каждого:

```text
ContentPolicy = DeferredBacklog
RuntimeCatalog = false
ProductionEnabled = false
LootEnabled = false
NewSaveSpawnEnabled = false
```

| № | Legacy class | Предварительная группа | Policy |
|---:|---|---|---|
| 1 | `Streitkolben` | melee | DeferredBacklog #177 |
| 2 | `Krumsaebel` | one-handed melee | DeferredBacklog #177 |
| 3 | `Hellebarde` | two-handed melee | DeferredBacklog #177 |
| 4 | `Lanze_1` | two-handed melee | DeferredBacklog #177 |
| 5 | `Lanze_2` | two-handed melee | DeferredBacklog #177 |
| 6 | `Zauberstab` | special/ranged | DeferredBacklog #177 |
| 7 | `Dolch_1` | one-handed melee | DeferredBacklog #177 |
| 8 | `Dolch_2` | one-handed melee | DeferredBacklog #177 |
| 9 | `Trollschild_1` | creature shield | DeferredBacklog #177 |
| 10 | `Trollschild_2` | creature shield | DeferredBacklog #177 |
| 11 | `Trollschild_3` | creature shield | DeferredBacklog #177 |
| 12 | `Axt_1` | melee | DeferredBacklog #177 |
| 13 | `Axt_2` | melee | DeferredBacklog #177 |
| 14 | `Axt_3` | melee | DeferredBacklog #177 |
| 15 | `Axt_4` | melee | DeferredBacklog #177 |
| 16 | `Axt_unq_1` | unique melee | DeferredBacklog #177 |
| 17 | `Axt_unq_2` | unique melee | DeferredBacklog #177 |
| 18 | `Axt_unq_3` | unique melee | DeferredBacklog #177 |
| 19 | `Axt_unq_4` | unique melee | DeferredBacklog #177 |
| 20 | `Schwert_1` | one-handed melee | DeferredBacklog #177 |
| 21 | `Schwert_2` | one-handed melee | DeferredBacklog #177 |
| 22 | `Schwert_3` | one-handed melee | DeferredBacklog #177 |
| 23 | `Schwert_4` | one-handed melee | DeferredBacklog #177 |
| 24 | `Schild_1` | shield | DeferredBacklog #177 |
| 25 | `Schild_2` | shield | DeferredBacklog #177 |
| 26 | `Schild_3` | shield | DeferredBacklog #177 |
| 27 | `Schild_unq_1` | unique shield | DeferredBacklog #177 |
| 28 | `Schild_unq_2` | unique shield | DeferredBacklog #177 |
| 29 | `Amulett_1` | special equipment | DeferredBacklog #177 |
| 30 | `Amulett_2` | special equipment | DeferredBacklog #177 |
| 31 | `Amulett_3` | special equipment | DeferredBacklog #177 |
| 32 | `Drachenschuppe` | creature shield/special | DeferredBacklog #177 |

Они могут вернуться в scope только отдельным owner decision с stable IDs, источниками получения, rarity, profiles, Presentation и migration.

## Современный special-mode набор — окончательно исключён

Следующие class IDs не являются контентом colony mode:

1. `AK47`;
2. `MP5`;
3. `M4`;
4. `Para`;
5. `M3_super_90`;
6. `Duals`;
7. `Awp`;
8. `Deagle`;
9. `Bombe` — отдельный multiplayer object со специальным state machine.

Authoritative policy:

```text
ContentPolicy = ExcludedFromColonyMode
RuntimeCatalog = false
ProductionEnabled = false
LootEnabled = false
SaveLoadSupport = false
```

Они не должны попадать в content registry, technology tree, loot tables, colony saves или migration.

## Уже учтённые generic classes

`Keule` и `Streitaxt` имеют recipes и technology placement. Они входят в основной производимый каталог как `weapon.club` и `weapon.battle_axe` и здесь повторно не считаются.

## Validation

- `DeferredBacklog` class не разрешён в active runtime catalog;
- `ExcludedFromColonyMode` class является validation error при ссылке из colony content;
- legacy class без явной policy является validation error;
- возвращение deferred item в scope требует отдельной migration policy.