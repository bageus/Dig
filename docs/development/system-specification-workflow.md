# Процесс системной спецификации и сверки требований

Статус: обязательное правило разработки.

Tracking issues: [#385](https://github.com/bageus/Dig/issues/385), [#393](https://github.com/bageus/Dig/issues/393).

## 1. Назначение

Любая новая функция, изменение поведения или исправление пользовательского workflow сначала сопоставляется с системной документацией. Код не должен становиться местом, где неявно принимаются бизнес-решения.

Основная точка входа: [`../systems/README.md`](../systems/README.md).

## 2. Источники истины

Приоритет источников:

1. последнее явно подтверждённое решение пользователя, уже внесённое в authoritative system specification и tracking issue;
2. authoritative design-файл системы со статусом `APPROVED`, `IMPLEMENTED` или `VERIFIED`;
3. acceptance criteria активного tracking issue;
4. ADR и общие правила архитектуры;
5. implementation notes и фактический код как описание текущей реализации, но не как автоматический источник игровых требований;
6. старые сообщения чата, не перенесённые в документацию.

Текущий запрос пользователя не переписывает документацию молча. При конфликте необходимо спросить, какое поведение становится новым источником истины.

## 3. Обязательный первый проход

Для каждой задачи:

1. открыть `docs/systems/README.md`;
2. найти систему по заголовку и aliases;
3. прочитать linked authoritative design;
4. прочитать linked implementation notes и tracking issues;
5. открыть фактический код и все места использования;
6. составить полный пользовательский workflow от ввода до результата;
7. проверить, описаны ли success, cancel, blocked, retry, save/load и UI feedback;
8. только после этого менять документацию или код.

## 4. Матрица решения

### Запрос полностью соответствует документации

Сразу выполнять изменение. Уточняющие вопросы не нужны, если ответ есть в документации, issue или коде.

### Запрос расширяет систему без конфликта

Добавить недостающие вопросы в specification/issue. Спросить только те бизнес-решения, которые нельзя вывести из уже утверждённых правил.

### Запрос конфликтует с документацией

До спорной реализации показать кратко:

- что говорит текущий authoritative документ;
- что требует новый запрос;
- какие связанные правила изменятся;
- один целевой вопрос: оставить старое поведение или утвердить новое.

После ответа сначала обновить design и issue, затем код.

### Система или функция не описана

1. создать draft по [`system-specification-template.md`](system-specification-template.md);
2. создать GitHub Issue с ссылкой на файл;
3. внести систему в `docs/systems/README.md` со статусом `QUESTIONNAIRE`;
4. задать опросник;
5. не выдавать предположения за утверждённое поведение.

### Документация и код расходятся

- если design утверждён и запрос ему соответствует, код считается дефектом;
- если design неполон или противоречив, сначала проводится опрос;
- implementation note обновляется после исправления фактической реализации;
- обнаруженное расхождение фиксируется в issue acceptance/regression scenario.

## 5. Статусы системной спецификации

- `DRAFT` — структура создана, но пользовательский workflow неполон;
- `QUESTIONNAIRE` — перечислены конкретные открытые бизнес-вопросы;
- `APPROVED` — вопросы закрыты, документ является источником требований;
- `IMPLEMENTED` — утверждённый workflow реализован и покрыт автоматическими проверками;
- `VERIFIED` — дополнительно пройден полный end-to-end runtime/Play Mode сценарий.

Статус не означает отсутствие любых багов. `VERIFIED` нельзя ставить только по source-contract или поиску строк в коде.

## 6. Требования к опроснику

Вопросы должны быть:

- numbered и привязаны к конкретной системе;
- сформулированы через observable behavior;
- содержать разумные варианты, когда они известны;
- объяснять, какие соседние правила меняет ответ;
- не спрашивать то, что уже можно прочитать в репозитории.

Минимальные темы:

1. запуск сценария;
2. success path;
3. cancel/undo;
4. недоступность и blocked state;
5. приоритет при конфликте целей;
6. повторная команда и re-entry;
7. участие нескольких residents/objects;
8. save/load;
9. UI/cursor/status;
10. acceptance scenario.

## 7. Связь документа и issue

Для каждой системы:

- design-файл содержит tracking issue;
- issue содержит repository path design-файла;
- system index содержит оба;
- implementation notes ссылаются на design, но не переопределяют его;
- PR с кодом обновляет status и acceptance evidence.

## 8. Проверка полного рабочего процесса

Перед заявлением «исправлено» проверить весь путь:

```text
input/command
-> routing and validation
-> authoritative state transition
-> job/action execution
-> world/inventory/building commit
-> presentation refresh
-> selection/cursor/status feedback
-> repeated use / next cell / next job
-> cancel, failure and retry
-> save/load when applicable
```

Для reported runtime bug недостаточно:

- source-contract теста;
- наличия нужной строки в файле;
- успешной компиляции отдельного модуля;
- исправления только первого шага workflow.

Нужен regression test на первопричину и, для Unity interaction, Play Mode или эквивалентный end-to-end scenario.

## 9. Порядок изменения

1. index/status;
2. authoritative specification;
3. tracking issue и acceptance;
4. Domain/Application contracts;
5. Infrastructure/Presentation integration;
6. tests and diagnostics;
7. implementation notes;
8. verification evidence и status.

## 10. Завершение задачи

Краткий отчёт должен перечислять:

- какие системные решения подтверждены или изменены;
- какие docs/issues обновлены;
- какие файлы кода затронуты;
- какие end-to-end сценарии реально проверены;
- какие вопросы остаются открытыми.
