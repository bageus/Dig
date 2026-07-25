# Состояние реализации

Статус: актуальная навигационная страница, не authoritative specification.

Последний аудит: [`implementation/implemented-systems-audit-2026-07-26.md`](implementation/implemented-systems-audit-2026-07-26.md), tracking [#403](https://github.com/bageus/Dig/issues/403).

## Источник истины

Для определения состояния конкретной системы сначала открыть [`systems/README.md`](systems/README.md), затем связанный authoritative design и tracking issue.

Эта страница больше не перечисляет системы вручную: такой список быстро устаревает и создаёт второй источник истины.

## Текущее общее состояние

В репозитории реализованы Domain/Application/Infrastructure/Presentation foundations, simulation, world, agents, jobs, inventory, buildings и Unity presentation vertical slices. Однако `IMPLEMENTED` не означает `VERIFIED`: полный Unity Play Mode workflow пока не запускается текущим CI.

Известные системные расхождения и завышенные статусы перечислены в актуальном аудите. Историческое утверждение о том, что simulation loop, world, residents, jobs, navigation и Unity adapter отсутствуют, относилось только к первому architecture-foundation этапу и больше не применимо.
