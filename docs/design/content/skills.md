# Каталог навыков гномов

Полная системная спецификация: [`../skills-and-progression.md`](../skills-and-progression.md).

Этот файл является точкой расширения content-каталога для стабильных `AgentSkillId`, display names и ссылок из Jobs, Production, Buildings и Combat.

## Подтверждённые навыки

| AgentSkillId | Display name | Основная область |
|---|---|---|
| `skill.stonework` | Камень / Каменное дело | порода, камень, каменные конструкции |
| `skill.woodworking` | Дерево | грибы, ножки грибов и деревянные конструкции |
| `skill.cooking` | Еда | приготовление пищи |
| `skill.logistics` | Логистика | доставка, перенос, сборка и разборка |
| `skill.metallurgy` | Железо | железная/золотая руда, железо, золото и литьё |
| `skill.alchemy` | Алхимия | кристаллы, уголь, напитки, лекарства и зелья |
| `skill.service` | Услуги | бар, кинотеатр, театр, обучение и сервисы |
| `skill.defense` | Защита | входящие удары, обработанные щитом |
| `skill.ranged_combat` | Дальнобойное оружие | подтверждённые дальнобойные попадания |
| `skill.unarmed_combat` | Кулачный бой | подтверждённые удары без оружия |
| `skill.two_handed_combat` | Двуручный бой | подтверждённые удары двуручным оружием |
| `skill.one_handed_combat` | Одноручный бой | подтверждённые удары одноручным оружием |

## Grants

- один result может содержать несколько skills;
- Production experience начисляется за committed produced unit;
- Jobs experience начисляется после JobCompleted;
- Combat experience начисляется per confirmed result event;
- one-handed hit и shield-defense могут начисляться в одном encounter;
- animation callback experience не выдаёт;
- один source/idempotency key применяется один раз.

## Capacity

- один pool из всех 12 skills;
- base 100, University max 200;
- individual max 100;
- overflow снимается с non-recipient donors пропорционально их текущим values;
- mixed bundle recipients исключаются из donor pool;
- fixed-point largest-remainder rounding сохраняет точную сумму.

## Content validation

- `AgentSkillId` уникален;
- display name не используется как ссылка;
- work/recipe/combat profile ссылается на существующий skill;
- amounts не отрицательны;
- mixed grants имеют stable source;
- one idempotency key не создаёт duplicate grant;
- donor loss report сохраняет capacity;
- изменение stable ID/precision требует migration;
- `skill.stonework` и `material.stone` не смешиваются;
- `skill.metallurgy` и material ItemIds не смешиваются.

## Открыто

- Q-039 — влияет ли Cooking только на speed или также на output/effects.
