# Детерминированный темп копания, инструменты и опыт за quarter commit

Статус: `IMPLEMENTED`.

Tracking issues: [#388](https://github.com/bageus/Dig/issues/388), [#601](https://github.com/bageus/Dig/issues/601).

Связанные документы:

- [`excavation-command-execution.md`](excavation-command-execution.md);
- [`skills-and-progression.md`](skills-and-progression.md);
- [`resident-movement-occupancy-and-vertical-traversal.md`](resident-movement-occupancy-and-vertical-traversal.md);
- [`unified-game-time-and-action-cadence.md`](unified-game-time-and-action-cadence.md);
- [`../implementation/excavation-cadence-profiles-2026-07-29.md`](../implementation/excavation-cadence-profiles-2026-07-29.md).

## 1. Назначение

Система определяет, когда resident может завершить следующий excavation quarter. Темп зависит от authoritative material hardness, Stonework, используемого mining equipment и work posture, но не от Unity frame rate, animation callback или случайного числа.

## 2. Владение состоянием

- World владеет material, hardness, completed-quarter mask, cut pattern и terrain commit.
- Inventory/Equipment владеет tool item и его mining interval.
- Agents/Skills владеет `skill.stonework` и применёнными source keys.
- Jobs владеет assignment, stage и cadence execution, но не копирует World quarter progress.
- Application `ExcavationCadenceResolver` рассчитывает interval и typed flavor cue.
- Application `CommitExcavationQuarterCommandHandler` является единственным путём `World quarter commit + confirmed skill grant`.
- Presentation только проигрывает работу/flavor cue и не завершает quarter.

## 3. Запуск и основной workflow

1. Ordinary или spatial excavation job достигает `PerformWork` в зарезервированной work cell.
2. Runtime читает текущую target cell, Stonework resident, mining equipment interval и authoritative `TerrainWorkPosture`.
3. Resolver вычисляет `IntervalTicks`.
4. Если текущий fixed tick не due, World и skill state не меняются.
5. На due tick coordinator завершает ровно один зарезервированный quarter.
6. Application валидирует получателя опыта до World mutation.
7. Quarter немедленно коммитится в World.
8. Только если World действительно изменил клетку, применяется доля `SkillGrantProfile` этого quarter.
9. Job переоценивает support/work position и продолжает следующий quarter либо переходит к finalization.

## 4. Формула cadence

```text
IntervalTicks = max(1, ceil(
    EquipmentIntervalTicks
    × MaterialHardness / ReferenceHardness
    × SkillIntervalRatio
    × PostureIntervalRatio))
```

Текущий reference hardness использует существующую demo rock hardness `120`.

### Stonework bands

Bands восстанавливают уже существовавшие границы legacy quarter planner, но удаляют случайный swing count:

| Stonework | Interval ratio |
|---:|---:|
| 0–10 | `3/1` |
| 11–20 | `2/1` |
| 21–50 | `1/1` |
| 51–70 | `1/2` |
| 71–100 | `1/3` |

### Equipment и базовый cooldown

- один mining impact tick коммитит не более одного quarter;
- после impact действуют два recovery ticks;
- поэтому текущий demo pickaxe использует базовый equipment interval `3`;
- runtime не разрешает demo mining interval опуститься ниже `3`, даже если старый equipment profile возвращает меньшее значение;
- без подходящего mining tool также используется base interval не меньше `3` до применения hardness/skill/posture formula;
- новые инструменты добавляются через `EquipmentProfile`, а не через проверки display name;
- tool меняет cadence, но не World progress owner и не количество quarters.

Базовая обычная клетка с четырьмя required quarters получает impact attempts на ticks `T`, `T+3`, `T+6`, `T+9`, если route, support, reservation и target остаются допустимыми. Фактический interval может увеличиться из-за hardness/skill/posture. Animation swing не создаёт дополнительный commit.

### Posture

`Standing`, `DepthBraced` и `Climbing` имеют отдельные data ratios. Текущие production defaults равны `1/1`, потому что числовой штраф/бонус posture не подтверждён и относится к Q-014 `BALANCE_TBD`. Архитектурный input обязателен для всех путей копания.

## 5. Опыт за committed quarter

Полный `SkillGrantProfile.PerUnit` делится между четырьмя quarters в стабильном порядке:

1. `UpperLeft`;
2. `LowerLeft`;
3. `UpperRight`;
4. `LowerRight`.

Для каждого skill grant:

```text
Base = RequestedUnits / 4
Remainder = RequestedUnits % 4
QuarterUnits = Base + 1 для первых Remainder quarters, иначе Base
```

Сумма четырёх shares точно равна исходному profile. Half-cell target получает только shares фактически committed required quarters. Full и partial job finalization больше не начисляют этот profile повторно.

Idempotency key имеет вид `ExcavationQuarterCommitted + target XYZ + quarter`. Повторный commit уже завершённого quarter не меняет World и не выдаёт опыт.

## 6. Повтор, отмена, failure и retry

- cancel/release/reassignment не сохраняют скрытый sub-quarter swing progress;
- уже committed quarter и его skill grant остаются;
- следующий worker начинает с оставшегося World mask;
- недоступный route не создаёт cadence progress;
- missing/dead skill recipient отклоняет due commit до World mutation;
- повтор после успешного World commit является idempotent и не выдаёт второй grant;
- failure finalization не повторяет quarter skill.

## 7. Одновременная работа

Несколько residents могут работать над разными зарезервированными quarters одной target cell. Каждый due action завершает не более одного quarter. Coordinator не позволяет двум workers владеть одним текущим quarter; World commit и skill source key дополнительно защищают от повторного результата.

## 8. Input priority и interruption

Cadence не изменяет утверждённый приоритет действий. Combat/self-defense может прервать direct/automatic excavation. Новый direct order или reassignment меняет active job, но не восстанавливает committed quarters. Animation interruption не отменяет уже подтверждённый commit.

## 9. Save/Load

Сохраняются World quarter mask, cut pattern, source material, Jobs stages/assignments, Inventory equipment и Skills applied source keys. Отдельный random seed, animation swing counter или sub-quarter progress не сохраняются. После load следующий due tick вычисляется из fixed tick и текущих data profiles. Existing global simulation tick не масштабируется при cadence migration.

## 10. Presentation и flavor accidents

`ExcavationCadenceDecision` может вернуть typed `ClumsySwing`, но cue не меняет cadence, World или skill result. В production profile frequency сейчас `0`, поэтому cue отключён до утверждения Q-014. Presentation не может через callback создать success/failure удара.

## 11. Диагностика

Доступны:

- target material hardness и reference hardness;
- Stonework и выбранный skill band;
- equipment item/profile interval и применённый base floor `3`;
- posture и posture ratio;
- итоговый `IntervalTicks` и due/not-due;
- committed quarter, profile share и source id;
- duplicate/no-change commit;
- optional flavor cue.

## 12. Acceptance

- одинаковые inputs и tick дают одинаковый cadence result;
- обычная, direct и spatial excavation используют один resolver;
- material hardness изменяет interval;
- demo pickaxe использует базовый interval `3`: один impact tick и два recovery ticks;
- runtime не коммитит второй quarter внутри одного due impact;
- Stonework bands дают ожидаемые deterministic intervals без random swings;
- posture передаётся во все runtime paths и регулируется data profile;
- один due step завершает ровно один reserved quarter;
- четыре quarters выдают ровно полный skill profile;
- half-cell выдаёт только committed shares;
- duplicate quarter не выдаёт опыт повторно;
- missing skill recipient не допускает World mutation;
- finalization не выдаёт второй completion grant;
- cancel/retry/save/load продолжают World-owned remaining mask;
- Unity Play Mode fixture проверяет cadence, recovery ticks и четыре authoritative quarter commits.

## 13. Открытая balance boundary

Q-014 остаётся владельцем точных posture ratios, дополнительных tool intervals, material tuning и частоты flavor cues. Подтверждённый demo pickaxe floor `3` больше не относится к открытому вопросу.
