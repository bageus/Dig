# Dig Dwarfs Unity Integration Package

Этот пакет подготовлен для внедрения low-poly гномов в Unity как **Generic Rig**.

## Что внутри
- `Models/` — 6 rigged моделей (`.gltf` + `.bin`)
- `Scripts/Runtime/` — bridge, action enum и auto-binding attachment sockets
- `Scripts/Editor/` — мастер генерации prefab'ов и Animator Controller'ов

## Зависимости
Нужен импортёр glTF, например:
- **glTFast**
- **UniGLTF**

Runtime bridge использует `UnityEngine.Animator`. Unity host обязан напрямую объявлять built-in package:

```json
"com.unity.modules.animation": "1.0.0"
```

Если runtime scripts входят в custom `.asmdef`, его `references` должен содержать `UnityEngine.AnimationModule`. Без этого Unity 6 входит в Safe Mode с `CS1069` для типа `UnityEngine.Animator`.

## Быстрый старт
1. Импортируйте весь пакет в Unity проект.
2. Добавьте `com.unity.modules.animation` в `Packages/manifest.json` host-проекта.
3. Установите glTF импортер.
4. Дождитесь импорта моделей из `Assets/DigDwarfs/Models`.
5. Запустите меню: `Tools -> Dig Dwarfs -> Generate Prefabs And Controllers`.
6. Готовые prefab'ы и контроллеры появятся в `Assets/DigDwarfs/Generated`.

## Что создаётся автоматически
- по одному `AnimatorController` на персонажа;
- prefab с `Animator`;
- `DwarfAttachmentSockets`;
- `DwarfAnimatorBridge`;
- примерный `DwarfSampleDriver` для теста в сцене.

## Рекомендованная схема параметров Animator
- `Speed` (0..1)
- `IsClimbing`
- `IsCarrying`
- `ActionKind`
- `AttackTrigger`
- `HitTrigger`
- `DieTrigger`

## Loop клипы
- Idle
- Walk
- Run
- Climb
- Carry

## One-shot клипы
- Mine
- Chop
- Build
- Eat
- Attack
- Hit
- Death

## Attachment bones
- `RightHandTool`
- `LeftHandTool`
- `CarryAnchor`
- `BackAttachment`
- `HeadAccessory`

## Для Dig
Рекомендуется использовать **in-place** анимации и не включать root motion. Положение персонажа должно оставаться authoritative в simulation/runtime коде.

## Ограничения
Пакет подготовлен вне Unity Editor, поэтому готовые `.prefab` и `.controller` не сериализованы заранее. Вместо этого они генерируются editor-script'ом внутри Unity.
