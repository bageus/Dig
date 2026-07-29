# Аудит открытых issues: готовность к закрытию

Дата исходного аудита: 2026-07-28  
Последняя синхронизация: 2026-07-29  
Репозиторий: `bageus/Dig`  
Текущий baseline: `main` после PR #507 и последующей status synchronization  
Связанный системный аудит: [#403](https://github.com/bageus/Dig/issues/403)  
Индекс систем: [`../systems/README.md`](../systems/README.md)

## 1. Правило классификации

Issue можно закрывать как `completed`, только когда её собственный repository acceptance выполнен и отсутствует явно оставленный blocker, принадлежащий этой issue.

`IMPLEMENTED` означает, что требования реализованы и покрыты repository tests. `VERIFIED` дополнительно требует фактически выполненный runtime/Unity Play Mode workflow. Workflow-level success со skipped `Run Play Mode tests` не является runtime evidence.

Issue может быть закрыта как `IMPLEMENTED`, если её repository acceptance выполнен, а оставшиеся внешне-зависимые licensed Unity gates явно перенесены без потери требований в единого approved verification owner #511. Такой перенос не повышает систему до `VERIFIED`.

Issue закрывается как `not planned/superseded`, когда её правила заменены более поздней authoritative specification и сохранение старого acceptance создало бы второй источник истины.

## 2. Закрыты корректно

### Completed

- #13 — production save composition, migration v0–v9 и complete current job codec registry реализованы в PR #500.
- #14 — Presentation foundation полностью реализован в repository; licensed Unity evidence сохранён без ослабления в #511. Статус системы остаётся `IMPLEMENTED`, не `VERIFIED`.
- #15 — repository quality/CI scope, Release/.NET gates, smoke, deterministic soaks и Unity evidence tooling реализованы; фактическое licensed execution и runtime budgets принадлежат #511.
- #16 — базовая roadmap stages 0–6 выполнены; remaining licensed verification вынесена в #511 как approved continuation owner.
- #207 — building visual authoring pipeline repository scope завершён; дальнейшие authored assets не меняют architecture contract.
- прочие ранее закрытые foundation issues остаются закрытыми, если их собственный acceptance выполнен.

### Superseded / not planned

- #288 — отдельная manual excavation action заменена ordinary Jobs lifecycle из #388.
- #289 — независимый quarter owner заменён World-owned quarter model из #388.
- #290 — skill cadence сохранена в общем coordinator, но старый contract был привязан к superseded #288/#289.

## 3. Оставить открытыми: explicit Unity/runtime verification

- #511 — единый owner licensed Unity EditMode/PlayMode execution, XML/log artifacts, representative-scene Console acceptance и measured runtime budgets.
- #67, #68, #69, #71 — item/hauling/attachments/save round-trip runtime acceptance.
- #93 — cave preview/layers/arches/deposits/drops Play Mode.
- #118 и #398 — BuildingBox lifecycle `IMPLEMENTED`, но полный runtime workflow не `VERIFIED`.
- #212 — shader reimport, lighting readability, VFX budgets и representative Play Mode slice.
- #386 — climbing/gap/recovery/opposite-climbers Play Mode.
- #388 — combat interruption и полный excavation Play Mode workflow.
- #423 — mushroom workflow `IMPLEMENTED`, runtime verification отсутствует.
- #433 — изменённый production/input contract реализован в PR #501; actual licensed Unity evidence отсутствует.
- #443 — barrel workflow `IMPLEMENTED`, включая correction PR #494; runtime verification отсутствует.
- #459 — repository acceptance выполнен, hosted Unity Test Runner/result XML отсутствует.
- #497 — HUD initialization regression `IMPLEMENTED`, фактический Unity run отсутствует.

Зелёные source contracts, `.NET` tests, smoke и deterministic soak не заменяют эти explicit runtime criteria.

## 4. Оставить открытыми: implementation/design scope неполон

### Roadmap, foundation, input и documentation

- #113, #115, #116, #117 — полный HUD/input/notification/skill-report acceptance не завершён.
- #347 — unit-item migration не завершена.
- #387, #389, #390, #393, #396, #403 — questionnaire, normalization и runtime workflows остаются.

### Excavation, world, resources и hauling

- #87, #88, #89, #90, #91, #92, #94, #109, #110.

Эти задачи шире реализованного excavation vertical slice: полная XYZ migration, все templates, deposits, output/hauling, persistence, fog-aware demand и end-to-end presentation остаются отдельным scope.

### Food, needs и schedule

- #96, #97, #98, #99, #100, #101, #159, #234.

#459 реализует конкретный grilled-mushroom direct-use workflow, но не заменяет полный food catalog, autonomous selection, continuous needs, schedule/free-time chain и все save/interruption scenarios.

### Skills, technology, production и energy

- #75, #82, #103–#108, #126–#128.

Открыты TotalSkillCapacity/redistribution lifecycle, idempotent grants, полный technology catalog/research UI, energy classes/generators и production content chains.

### Buildings, combat, health, society, ecology и navigation content

- #74, #76–#81;
- #129–#133;
- #136–#152;
- #165.

Эти issues содержат отсутствующие authoritative models, commands, UI, save/load или concrete content. Закрытие потеряет реальный backlog.

### Visual production

- #203, #208, #209, #210, #211.

#208 синхронизирована с более поздним unit-item invariant #347: physical world/resident items имеют quantity `1`; старые aggregate quantity bands не являются целевым world/resident contract. Authored catalog, overlays, budgets и runtime acceptance остаются.

### Deferred backlog

- #177 остаётся открытой как явно обозначенный будущий scope.

## 5. Выполненная status synchronization

На 2026-07-29 часть issues была возвращена в `open` для синхронизации acceptance. После создания approved verification owner #511 repository-complete umbrella issues #14, #15 и #16 могут быть закрыты как `IMPLEMENTED`/completed без утверждения `VERIFIED`.

Открытыми сохраняются:

- #87, #88, #89, #90, #91, #93, #94;
- #115, #117;
- #212;
- #386, #388;
- #423.

Дополнительно:

- #89: Small room depth исправлена с `2` на `3`; status зафиксирован как partial/open.
- #388: реализованные PR #489 пункты отмечены completed; остаются combat interruption и Play Mode.
- #208: acceptance переписана под #347 без parallel aggregate world/resident contract.
- #403: body обновлена под текущий baseline и status policy.
- #423/#443: добавлены ссылки на последние runtime corrections.
- #433: после merge PR #501 возвращена в `IMPLEMENTED`, но её explicit runtime evidence остаётся у #511 и собственного acceptance.
- #511: создан как единственный machine-readable owner фактически выполненного licensed Unity evidence.

## 6. Текущая итоговая policy

- Закрыты completed: #13, #14, #15, #16 и другие задачи с выполненным собственным repository acceptance.
- Закрыты superseded: #288, #289, #290.
- `IMPLEMENTED` issues могут закрываться, когда их repository scope завершён, а external licensed evidence перенесено в #511 без потери acceptance. Узкие issues с собственным observable runtime acceptance остаются открытыми до выполнения этого acceptance.
- `APPROVED`, `QUESTIONNAIRE`, `DRAFT`, `PARTIAL/OPEN` задачи не закрываются до завершения observable workflow и обязательных business decisions.
- Ни одна Unity interaction system не повышается до `VERIFIED`, пока #511 не содержит executed EditMode/PlayMode XML, runtime logs, clean representative-scene Console и commit-bound `unity-runtime-evidence.json`.
