# Dwarf Animator Unity module compile correction

Дата: 2026-08-04.  
Статус: `IMPLEMENTED IN BRANCH`.

Authoritative system: [`../design/presentation-input-ui-and-diagnostics.md`](../design/presentation-input-ui-and-diagnostics.md).  
Runtime verification owner: [#511](https://github.com/bageus/Dig/issues/511).

## Runtime report

После добавления `Assets/DigDwarfs` Unity compilation остановилась с двумя `CS1069` в `DwarfAnimatorBridge.cs`: тип `UnityEngine.Animator` был forwarded в `UnityEngine.AnimationModule`, но соответствующий встроенный Unity module отсутствовал в project manifest.

Ошибка `Failed to resolve assembly: Assembly-CSharp-Editor` возникла после отказа runtime compilation и является вторичной.

## Root cause

Dwarf integration добавила runtime/editor scripts, которые используют:

- `Animator`;
- `AnimationClip`;
- `RuntimeAnimatorController`;
- `UnityEditor.Animations`.

Project manifest содержал glTFast и остальные используемые Unity modules, но не содержал direct dependency `com.unity.modules.animation`. Поэтому чистая Unity compilation не могла подключить `UnityEngine.AnimationModule`.

## Correction

- в `unity/Dig.Unity/Packages/manifest.json` добавлен `com.unity.modules.animation: 1.0.0`;
- glTFast `6.19.0` и существующие package versions сохранены;
- `DwarfAnimatorUnityModuleContractTests` связывает checked-in `Animator` bridge с обязательной manifest dependency;
- gameplay, simulation ownership, root motion policy и action cadence не изменяются.

## Expected local recovery

После получения исправления Unity Package Manager должен перечитать manifest и пересобрать `Assembly-CSharp`/`Assembly-CSharp-Editor`. Если Console продолжает показывать прежние `CS1069`, Unity следует закрыть, удалить generated `Library/ScriptAssemblies` или весь `Library`, затем снова открыть `unity/Dig.Unity`.

## Verification boundary

Repository build/tests не компилируют реальный Unity `Assembly-CSharp`. Исчезновение `CS1069` подтверждается только локальной повторной Unity compilation либо фактически выполненным licensed Unity Test Runner по #511.
