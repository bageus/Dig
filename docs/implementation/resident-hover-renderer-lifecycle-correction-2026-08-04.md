# Resident hover renderer lifecycle correction — 2026-08-04

Статус: `IMPLEMENTED`.

Tracking issue: [#629](https://github.com/bageus/Dig/issues/629).

Связанные authoritative specifications:

- [`../design/presentation-input-ui-and-diagnostics.md`](../design/presentation-input-ui-and-diagnostics.md);
- [`../design/resident-work-tool-visual-projection.md`](../design/resident-work-tool-visual-projection.md);
- [`../design/full-depth-tunnel-eating-and-tent-presentation-correction.md`](../design/full-depth-tunnel-eating-and-tent-presentation-correction.md).

## Runtime report

Unity Console показала `MissingReferenceException` для уничтоженного
`UnityEngine.MeshRenderer` после переключения visual в правой руке resident.

## Root cause

`DigAgentVisual` сохранял массив hover `Renderer[]` между hover enter и hover exit.
`DigAgentEquipmentVisual.Clear()` уничтожает дочерние meshes при переходах
между фактической экипировкой, transient work tool и meal portion. Старый hover
cache продолжал содержать уничтоженные `MeshRenderer`, после чего
`RestoreHover()` обращался к ним через `GetPropertyBlock`/`SetPropertyBlock`.

## Correction

- перед hand mutation hover restore пропускает Unity-destroyed references;
- hand visual mutation сначала снимает текущий hover, очищает cache, выполняет
  замену и повторно применяет hover к новой геометрии;
- уничтожаемые hand children перед `Destroy` отсоединяются от resident hierarchy
  и деактивируются, поэтому новый cache не может повторно подобрать pending-destroy
  meshes в том же frame;
- повторная projection того же item id не перестраивает visual.

Изменение не затрагивает Inventory ownership, item stacks, work jobs, combat,
action completion, save/load или приоритет transient tools/meal/equipment.

## Regression coverage

- source contract требует prepare/complete hover mutation lifecycle, fresh capture,
  destroyed-reference guard и detach-before-destroy;
- checked-in Unity Play Mode scenario выполняет
  `club -> hover -> pickaxe rebuild -> frame -> hover exit` и запрещает
  unexpected Unity logs/exceptions;
- существующая последовательность club/pickaxe/axe/hammer/club/empty остаётся
  покрытой.

## Verification

Локально выполнены:

- `python tools/quality/check_quality.py` — pass;
- `python tools/quality/check_unity_source_contracts.py` — pass;
- `python tools/quality/check_unity_resident_visual_contracts.py` — pass.

Release build, full .NET suite, smoke и deterministic soaks фиксируются по exact
PR head. Фактический лицензированный Unity Play Mode result остаётся обязательным
для статуса `VERIFIED`.
