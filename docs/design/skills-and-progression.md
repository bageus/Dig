# Навыки гномов и прогрессия за деятельность

## Назначение

Документ фиксирует каталог навыков, источники прогресса, общий предел характеристик, mixed grants и точную формулу перераспределения. Навыки принадлежат конкретному resident и повышаются только через подтверждённые симуляцией результаты.

Display name не используется как техническая ссылка. Для логики, сохранений и контента применяются стабильные `AgentSkillId`.

## Общие правила

- Unity animation не начисляет опыт;
- Jobs, Production, Construction, Combat и Services публикуют типизированные result events;
- Skills aggregate применяет grant bundle, capacity rule и дедупликацию одной транзакцией;
- один source result может начислить несколько навыков;
- отменённое/проваленное действие не выдаёт полный completion grant;
- retry/load не применяют один source result повторно;
- amounts и work profiles задаются content data;
- UI читает immutable snapshot.

## Каталог из 12 навыков

Все двенадцать характеристик входят в один `TotalSkillCapacity`.

| AgentSkillId | Отображаемое название | Подтверждённые источники |
|---|---|---|
| `skill.stonework` | Каменное дело | копка породы, каменные жилы/глыбы, каменные конструкции |
| `skill.woodworking` | Дерево | рубка грибов, ножки грибов, деревянные конструкции |
| `skill.cooking` | Еда | приготовление пищи |
| `skill.logistics` | Логистика | перенос, доставка, сборка и разборка зданий |
| `skill.metallurgy` | Железо | железная/золотая руда, плавка, литьё, металл и золото |
| `skill.alchemy` | Алхимия | кристаллы, уголь, алкоголь, лекарства и зелья |
| `skill.service` | Услуги | бар, кинотеатр, театр, обучение и другие сервисы |
| `skill.defense` | Защита | входящие удары, обработанные используемым щитом |
| `skill.ranged_combat` | Дальнобойное оружие | подтверждённые попадания дальнобойным оружием |
| `skill.unarmed_combat` | Кулачный бой | подтверждённые удары без оружия |
| `skill.two_handed_combat` | Двуручный бой | подтверждённые удары двуручным оружием |
| `skill.one_handed_combat` | Одноручный бой | подтверждённые удары одноручным оружием |

Старый общий `Kampf` не становится тринадцатым навыком.

## Общий предел

- базовый `TotalSkillCapacity` взрослого resident — `100`;
- Университет постепенно расширяет тот же предел до `200`;
- максимум отдельного навыка — `100`;
- все 12 навыков используют один pool;
- значение `120` не используется;
- до заполнения capacity рост не уменьшает другие навыки.

## Skill grant bundle

Один подтверждённый результат создаёт `SkillGrantBundle`:

```text
SkillGrantBundle
- AgentId
- SourceKind
- SourceId / idempotency key
- Tick
- Grants[]: (AgentSkillId, RequestedAmount)
```

Примеры mixed work:

- коробка/строительство здания может выдать Дерево + Каменное дело + Логистику;
- производственный цикл материала может выдать основной технологический навык и Логистику;
- один бой может выдать Одноручный бой за удар и Защиту за отдельный shield event.

Все навыки, которые получают положительный gain в одном bundle, считаются **получателями** и не участвуют в снижении в этой же транзакции. Это устраняет зависимость результата от порядка обработки списка grants.

## Момент начисления опыта

### Production

- grant создаётся после успешного commit output;
- опыт начисляется **за каждую произведённую единицу**;
- если цикл создал `Quantity = 3`, amount профиля умножается на 3 либо создаётся эквивалентный агрегированный bundle;
- rollback/cancel до commit output не выдаёт grant.

### Combat

- grant создаётся по подтверждённому combat result event;
- нанесённый удар одноручным оружием → `skill.one_handed_combat`;
- нанесённый удар двуручным оружием → `skill.two_handed_combat`;
- подтверждённое дальнобойное попадание → `skill.ranged_combat`;
- подтверждённый безоружный удар → `skill.unarmed_combat`;
- входящий удар, обработанный используемым щитом → `skill.defense`;
- простое наличие оружия/щита, замах или animation callback опыт не дают.

### Jobs

- общий job grant создаётся после `JobCompleted`;
- delivery/hauling относится к completed job;
- cancelled/failed job полного grant не даёт;
- staged contribution может иметь отдельный source event только если definition явно это задаёт, но один и тот же результат не считается одновременно stage и completion grant без отдельного профиля.

## Формула перераспределения

Обозначения:

- `C` — текущий `TotalSkillCapacity`;
- `x_i` — текущее значение навыка `i`;
- `g_i` — запрошенный gain навыка-получателя;
- `A` — множество навыков-получателей bundle;
- `D` — остальные навыки-доноры;
- `M = 100` — индивидуальный максимум;
- `T = Σ x_i` — текущая сумма.

### Шаг 1. Ограничение individual max

```text
requested_i = min(g_i, M - x_i)
RequestedTotal = Σ requested_i
```

### Шаг 2. Свободная capacity

```text
Free = max(0, C - T)
```

Эта часть gain применяется без снижения других навыков.

### Шаг 3. Доступный donor pool

```text
S = Σ x_j, где j ∈ D
PossibleTotal = min(RequestedTotal, Free + S)
```

Если bundle требует больше, чем можно добавить без снижения самих получателей, фактически применяемый gain уменьшается пропорционально requested gains.

Для одного получателя:

