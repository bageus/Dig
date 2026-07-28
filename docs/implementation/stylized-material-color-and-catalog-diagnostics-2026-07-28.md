# Stylized material colour and visual-catalog diagnostics regression — 2026-07-28

Статус: `IMPLEMENTED`; фактический Unity Editor/Play Mode запуск остаётся обязательным для `VERIFIED`.

Связанные системы и tracking:

- Presentation visuals: [`../architecture/systems-gameplay.md`](../architecture/systems-gameplay.md#6-presentation), issue [#14](https://github.com/bageus/Dig/issues/14);
- visual asset pipeline: [`unity-visual-asset-pipeline.md`](unity-visual-asset-pipeline.md), issues [#207](https://github.com/bageus/Dig/issues/207) и [#208](https://github.com/bageus/Dig/issues/208);
- stylized rendering: [`unity-stylized-render-vfx-pipeline.md`](unity-stylized-render-vfx-pipeline.md), issue [#212](https://github.com/bageus/Dig/issues/212).

## Runtime symptoms

Unity reported repeated errors:

```text
Material 'DigStylizedLit' with Shader 'Dig/Stylized Lit' doesn't have a color property '_Color'
UnityEngine.Material:get_color()
```

Startup also emitted warnings that building and item visual catalogs were not assigned even though both systems intentionally support representative/runtime fallback visuals.

## Root causes

`Material.color` assumes a shader property named `_Color`. The project-owned stylized shaders expose `_BaseColor`, so fallback creation and hover/highlight code could read or write the wrong Unity convenience property.

`DigVisualCatalogDiagnostics` treated every null catalog as a warning, although the approved lookup contracts allow an absent optional authored catalog and continue through representative or bounded generic fallbacks.

## Correction

- `DigMaterialColorUtility` resolves `_BaseColor` first and `_Color` only for compatible shaders;
- fallback material creation uses the same explicit property boundary;
- barrel, resident and world-item hover code no longer reads `Material.color`;
- a null optional visual catalog is quiet;
- every non-null authored catalog is still validated and malformed entries remain errors.

No Domain/Application state, stable gameplay identity, input behavior, save data or visual lookup priority changed.

## Regression coverage

`StylizedMaterialRuntimeContractTests` requires the explicit shader-property utility, rejects the previous direct color API calls and keeps non-null catalog validation active while rejecting the obsolete null-catalog warnings.

`StylizedMaterialConsolePlayModeTests` creates the project-owned stylized Lit barrel material, renders the complete barrel, applies highlight and requires no unexpected Unity log messages. A licensed Unity Test Runner execution is still required to confirm that the Console stays free of the reported messages.
