# Аудит открытых issues: готовность к закрытию

Дата: 2026-07-28  
Репозиторий: `bageus/Dig`  
Baseline: `main` после PR #495 (`4514edcc529828a6bb3f7fcea67e9ba0631bcb33`)  
Связанный системный аудит: [#403](https://github.com/bageus/Dig/issues/403)  
Индекс систем: [`../systems/README.md`](../systems/README.md)

## 1. Правило классификации

Issue можно закрывать как `completed`, только когда её собственный acceptance выполнен в текущем `main` и отсутствует явно оставленный runtime/save/migration/business blocker.

Отсутствие лицензированного Unity Test Runner не блокирует закрытие каждой Domain-only задачи автоматически, но блокирует задачи, в которых Play Mode или полный observable Unity workflow прямо входят в acceptance. Статус `IMPLEMENTED` в индексе не равен `VERIFIED`.

Issue закрывается как `not planned/superseded`, когда её правила заменены более поздней authoritative specification и выполнение старого acceptance создало бы второй источник истины.

## 2. Можно закрыть сейчас как superseded / not planned

### #288 — Support concurrent manual excavation without job ownership

Закрыть как superseded by #388.

Причина: issue требует отдельную manual work action, которая не использует ordinary excavation job. Утверждённая specification `excavation-command-execution.md` требует противоположное: direct order использует обычный Jobs lifecycle и не создаёт второго manual owner/progress owner.

### #289 — Split excavation cells into four independently workable sectors

Закрыть как superseded by #388.

Сохранившиеся полезные правила — четыре quarters, deterministic progress, concurrency и save/load — уже принадлежат World-owned quarter model в #388. Устарели требования о самостоятельном manual owner, approach-owned progress и отдельном quarter lifecycle вне authoritative World/Jobs pipeline.

### #290 — Make excavation progress depend on resident mining skill

Закрыть как superseded by #388, а не как completed.

Skill-driven quarter cadence уже интегрирована в общий excavation coordinator, но issue привязана к отменённой модели manual action из #288/#289. Актуальные skill/cadence/quarter acceptance должны отслеживаться только в #388 и системной specification, чтобы не оставлять параллельный контракт.

## 3. Оставить открытыми: собственный acceptance прямо требует runtime/Play Mode evidence

- #15 — Unity EditMode/PlayMode evidence всё ещё отсутствует; текущий workflow пропускает фактический Test Runner без activation credentials.
- #67, #68, #69, #71 — кодовые slices существуют, но в комментариях issues прямо зафиксирована незавершённая runtime-приёмка item/hauling/attachments/save round-trip.
- #93 — acceptance требует Unity Play Mode для preview, layers, arches, deposits и drops.
- #118 — BuildingBox workflow после PR #495 имеет статус `IMPLEMENTED`, но issue прямо оставлена для licensed Unity Test Runner и result artifacts.
- #386 — остаются фактические Play Mode сценарии climbing/gap/recovery/opposite climbers.
- #388 — остаются combat interruption и полный excavation Play Mode workflow.
- #398 — source/runtime corrections объединены, но полный BuildingBox Play Mode workflow остаётся неподтверждённым.
- #423 — mushroom system `IMPLEMENTED`, но issue специально оставлена для полного Unity workflow.
- #433 — issue прямо оставлена открытой только для фактического Unity Test Runner evidence.
- #443 — barrel workflow получил несколько runtime corrections, но фактическая Play Mode приёмка не выполнена.
- #459 — весь repository acceptance отмечен, кроме hosted Unity Test Runner result XML.

Эти issues нельзя закрывать только на основании зелёных source contracts, `.NET` tests, smoke или deterministic soak.

## 4. Оставить открытыми: реализация или системный workflow неполны

### Foundation, save, presentation и input

- #13, #14;
- #113, #115, #116, #117;
- #347;
- #387, #389, #390, #393, #396, #403.

Причины включают umbrella scope, открытые business questions, неполную unit-item migration, незавершённую нормализацию authoritative docs и отсутствие полного runtime evidence.

### Excavation, world, resources и hauling

- #87, #88, #89, #90, #91, #92, #94, #109, #110.

Это не дубликаты #388: они охватывают полную XYZ migration, template catalog/instances, deposits, output/hauling, persistence и fog-aware demand. Их собственный acceptance шире реализованного vertical slice.

### Food, needs и schedule

- #96, #97, #98, #99, #100, #101, #159, #234.

#459 закрыл конкретный grilled-mushroom direct-use workflow, но не заменил полный food catalog, autonomous food selection, continuous needs effects, schedule/free-time chain и все interruption/save scenarios. Комментарий #97 прямо сохраняет broader settlement food-source selection за #97.

### Skills, technology, production и energy

- #103, #104, #105, #106, #107, #108;
- #126, #127, #128;
- #75, #82.

Открыты полный TotalSkillCapacity/redistribution lifecycle, idempotent grants, полный technology catalog/research UI, energy classes/generators и content production chains.

### Buildings, combat, health, society, ecology и navigation content

- #74, #76, #77, #78, #79, #80, #81;
- #129, #130, #131, #132, #133;
- #136, #137, #138, #139, #140, #141, #142, #143, #145, #146, #149, #150, #151, #152, #165.

Большинство этих issues сами описывают отсутствующие authoritative models, commands, UI, save/load или concrete content. Закрытие сейчас потеряет реальный backlog.

### Visual production

- #203, #208, #209, #210, #211.

Архитектурный visual foundation существует, но parent acceptance и дочерние authored catalog/rig/creature/overlay/runtime budgets ещё не завершены.

### Отложенный backlog

- #177 оставить открытой как явно обозначенный deferred backlog. Это валидный будущий scope, а не выполненная или ошибочная issue.

## 5. Issues, которые нужно сначала актуализировать

### #89

В таблице всё ещё указана глубина малой комнаты `2`, тогда как последнее подтверждённое правило и runtime correction используют глубину `3`. До дальнейшей оценки closure acceptance/body нужно синхронизировать с authoritative cave-room specification.

### #388

Четыре пункта acceptance о centered Small room, half-cell `2/4`, terminal climbing и nearest-supported recovery уже реализованы в PR #489, но в issue остаются unchecked. Их нужно отметить выполненными, сохранив открытыми только combat interruption и фактический Play Mode workflow.

### #208

Issue требует stack quantity bands и badges для world/resident stacks, что конфликтует с более поздним invariant #347: физические world/resident items имеют quantity `1`. Acceptance нужно переписать под unit-item visuals; aggregate quantities допустимы только в тех storage/read-model contexts, где это отдельно утверждено.

### #403

Текущий audit report использует baseline после PR #402. После PR #403–#495 часть найденных runtime gaps исправлена, а новые compile/runtime regressions получили owners. #403 остаётся открытой до обновления baseline, матрицы статусов и ссылок на текущие issue owners.

### #423 и #443

Implementation evidence в bodies отстаёт от последних runtime hotfixes. Перед закрытием либо повышением статуса следует добавить последние PRs и фактический Unity result, а не только ранний implementation head.

## 6. Итоговое решение

Сейчас безопасно закрыть только #288, #289 и #290 как `not planned/superseded` с обратной ссылкой на #388. Issues, отмеченные `IMPLEMENTED`, но специально оставленные для runtime verification, сохраняются открытыми. Остальные перечисленные issues имеют неполный acceptance, unresolved business/design decisions, активный regression или реальный будущий content scope.

Состояния issues этим аудитом не изменяются.