```text
AppliedGain = PossibleTotal
```

Для нескольких получателей:

```text
AppliedGain_i = PossibleTotal × requested_i / RequestedTotal
```

### Шаг 4. Overflow

```text
R = max(0, PossibleTotal - Free)
```

`R` — точная сумма, которую необходимо снять с donors.

### Шаг 5. Потеря каждого donor

```text
Loss_j = R × x_j / S
```

Итог:

```text
x'_i = x_i + AppliedGain_i,  i ∈ A
x'_j = x_j - Loss_j,         j ∈ D
```

Чем выше donor, тем больше его доля `x_j / S` и тем больше абсолютная потеря.

## Точный пример

Исходная сумма уже равна `C = 100`.

Увеличиваемый навык `N5 = 1` получает `Nдоб = 4` и должен стать `5`. Он исключён из donors.

Остальные ненулевые значения:

```text
N1=50, N2=20, N3=10, N4=5,
N6=3, N7=3,
N8=2, N9=2, N10=2, N11=2
```

Сумма donors:

```text
S = 99
R = 4
```

Потери:

| Навык | До | Формула | Потеря | После |
|---|---:|---|---:|---:|
| N1 | 50 | `4×50/99` | 2.020202 | 47.979798 |
| N2 | 20 | `4×20/99` | 0.808081 | 19.191919 |
| N3 | 10 | `4×10/99` | 0.404040 | 9.595960 |
| N4 | 5 | `4×5/99` | 0.202020 | 4.797980 |
| N6 | 3 | `4×3/99` | 0.121212 | 2.878788 |
| N7 | 3 | `4×3/99` | 0.121212 | 2.878788 |
| N8 | 2 | `4×2/99` | 0.080808 | 1.919192 |
| N9 | 2 | `4×2/99` | 0.080808 | 1.919192 |
| N10 | 2 | `4×2/99` | 0.080808 | 1.919192 |
| N11 | 2 | `4×2/99` | 0.080808 | 1.919192 |
| N5 | 1 | получает +4 | — | 5.000000 |

Новая сумма остаётся ровно `100`.

## Fixed-point и округление

Runtime не использует binary floating point как authoritative storage.

Рекомендуемый алгоритм:

1. хранить skill values в целых минимальных единицах, например `1 point = 1000 units`;
2. для каждого donor вычислить numerator `R_units × x_j_units`;
3. взять `floor(numerator / S_units)`;
4. посчитать остаток до точной суммы `R_units`;
5. распределить оставшиеся минимальные units по убыванию дробного остатка;
6. при равенстве использовать stable `AgentSkillId` ascending.

Та же largest-remainder policy применяется при пропорциональном уменьшении mixed requested gains.

## Инварианты транзакции

- `Σ x'_i <= C`;
- при достаточном donor pool `Σ x'_i = min(C, T + RequestedTotal)`;
- каждый навык остаётся в `0..100`;
- получатели bundle не теряют points в этой транзакции;
- сумма donor losses равна `R` точно в fixed-point units;
- bundle применяется целиком атомарно;
- один source id применяется один раз;
- report перечисляет requested, applied, free-capacity gain и loss каждого donor.

## Каменное дело и пещеры

Пороги используют `skill.stonework`:

- средняя пещера — 20;
- большая — 40;
- высокая — 60.

Доступность вычисляется по максимальному текущему Stonework. Падение навыка блокирует новые placements, но не отменяет существующие plans.

## Production и здания

Recipe/Work definition содержит `WorkSkillProfile` с одним или несколькими grants. Навык не определяется по display name здания или продукта.

Подтверждённые связи:

- cooking → `skill.cooking`;
- алкоголь/зелья → `skill.alchemy`;
- плавка/литьё → `skill.metallurgy`;
- service → `skill.service`;
- hauling/assembly/packing → `skill.logistics`;
- stone work → `skill.stonework`;
- wood work → `skill.woodworking`.

Точные mixed amounts задаются content definitions.

## Контракт результата

`SkillRedistributionReport` содержит:

- AgentId;
- source/idempotency key;
- capacity before/after;
- sum before/after;
- requested grants;
- applied grants;
- free-capacity portion;
- overflow;
- donor weights/losses;
- rounding remainder allocation;
- rejected/clamped amount;
- resulting skill values.

## HUD

Раскрытая строка показывает пять наибольших навыков из всех 12:

- level descending;
- stable `AgentSkillId` ascending при равенстве;
- gradient тёмно-синий → зелёный;
- число и maximum;
- последний gain/loss report доступен в tooltip/inspector.

## Save/Load

Сохраняются:

- values всех 12 skills в fixed-point units;
- `TotalSkillCapacity`;
- catalog/schema/precision version;
- applied source keys, необходимые для idempotency;
- active training/work state;
- migration report.

Migration при смене precision выполняется один раз и сохраняет сумму через ту же largest-remainder policy.

## Результат проверки исходных scripts

Исторические TCL-файлы подтвердили `atr_ExpMax`, семь рабочих и пять боевых характеристик и вызовы встроенной `add_expattrib`, но не содержали формулу потерь. Поэтому Dig использует подтверждённую выше собственную пропорциональную и объяснимую формулу, а не предполагаемый engine behavior.

## Связанные issues

- #82 — Университет;
- #103 — feature навыков;
- #104 — Domain/capacity;
- #105 — work/production grants;
- #106 — combat grants;
- #107 — UI/Save;
- #117 — шкалы и redistribution UI.
